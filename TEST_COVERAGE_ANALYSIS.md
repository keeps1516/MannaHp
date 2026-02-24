# Test Coverage Analysis — Manna Ordering App

## Current State

The project has **3 test projects** with **~188 test methods** across **21 test files**, using **xUnit v3** with **FluentAssertions**.

| Test Project | Files | Focus |
|---|---|---|
| `MannaHp.Server.Tests` | 7 | API endpoint integration tests (via `WebApplicationFactory`) |
| `MannaHp.Shared.Tests` | 8 | FluentValidation validators + unit conversion helpers |
| `MannaHp.Client.Tests` | 3 | Cart service logic + Blazor component tests (bunit) |

### What's Well-Covered

- **Order pricing logic** — 28 tests in `OrderEndpointsTests.cs` covering fixed items, customizable bowls, duplicate ingredients, mixed orders, tax calculations, and validation failures.
- **Auth flow & authorization** — 18 tests in `AuthEndpointsTests.cs` covering login, registration, role-based access (Owner/Staff/Anonymous), and JWT validation.
- **CRUD endpoints** — `CategoryEndpointsTests`, `IngredientEndpointsTests`, `MenuItemEndpointsTests` cover the core admin management APIs.
- **All validators** (except Auth) — 7 of 8 validator files have dedicated tests using FluentValidation's `TestValidate()` helper.
- **Cart service** — 24 tests covering add/remove/update/clear and price calculations on the client side.

---

## Coverage Gaps & Recommendations

### 1. Variant, Available Ingredient, and Recipe Ingredient Endpoints — NO TESTS

**Files:** `VariantEndpoints.cs`, `AvailableIngredientEndpoints.cs`, `RecipeIngredientEndpoints.cs`

These three endpoint groups handle the menu configuration that underpins pricing — variants define fixed-item prices, available ingredients define bowl ingredient prices, and recipe ingredients define inventory deductions. None have direct tests.

**Recommended tests:**
- CRUD operations (GET list, POST create, PUT update, DELETE/soft-delete)
- 404 when parent entity doesn't exist (e.g., creating a variant for a nonexistent menu item)
- Authorization enforcement (Owner-only for write operations)
- Validation rejection for invalid input (empty name, negative price)
- Relationship constraints (variant must belong to correct menu item, can't delete ingredient in use)

**Priority: High** — Bugs here silently break order pricing.

---

### 2. Stripe Payment Flow — NO TESTS

**Files:** `StripeService.cs`, `StripeWebhookEndpoints.cs`, order creation with `PaymentMethod.Card`

The entire card payment path is untested:
- `StripeService.CreatePaymentIntentAsync()` — amount conversion (decimal to cents), currency, options
- `StripeService.GetChargeAsync()` — card brand/last4 extraction
- Webhook signature validation (invalid signature → 400)
- `HandlePaymentSucceeded` — order status update, card detail extraction, SignalR broadcast
- `HandlePaymentFailed` — order marked as failed
- Idempotency (already-paid orders ignored on duplicate webhook)

**Recommended approach:**
- **Unit tests** for `StripeService` by extracting an `IStripeService` interface and mocking the Stripe SDK types in webhook tests.
- **Integration tests** for the webhook endpoint using Stripe's test mode or by mocking `EventUtility.ConstructEvent`.
- Test that the order's `PaymentStatus`, `CardBrand`, and `CardLast4` are correctly set after a successful payment webhook.

**Priority: High** — This is the revenue path. A bug here means lost payments or incorrect receipts.

---

### 3. Auth Validators — NO TESTS

**File:** `AuthValidators.cs` (contains `LoginRequestValidator` and `RegisterRequestValidator`)

Every other validator file has tests, but `AuthValidators` was missed. The `RegisterRequestValidator` has nuanced rules (min length 8, must contain uppercase, must contain digit) that are worth verifying.

**Recommended tests:**
- `LoginRequestValidator`: empty email, invalid email format, empty password, password too short
- `RegisterRequestValidator`: all of the above plus uppercase requirement, digit requirement, display name max length

**Priority: Medium** — Low risk since auth endpoints are integration-tested, but these validators are shared code that could be used client-side for pre-validation.

---

### 4. SignalR Order Hub — NO TESTS

**File:** `OrderHub.cs`

The hub itself is simple (join/leave groups), but the **broadcast behavior** embedded in endpoints (`OrderEndpoints.cs`, `StripeWebhookEndpoints.cs`) that calls `hub.Clients.Group("kitchen").SendAsync("OrderCreated", dto)` is completely untested.

**Recommended tests:**
- Verify that creating an InStore order broadcasts `OrderCreated` to the `kitchen` group.
- Verify that updating order status broadcasts `OrderStatusChanged` to both `kitchen` and `order-{id}` groups.
- Verify that a successful payment webhook triggers `OrderCreated` to the kitchen.
- Test group subscription (JoinKitchen, JoinOrder) and that messages are isolated correctly.

**Recommended approach:** Use the `Microsoft.AspNetCore.SignalR.Client` package in integration tests. Connect a test hub client, subscribe to events, then trigger API calls and assert that the expected messages arrive.

**Priority: Medium** — Kitchen display relies entirely on SignalR. If broadcasts stop, staff won't see new orders.

---

### 5. TokenService — NO TESTS

**File:** `TokenService.cs`

The JWT token generation is indirectly validated by the auth integration tests (a generated token is used to call `/api/auth/me`), but there are no tests that verify:
- Token contains the correct claims (NameIdentifier, Email, Name, Role)
- Token expiration matches the configured `Jwt:ExpiresInMinutes` value
- Token is signed with HS256
- Token issuer and audience match configuration

**Recommended approach:** Unit test `CreateToken()` by providing an in-memory `IConfiguration` and decoding the resulting JWT to assert claims.

**Priority: Low** — Already implicitly validated, but explicit tests document expectations and catch regressions if the token structure changes.

---

### 6. Receipt Generation (PrintAgent) — NO TESTS

**Files:** `ReceiptDocument.cs`, `Worker.cs`

The receipt PDF generation has meaningful formatting logic (bowl names, ingredient lists, add-on prefixes, tax formatting, payment method display, timezone conversion) that is not tested.

**Recommended tests for `ReceiptDocument`:**
- Generates without throwing for a typical order (bowl + drinks)
- Correct formatting: bowl with quoted name prefix, drink with variant in parentheses
- Add-ons prefixed with "+" for non-customizable items
- Tax percentage display formatting
- Payment method text (Card with brand/last4, InStore, fallback)
- Timezone conversion from UTC to Central
- Missing store settings use fallback defaults

**Recommended tests for `Worker`:**
- Processes unprinted orders and marks them `Printed = true`
- Skips already-printed orders
- Handles empty result set gracefully
- Recovers from exceptions without crashing the loop

**Recommended approach:** QuestPDF supports `GeneratePdf()` to byte array — create test orders with known data and verify the document generates without exceptions. For the worker, mock `IServiceScopeFactory` and the `DbContext`.

**Priority: Medium** — Receipt errors won't break ordering, but incorrect receipts cause customer confusion and accounting issues.

---

### 7. Authorization Edge Cases — PARTIAL COVERAGE

**Existing:** `AuthEndpointsTests.cs` tests Owner/Staff/Anonymous for categories and active orders.

**Missing:**
- Staff trying to access Owner-only endpoints beyond categories (e.g., `POST /api/ingredients`, `PUT /api/menu-items/{id}`, variant/recipe/available-ingredient write endpoints)
- Anonymous access to `PATCH /api/orders/{id}/status` (should be 401)
- Token expiration handling
- Concurrent order status updates (race conditions)

**Priority: Low** — The authorization middleware is framework-level, so if it works for one endpoint, it works for all. But a regression test per endpoint group would catch accidental policy removal.

---

### 8. Client-Side Components — MINIMAL COVERAGE

**Existing:** `CartDrawerTests.cs`, `CartItemTests.cs`, `CartServiceTests.cs`

**Missing client components likely exist for:**
- Menu browsing / item selection
- Order placement flow
- Order status tracking
- Admin panels (menu management, ingredient management, inventory)

**Priority: Low** — The Blazor WASM components are primarily UI wiring. The critical business logic (pricing, validation) runs server-side and is tested there.

---

### 9. No CI/CD Pipeline

There are no GitHub Actions, GitLab CI, or other CI configurations. Tests only run when a developer remembers to run `dotnet test` locally.

**Recommendation:** Add a GitHub Actions workflow that:
- Starts a PostgreSQL service container
- Runs `dotnet test` across all three projects
- Fails the PR check if any test fails

**Priority: High** — Without CI, tests degrade over time as broken tests go unnoticed.

---

## Summary Table

| Area | Current Tests | Gap Severity | Effort |
|---|---|---|---|
| Variant/AvailableIngredient/RecipeIngredient endpoints | 0 | **High** | Medium |
| Stripe payment flow (service + webhooks) | 0 | **High** | High |
| CI/CD pipeline | None | **High** | Low |
| Auth validators | 0 | Medium | Low |
| SignalR broadcasts | 0 | Medium | Medium |
| Receipt generation (PrintAgent) | 0 | Medium | Medium |
| TokenService | 0 | Low | Low |
| Authorization edge cases | Partial | Low | Low |
| Client components | 3 files | Low | High |
