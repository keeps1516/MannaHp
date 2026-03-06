# Manna HP - UX Evaluation Report

**Date:** March 5, 2026
**Method:** Automated browser testing via Playwright (Firefox) against `http://localhost:3000`
**Last Updated:** March 5, 2026 (Round 2 — full re-evaluation)

---

## Summary

The app is functional and visually polished with a dark theme. The core ordering flow works — bowl builder, drink selection, cart, in-store payment, admin dashboard, kitchen orders, and menu management are all operational. However, there are several bugs and missing features that would significantly impact real-world usage.

**Critical: 3 | High: 5 | Medium: 8 | Low/Enhancement: 10**

---

## Bugs

### CRITICAL

#### 1. Edit button on bowl cart item does not populate the bowl builder
- **Location:** `src/next-client/src/components/cart-drawer.tsx:134-137` and `src/next-client/src/components/bowl-builder.tsx:30-36`
- **Issue:** Clicking the Edit (pencil) button on a bowl in the cart drawer removes the item from the cart entirely, then navigates to a blank bowl builder. The bowl name, ingredient selections, and quantity are all lost. The `BowlBuilder` component has no mechanism to receive previous selections — it initializes all quantities from `isDefault` flags.
- **Root cause:** The Edit onClick handler calls `cart.removeItem(item.id)` then `router.push(...)` without passing any state. The `CartItem` type stores `selectedIngredients`, `notes` (bowl name), and `quantity` — all the data needed — but the edit flow discards it.
- **Observed:** Built a "Simple Bowl" ($12.00, named "Test Bowl"), added to cart, clicked Edit. Bowl builder loaded with $0.00 total, empty name, all ingredient quantities at 0.
- **Impact:** Customers who want to tweak their bowl lose their entire build and must start over. The Edit button is actively destructive — worse than not having it.
- **Fix:** Add an `editingItem` state to the cart context. The Edit button should set it (not remove the item). The `BowlBuilder` should read from it on mount to initialize `quantities`, `bowlName`, and `bowlQty`. On "Add to Cart" while editing, update the existing item rather than creating a new one.

#### 2. Admin login fails silently — no error feedback on 401
- **Location:** `/admin/login` — login form + `POST /api/auth/login`
- **Issue:** When login fails (API returns 401 with empty body), the login page shows absolutely no error message. The form stays as-is — no "Invalid credentials", no red border, no toast. The user has zero feedback that anything went wrong.
- **Observed:** Entered `owner@manna.local` / `Owner123!` — API responded with 401, page stayed on `/admin/login` with no visible change.
- **Impact:** Staff/owner can't tell if their password is wrong, the server is down, or something else. They'll keep re-entering credentials with no guidance.
- **Fix:** Display a clear error message like "Invalid email or password" when login returns 401. Also ensure the API returns a JSON error body (currently returns `Content-Length: 0`).

#### 2. Mobile bowl builder — sticky footer overlaps protein cards
- **Location:** Bowl builder page on mobile (375px viewport)
- **Issue:** The sticky "Bowl Total / Qty / Add to Cart" footer bar overlaps directly on top of the Proteins section. Protein ingredient names (Ground Beef, Chicken) are completely hidden behind the footer. Only the prices and +/- buttons are visible below the footer bar.
- **Observed:** On iPhone-sized viewport, the footer sits at the same Y position as the protein cards, making it impossible to see what protein you're selecting.
- **Impact:** Customers on mobile (the primary use case) can't see protein names when building their bowl. They have to guess what the $3.00 items are.
- **Fix:** Add sufficient bottom padding/margin to the scrollable content area so the sticky footer doesn't overlap ingredients. Or make the footer position fixed at the viewport bottom with proper scroll padding.

### HIGH

#### 3. Order status page shows raw "API error 404:" for invalid orders
- **Location:** `/order/{id}` page
- **Issue:** Navigating to an order status page (e.g., `/order/test-id-123`) shows a red banner with "API error 404:" and nothing else. No helpful message, no link back to menu.
- **Impact:** If a customer bookmarks an order URL and returns later (after the order data is cleaned up), or if the URL is malformed, they see an ugly error with no recovery path.
- **Fix:** Show a friendly "Order not found" page with a link back to the menu. Handle 404 API responses gracefully.

#### 4. Seasonal specials have broken/missing item images
- **Location:** Item detail pages for Seasonal Specials (e.g., Pumpkin Spice Latte)
- **Issue:** The image area shows a large gray-teal box with just the item name as text ("Pumpkin Spice Latte"). There's a small "Not an accurate image" disclaimer in the corner. The image is either missing or the URL is broken.
- **Observed:** Traditional Drinks have proper latte art photos. Seasonal Specials have text-placeholder blocks instead.
- **Impact:** Looks unfinished and unprofessional. Seasonal specials should be the most visually appealing items on the menu.
- **Fix:** When there is no image, use a styled text-only card design that looks intentional rather than broken.

#### 5. Seasonal specials category thumbnails are truncated text blocks
- **Location:** `/category/...seasonal-specials` — item list
- **Issue:** Instead of images, each seasonal item shows a small dark square with the item name in truncated text: "Pumpki Spice", "Maple Brown", "Toasted Marshn", "Pepper Mocha". The text is cut off because the thumbnail area is too small.
- **Impact:** Looks broken. The truncated text ("Pumpki", "Marshn") is unprofessional and unhelpful.
- **Fix:** Either provide proper thumbnail images, or adjust the fallback to show the full item name (perhaps with a gradient/icon instead of squished text).

#### 6. "Today's Revenue" shows "Coming Soon" placeholder
- **Location:** Admin dashboard at `/admin`
- **Issue:** The revenue card just shows "Coming Soon" — no actual revenue tracking.
- **Impact:** Owner has no way to see daily sales without manually counting orders.
- **Fix:** Query completed orders for today, sum totals, and display. This data already exists in the `orders` table.

#### 7. Store Settings page is a placeholder
- **Location:** `/admin/settings`
- **Issue:** "Tax rate, store name, address, and other settings will be configurable here in a future update."
- **Impact:** Tax rate is hardcoded at 8.25% in both the frontend and backend. Store name/address are hardcoded. Owner can't change these without code changes.
- **Fix:** Implement the `app_settings` table and admin UI for: tax rate, store name, address, phone, receipt footer text.

### MEDIUM

#### 8. Cart button shows dollar amount instead of item count
- **Location:** Header cart icon (all pages)
- **Issue:** The cart button in the header shows the cart total in dollars (e.g., "$4.75", "$12.00") instead of the number of items. As items are added, the dollar amount changes which makes the button width inconsistent.
- **Observed:** After adding a $4.75 latte, button shows "$4.75". After adding a $12.00 bowl, it shows "$16.75".
- **Impact:** Users typically expect to see item count on a cart badge (e.g., "2"). The dollar amount is useful information but belongs inside the cart drawer, not on the small header button.
- **Fix:** Show item count as a badge number on the cart icon. Move dollar total to inside the cart drawer.

#### 9. No order confirmation feedback for in-store orders
- **Location:** Cart drawer — checkout flow
- **Issue:** After "Pay In-Store" succeeds, a victory video overlay plays but there's no order number shown, no receipt, and no way for the customer to track their order status.
- **Impact:** Customer doesn't know their order number to reference when picking up.
- **Fix:** After placing an in-store order, show the order number prominently and optionally navigate to the order status page (`/order/{id}`).

#### 10. Add-Ons category items have no images
- **Location:** `/category/...add-ons` — Flavor Shot, Espresso Shot, Whipped Cream, Alternative Milk
- **Issue:** Unlike other categories, Add-On items show no thumbnail image at all — just text and price on a flat card.
- **Impact:** Inconsistent with other categories that have images/emojis. The page looks sparse and unfinished.
- **Fix:** Add simple icons or images for add-on items to match the visual style of other categories.

#### 11. Mobile client-side navigation may show empty skeleton cards briefly
- **Location:** Homepage -> Category navigation on mobile
- **Issue:** When navigating from the homepage to a category via client-side navigation on mobile (375px), empty skeleton card placeholders were briefly observed with no text content. On direct URL navigation, content loads fine.
- **Observed:** Inconsistent — happened in initial test round but not in follow-up. May be a race condition with client-side hydration/data fetching.
- **Impact:** Users may briefly see blank cards before content renders, creating a perception of brokenness.
- **Fix:** Investigate React/Next.js hydration timing on mobile. Ensure skeleton states show proper shimmer animation and transition smoothly to loaded content.

#### 12. No way to edit a bowl after adding to cart
- **Location:** Cart drawer
- **Issue:** Once a bowl is added to cart, the only option is to remove it and rebuild from scratch. The edit button navigates to the bowl builder but doesn't pre-populate existing selections properly.
- **Impact:** If a customer wants to add cheese to their bowl after adding it to cart, they have to remove and rebuild the entire bowl.
- **Fix:** Ensure the "Edit" button on cart bowl items navigates back to the bowl builder pre-populated with all current selections.

#### 13. Console warning: Missing `Description` or `aria-` attribute on cart drawer
- **Location:** Cart drawer Sheet component
- **Issue:** `Warning: Missing Description or aria-describedby` from the Sheet/Dialog component.
- **Impact:** Accessibility violation. Screen readers won't properly announce the cart drawer purpose.
- **Fix:** Add a `<SheetDescription>` or `aria-describedby` to the cart drawer Sheet component.

#### 14. JavaScript resource loading warnings on every page
- **Location:** Every page in the app
- **Issue:** Console shows `JavaScript Warning: "The resource at http://localhost:3000/_next/static/chunks/..." Loading failed for the <script>` on page navigations.
- **Impact:** While not user-facing, indicates potential performance issue with failed/duplicate script loading. May cause intermittent functionality issues.
- **Fix:** Investigate the broken script chunk references. May be a Next.js build caching issue.

#### 15. No empty state for "Preparing" and "Ready" columns in admin orders
- **Location:** Admin orders page (Kanban view)
- **Issue:** When there are no orders in "Preparing" or "Ready" status, those columns may not appear. Only columns with orders show.
- **Impact:** Staff can't see the full Kanban workflow at a glance. Unclear if the other columns exist.
- **Fix:** Always show all three columns with empty state messages like "No orders preparing" when empty.


#### 16. Changing tax on admin settings does not change the tax calculation in the customers order drawer
- You will need to research further

#### 17. Blue background does not fill entire space, there is now a section that is just black. Make the blue background fill the backgroun.

#### 18. Bowl-added toast blocks cart payment area
- **Status:** FIXED
- **Issue:** When a burrito bowl is added to the cart, the success toast appears at the bottom of the screen, blocking the Bowl Total and the payment options. The toast should show above the Bowl Total so it does not block when the user clicks the cart to pay.
- **Fix:** Changed `<Toaster position="bottom-right" ...>` to `position="top-center"` in `src/next-client/src/app/layout.tsx:36`. Toasts now appear at the top of the screen, clear of the bowl builder sticky footer and cart drawer payment buttons.

#### 19. Order number not prominently displayed after payment
- **Status:** FIXED
- **Issue:** After paying for an order, the order number blurb needs to display at the top fourth of the screen so customers can clearly see their order number.
- **Fix:** Moved the order number overlay in `cart-drawer.tsx` from `bottom-16` to `top-[20%]` and increased font size to `text-5xl`. Also added the order number (`#{orderNumber}`) prominently at the top of the order confirmation page (`/order/[id]/page.tsx`) above "Order Placed!" heading. Test added in `order-status-page.test.tsx`.

#### 20. Bowl builder ingredient card click behavior is unintuitive
- **Status:** FIXED
- **Issue:** On the bowl builder screen, when the user clicks an ingredient card once it adds quantity 1, and when they click again it subtracts it. Instead, the user should be able to click anywhere on the card to increase the quantity, except for a minus indicator which will decrement the quantity.
- **Fix:** Changed the card body click handler in `bowl-builder.tsx:263` from toggle behavior (`qty === 0 ? +1 : -qty`) to always increment (`updateQty(ing.id, 1)`). The minus button in the quantity controls still decrements as before. Test added to verify multiple clicks increment correctly.

#### 21. Edit bowl from cart doesn't populate when just added
- **Status:** FIXED
- **Issue:** If a user has just added a bowl to the cart, opens the cart drawer, then clicks edit, the burrito bowl screen is not populated with their selections. However, if the user navigates away first and then clicks edit, the bowl builder correctly shows their previous choices.
- **Root cause:** `BowlBuilder` used `useState` initializers to read `editingItem`, which only runs once at mount time. If the component was already mounted (user stayed on the bowl page), clicking Edit set `editingItem` in context but the existing `quantities`, `bowlName`, and `bowlQty` state wouldn't update.
- **Fix:** Added a `useEffect` in `bowl-builder.tsx` that watches `editingItem` and re-initializes `quantities`, `bowlName`, and `bowlQty` when `editingItem` changes after mount. Also added `beforeEach(localStorage.clear())` to bowl builder tests to prevent state leaks between tests. Test added to verify the scenario of editing while BowlBuilder is already mounted.

---

## Resolved Bugs (Previously Reported, Now Fixed)

#### Cart state not persisted across page reloads (was Critical #1)
- **Status:** FIXED
- **Verified:** Added a Latte ($4.75) to cart, reloaded page — cart badge still showed $4.75 after reload. Cart now persists via localStorage.

#### SignalR shows "Polling" briefly then switches to "Live" (was Medium #16)
- **Status:** FIXED (per commit `ee307b8`)
- **Change:** Now shows "Connecting..." instead of "Polling" during initial connection.

#### Empty kanban columns hidden on mobile (was Medium #15)
- **Status:** PARTIALLY FIXED (per commit `5cf7c28`)
- **Note:** Fix was specifically for mobile; desktop behavior needs verification with active orders.

#### Bowl builder doesn't show total including tax (was Medium #11)
- **Status:** FIXED
- **Verified:** Bowl builder now shows "Est. with tax: $12.99" below the bowl total ($12.00). Tax estimate is visible before adding to cart.

#### Edit button on bowl cart item does not populate the bowl builder (was Critical #1)
- **Status:** FIXED
- **Change:** Added `editingItem`, `setEditingItem`, `clearEditingItem`, and `updateItem` to cart context. Edit button now sets the editing state instead of removing the item. BowlBuilder reads `editingItem` on mount to pre-populate quantities, bowl name, and quantity. "Add to Cart" becomes "Update Cart" when editing. Tests added to cart-context, bowl-builder, and cart-drawer test suites.

#### Admin login fails silently — no error feedback on 401 (was Critical #2)
- **Status:** FIXED
- **Change:** `adminApi.login` now uses a direct fetch instead of `adminFetch` (which redirected to `/admin/login` on 401, causing a full page reload that wiped the error state). On 401 it throws "Invalid email or password" which the login form displays. Tests added in `admin-login.test.tsx` and `admin-api.test.ts`.

#### Mobile bowl builder — sticky footer overlaps protein cards (was Critical #2)
- **Status:** FIXED
- **Change:** Added `pb-32` bottom padding to the bowl builder container so content scrolls clear of the sticky footer on all viewports. Test added to bowl-builder test suite.

#### Order status page shows raw "API error 404:" for invalid orders (was High #3)
- **Status:** FIXED
- **Change:** 404 errors now show a friendly "Order not found" message with a "Back to menu" link instead of the raw API error string. Test added in `order-status-page.test.tsx`.

#### Seasonal specials have broken/missing item images (was High #4)
- **Status:** FIXED
- **Change:** `FixedItemDetail` now renders a styled gradient fallback with the item name when `imageUrl` is null, instead of showing nothing. Tests added.

#### Seasonal specials category thumbnails are truncated text blocks (was High #5)
- **Status:** FIXED
- **Change:** `ItemCard` now renders a gradient fallback thumbnail with the item's first letter when `imageUrl` is null, instead of showing no thumbnail at all. Tests added.

#### "Today's Revenue" shows "Coming Soon" placeholder (was High #6)
- **Status:** FIXED (previously resolved)
- **Verified:** Dashboard calls `adminApi.getTodayRevenue` and displays the result. Server endpoint exists at `GET /api/orders/today-revenue`.

#### Store Settings page is a placeholder (was High #7)
- **Status:** FIXED (previously resolved)
- **Verified:** Settings page is fully implemented with store name, address, city, phone, tax rate, and receipt footer fields. Loads from and saves to `app_settings` table.

#### Cart button shows dollar amount instead of item count (was Medium #8)
- **Status:** FIXED
- **Change:** Header cart button now shows a numeric item count badge instead of the dollar subtotal. Dollar total remains visible inside the cart drawer. Tests added to header test suite.

#### No order confirmation feedback for in-store orders (was Medium #9)
- **Status:** FIXED (previously resolved)
- **Verified:** After successful in-store order, a video overlay displays with the order number prominently shown. Tapping navigates to the order status page.

#### Add-Ons category items have no images (was Medium #10)
- **Status:** FIXED
- **Change:** Covered by the `ItemCard` fallback thumbnail fix from Bug #5 — items without images now show a styled gradient initial.

#### No way to edit a bowl after adding to cart (was Medium #12)
- **Status:** FIXED
- **Change:** Same fix as Bug #1. Edit button now pre-populates the bowl builder with existing selections.

#### Console warning: Missing `Description` or `aria-` attribute on cart drawer (was Medium #13)
- **Status:** FIXED (previously resolved)
- **Verified:** `SheetDescription` with `sr-only` class is present in the cart drawer. Existing test verifies `aria-describedby` is set.

#### No empty state for "Preparing" and "Ready" columns in admin orders (was Medium #15)
- **Status:** FIXED (previously resolved)
- **Verified:** All three kanban columns always render with "No orders" empty state. Tests exist in `orders-page.test.tsx`.

#### Changing tax on admin settings does not change the tax calculation in the customer order drawer (was Medium #16)
- **Status:** FIXED
- **Change:** Server-side `OrderEndpoints.cs` now reads `DefaultTaxRate` from `app_settings` table instead of hardcoded `0.0825`. Added public `GET /api/settings/public` endpoint (no auth required) that returns the tax rate. Cart context fetches tax rate dynamically on mount via `api.getPublicSettings()`. All hardcoded `TAX_RATE = 0.0825` values in cart-context, bowl-builder, order-summary-panel, and submit-order page replaced with dynamic values. Cart drawer and bowl builder now display the actual tax rate percentage. Tests added.

#### Blue background does not fill entire space (was Medium #17)
- **Status:** FIXED
- **Change:** Added `bg-background` to `html` element and `min-h-screen` to `body` in `globals.css` so the dark navy background fills the entire viewport. Also cleaned up duplicate `@apply` rules.

---

## Feature Enhancements

### Customer Experience

#### 16. Add order tracking page for customers
- After placing an order, customers should be able to see real-time status updates (Received -> Preparing -> Ready) on their order page using the existing SignalR infrastructure.
- The `/order/{id}` page exists but currently shows an API error for invalid IDs (see Bug #3) and needs real-time status updates.

#### 17. Add popular items / featured items to homepage
- The homepage just shows categories. Adding a "Popular" or "Quick Order" section with the most-ordered items would reduce clicks for regulars.

#### 18. Add search functionality to customer menu
- There's a search bar on the admin menu page but not on the customer-facing menu. Regulars who know what they want should be able to search "Mocha" directly.

#### 19. Add "Recently Ordered" / "Order Again"
- For returning customers (once auth is implemented), show their previous orders for quick reordering.

#### 20. Add breadcrumb navigation on item pages
- Item detail pages have a small "Back to menu" link but no breadcrumb showing the path (Menu > Traditional Drinks > Latte). Users lose context of where they are in the hierarchy.

### Admin Experience

#### 21. Make Ingredients grid more mobile friendly
- Ther is too much info displayed. Show only the Name, combine Stock and abbreivated Unit to read for example "300 oz". When the user clicks on the row take them to an edit screen that shows all info and allows a delete of the item. 

#### 21. Add order history / completed orders view
- The orders page only shows active orders. There's no way for the owner to view completed orders, search past orders, or see order history.

#### 22. Low Stock Items Card 
- On the /admin page Low Stock Items displays even if there are none, only display this card when there are low stock inventory. Use a signalR connection to refresh this in real time when items reach the threshold.

#### 23. Add bulk stock update
- Updating stock for 34 ingredients one by one in the ingredients table is tedious. A "Restock" form with quick quantity updates would save time during inventory.

#### 24. Add order sound notification
- When a new order arrives on the kitchen display, there should be an audio alert (beep/chime) so staff don't have to constantly watch the screen.

#### 25. Add revenue/analytics dashboard
- Beyond "Today's Revenue", add: average order value after ingredient cost is taken out, most popular items, daily/weekly/monthly trends.

#### 26. Merge Active Orders card and View Active Orders into the same
- On the /admin page their are 2 cards that deal with orders, merge them into the same element that when clicked takes the user to view the active orders, keep it as the top first card on the screen.

---

## Infrastructure Bugs

#### 27. Inventory not decremented when order is completed
- **Status:** FIXED
- **Location:** `src/Server/EndPoints/OrderEndpoints.cs` — `PATCH /api/orders/{id}/status`
- **Issue:** The CLAUDE.md architecture doc specifies "When an order is marked Completed, ingredient stock quantities are automatically decremented based on the recipe." However, the `PATCH /api/orders/{id}/status` endpoint only updated the status and broadcast via SignalR — it never touched `Ingredient.StockQuantity`. Inventory was never subtracted regardless of how many orders were completed.
- **Root cause:** The inventory decrement logic was never implemented. The endpoint used `db.Orders.FindAsync(id)` (no includes) and had no awareness of order items or ingredients.
- **Fix:** Added `DecrementInventoryAsync` method to `OrderEndpoints.cs`. On status transition to `Completed`:
  - **Customizable items (bowls):** Decrements `Ingredient.StockQuantity` by `OrderItemIngredient.QuantityUsed * OrderItem.Quantity` for each ingredient in the order.
  - **Fixed items (drinks/sides):** Looks up `RecipeIngredient` records via the order item's variant and decrements `Ingredient.StockQuantity` by `RecipeIngredient.Quantity * OrderItem.Quantity`.
  - Aggregates all decrements per ingredient to minimize DB reads (single query for all affected ingredients).
  - Guards against double-decrement: captures `previousStatus` before mutation and only decrements on actual transition to `Completed`.
  - Changed `FindAsync` to `FirstOrDefaultAsync` with `.Include()` for `Items.Ingredients` and `Items.Variant.RecipeIngredients`.
- **Tests added:** 5 new tests in `OrderEndpointsTests.cs`:
  1. `Completing_BowlOrder_DecrementsIngredientStock` — verifies rice and chicken stock decrease by `QuantityUsed`
  2. `Completing_BowlOrder_Qty2_DecrementsDoubleStock` — verifies qty multiplier works
  3. `Completing_FixedItem_DecrementsViaRecipeIngredients` — verifies espresso and milk decrease per recipe
  4. `Completing_FixedItem_Qty2_DecrementsDoubleRecipe` — verifies qty multiplier for fixed items
  5. `NonCompleted_StatusChange_DoesNotDecrementStock` — verifies Preparing status doesn't decrement





---

## Visual/Polish Issues

| Issue | Location | Description |
|-------|----------|-------------|
| Page title doesn't update | All pages | Always shows "Manna + HP — Order" regardless of page. Should show "Manna + HP — Latte" on item pages. |
| Topping prices cut off on desktop | Bowl builder, scroll position | Bottom of Fresh Toppings row is partially hidden by the sticky footer. Prices ($0.50, $0.25) are cut off. Less severe than mobile but still present. |
| No favicon | Browser tab | Uses default Next.js favicon. Should use the Manna + HP logo. |
| Sides & Drinks items have text-only thumbnails | Category list | "Side of Chips" and "Chips & Salsa" show small dark boxes with truncated text instead of images. |
| "Not an accurate image" disclaimer | Seasonal item detail | Small text in corner of the broken image placeholder. If images are AI-generated, this disclaimer should be styled better or removed. |

---

## Priority Recommendations

### Do First (Blocks Real Usage)
1. **Fix bowl edit to populate builder** — Bug #1. Edit button is actively destructive — destroys the bowl.
2. **Fix admin login error feedback** — Bug #2. Users can't log in without knowing what's wrong.
3. **Fix mobile bowl builder footer overlap** — Bug #3. Primary ordering flow broken on mobile.
4. **Fix seasonal item images** — Bugs #5, #6. Looks obviously broken.

### Do Next (Significantly Improves Experience)
4. **Implement Today's Revenue** on dashboard — Bug #6.
5. **Fix order status page error handling** — Bug #3.
6. **Cart button: show item count not dollar amount** — Bug #8.
7. **In-store order confirmation with order number** — Bug #9.
8. **Cart drawer aria attributes** — Bug #13.

### Do Later (Polish & Features)
9. Store Settings page — Bug #7.
10. Order history for admin — Enhancement #21.
11. Real-time order tracking for customers — Enhancement #16.
12. Order sound notifications — Enhancement #24.
13. Add-On item images — Bug #10.
