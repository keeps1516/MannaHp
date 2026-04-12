# Plan: Refunds, Cancellations & In-Store Payment Tracking

> Source PRD: [keeps1516/MannaHp#4](https://github.com/keeps1516/MannaHp/issues/4)

## Architectural decisions

- **Routes**:
  - `PATCH /api/orders/{id}/mark-paid` — mark in-store order as paid
  - `POST /api/orders/{id}/cancel` — cancel order with reason + restock decisions
  - `POST /api/orders/{id}/refund` — create refund record with item selection
  - `GET /api/orders/{id}/refunds` — refund history for an order
- **Schema**:
  - New `refunds` table: `Id`, `OrderId`, `Amount`, `TaxAmount`, `Reason` (required), `StripeRefundId` (nullable), `CreatedBy`, `CreatedAt`
  - New `refund_items` table: `Id`, `RefundId`, `OrderItemId`, `Amount`, `Restocked`
  - New `RestockPolicy` column on `menu_items` (enum: `NonReturnable = 0`, `Returnable = 1`, default `NonReturnable`)
  - `InventoryChangeType` enum gains `OrderRestock = 3`
- **Key models**: `Refund`, `RefundItem`, `RestockPolicy`
- **Auth**: Existing `"Staff"` authorization policy (covers both Staff and Owner roles) applies to all new endpoints
- **Stripe**: No in-app refund initiation — refunds are issued via Stripe dashboard and synced into the app via `charge.refunded` webhook
- **SignalR**: Three new broadcast events — `OrderCancelled`, `OrderRefunded`, `OrderPaymentUpdated` — sent to both the `kitchen` group and the order-specific `order-{orderId}` group
- **Restock rule**: Inventory restoration only runs when a cancelled/refunded order had reached `Completed` status (the point at which `DecrementInventoryAsync` ran)
- **Partial refunds**: `PaymentStatus` stays `Paid` until the full order amount is covered; only then does it flip to `Refunded`

---

## QA: how to build, run, and test completed phases

### Build & run (Docker — recommended)

From the repo root (`MannaHp/`):

```bash
docker compose up -d --build api next-client
docker compose logs -f api   # watch until you see "Now listening on..." and the migration applied
```

The API auto-applies EF Core migrations on startup, so `AddRefundsAndRestockPolicy` runs on boot. For a fresh DB (to exercise the "runs cleanly on fresh database" criterion):

```bash
docker compose down -v && docker compose up -d --build
```

App: `http://localhost:3000`. Default login: `owner@manna.local` / `MannaOwner123!`.

### Build & run (local, no Docker)

```bash
# Terminal 1 — API (needs Postgres on localhost:5432)
cd src/Server && dotnet run

# Terminal 2 — Next.js
cd src/next-client && npm run dev
```

If Postgres isn't running locally: `docker compose up -d postgres` and leave the rest native.

### Getting an owner JWT for curl

```bash
curl -s -X POST http://localhost:5082/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"owner@manna.local","password":"MannaOwner123!"}' | jq -r .token
```

### Phase 1 QA steps

1. **Migration applied cleanly.** `docker compose logs api | grep -i migrat` shows `AddRefundsAndRestockPolicy` with no errors.
2. **Schema check** — inspect Postgres:
   ```bash
   docker exec -it manna-postgres psql -U app -d restaurant -c "\d menu_items" | grep -i restock
   docker exec -it manna-postgres psql -U app -d restaurant -c "\dt refund*"
   docker exec -it manna-postgres psql -U app -d restaurant -c "SELECT COUNT(*) FROM menu_items WHERE \"RestockPolicy\" = 0;"
   ```
   Expect: `RestockPolicy` column present, `refunds` + `refund_items` tables listed, all existing rows default to 0 (NonReturnable).
3. **API exposes the field.** `curl http://localhost:5082/api/menu-items | jq '.[0].restockPolicy'` → `0`.
4. **Admin UI.** Sign in as owner → Admin → Menu Items → edit any item. The form should show a **Restock Policy** dropdown with "Non-returnable" / "Returnable". Change it to Returnable, save, reopen — value persists. Re-hit the API and confirm `restockPolicy: 1`.

### Phase 2 QA steps

**Setup:** place an in-store order first.
1. Admin → Store Tokens → Generate, copy the token string.
2. In an incognito window, visit `http://localhost:3000/order?token=<TOKEN>` and place any order. It lands in the kitchen queue as **In-Store / Unpaid**.

Then run through the criteria:

1. **Button visibility** — Admin → Orders. The new in-store order shows an **Unpaid** badge and a **Mark Paid** button. A card-paid order shows **Paid** and no button.
2. **Happy path** — tap Mark Paid. Button disappears, badge flips to **Paid**, no console errors. Verify server-side:
   ```bash
   curl -s http://localhost:5082/api/orders/<ORDER_ID> | jq .paymentStatus
   ```
   → `1` (Paid).
3. **Real-time broadcast** — open Admin → Orders in two tabs. Mark Paid in tab A; tab B's badge updates without refresh (SignalR `OrderPaymentUpdated`).
4. **400 on card order:**
   ```bash
   curl -i -X PATCH http://localhost:5082/api/orders/<CARD_ORDER_ID>/mark-paid \
     -H "Authorization: Bearer <OWNER_JWT>"
   ```
   → `400` with `"Only in-store orders can be marked paid."`
5. **400 on already-paid** — repeat the PATCH on the order you just paid → `400` with `"Order is already marked as paid."`
6. **401 without auth** — same PATCH with no `Authorization` header → `401`.
7. **204 on success** — fresh in-store+pending order → `204 No Content`.

### Automated checks already green locally

- Backend: `dotnet build src/Server/MannaHp.Server.csproj` — 0 errors
- Frontend: `cd src/next-client && npx tsc --noEmit` — clean

No unit/integration tests were added for Phases 1–2 (plan didn't specify TDD) — tracked in the Phase 2 "Deferred" list below.

---

## Phase 1: Data Foundation & RestockPolicy on Menu Items — code complete, awaiting QA

**User stories**: 13

### What to build

Add the `Refund` and `RefundItem` entities to the data model and run an EF Core migration to create the new tables. Add `RestockPolicy` as a column on `menu_items` (default `NonReturnable`, no data migration needed for existing rows). Extend the menu item form in the admin UI with a RestockPolicy dropdown so owners can tag items like sealed canned drinks as `Returnable`.

This is the only phase without a net-new API action — its value is that every subsequent phase has a stable schema and owners can start classifying items immediately.

### Acceptance criteria

- [ ] EF Core migration runs cleanly on a fresh database and on an existing database with seeded menu items
- [ ] All existing menu items default to `NonReturnable` after migration
- [ ] Owner can open a menu item in the admin UI, set RestockPolicy to `Returnable`, save, and the value persists on reload
- [ ] `RestockPolicy` is returned in the menu item API response

### Implementation notes

- Entities: `Refund`, `RefundItem` in `src/Shared/Entities/`; `RestockPolicy` enum in `src/Shared/Enums/`; `InventoryChangeType` gained `OrderRestock = 3`
- Migration: `src/Server/Data/Migrations/20260412005048_AddRefundsAndRestockPolicy.cs` — adds column with default 0, creates `refunds` + `refund_items` with FK cascades and indexes on `OrderId` / `StripeRefundId`
- DTOs: `MenuItemDto`, `CreateMenuItemRequest`, `UpdateMenuItemRequest` all carry `RestockPolicy`; endpoint handlers in `MenuItemEndpoints.cs` thread it through GET/POST/PUT/image
- Frontend: `RestockPolicy` enum + field in `src/next-client/src/types/api.ts`; admin select in `components/admin/menu-item-form-sheet.tsx`
- Incidental fix: pre-existing typo in `OrderEndpoints.cs:181` (`oia => oi.Ingredients` → `oi => oi.Ingredients`) repaired so the Server project could build

---

## Phase 2: Mark In-Store Order as Paid — code complete, awaiting QA

**User stories**: 1

### What to build

A single new endpoint that flips an in-store order's `PaymentStatus` from `Pending` to `Paid`. The endpoint rejects card orders and already-paid orders. On success it broadcasts `OrderPaymentUpdated` via SignalR so every open screen updates without a refresh. The OrderCard gains a "Mark Paid" button that is visible only when `PaymentMethod === InStore && PaymentStatus === Pending`.

### Acceptance criteria

- [ ] `PATCH /api/orders/{id}/mark-paid` returns 204 for a valid InStore + Pending order
- [ ] Returns 400 when called on a Card order
- [ ] Returns 400 when called on an already-Paid order
- [ ] Requires Staff/Owner auth; returns 401 otherwise
- [ ] "Mark Paid" button appears on OrderCard only for InStore + Pending orders
- [ ] Pressing the button updates the payment badge in real-time on all connected clients via SignalR

### Implementation notes

- Endpoint: `OrderEndpoints.cs` — `PATCH /api/orders/{id}/mark-paid`, `RequireAuthorization("Staff")`, broadcasts `OrderPaymentUpdated` to both `kitchen` and `order-{id}` groups
- Client API: `adminApi.markOrderPaid` in `lib/admin-api.ts` (uses `adminFetchNoBody` for 204)
- SignalR: `connectOrderHub` in `lib/order-hub.ts` gained a third required callback `onOrderPaymentUpdated`. Both call sites updated (`app/admin/(dashboard)/orders/page.tsx`, `app/admin/(dashboard)/page.tsx`)
- UI: `components/admin/order-card.tsx` — body wrapper changed from `<button>` to `div[role=button]` (so the nested Mark Paid button is valid HTML; keyboard handler preserved). New Unpaid/Paid badge beside the payment-method badge. `onMarkPaid` is an optional prop so existing tests remain green
- Orders page: `handleMarkPaid` does optimistic update with rollback on failure; SignalR handler merges `paymentStatus` into local state
- **Tests not yet written** — no unit/integration coverage added for Phase 2 (acceptance criteria verified manually). If we want automated coverage, add cases to `tests/MannaHp.Server.Tests/Endpoints/` and `src/next-client/src/__tests__/components/order-card.test.tsx`

### Deferred from Phase 2 / backlog for later phases

- Add server integration tests for `mark-paid` (happy path, card-order 400, already-paid 400, 401/403)
- Add OrderCard unit tests for button visibility + click handler
- Consider whether `mark-paid` should also log an `InventoryLog`-like audit entry (decided no for now — payment state changes aren't tracked in inventory_logs)

---

## Phase 3: Cancel Any Order

**User stories**: 2, 3, 7, 8, 9, 14, 16, 19, 20

### What to build

A cancel endpoint that accepts a mandatory reason and per-item restock decisions. Cancellation is allowed from any `OrderStatus`. When the order had previously reached `Completed`, the endpoint runs inventory restoration for items the staff marks as restock: stock is incremented and an `InventoryLog` entry with `ChangeType = OrderRestock` is created. NonReturnable items are excluded from restock entirely.

The Cancel Dialog on the OrderCard shows:
- A required reason text field
- Restock checkboxes (default checked) for each Returnable item — but only when the order was Completed
- A "Cannot restock" label for NonReturnable items (no checkbox)

`OrderCancelled` is broadcast to the kitchen group (removes the card from the kanban in real-time) and to the individual order group (updates the customer order-status page).

### Acceptance criteria

- [ ] `POST /api/orders/{id}/cancel` accepts `{ reason, restockDecisions: [{ orderItemId, restock }] }` and returns 204
- [ ] Reason is required; returns 400 if missing or empty
- [ ] Cancelling a non-Completed order succeeds without touching inventory
- [ ] Cancelling a Completed order with a Returnable item checked restores stock and creates an `InventoryLog` entry with `ChangeType = OrderRestock`
- [ ] Cancelling a Completed order with a NonReturnable item never touches inventory regardless of payload
- [ ] Double-cancel returns 400
- [ ] "Cancel" button appears on every OrderCard regardless of status
- [ ] Cancel Dialog shows restock checkboxes only for Returnable items on Completed orders; NonReturnable items show "Cannot restock"
- [ ] Restock checkboxes default to checked
- [ ] Kitchen kanban removes the cancelled card in real-time via SignalR
- [ ] Customer order-status page updates to "Cancelled" in real-time

---

## Phase 4: In-Store Refunds

**User stories**: 4, 5, 6, 7, 8, 9, 15, 17, 18

### What to build

A refund endpoint that creates a `Refund` record with selected items, a calculated amount (line totals + proportional tax), a mandatory reason, and per-item restock decisions. The endpoint prevents refunding items that have already been refunded in a prior request. When the cumulative refunded amount equals the order total, `PaymentStatus` flips to `Refunded`.

A GET endpoint returns the full refund history for an order.

The Refund Dialog on the OrderCard (visible for Paid orders) shows:
- Per-item checkboxes to select which items to refund
- A live-updating running total (selected item amounts + proportional tax)
- A mandatory reason field
- Restock checkboxes for Returnable items (same logic as Phase 3)

Refund history is surfaced in the OrderCard (e.g., a collapsible section or badge count).

`OrderRefunded` is broadcast via SignalR on success.

### Acceptance criteria

- [ ] `POST /api/orders/{id}/refund` accepts `{ reason, items: [{ orderItemId, restock }] }` and returns 201 with the created `RefundDto`
- [ ] Reason is required; returns 400 if missing
- [ ] At least one item must be selected; returns 400 otherwise
- [ ] Attempting to refund an already-refunded item returns 400
- [ ] Refund amount is calculated server-side (not trusted from client): sum of selected item line totals + proportional share of order tax
- [ ] When cumulative refund amount ≥ order total, `PaymentStatus` is set to `Refunded`
- [ ] `GET /api/orders/{id}/refunds` returns all refund records with items, amounts, reason, createdBy, createdAt
- [ ] Both endpoints require Staff/Owner auth
- [ ] "Refund" button appears on OrderCard only for Paid orders
- [ ] Refund Dialog item checkboxes update the running total live
- [ ] Reason field is required — dialog submit is disabled until filled
- [ ] Restock checkboxes appear for Returnable items; NonReturnable items show "Cannot restock"
- [ ] After successful refund, refund history is visible in the OrderCard
- [ ] `OrderRefunded` SignalR event is broadcast on success

---

## Phase 5: Stripe Webhook Auto-Sync

**User stories**: 10, 11

### What to build

Extend the Stripe webhook handler to consume `charge.refunded` events. On receipt, the handler looks up the order by the charge's payment intent ID, creates a `Refund` record with `CreatedBy = "stripe-webhook"` and `StripeRefundId` set to the Stripe refund ID, updates `PaymentStatus`, and broadcasts `OrderRefunded` via SignalR.

The handler is idempotent: if a `Refund` record already exists with the same `StripeRefundId`, it skips creation and returns 200 (handles duplicate webhook delivery).

### Acceptance criteria

- [ ] A `charge.refunded` webhook event with a valid signature creates a `Refund` record and returns 200
- [ ] A duplicate `charge.refunded` event with the same `StripeRefundId` is ignored (no duplicate record); returns 200
- [ ] `PaymentStatus` is updated to `Refunded` when the Stripe refund covers the full charge
- [ ] `OrderRefunded` SignalR event is broadcast after a webhook-triggered refund
- [ ] Invalid webhook signature returns 400
- [ ] If no order matches the charge's payment intent ID, the handler returns 400 (or logs and returns 200 to prevent Stripe retries — decision to confirm)

---

## Phase 6: Net Revenue Calculation

**User stories**: 12, 17

### What to build

Modify `GET /api/orders/today-revenue` to subtract the sum of all `Refund.Amount` values for today's orders from the gross revenue total, returning accurate net revenue. The change is additive — the response shape stays the same, only the value changes.

### Acceptance criteria

- [ ] `GET /api/orders/today-revenue` returns gross revenue minus refund amounts for the current day
- [ ] A day with no refunds returns the same value as before
- [ ] A fully-refunded order contributes zero to net revenue
- [ ] A partially-refunded order contributes `orderTotal - refundedAmount` to net revenue
- [ ] Refunds from Stripe webhooks (Phase 5) are included in the deduction
