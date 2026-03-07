# Manna HP - Undocumented Feature Gaps

**Created:** March 6, 2026
**Source:** Codebase audit comparing architecture doc (CLAUDE.md) against actual implementation and existing feature plans (F1-F14).

This document captures features and gaps **not covered** by the existing [feature-plans.md](feature-plans.md). Items here are candidates for promotion into full feature plans.

---

## Priority Overview

| Priority | Feature | Section | Status |
|----------|---------|---------|--------|
| Tier 1 | Customer Authentication (Google OAuth) | [G1](#g1-customer-authentication-google-oauth) | Not built — blocks multiple features |
| Tier 1 | Order Ready Notifications (Email/SMS) | [G2](#g2-order-ready-notifications-emailsms) | Not built |
| Tier 1 | Order Cancellation + Refund Flow | [G3](#g3-order-cancellation--refund-flow) | Not built |
| Tier 1 | QR Code In-Store Token Flow | [G4](#g4-qr-code-in-store-token-flow) | Not built — described in arch doc |
| Tier 1 | Staff Account Management UI | [G5](#g5-staff-account-management-ui) | Not built — API-only account creation |
| Tier 2 | Estimated Ready Time | [G6](#g6-estimated-ready-time) | Not built |
| Tier 2 | Saved Payment Methods (Stripe Customer) | [G7](#g7-saved-payment-methods-stripe-customer) | Not built — described in arch doc |
| Tier 2 | Category Reordering UI | [G8](#g8-category-reordering-ui) | Partial — DB field exists, no UI |
| Tier 2 | Ingredient Cost History | [G9](#g9-ingredient-cost-history) | Not built |
| Tier 3 | Discount / Promo Codes | [G10](#g10-discount--promo-codes) | Not built |
| Tier 3 | Pickup vs Delivery Mode | [G11](#g11-pickup-vs-delivery-mode) | Not built |
| Tier 3 | API Rate Limiting | [G12](#g12-api-rate-limiting) | Not built |
| Tier 3 | Offline / PWA Caching | [G13](#g13-offline--pwa-caching) | Not built — arch doc claims PWA |

---

## Tier 1 — Needed Before Launch

### G1: Customer Authentication (Google OAuth)

**Arch doc reference:** Auth section — "Google OAuth for customer sign-in"
**Blocks:** F12 (Order Again), G7 (Saved Payment Methods), G2 (Notifications with customer contact info)

#### Current State
- ASP.NET Core Identity is configured and works for **admin/staff only** (login + register endpoints in `AuthEndpoints.cs`)
- Google OAuth is referenced in Identity config but **not wired to the API or frontend**
- No customer sign-up, sign-in, or account page exists
- No customer-facing JWT flow
- Customers order completely anonymously — no `UserId` on orders placed via the customer app

#### What's Needed
- Wire Google OAuth to the API (callback endpoint, JWT issuance for customers)
- Customer sign-in UI in the Next.js frontend (Google button on header/checkout)
- Associate orders with authenticated customer's `UserId`
- Customer account page (name, email, order history link)
- Guest checkout must still work (don't force sign-in to order)
- Auto-create Stripe Customer object on first authenticated order (prerequisite for G7)

#### Dependencies
- This is the **single biggest prerequisite** in the app — F12, G2, G7, and future loyalty/personalization all depend on it

---

### G2: Order Ready Notifications (Email/SMS)

**Why:** Customers have no way to know their order is ready unless they watch the order status page. In a real restaurant, this is critical — customers may be seated, in their car, or browsing nearby.

#### Current State
- SignalR broadcasts `OrderStatusChanged` events, but only the admin kitchen display listens
- Customer order page fetches once on mount with no live updates (F1 addresses the real-time UI piece)
- No email or SMS infrastructure exists in the codebase
- No customer contact info is captured during ordering

#### What's Needed
- Capture customer email (or phone) during checkout — either via authentication (G1) or as a checkout form field for guests
- Send a notification when order status changes to "Ready"
- Options:
  - **Email:** lightweight — use a transactional email service (SendGrid, Resend, or SMTP)
  - **SMS:** more immediate — use Twilio or similar
  - **Push notification:** possible since the app is served as a web app, but requires notification permission
- Admin setting to enable/disable notifications
- Unsubscribe / opt-out mechanism

#### Relationship to Other Features
- F1 (Real-time order tracking) handles the **in-app** live update
- G2 handles **out-of-app** notification when the customer isn't watching the page

---

### G3: Order Cancellation + Refund Flow

**Why:** Once an order is paid via Stripe, there is no way to cancel it or issue a refund through the app. Staff would have to log into the Stripe dashboard directly, and the order would remain in the app as if nothing happened.

#### Current State
- Order status flow is: Received -> Preparing -> Ready -> Completed
- No "Cancelled" status transition exists in the UI (though the entity may support it)
- No Stripe refund API calls anywhere in the codebase
- No admin UI for cancellations or refunds

#### What's Needed
- Add "Cancel" action on admin orders page (available in Received/Preparing status)
- Cancel flow:
  - If paid via Stripe: issue full refund via Stripe Refund API, update order status to "Cancelled"
  - If in-store payment: just mark as "Cancelled" (no charge was made)
- Partial refund support (e.g., one item was wrong — refund that item's amount)
- Record refund details on the order (refund amount, reason, timestamp, who initiated)
- Notify customer if notifications are enabled (G2)
- Prevent cancellation of Completed orders (or require owner-level override)

---

### G4: QR Code In-Store Token Flow

**Arch doc reference:** "QR Code In-Store Ordering" section — describes short-lived GUID tokens, `X-Store-Token` header, custom authorization handler

#### Current State
- The architecture doc describes this in detail, but **none of it is built**
- No `POST /api/generate-store-token` endpoint
- No `X-Store-Token` authorization handler
- No store token entity or table
- In-store ordering currently works by selecting "Pay at Counter" in the checkout — no QR code needed
- The customer must navigate to the app URL manually

#### What's Needed
- `store_tokens` table: `Id`, `Token` (GUID), `ExpiresAt`, `CreatedBy` (staff user)
- `POST /api/generate-store-token` — Staff authorization, creates token valid for 30-60 minutes
- Custom authorization handler that accepts either valid JWT or valid `X-Store-Token` header
- QR code generation UI for staff (displays QR code encoding `https://manna.example.com/order?token=abc123`)
- Frontend: detect `token` query param, store in session, attach as `X-Store-Token` header on API calls
- Token cleanup: Hangfire job or background service to purge expired tokens

---

### G5: Staff Account Management UI

**Why:** Staff accounts can only be created via the `POST /api/auth/register` endpoint. The owner has no way to view, edit, deactivate, or change roles for staff through the admin interface.

#### Current State
- `AuthEndpoints.cs` has register and login endpoints
- Roles (Owner, Staff, Customer) are seeded
- No admin page lists existing users or their roles
- No way to deactivate a staff member who leaves
- No audit of who performed admin actions

#### What's Needed
- Admin page: `/admin/staff` — list all staff/owner accounts with name, email, role, active status
- Add staff: invite by email or create account directly
- Edit staff: change role (promote Staff to Owner or vice versa)
- Deactivate staff: soft-delete (disable login without deleting history)
- Owner-only authorization on all staff management actions

---

## Tier 2 — Should Have Soon After Launch

### G6: Estimated Ready Time

**Why:** Customers have no idea how long their order will take. The kitchen can't communicate "15 minutes" — there's no ETA field on orders.

#### Current State
- No `estimated_ready_at` or `prep_time_minutes` field on orders
- No UI for kitchen staff to set an estimate
- Customer order page shows status but no time estimate

#### What's Needed
- Add `EstimatedReadyAt` (DateTime, nullable) field to Order entity
- Kitchen staff can set/update the estimate when moving order to "Preparing"
- Optional: auto-estimate based on item count or historical average prep time
- Display countdown or "Ready in ~X min" on customer order page
- Update estimate in real-time via SignalR (pairs with F1)

---

### G7: Saved Payment Methods (Stripe Customer)

**Arch doc reference:** "Stripe Customer objects store payment methods — returning customers don't re-enter card info"

#### Current State
- Every Stripe payment creates a new PaymentIntent with no customer association
- No Stripe Customer objects created
- No saved payment method list or selection UI
- Architecture doc describes this as a core feature but it's completely unimplemented

#### What's Needed
- On first authenticated payment, create a Stripe Customer and link to app user
- Store `StripeCustomerId` on the user entity
- Use `setup_future_usage` on PaymentIntent to save the card
- `GET /api/payments/methods` — list saved payment methods for the authenticated user
- Checkout UI: show saved cards with "Use this card" option, or "Add new card"
- `DELETE /api/payments/methods/{id}` — remove a saved card
- Requires G1 (Customer Authentication) first

---

### G8: Category Reordering UI

**Why:** Categories have a `SortOrder` field in the database but the admin UI provides no way to change it. Categories are stuck in whatever order they were seeded.

#### Current State
- `Category` entity has `SortOrder` (int)
- Categories rendered in `SortOrder` on customer homepage
- Admin categories section (part of menu page) has no reorder mechanism
- `PUT /api/categories/{id}` endpoint exists and accepts `SortOrder` but no UI exposes it

#### What's Needed
- Drag-and-drop reordering on the admin menu page's category list
- Or: up/down arrow buttons on each category row
- Save new sort orders via existing update endpoint (or a bulk reorder endpoint)
- Same pattern could apply to menu item reordering within a category

---

### G9: Ingredient Cost History

**Why:** When `CostPerUnit` changes on an ingredient (e.g., supplier raises chicken price), the old value is overwritten. There's no way to see cost trends or calculate accurate historical profit margins.

#### Current State
- `Ingredient.CostPerUnit` is a single decimal field — overwritten on every update
- `order_item_ingredients.price_charged` captures the **customer-facing** price at order time (good)
- But the **cost to the restaurant** at order time is not captured — can't calculate per-order profit accurately after a cost change

#### What's Needed
- `ingredient_cost_log` table: `Id`, `IngredientId`, `OldCostPerUnit`, `NewCostPerUnit`, `ChangedAt`, `ChangedBy`
- Automatically log when `CostPerUnit` changes via the update endpoint
- Optional: capture `CostPerUnit` on order items at time of order (for accurate profit calculation)
- Admin view: cost history chart per ingredient

---

## Tier 3 — Nice to Have

### G10: Discount / Promo Codes

**Why:** No way to run promotions, coupons, or percentage-off deals. Common for new restaurants trying to build a customer base.

#### What's Needed
- `promo_codes` table: code, type (percentage/flat), amount, usage limit, expiration, active
- Apply promo code at checkout — validate and calculate discount
- Track usage per code (and optionally per customer if G1 is built)
- Admin CRUD for promo codes

---

### G11: Pickup vs Delivery Mode

**Why:** All orders assume pickup. No delivery address, delivery fee, or fulfillment mode selection.

#### What's Needed
- Order mode selection at checkout: "Pickup" or "Delivery"
- Delivery address form (with validation)
- Delivery fee configuration in app settings
- Delivery radius / zone restrictions
- Kitchen display indicates pickup vs delivery
- Receipt includes delivery address when applicable

---

### G12: API Rate Limiting

**Why:** Order and auth endpoints are unprotected against abuse. Someone could spam fake orders or brute-force login.

#### What's Needed
- Rate limiting middleware on sensitive endpoints:
  - `POST /api/orders` — e.g., 5 orders per minute per IP
  - `POST /api/auth/login` — e.g., 10 attempts per minute per IP
- Use ASP.NET Core's built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`)
- Return `429 Too Many Requests` with `Retry-After` header

---

### G13: Offline / PWA Caching

**Arch doc reference:** "Blazor WASM served as a PWA" (note: frontend is now Next.js, not Blazor)

**Why:** The architecture doc describes the app as a PWA, but there's no service worker or offline caching. The app is completely unusable without internet.

#### What's Needed
- Service worker for static asset caching (Next.js pages, CSS, JS)
- Cache the menu data for offline browsing (read-only — can view menu without connectivity)
- Show "You're offline" banner when connectivity is lost
- Queue orders placed offline and submit when connectivity restores (complex — may not be worth it)
- Use `next-pwa` or similar library for Next.js service worker generation

---

## Relationship to Existing Feature Plans

| Gap | Related Feature Plan | Notes |
|-----|---------------------|-------|
| G1 (Customer Auth) | F12 (Order Again) | F12 lists auth as a prerequisite but has no plan for it |
| G2 (Notifications) | F1 (Real-time Tracking) | F1 is in-app live updates; G2 is out-of-app alerts |
| G3 (Cancellation/Refund) | F5 (Order History) | Cancelled orders should appear in order history |
| G4 (QR Code Flow) | — | Standalone; described in arch doc but not in feature plans |
| G6 (Estimated Ready Time) | F1 (Real-time Tracking) | ETA display pairs naturally with the order tracking page |
| G7 (Saved Cards) | G1 (Customer Auth) | Requires customer accounts first |
| G9 (Cost History) | F13 (Inventory Check-In) | Both relate to inventory audit trail; could share the log table |
