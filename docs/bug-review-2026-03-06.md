# Bug Review - 2026-03-06

Scope: static review of the current server, Next.js client, and print-agent code.

## Findings

### 1. Critical: anonymous clients can join the kitchen SignalR feed

- `OrderHub.JoinKitchen()` and `LeaveKitchen()` are public hub methods with no authorization checks.
- The hub itself is mapped without `.RequireAuthorization()`.
- The admin client opens the hub without sending any bearer token, which only works because the hub is currently anonymous.

Relevant code:
- `src/Server/Hubs/OrderHub.cs:10`
- `src/Server/Program.cs:143`
- `src/next-client/src/lib/order-hub.ts:43`
- `src/next-client/src/lib/order-hub.ts:70`

Impact:
- Any browser running on an allowed CORS origin can subscribe to `OrderCreated` and `OrderStatusChanged` for the kitchen queue.
- That exposes order numbers, items, notes, and live status transitions to non-staff users.

Repro:
1. Open the public frontend.
2. Establish a SignalR connection to `/hubs/orders`.
3. Invoke `JoinKitchen`.
4. Place an order from another browser and observe the full kitchen event payload.

Recommended fix:
- Require auth on the hub or at least on `JoinKitchen`.
- Send the admin JWT in the SignalR connection.
- Keep customer order tracking separate from staff kitchen events.

### 2. High: the print agent prints unpaid card orders and marks them as printed

- Card orders are persisted before payment is confirmed.
- The print agent polls for every order where `Printed == false`, with no payment-status filter.
- After generating a receipt PDF, it flips `Printed = true` immediately.

Relevant code:
- `src/Server/EndPoints/OrderEndpoints.cs:123`
- `src/Server/EndPoints/OrderEndpoints.cs:136`
- `src/PrintAgent/Worker.cs:50`
- `src/PrintAgent/Worker.cs:82`
- `src/Server/EndPoints/StripeWebhookEndpoints.cs:53`

Impact:
- A customer can start checkout, abandon or fail payment, and still generate a kitchen receipt.
- The order is then marked printed, so later recovery paths cannot distinguish it from a legitimately paid order.

Repro:
1. Create a card order so the server saves it with `PaymentStatus.Pending`.
2. Let the print agent poll before `confirm-payment` or the Stripe webhook marks it paid.
3. Observe a receipt generated for an unpaid order.

Recommended fix:
- Only print `InStore` orders immediately.
- For card orders, print only after `PaymentStatus == Paid`.
- Consider filtering out `Failed` and `Cancelled` orders as well.

### 3. High: disabled menu items remain directly orderable

- `CreateOrder` checks only that the menu item and variant IDs exist.
- It does not reject inactive menu items or inactive variants.
- Public menu endpoints also return inactive categories/items and nested inactive variants/add-ons, leaving filtering to the client.

Relevant code:
- `src/Server/EndPoints/OrderEndpoints.cs:40`
- `src/Server/EndPoints/OrderEndpoints.cs:60`
- `src/Server/EndPoints/MenuItemEndpoints.cs:15`
- `src/Server/EndPoints/MenuItemEndpoints.cs:34`
- `src/Server/EndPoints/CategoryEndpoints.cs:15`

Impact:
- A stale cart, bookmarked item page, or direct API caller can still submit orders for products staff already disabled.
- This breaks the expected behavior of soft-deletes and can create orders for inventory that should no longer be sellable.

Repro:
1. Deactivate a menu item or one of its variants in admin.
2. Submit `POST /api/orders` with the previously valid IDs.
3. The order is accepted because the endpoint only checks existence.

Recommended fix:
- Filter inactive records out of public read endpoints.
- In `CreateOrder`, reject inactive menu items and inactive variants explicitly.

### 4. Medium: “today’s revenue” is calculated in UTC, not the store’s business timezone

- The revenue endpoint uses `DateTime.UtcNow.Date` as the lower bound.
- The store configuration and receipt formatting are clearly Central time oriented.

Relevant code:
- `src/Server/EndPoints/OrderEndpoints.cs:237`
- `src/PrintAgent/ReceiptDocument.cs:59`
- `src/next-client/src/app/admin/(dashboard)/page.tsx:25`

Impact:
- Around local midnight, the dashboard reports the wrong day’s revenue.
- For a Central-time store, this starts drifting several hours before the local day actually changes.

Example:
- At 7:30 PM Central on March 6, 2026, `DateTime.UtcNow.Date` is already March 7, 2026 during standard offset assumptions, so late-evening March 6 sales can be excluded from “today.”

Recommended fix:
- Convert `CreatedAt` comparisons to the store timezone before applying the day boundary.
- Keep the timezone source centralized so dashboard, receipts, and reporting agree.

### 5. Low: checkout hardcodes an 8.25% tax label even though tax is configurable

- The cart state fetches tax from `/api/settings/public`.
- The checkout summary still renders `Tax (8.25%)` as a literal string.

Relevant code:
- `src/next-client/src/store/cart-context.tsx:118`
- `src/next-client/src/store/cart-context.tsx:144`
- `src/next-client/src/app/(customer)/checkout/page.tsx:143`

Impact:
- If the owner changes `DefaultTaxRate`, checkout can show the wrong percentage while still charging the dynamically computed amount.
- That creates a user-visible mismatch on the payment page.

Repro:
1. Change `DefaultTaxRate` in admin settings to anything other than `0.0825`.
2. Go to checkout.
3. The total uses the new rate, but the label still says `Tax (8.25%)`.

Recommended fix:
- Render the tax label from `cart.taxRate`, as already done in other parts of the client.

## Notes

- I did not run the full test suite as part of this pass.
- The repository already contained unrelated uncommitted changes before this review; this document only adds a new file under `docs/`.
