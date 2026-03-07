# Manna HP - Feature Plans

**Source:** [UX Evaluation Report](../ux-evaluation.md) (Feature Enhancements section, items #16-26)
**Created:** March 6, 2026

---

## Priority Ranking Overview

| Priority | Feature | Section | Mobile Impact |
|----------|---------|---------|---------------|
| P1 | Real-time order tracking for customers | [F1](#f1-real-time-order-tracking-for-customers) | Critical - primary use case |
| P2 | Merge admin order cards | [F2](#f2-merge-active-orders-cards-on-admin-dashboard) | High - reduces clutter |
| P3 | Low stock card conditional display + real-time | [F3](#f3-low-stock-items-card-conditional-display--real-time) | Medium |
| P4 | Mobile-friendly ingredients grid | [F4](#f4-mobile-friendly-ingredients-grid) | Critical - currently unusable on mobile |
| P5 | Order history / completed orders view | [F5](#f5-order-history--completed-orders-view) | High - essential admin feature |
| P6 | Search functionality on customer menu | [F6](#f6-search-functionality-on-customer-menu) | Critical - speeds up mobile ordering |
| P7 | Popular / featured items on homepage | [F7](#f7-popular--featured-items-on-homepage) | High - reduces taps for regulars |
| P8 | Order sound notification | [F8](#f8-order-sound-notification) | High - kitchen staff awareness |
| P9 | Bulk stock update | [F9](#f9-bulk-stock-update) | High - inventory workflow |
| P10 | Breadcrumb navigation on item pages | [F10](#f10-breadcrumb-navigation-on-item-pages) | Medium - orientation |
| P11 | Revenue / analytics dashboard | [F11](#f11-revenue--analytics-dashboard) | Medium - owner insight |
| P12 | "Recently Ordered" / Order Again | [F12](#f12-recently-ordered--order-again) | High - requires auth first |
| P13 | Inventory Check-In / Receiving | [F13](#f13-inventory-check-in--receiving) | High - restocking workflow |
| P14 | Menu Item Image Upload | [F14](#f14-menu-item-image-upload) | Medium - admin photo management |
| G4 | QR Code In-Store Token Flow | [G4](#g4-qr-code-in-store-token-flow) | High - in-store ordering |
| G12 | API Rate Limiting | [G12](#g12-api-rate-limiting) | Low - security hardening |
| F15 | Email-Based Inventory Restocking | [F15](#f15-email-based-inventory-restocking) | Medium - automates supplier receipt processing |

---

## F1: Real-time Order Tracking for Customers

**UX Evaluation Ref:** [Enhancement #16](../ux-evaluation.md) - "Add order tracking page for customers"
**Priority:** P1
**Mobile Impact:** Critical - customers track orders on their phones

### Current State
- `/order/[id]` page exists and displays order details statically
- Page fetches order once on mount via `api.getOrder(id)` — no updates after that
- SignalR hub already broadcasts `OrderStatusChanged` events to `order-{orderId}` groups
- Admin kitchen display already uses SignalR for real-time updates
- `src/lib/order-hub.ts` exports `connectOrderHub()` / `disconnectOrderHub()`

### Plan

#### Step 1: Add SignalR subscription to order status page
- **File:** `src/next-client/src/app/(customer)/order/[id]/page.tsx`
- Import `connectOrderHub` / `disconnectOrderHub` from `src/lib/order-hub.ts`
- On mount (after fetching order), connect to SignalR and join the `order-{orderId}` group
- Listen for `OrderStatusChanged` events and update local order state
- On unmount, disconnect from SignalR
- Add a connection status indicator (subtle, bottom of page)

#### Step 2: Add visual status stepper component
- **File:** `src/next-client/src/components/order-status-stepper.tsx` (new)
- Horizontal stepper showing: Received -> Preparing -> Ready -> Completed
- Active step highlighted with cyan accent, completed steps with checkmarks
- Animate transitions between steps
- Mobile-friendly: horizontal scroll or compact layout on small screens

#### Step 3: Add fallback polling
- If SignalR connection fails, poll `api.getOrder(id)` every 10 seconds
- Show "Live" vs "Updating..." indicator (same pattern as admin orders page)

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/order-status-stepper.test.tsx`
```
1. renders all four status steps (Received, Preparing, Ready, Completed)
2. highlights the current active step based on `status` prop
3. marks previous steps as completed (checkmark icon)
4. does not mark future steps as active or completed
5. renders correctly on narrow viewport (no overflow)
```

#### Integration Tests — `src/next-client/src/__tests__/components/order-tracking.test.tsx`
```
1. order page subscribes to SignalR on mount
2. order status updates when SignalR event received
3. stepper advances when status changes from Received to Preparing
4. falls back to polling when SignalR connection fails
5. disconnects SignalR on unmount (cleanup)
6. shows connection status indicator ("Live" / "Updating...")
```

---

## F2: Merge Active Orders Cards on Admin Dashboard ✅

**Status:** Complete
**UX Evaluation Ref:** [Enhancement #26](../ux-evaluation.md) - "Merge Active Orders card and View Active Orders into the same"
**Priority:** P2
**Mobile Impact:** High - fewer cards = less scrolling on mobile

### Implementation Notes
- Merged "Active Orders" stat card and "View Active Orders" quick-link into a single clickable card
- Card links to `/admin/orders`, shows total count + status breakdown (e.g., "2 Received · 1 Preparing · 3 Ready")
- Card is the first in the stats grid (`data-testid="orders-card"`)
- Removed redundant "View Active Orders" quick-link; quick-links grid now shows 2 items (Menu, Inventory)
- Added `computeBreakdown()` helper using `OrderStatus` enum to count Received/Preparing/Ready
- Breakdown text only shows statuses with count > 0

### Files Modified
- `src/next-client/src/app/admin/(dashboard)/page.tsx` — merged card, removed quick-link, added breakdown logic
- `src/next-client/src/__tests__/components/admin-dashboard.test.tsx` — 5 new tests (7 total)

---

## F3: Low Stock Items Card Conditional Display + Real-time ✅

**Status:** Complete
**UX Evaluation Ref:** [Enhancement #22](../ux-evaluation.md) - "Low Stock Items Card"
**Priority:** P3
**Mobile Impact:** Medium - cleaner dashboard

### Implementation Notes
- Low stock card only renders when `lowStockCount > 0` — hidden entirely when all stock is healthy
- Card is clickable, links to `/admin/ingredients` (`data-testid="low-stock-card"`)
- Card uses amber border/text styling to draw attention
- Dashboard subscribes to SignalR `LowStockAlert` event via `connectOrderHub` for real-time updates
- Backend: after `DecrementInventoryAsync`, counts active ingredients below threshold and broadcasts `LowStockAlert { lowStockCount }` to kitchen group (only when count > 0)

### Files Modified
- `src/next-client/src/app/admin/(dashboard)/page.tsx` — conditional low stock card, SignalR subscription
- `src/next-client/src/__tests__/components/admin-dashboard.test.tsx` — 5 new F3 tests (12 total)
- `src/Server/EndPoints/OrderEndpoints.cs` — low stock check + broadcast after inventory decrement
- `tests/MannaHp.Server.Tests/SignalR/LowStockAlertTests.cs` — 3 backend integration tests (new file)

---

## F4: Mobile-Friendly Ingredients Grid ✅

**Status:** Complete
**UX Evaluation Ref:** [Enhancement #21 (first)](../ux-evaluation.md) - "Make Ingredients grid more mobile friendly"
**Priority:** P4
**Mobile Impact:** Critical - ingredients table is unusable on small screens

### Implementation Notes
- Mobile (< md): compact card layout showing name + "300 oz" stock summary, LOW badge, inactive opacity
- Desktop (>= md): existing 7-column table preserved unchanged
- Uses Tailwind `md:hidden` / `hidden md:block` for responsive switching
- Tapping a mobile card opens a detail Sheet showing all fields (unit, cost, stock, threshold, status)
- Detail sheet has Edit + Deactivate buttons; Edit opens the existing `IngredientFormSheet`
- Added `unitShortLabel()` helper in `unit-options.ts` for compact unit abbreviations (oz, lb, cups, etc.)
- Search filters both mobile cards and desktop table rows

### Files Modified
- `src/next-client/src/app/admin/(dashboard)/ingredients/page.tsx` — mobile card layout, detail sheet, responsive views
- `src/next-client/src/lib/unit-options.ts` — added `unitShortLabel()` helper
- `src/next-client/src/__tests__/components/ingredients-page.test.tsx` — 9 new tests (new file)

---

## F5: Order History / Completed Orders View

**UX Evaluation Ref:** [Enhancement #21 (second)](../ux-evaluation.md) - "Add order history / completed orders view"
**Priority:** P5
**Mobile Impact:** High - owner needs to check past orders on the go

### Current State
- Admin orders page only shows active orders (Received, Preparing, Ready)
- `getActiveOrders` endpoint filters out Completed and Cancelled
- No endpoint exists for fetching completed/historical orders
- No search or date filtering

### Plan

#### Step 1: Add backend endpoint for order history
- **File:** `src/Server/EndPoints/OrderEndpoints.cs`
- `GET /api/orders/history?page=1&pageSize=20&search=&from=&to=`
  - Returns paginated completed/cancelled orders, newest first
  - Search by order number or customer notes
  - Filter by date range
  - Staff+ authorization

#### Step 2: Add order history tab to admin orders page
- **File:** `src/next-client/src/app/admin/(dashboard)/orders/page.tsx`
- Add tabs: "Active Orders" (existing kanban) | "Order History" (new)
- History tab shows a scrollable list/table:
  - Order number, date/time, items summary, total, status (Completed/Cancelled)
  - Tap to expand and see full order details
  - Search bar + date range picker at top
  - Pagination (load more on scroll or page buttons)

#### Step 3: Add admin API method
- **File:** `src/next-client/src/lib/admin-api.ts`
- `getOrderHistory(token, { page, pageSize, search, from, to })` method

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/order-history.test.tsx`
```
1. renders order history list with completed orders
2. displays order number, date, total for each order
3. search input filters orders by order number
4. date range picker filters orders by date
5. shows "No orders found" when list is empty
6. pagination loads next page of results
7. tapping an order expands to show full details
```

#### Integration Tests (Backend) — `src/Server.Tests/OrderHistoryEndpointTests.cs`
```
1. GET /api/orders/history returns only Completed and Cancelled orders
2. results are paginated with correct page size
3. search parameter filters by order number
4. date range parameters filter correctly
5. requires Staff authorization (401 without token)
6. orders are sorted newest first
```

---

## F6: Search Functionality on Customer Menu

**UX Evaluation Ref:** [Enhancement #18](../ux-evaluation.md) - "Add search functionality to customer menu"
**Priority:** P6
**Mobile Impact:** Critical - regulars know what they want, searching is faster than browsing categories

### Current State
- Admin menu page has a search bar (client-side filtering)
- Customer-facing homepage has no search — only category grid
- All menu items are fetched on homepage load (`api.getMenuItems()`)

### Plan

#### Step 1: Add search bar to customer homepage
- **File:** `src/next-client/src/app/(customer)/page.tsx`
- Add a search input at the top of the page, above the category grid
- Styled to match dark theme: dark input with cyan focus ring
- Placeholder: "Search menu..."
- Mobile: full-width, sticky below header

#### Step 2: Implement client-side filtering
- When search input has text, hide the category grid and show filtered item results
- Filter menu items by name (case-insensitive, partial match)
- Display results as `ItemCard` components in a grid
- For customizable items, link to `/category/[categoryId]` (opens bowl builder)
- For fixed items, link to `/item/[id]`
- Show "No results for '[query]'" when nothing matches
- Clear search button (X icon) to return to category view

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/home-page.test.tsx`
```
1. renders search input on the homepage
2. typing in search hides category grid and shows filtered results
3. search filters items by name (case-insensitive)
4. shows "No results" message when no items match
5. clearing search restores category grid view
6. search results render as ItemCard components
7. customizable items in search results link to their category page
8. fixed items in search results link to their item detail page
```

---

## F7: Popular / Featured Items on Homepage

**UX Evaluation Ref:** [Enhancement #17](../ux-evaluation.md) - "Add popular items / featured items to homepage"
**Priority:** P7
**Mobile Impact:** High - reduces number of taps to order favorite items

### Current State
- Homepage shows only a grid of category cards
- All menu items fetched on mount but only used for counting items per category
- No concept of "popular" or "featured" in the data model

### Plan

#### Step 1: Add backend endpoint for popular items
- **File:** `src/Server/EndPoints/MenuItemEndpoints.cs`
- `GET /api/menu-items/popular?limit=4`
  - Returns top N menu items by order count (last 30 days)
  - Joins `order_items` -> `menu_items`, groups by menu item, counts, orders desc
  - Anonymous access
  - Falls back to random active items if no orders exist yet

#### Step 2: Add "Quick Order" section to homepage
- **File:** `src/next-client/src/app/(customer)/page.tsx`
- Above the category grid, add a horizontal scroll section titled "Popular"
- Show 4 items as compact cards with: image/fallback, name, starting price
- Tappable — navigates to item detail or bowl builder
- On mobile: horizontal scroll with snap points
- Only show section if popular items are returned

#### Step 3: Add API method
- **File:** `src/next-client/src/lib/api.ts`
- `getPopularItems(limit?)` method

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/home-page.test.tsx`
```
1. renders "Popular" section when popular items are returned
2. does not render "Popular" section when no popular items
3. popular items are displayed as compact cards
4. popular items section scrolls horizontally on mobile
5. tapping a popular item navigates to correct page
```

#### Integration Tests (Backend) — `src/Server.Tests/PopularItemsEndpointTests.cs`
```
1. GET /api/menu-items/popular returns items ordered by order count
2. respects the limit parameter
3. only returns active menu items
4. returns fallback items when no orders exist
5. does not require authentication
```

---

## F8: Order Sound Notification

**UX Evaluation Ref:** [Enhancement #24](../ux-evaluation.md) - "Add order sound notification"
**Priority:** P8
**Mobile Impact:** High - kitchen staff need audio alerts since they aren't always watching the screen

### Current State
- Admin orders page receives new orders via SignalR (`onOrderCreated`)
- Shows a toast notification for new orders
- No audio alert

### Plan

#### Step 1: Add notification sound
- **File:** `public/sounds/new-order.mp3` (new) — short chime/bell sound
- Use a royalty-free notification sound (< 50KB)

#### Step 2: Play sound on new order
- **File:** `src/next-client/src/app/admin/(dashboard)/orders/page.tsx`
- When `onOrderCreated` fires, play the notification sound via `Audio` API
- Add a mute/unmute toggle button in the orders page header
- Persist mute preference in `localStorage`
- Handle browser autoplay restrictions: show a "Click to enable sound" prompt if needed

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/orders-page.test.tsx`
```
1. plays notification sound when new order is received via SignalR
2. does NOT play sound when muted
3. mute toggle persists state to localStorage
4. mute button renders with correct icon (speaker vs muted)
5. shows "enable sound" prompt if autoplay is blocked
```

---

## F9: Bulk Stock Update

**UX Evaluation Ref:** [Enhancement #23](../ux-evaluation.md) - "Add bulk stock update"
**Priority:** P9
**Mobile Impact:** High - restocking is done in the kitchen/storage, likely on a phone

### Current State
- Ingredients page shows all 34+ ingredients in a table
- Each ingredient must be edited individually via the `IngredientFormSheet`
- No quick-update mechanism for stock quantities

### Plan

#### Step 1: Add "Restock" mode to ingredients page
- **File:** `src/next-client/src/app/admin/(dashboard)/ingredients/page.tsx`
- Add a "Restock" button in the page header
- When active, each ingredient row shows an inline number input for stock quantity
- User can update multiple quantities at once
- "Save All" button at the bottom to submit changes
- "Cancel" to discard changes

#### Step 2: Add backend bulk update endpoint
- **File:** `src/Server/EndPoints/IngredientEndpoints.cs`
- `PATCH /api/ingredients/bulk-stock`
  - Accepts array of `{ ingredientId, newStockQuantity }`
  - Updates all in a single transaction
  - Owner authorization

#### Step 3: Add admin API method
- **File:** `src/next-client/src/lib/admin-api.ts`
- `bulkUpdateStock(token, updates[])` method

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/ingredients-restock.test.tsx`
```
1. "Restock" button toggles restock mode
2. restock mode shows inline quantity inputs for each ingredient
3. changing a quantity marks the row as modified (visual indicator)
4. "Save All" submits only modified ingredients
5. "Cancel" discards changes and exits restock mode
6. shows success toast after successful bulk update
7. restock mode works on mobile layout (card view)
```

#### Integration Tests (Backend) — `src/Server.Tests/BulkStockUpdateTests.cs`
```
1. PATCH /api/ingredients/bulk-stock updates multiple ingredients
2. only updates provided ingredients, leaves others unchanged
3. requires Owner authorization
4. validates that quantities are non-negative
5. returns 400 for invalid ingredient IDs
```

---

## F10: Breadcrumb Navigation on Item Pages

**UX Evaluation Ref:** [Enhancement #20](../ux-evaluation.md) - "Add breadcrumb navigation on item pages"
**Priority:** P10
**Mobile Impact:** Medium - helps orientation but not blocking

### Current State
- Item detail pages have a small "Back to [category]" link
- No full breadcrumb trail (Menu > Category > Item)
- Category page has a "Back to menu" button

### Plan

#### Step 1: Create breadcrumb component
- **File:** `src/next-client/src/components/breadcrumb.tsx` (new)
- Renders: Menu > [Category Name] > [Item Name]
- Each segment is a link except the last (current page)
- Mobile: truncate middle segments if needed, show "..." with full path on tap
- Styled subtle (muted text, small font), below the header

#### Step 2: Add breadcrumbs to item detail and category pages
- **File:** `src/next-client/src/app/(customer)/item/[id]/page.tsx`
  - Breadcrumb: Menu > [Category Name] > [Item Name]
- **File:** `src/next-client/src/app/(customer)/category/[id]/page.tsx`
  - Breadcrumb: Menu > [Category Name]
- Replace existing "Back to..." links with the breadcrumb component

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/breadcrumb.test.tsx`
```
1. renders all breadcrumb segments
2. last segment is not a link (current page)
3. intermediate segments are clickable links
4. "Menu" link points to "/"
5. category segment links to /category/[id]
6. truncates on narrow viewport without breaking layout
```

---

## F11: Revenue / Analytics Dashboard

**UX Evaluation Ref:** [Enhancement #25](../ux-evaluation.md) - "Add revenue/analytics dashboard"
**Priority:** P11
**Mobile Impact:** Medium - owner may check on phone but detailed analytics better on desktop

### Current State
- Dashboard shows "Today's Revenue" as a single number
- `GET /api/orders/today-revenue` sums completed orders for today
- No historical trends, no per-item breakdown, no profit calculation

### Plan

#### Step 1: Add backend analytics endpoints
- **File:** `src/Server/EndPoints/AnalyticsEndpoints.cs` (new)
- `GET /api/analytics/revenue?period=daily|weekly|monthly&from=&to=`
  - Returns revenue data points for charting
- `GET /api/analytics/popular-items?from=&to=&limit=10`
  - Returns top items by order count and revenue
- `GET /api/analytics/summary`
  - Returns: today's revenue, avg order value, total orders today, top item today
- Owner authorization on all

#### Step 2: Create analytics page
- **File:** `src/next-client/src/app/admin/(dashboard)/analytics/page.tsx` (new)
- Summary cards at top: Today's Revenue, Avg Order Value, Orders Today
- Revenue chart (line/bar) with period selector (daily/weekly/monthly)
- Popular items table with rank, name, orders, revenue
- Mobile: cards stack vertically, chart scrolls horizontally
- Use a lightweight chart library (e.g., recharts — already common in Next.js projects)

#### Step 3: Add sidebar navigation link
- Add "Analytics" link to admin sidebar

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/analytics-page.test.tsx`
```
1. renders summary cards (revenue, avg order value, order count)
2. renders revenue chart
3. period selector switches between daily/weekly/monthly
4. popular items table shows ranked list
5. shows loading skeleton while data fetches
6. shows "No data" state when no orders exist
```

#### Integration Tests (Backend) — `src/Server.Tests/AnalyticsEndpointTests.cs`
```
1. GET /api/analytics/revenue returns data points for requested period
2. GET /api/analytics/popular-items returns items ranked by order count
3. GET /api/analytics/summary returns correct calculations
4. all endpoints require Owner authorization
5. date range filtering works correctly
```

---

## F12: "Recently Ordered" / Order Again

**UX Evaluation Ref:** [Enhancement #19](../ux-evaluation.md) - "Add 'Recently Ordered' / 'Order Again'"
**Priority:** P12 (requires customer authentication — not yet implemented)
**Mobile Impact:** High - once auth exists, this is a huge UX win

### Current State
- No customer authentication in the Next.js frontend
- No concept of "my orders" for customers
- Order confirmation page has an "Order Again" button but it just goes to homepage
- Backend has Google OAuth configured but not wired to the customer flow

### Plan

**Prerequisite:** Customer authentication (Google OAuth or guest accounts)

#### Step 1: Add customer order history endpoint
- **File:** `src/Server/EndPoints/OrderEndpoints.cs`
- `GET /api/orders/mine?limit=10`
  - Returns the authenticated customer's recent orders, newest first
  - Requires authentication (JWT)

#### Step 2: Add "Order Again" functionality
- **File:** `src/next-client/src/lib/api.ts`
  - `getMyOrders(limit?)` method
- **File:** `src/next-client/src/app/(customer)/page.tsx`
  - If customer is authenticated, show a "Recent Orders" section above categories
  - Each recent order shows: date, items summary, total, "Order Again" button
  - "Order Again" adds all items from that order back into the cart

#### Step 3: Add reorder logic to cart context
- **File:** `src/next-client/src/store/cart-context.tsx`
- `reorderFromHistory(order: OrderDto)` — maps order items back to cart items
- Must resolve current menu items (prices may have changed) — show notice if price changed

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/recent-orders.test.tsx`
```
1. renders recent orders section when user is authenticated
2. does not render when user is not authenticated
3. shows order date, items, and total for each order
4. "Order Again" button adds items to cart
5. shows price change notice when menu prices differ from order history
6. shows "No previous orders" when history is empty
```

#### Integration Tests (Backend) — `src/Server.Tests/MyOrdersEndpointTests.cs`
```
1. GET /api/orders/mine returns only the authenticated user's orders
2. orders are sorted newest first
3. respects limit parameter
4. requires authentication (401 without token)
5. does not return other users' orders
```

---

## F14: Menu Item Image Upload

**Priority:** P14
**Status:** COMPLETED
**Mobile Impact:** Medium - admin may snap a photo on their phone and upload directly

### Current State
- `MenuItem` entity has `ImageUrl` (string, nullable) and `ImageApproximate` (bool) fields
- Seed data populates `ImageUrl` with static paths like `/menu/burrito-bowl.jpg` — these are placeholder/stock images
- `ImageApproximate` was seeded as `true` for all items (indicating the image is not an actual photo of the dish)
- **No file upload endpoint exists** — there is no `IFormFile`, `multipart/form-data`, or any upload handling in the codebase
- The admin `MenuItemFormSheet` does **not** expose `ImageUrl` or `ImageApproximate` — when editing, it silently passes through the existing `imageUrl` value unchanged; when creating, it sends `imageUrl: null`
- The admin `MenuItemList` grid shows no image thumbnail or any indication of what image (if any) is set
- Customer-facing `ItemCard` renders the image via Next.js `<Image>` if `imageUrl` is set, otherwise shows a letter fallback
- `FixedItemDetail` also displays the image if available

### Plan

#### Step 1: Add image upload endpoint
- **File:** `src/Server/EndPoints/MenuItemEndpoints.cs`
- `POST /api/menu-items/{id}/image`
  - Accepts `multipart/form-data` with a single image file
  - Validates: file size (max 5 MB), allowed types (JPEG, PNG, WebP)
  - Saves file to a local directory on disk (e.g., `wwwroot/uploads/menu/`)
  - Generates a unique filename: `{menuItemId}-{timestamp}.{ext}` (prevents caching issues on update)
  - Deletes the previous image file if one exists
  - Updates `MenuItem.ImageUrl` to `/uploads/menu/{filename}`
  - Owner authorization
  - Returns updated `MenuItemDto`

#### Step 2: Add image delete endpoint
- **File:** `src/Server/EndPoints/MenuItemEndpoints.cs`
- `DELETE /api/menu-items/{id}/image`
  - Deletes the image file from disk
  - Sets `MenuItem.ImageUrl = null`, `MenuItem.ImageApproximate = false`
  - Owner authorization

#### Step 3: Serve uploaded images via static files
- **File:** `src/Server/Program.cs`
- Configure `UseStaticFiles` to serve `wwwroot/uploads/` directory
- Add appropriate cache headers for uploaded images

#### Step 4: Add upload UI to admin menu item form
- **File:** `src/next-client/src/components/admin/menu-item-form-sheet.tsx`
- Add an image section at the top of the form:
  - If image exists: show a preview thumbnail (120x120) with "Change" and "Remove" buttons
  - If no image: show an upload drop zone / file picker with camera icon
  - "Change" opens the file picker; selecting a file shows a **confirmation dialog** with a side-by-side preview (current image vs new image) and "Replace" / "Cancel" buttons — upload only proceeds on confirm
  - "Remove" calls the delete endpoint and clears the preview
  - Show upload progress indicator (spinner/progress bar)
  - On mobile: file picker allows camera capture (`accept="image/*"` triggers camera option on phones)
  - Provide a check box to set `imageApproximate`. When it is true then show the "Not an accurate image" on the item   the customer sees, else do not show it.
#### Step 5: Add image thumbnail to admin menu grid
- **File:** `src/next-client/src/components/admin/menu-item-list.tsx`
- In each item row, show a small thumbnail (32x32) before the item name
  - If `imageUrl` set show the image
  - If `imageUrl` is not set show the image with a subtle "stock" indicator (e.g., small icon overlay)
  - If no `imageUrl`: show the letter fallback (same pattern as customer `ItemCard`)
- Clicking the thumbnail opens the form sheet in edit mode (quick access to image upload)

#### Step 6: Add admin API methods
- **File:** `src/next-client/src/lib/admin-api.ts`
- `uploadMenuItemImage(token, menuItemId, file: File): Promise<MenuItemDto>` — sends `FormData` with the file
- `deleteMenuItemImage(token, menuItemId): Promise<void>`

### Storage Considerations
- **Local disk** (`wwwroot/uploads/`) is simplest for a self-hosted Docker setup — mount as a Docker volume so uploads persist across container rebuilds
- Add `uploads/` volume to `docker-compose.yml`: `./uploads:/app/wwwroot/uploads`
- Future option: swap to S3-compatible storage if the shop outgrows local disk (unlikely for a menu with ~30 items)

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/menu-item-form-sheet.test.tsx`
```
1. renders image upload zone when no image exists
2. renders image preview when imageUrl is set
3. "Change" button opens file picker
4. "Remove" button calls delete endpoint and clears preview
5. selecting a file when no image exists triggers upload immediately
6. selecting a file when image already exists shows replacement confirmation dialog
7. confirmation dialog shows current and new image side by side
8. confirming replacement triggers upload API call
9. cancelling replacement discards the selected file and keeps current image
10. shows loading spinner during upload
11. updates preview after successful upload
12. shows error toast on upload failure
13. file picker accepts image/* (allows camera on mobile)
```

#### Unit Tests — `src/next-client/src/__tests__/components/menu-item-list.test.tsx`
```
1. renders image thumbnail for items with imageUrl
2. renders letter fallback for items without imageUrl
3. shows "stock" indicator for items where imageApproximate is true
4. clicking thumbnail opens edit form sheet
```

#### Integration Tests (Backend) — `tests/MannaHp.Server.Tests/Endpoints/MenuItemImageTests.cs`
```
1. POST /api/menu-items/{id}/image uploads file and updates imageUrl
2. upload rejects files over 5 MB (400)
3. upload rejects non-image file types (400)
4. upload deletes previous image file when replacing
5. sets imageApproximate to false after upload
6. DELETE /api/menu-items/{id}/image removes file and clears imageUrl
7. both endpoints require Owner authorization
8. upload returns 404 for non-existent menu item
```

### Implementation Notes

**Backend changes:**
- `src/Server/EndPoints/MenuItemEndpoints.cs`: Added `POST /{id}/image` (upload) and `DELETE /{id}/image` (delete) endpoints. Upload validates file size (max 5 MB), content type (JPEG/PNG/WebP), saves to `wwwroot/uploads/menu/{menuItemId}-{timestamp}.{ext}`, deletes previous file on replace. Both require Owner authorization.
- `src/Server/Program.cs`: Added `app.UseStaticFiles()` to serve uploaded images from `wwwroot/uploads/`.
- `docker-compose.yml`: Added `uploads` Docker volume mounted at `/app/wwwroot/uploads` for persistence across container rebuilds.

**Frontend changes:**
- `src/next-client/src/lib/api.ts`: Added `resolveImageUrl()` helper. Only prepends `NEXT_PUBLIC_API_URL` for `/uploads/...` paths (API-served uploaded images). Other relative paths (e.g. `/menu/...` seed images) are left as-is so they resolve against the Next.js origin. Used by all components that display menu item images.
- `src/next-client/src/lib/admin-api.ts`: Added `uploadMenuItemImage()` (multipart/form-data POST) and `deleteMenuItemImage()` methods.
- `src/next-client/src/components/admin/menu-item-form-sheet.tsx`: Added image section at top of edit form — upload zone when no image, preview with Change/Remove buttons when image exists, "Not an accurate image" checkbox for `imageApproximate`.
- `src/next-client/src/components/admin/menu-item-list.tsx`: Added 32x32 image thumbnails before item names in the grid, with letter fallback when no image.
- `src/next-client/src/components/item-card.tsx` and `fixed-item-detail.tsx`: Switched from Next.js `Image` to plain `<img>` with `resolveImageUrl()`. Next.js `Image` requires server-side optimization proxy which can't reliably reach the API server for uploaded images. Plain `<img>` works for both seed images (relative paths) and uploaded images (API URLs).

**Test infrastructure:**
- `src/next-client/src/__tests__/setup.ts`: Added `ResizeObserver` polyfill for Radix UI Sheet components in jsdom.
- Installed `@testing-library/user-event` as dev dependency.

---

## F13: Inventory Check-In / Receiving ✅

**Status:** Complete
**Priority:** P13
**Mobile Impact:** High - restocking happens in the kitchen/storage, likely on a phone

### Implementation Notes

**Backend:**
- `InventoryLog` entity with fields: Id, IngredientId (FK), ChangeType (Received/OrderDecrement/Adjustment), QuantityChange, NewStockQuantity (snapshot), Notes, CreatedBy, CreatedAt
- `InventoryChangeType` enum in `Shared/Enums`
- `POST /api/ingredients/{id}/restock` — additive restock (not overwrite), creates log entry, Owner auth
- `POST /api/ingredients/bulk-restock` — batch restock with per-item notes, Owner auth
- `GET /api/ingredients/{id}/history` — returns last 100 log entries, newest first, Owner auth
- Order decrements now create `InventoryLog` entries with `ChangeType = OrderDecrement` and note `"Order #{number}"`
- DTOs: `RestockRequest`, `BulkRestockRequest`, `BulkRestockItem`, `InventoryLogDto`

**Frontend:**
- Restock page (`/admin/ingredients/restock`) — lists all active ingredients with current stock, quantity input per item, delivery notes, "Submit Delivery" button
- Ingredient history page (`/admin/ingredients/[id]/history`) — chronological log with change type badges (Received/Order/Adjustment), green/red quantity indicators, stock snapshots
- "Check In Delivery" button added to ingredients page header
- "View History" link added to mobile ingredient detail sheet
- Admin API methods: `restockIngredient`, `bulkRestock`, `getInventoryHistory`, `getIngredient`

### Files Created
- `src/Shared/Entities/InventoryLog.cs`
- `src/Shared/Enums/InventoryChangeType.cs`
- `src/Shared/DTOs/InventoryLogDto.cs`
- `src/next-client/src/app/admin/(dashboard)/ingredients/restock/page.tsx`
- `src/next-client/src/app/admin/(dashboard)/ingredients/[id]/history/page.tsx`
- `src/next-client/src/__tests__/components/restock-page.test.tsx` — 7 tests
- `src/next-client/src/__tests__/components/ingredient-history.test.tsx` — 5 tests
- `tests/MannaHp.Server.Tests/Endpoints/RestockEndpointTests.cs` — 7 tests

### Files Modified
- `src/Server/Data/MannaDbContext.cs` — InventoryLog DbSet + model config
- `src/Server/EndPoints/IngredientEndpoints.cs` — restock, bulk-restock, history endpoints
- `src/Server/EndPoints/OrderEndpoints.cs` — log order decrements
- `src/next-client/src/types/api.ts` — InventoryChangeType enum, InventoryLogDto, request types
- `src/next-client/src/lib/admin-api.ts` — 4 new API methods
- `src/next-client/src/app/admin/(dashboard)/ingredients/page.tsx` — "Check In Delivery" button, "View History" link

### Relationship to F9 (Bulk Stock Update)
F13 supersedes F9's overwrite-based restock. The additive receiving flow with audit trail is superior. F9 can be considered resolved by F13.

---

## Implementation Notes

### Mobile-First Approach
All features should be designed mobile-first since customers primarily order on phones:
- Touch targets minimum 44x44px
- No hover-only interactions
- Horizontal scrolling sections use snap points
- Forms use appropriate input types (`inputmode="numeric"` for quantities)
- Test on 375px viewport minimum

### Testing Strategy
- **Write failing tests first** (TDD) before implementing each feature
- Unit tests use Jest + React Testing Library (existing setup)
- Mock API calls and SignalR connections in tests
- Backend integration tests use the existing xUnit + WebApplicationFactory pattern
- All tests should pass on CI before merging

### Shared Patterns
- SignalR connections: follow the pattern in `src/lib/order-hub.ts` with fallback polling
- API methods: follow `src/lib/api.ts` fetch pattern with error handling
- Admin API methods: follow `src/lib/admin-api.ts` with token-based auth
- UI components: use shadcn/ui primitives (Sheet, Card, Button, Input, etc.)
- Toasts: use Sonner via `toast()` for success/error feedback

### Dependencies Between Features
- **F12** (Order Again) depends on customer authentication (not yet built)
- **F7** (Popular Items) backend can be built independently; pairs well with **F11** (Analytics)
- **F1** (Real-time Tracking) and **F8** (Sound Notifications) both extend SignalR — can share implementation effort
- **F4** (Mobile Ingredients) and **F9** (Bulk Restock) both modify the ingredients page — implement together
- **F13** (Inventory Check-In) supersedes parts of **F9** (Bulk Stock Update) — F13 adds additive receiving + audit trail vs F9's overwrite approach. Consider merging F9's UI into F13

---

## G4: QR Code In-Store Token Flow

**Source:** [undocumented-gaps.md](undocumented-gaps.md) (G4), [CLAUDE.md](../../CLAUDE.md) (QR Code In-Store Ordering section)
**Priority:** Tier 1 — Needed Before Launch
**Dependencies:** None (standalone feature)
**Status:** ✅ Complete

### Overview

Staff generate a single long-lived token (configurable in days), displayed as a QR code on a tablet or printout at the counter. Customers scan it, browse the menu normally, and when they choose "Pay at Counter" at checkout the app validates their token. If valid, the order goes through as `PaymentMethod.InStore`. If they don't have a token (or it's expired), they see a custom message written by staff in the admin settings.

Only one active token exists at a time. When it expires, staff generates a new one and reprints/redisplays it.

### Token Lifecycle

```
Staff generates token (configurable TTL in days)
  -> QR code displayed on admin page (one at a time)
  -> Customer scans QR code, lands on menu
  -> Token stored in localStorage
  -> Customer browses menu, adds items to cart
  -> At checkout, selects "Pay at Counter"
  -> Frontend validates token via API
  -> If valid: token sent as X-Store-Token header on order placement
  -> If invalid/missing: show staff-configured message, block order
  -> Token expires after N days -> staff generates a new one
```

### New Database Entity

```csharp
// src/Shared/Entities/StoreToken.cs
public class StoreToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }           // Short GUID
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserId { get; set; }  // Staff user who generated it
    public bool Revoked { get; set; }            // Manual revocation
}
```

### New/Modified Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/store-tokens` | Staff | Generate a new token (revokes any existing active token) |
| `GET` | `/api/store-tokens/current` | Staff | Get the current active token (if any) |
| `DELETE` | `/api/store-tokens/{id}` | Staff | Revoke a token |
| `GET` | `/api/store-tokens/{token}/validate` | Anonymous | Validate a token (used by frontend at checkout) |

### AppSettings Keys

| Key | Default | Description |
|-----|---------|-------------|
| `StoreTokenDurationDays` | `7` | Default token TTL in days (staff can override per token) |
| `StoreTokenRequiredMessage` | `"Please scan the QR code at our counter to place an in-store order."` | Message shown to customers without a valid token who try to pay at counter |

### Custom Authorization Handler

Modify order placement (`POST /api/orders`) to use a `CanOrder` policy that accepts **either**:
- A valid JWT (authenticated user), **or**
- A valid `X-Store-Token` header with a non-expired, non-revoked token

### Implementation Plan

#### Step 1: Entity + Migration
- **New file:** `src/Shared/Entities/StoreToken.cs`
- **Edit:** `src/Server/Data/MannaDbContext.cs` — add `DbSet<StoreToken>`
- **New migration:** `AddStoreTokens`
- Unique index on `Token` column

#### Step 2: Store Token Endpoints
- **New file:** `src/Server/EndPoints/StoreTokenEndpoints.cs`
- `POST /api/store-tokens` — accepts `{ durationDays: int }` (optional, falls back to `StoreTokenDurationDays` setting). **Revokes any existing active token** before creating the new one (enforces single active token).
- `GET /api/store-tokens/current` — returns the single active (non-expired, non-revoked) token, or 404
- `DELETE /api/store-tokens/{id}` — sets `Revoked = true`
- `GET /api/store-tokens/{token}/validate` — anonymous, returns `{ valid: true, expiresAt }` or `{ valid: false }`
- All mutating endpoints require `Staff` policy

#### Step 3: Custom Authorization — CanOrder Policy
- **New file:** `src/Server/Auth/CanOrderAuthorizationHandler.cs`
- Implements `IAuthorizationHandler` for a `CanOrderRequirement`
- Succeeds if:
  - User is authenticated via JWT, **or**
  - Request has `X-Store-Token` header with a valid (non-expired, non-revoked) token in the DB
- **Edit:** `src/Server/Program.cs` — register the handler and add `"CanOrder"` policy
- **Edit:** `src/Server/EndPoints/OrderEndpoints.cs` — add `.RequireAuthorization("CanOrder")` to `POST /api/orders`

#### Step 4: Token Cleanup Background Job
- Hangfire recurring job (or `IHostedService`) to delete tokens expired more than 7 days ago
- Keeps table clean without affecting active or recently-expired tokens

#### Step 5: Admin QR Code Page (Frontend)
- **New file:** `src/next-client/src/app/admin/(dashboard)/qr-code/page.tsx`
- **Edit:** `src/next-client/src/components/admin/sidebar.tsx` — add nav item with `QrCode` icon from lucide-react
- Page layout:
  - **Current QR code section:** If an active token exists, display:
    - Large scannable QR code (using `qrcode.react`) encoding `{PUBLIC_BASE_URL}?token={token}`
    - Expiry date and days remaining
    - Revoke button
  - **Generate section:** If no active token (or to replace current):
    - Duration picker (number input in days, default from settings)
    - "Generate New QR Code" button — calls `POST /api/store-tokens`, which auto-revokes the old one
  - **Message configuration:** Text area to edit `StoreTokenRequiredMessage` (calls existing settings endpoint)
  - **Full-screen mode:** Button to display just the QR code full-screen for tablet counter display
- **No active tokens list** — only one token at a time, so the page just shows the current one or an empty state

#### Step 6: Customer Frontend — Token Handling
- **Edit:** `src/next-client/src/lib/api.ts`
  - On app load, check URL for `?token=` query param. If present, store in `localStorage` key `storeToken`.
  - Attach `X-Store-Token` header from `localStorage` on API calls when present.
- **Edit:** `src/next-client/src/app/(customer)/checkout/page.tsx`
  - When customer selects "Pay at Counter":
    - Check `localStorage` for `storeToken`
    - If found, call `GET /api/store-tokens/{token}/validate`
    - If valid: proceed with order, send `X-Store-Token` header
    - If invalid/expired: clear token from `localStorage`, show the staff-configured `StoreTokenRequiredMessage` (fetched from `GET /api/settings`)
    - If no token at all: show the same `StoreTokenRequiredMessage`
  - Card payment flow remains unchanged (no token needed)

### Tests (Write Before Implementation)

#### Backend — `tests/MannaHp.Server.Tests/StoreTokens/StoreTokenEndpointTests.cs`
```
1. POST /api/store-tokens — creates token with correct expiry based on durationDays
2. POST /api/store-tokens — uses default duration from AppSettings when durationDays not provided
3. POST /api/store-tokens — revokes existing active token when generating new one
4. POST /api/store-tokens — returns 401 for unauthenticated request
5. POST /api/store-tokens — returns 403 for non-staff user
6. GET /api/store-tokens/current — returns active token
7. GET /api/store-tokens/current — returns 404 when no active token exists
8. GET /api/store-tokens/current — does not return expired tokens
9. GET /api/store-tokens/current — does not return revoked tokens
10. DELETE /api/store-tokens/{id} — sets Revoked = true
11. DELETE /api/store-tokens/{id} — returns 404 for nonexistent token
12. GET /api/store-tokens/{token}/validate — returns valid:true for active token
13. GET /api/store-tokens/{token}/validate — returns valid:false for expired token
14. GET /api/store-tokens/{token}/validate — returns valid:false for revoked token
15. GET /api/store-tokens/{token}/validate — returns valid:false for nonexistent token
```

#### Backend — `tests/MannaHp.Server.Tests/StoreTokens/CanOrderAuthorizationTests.cs`
```
1. Authenticated JWT user can place order (handler succeeds)
2. Valid X-Store-Token header allows order placement (handler succeeds)
3. Expired X-Store-Token is rejected (handler fails)
4. Revoked X-Store-Token is rejected (handler fails)
5. Missing both JWT and X-Store-Token is rejected (handler fails)
6. Invalid/garbage X-Store-Token is rejected (handler fails)
7. Order placed with valid token has PaymentMethod = InStore
```

#### Backend — `tests/MannaHp.Server.Tests/StoreTokens/StoreTokenCleanupTests.cs`
```
1. Cleanup job deletes tokens expired more than 7 days ago
2. Cleanup job does NOT delete active tokens
3. Cleanup job does NOT delete recently-expired tokens (< 7 days)
```

#### Frontend — `src/next-client/src/__tests__/components/admin/qr-code-page.test.tsx`
```
1. Renders generate button when no active token exists
2. Displays QR code and expiry info when active token exists
3. Clicking generate calls POST /api/store-tokens and displays new QR code
4. QR code encodes correct URL with token
5. Displays days remaining until expiry
6. Clicking revoke calls DELETE and shows empty state
7. Duration picker defaults to value from settings
8. Staff message text area saves via settings endpoint
9. Full-screen button shows QR code only
```

#### Frontend — `src/next-client/src/__tests__/hooks/store-token-checkout.test.tsx`
```
1. Checkout allows "Pay at Counter" when valid token in localStorage
2. Checkout blocks "Pay at Counter" with staff message when no token
3. Checkout blocks "Pay at Counter" with staff message when token is expired
4. Expired token is cleared from localStorage after failed validation
5. Card payment works regardless of token presence
```

#### Frontend — `src/next-client/src/__tests__/lib/api-store-token.test.ts`
```
1. Detects ?token= query param and stores in localStorage
2. API client attaches X-Store-Token header when token in localStorage
3. API client does NOT attach header when no token in localStorage
```

### Manual Testing Checklist

#### Prerequisites
- App running via `docker compose up`
- Staff account logged into admin panel
- A phone or second browser for simulating customer
- Set `PublicBaseUrl` in app settings to the accessible URL

#### Generate & Display
- [ ] Navigate to `/admin/qr-code` — page loads with empty state / generate button
- [ ] Set duration to 1 day, click "Generate New QR Code" — QR code appears
- [ ] QR code shows expiry date and days remaining
- [ ] QR code is scannable with phone camera — opens correct URL with `?token=...`
- [ ] Full-screen mode shows just the QR code (suitable for tablet display)

#### Customer Scan Flow (Happy Path)
- [ ] Scan QR code on phone — lands on menu page (no gate, no redirect)
- [ ] Browse menu, add items to cart normally
- [ ] Go to checkout, select "Pay at Counter" — order submits successfully
- [ ] Order appears on admin kitchen display with `PaymentMethod: InStore`
- [ ] Close browser tab, reopen menu — token persists (localStorage)
- [ ] Can place another order without rescanning

#### No Token / Invalid Token
- [ ] Open the app without scanning a QR code (no `?token=` in URL)
- [ ] Add items to cart, go to checkout, select "Pay at Counter"
- [ ] See the staff-configured message (e.g., "Please scan the QR code at our counter")
- [ ] Order is NOT placed
- [ ] Card payment still works normally

#### Token Expiration
- [ ] Generate token with 1-day duration
- [ ] Manually set token expiry to the past in the DB (for testing)
- [ ] Try to "Pay at Counter" with the expired token — see staff message
- [ ] Expired token is cleared from localStorage

#### Token Revocation
- [ ] Generate a token, scan it on phone
- [ ] Revoke the token from admin QR code page
- [ ] Try to "Pay at Counter" on phone — see staff message
- [ ] Admin page shows empty state after revocation

#### Token Replacement
- [ ] Generate token A, scan it on phone
- [ ] Generate token B from admin — token A is auto-revoked
- [ ] Phone still has token A in localStorage — "Pay at Counter" fails with message
- [ ] Scan new QR code (token B) on phone — works

#### Security
- [ ] Fabricate an `X-Store-Token` header with garbage value — order rejected
- [ ] Access `POST /api/store-tokens` without staff auth — 401/403
- [ ] Existing card payment flow still works (no regression)
- [ ] Admin submit-order page still works (no regression)

#### Staff Message
- [ ] Edit the "no token" message from admin QR code page
- [ ] Trigger the no-token error on customer side — updated message appears

### Dependencies & Packages

| Package | Where | Purpose |
|---------|-------|---------|
| `qrcode.react` | Next.js frontend | QR code rendering component |
| — | .NET backend | No new packages needed |

### Files Changed (Summary)

#### New Files
| File | Description |
|------|-------------|
| `src/Shared/Entities/StoreToken.cs` | Entity |
| `src/Server/EndPoints/StoreTokenEndpoints.cs` | CRUD + validate endpoints |
| `src/Server/Auth/CanOrderAuthorizationHandler.cs` | Custom auth handler |
| `src/next-client/src/app/admin/(dashboard)/qr-code/page.tsx` | Admin QR code page |
| `tests/MannaHp.Server.Tests/StoreTokens/StoreTokenEndpointTests.cs` | Backend endpoint tests |
| `tests/MannaHp.Server.Tests/StoreTokens/CanOrderAuthorizationTests.cs` | Auth handler tests |
| `src/next-client/src/__tests__/components/admin/qr-code-page.test.tsx` | Frontend admin tests |
| `src/next-client/src/__tests__/hooks/store-token-checkout.test.tsx` | Frontend checkout tests |

#### Modified Files
| File | Change |
|------|--------|
| `src/Server/Data/MannaDbContext.cs` | Add `DbSet<StoreToken>` |
| `src/Server/Program.cs` | Register auth handler, add CanOrder policy |
| `src/Server/EndPoints/OrderEndpoints.cs` | Add `.RequireAuthorization("CanOrder")` to POST |
| `src/next-client/src/components/admin/sidebar.tsx` | Add QR Code nav item |
| `src/next-client/src/lib/api.ts` | Detect `?token=` param, store in localStorage, attach header |
| `src/next-client/src/app/(customer)/checkout/page.tsx` | Validate token on "Pay at Counter", show staff message on failure |

### Open Questions

1. **Base URL configuration:** QR code needs the full public URL. Should come from `AppSettings` key `PublicBaseUrl` or an environment variable.
2. **Token format:** Simple GUID (`Guid.NewGuid().ToString("N")`) or shorter nanoid-style string for friendlier URLs? GUIDs are fine since customers scan, not type.

### Implementation Notes (Completed 2026-03-07)

#### Architecture Decision: Dual Authentication Scheme
The original plan called for a custom `IAuthorizationHandler` (`CanOrderAuthorizationHandler`). This approach **failed** because ASP.NET Core's JWT auth middleware returns 401 before the custom authorization handler ever runs for unauthenticated requests. The solution was to implement a proper `StoreTokenAuthenticationHandler` as a secondary **authentication scheme** alongside JWT, then configure the `CanOrder` policy to accept either scheme:

```csharp
// Program.cs — authentication config
.AddScheme<AuthenticationSchemeOptions, StoreTokenAuthenticationHandler>("StoreToken", null);

// CanOrder policy accepts either JWT or StoreToken scheme
options.AddPolicy("CanOrder", policy => {
    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "StoreToken");
    policy.RequireAuthenticatedUser();
});
```

The handler lives at `src/Server/Auth/StoreTokenAuthenticationHandler.cs` and reads the `X-Store-Token` header, validates it against the DB.

#### Test Isolation
Adding `RequireAuthorization("CanOrder")` to `POST /api/orders` broke all existing order tests (57 failures). Fixed by adding `CreateStoreTokenClient()` to `MannaApiFactory` — but using a **staff JWT** rather than an actual store token, since store token endpoint tests auto-revoke all active tokens, which would interfere with other test classes.

#### Not Implemented
- **Step 4 (Token Cleanup Job):** Hangfire/IHostedService to clean expired tokens >7 days old — deferred. Table stays small with manual revocation.
- **Frontend tests:** `qr-code-page.test.tsx`, `store-token-checkout.test.tsx`, `api-store-token.test.ts` — deferred.
- **Staff message editor on QR page:** The plan mentions a text area for editing `StoreTokenRequiredMessage` directly on the QR page. Currently, this is configured via the Settings page instead.

#### Key Files Created
- `src/Shared/Entities/StoreToken.cs` — entity
- `src/Shared/DTOs/StoreTokenDto.cs` — request/response DTOs
- `src/Server/Auth/StoreTokenAuthenticationHandler.cs` — custom auth handler
- `src/Server/EndPoints/StoreTokenEndpoints.cs` — CRUD + validate endpoints
- `src/next-client/src/app/admin/(dashboard)/qr-code/page.tsx` — admin QR management
- `tests/MannaHp.Server.Tests/Endpoints/StoreTokenEndpointTests.cs` — 14 tests
- `tests/MannaHp.Server.Tests/Endpoints/CanOrderAuthorizationTests.cs` — 7 tests

#### Resolved Open Questions
1. **Base URL:** Uses `NEXT_PUBLIC_BASE_URL` env var (defaults to `http://localhost:3000`)
2. **Token format:** `Guid.NewGuid().ToString("N")` — customers scan, never type

---

## F14: Check-In Delivery Redesign

**Status:** ✅ Complete
**Priority:** High — replaces current restock page with a more intuitive delivery workflow

### Overview

Redesign the Check-In Delivery page from a static grid of all ingredients to a search-driven flow where staff search for ingredients, enter quantities with an optional calculator, enter cost paid, and build a delivery list before submitting.

### UI Flow

1. **Search bar** at the top with autocomplete dropdown showing matching ingredients
   - If no match → show "Add new ingredient: [typed name]" option at bottom of dropdown

2. **When user selects an ingredient** → opens an "Add to delivery" card/modal:
   - Shows ingredient name, current stock, and unit
   - **Quantity input** — user enters amount in the ingredient's native unit
   - **"Calculator" toggle button** — expands a helper section:
     - **Count mode:** `___ items × ___ [unit] each = total`
     - **Convert mode:** enter value in a different unit, auto-converts to native unit
     - Supported conversions: oz↔lb, fl oz↔cups, tsp↔tbsp, kg↔lb, liters↔fl oz
   - **Cost paid** — user enters total dollar amount paid for this item
   - **Auto-calculated cost per unit** shown below (cost ÷ quantity)
   - "Add to Delivery" button → adds to the list

3. **When user clicks "Add new ingredient"** → inline form:
   - Name (pre-filled from search text)
   - Unit (dropdown)
   - Low stock threshold (default 0)
   - Active (default true, hidden)
   - Then same quantity + calculator + cost fields as above

4. **Delivery list** below the search:
   - Shows each item: name, quantity (with unit), cost paid, calculated cost/unit
   - Edit button → re-opens the add card with values pre-filled
   - Remove button (X)
   - **"Submit Delivery"** button at bottom

5. **On submit:**
   - For new ingredients: creates the ingredient first
   - For all items: calls bulk restock endpoint
   - Updates cost per unit using **weighted average** with existing stock:
     `newCostPerUnit = ((existingStock × oldCost) + (newQty × newCostPerUnit)) / (existingStock + newQty)`
   - Resets page to empty + shows success toast

### Backend Changes

- **Bulk restock endpoint** — accept `costPaid` per item and update `CostPerUnit` via weighted average
- **`BulkRestockItem` DTO** — add `CostPaid` field, remove `Notes`
- **New ingredient creation** — can reuse existing create endpoint or add to the bulk restock flow

### Frontend Changes

- Rewrite `restock/page.tsx` entirely with the new flow
- Add a `QuantityCalculator` component (count mode + conversion mode)
- Add inline "new ingredient" form
- Update tests

### Test Plan

#### Frontend Unit Tests (vitest + testing-library)

**Search & Autocomplete**
1. Renders search input on page load
2. Shows matching ingredients in dropdown as user types
3. Shows "Add new ingredient: [text]" when no matches found
4. Clears search and opens add-to-delivery card when ingredient selected

**Quantity Calculator**
5. Calculator hidden by default, visible after toggle
6. Count mode: calculates total from count × size (e.g., 3 × 16oz = 48oz)
7. Convert mode: converts between units correctly (lb→oz, kg→lb, liters→fl oz, cups→fl oz, tbsp→tsp)
8. Calculated total populates the quantity field

**Add to Delivery Card**
9. Shows ingredient name, current stock, and unit
10. Shows auto-calculated cost per unit (cost ÷ quantity)
11. "Add to Delivery" button disabled when quantity is 0 or cost is empty
12. Adds item to delivery list on confirm

**New Ingredient Inline Form**
13. Shows name pre-filled from search text
14. Shows unit dropdown and threshold input
15. Requires unit selection before allowing add

**Delivery List**
16. Displays all added items with name, quantity, unit, cost paid, cost/unit
17. Edit button re-opens card with pre-filled values
18. Remove button removes item from list
19. "Submit Delivery" button disabled when list is empty

**Submission**
20. Calls bulk restock API on submit
21. Creates new ingredients before restocking
22. Shows success toast and resets page on success
23. Shows error toast on failure

#### Backend Integration Tests (xUnit)

**Weighted Average Cost Update**
24. Restock updates cost per unit via weighted average: `((oldStock × oldCost) + (newQty × newCostPerUnit)) / (oldStock + newQty)`
25. Restock with zero existing stock sets cost per unit to new cost per unit
26. Restock with zero cost paid leaves existing cost per unit unchanged

**Bulk Restock with Cost**
27. Bulk restock accepts `costPaid` per item and updates ingredient cost
28. Bulk restock creates inventory log entries for each item
29. Bulk restock increments stock quantities additively

**New Ingredient via Delivery**
30. Creating ingredient + restocking in one flow sets correct initial stock and cost

#### Unit Conversion Tests (vitest)
31. oz ↔ lb (16 oz = 1 lb)
32. fl oz ↔ cups (8 fl oz = 1 cup)
33. tsp ↔ tbsp (3 tsp = 1 tbsp)
34. kg ↔ lb (1 kg = 2.20462 lb)
35. liters ↔ fl oz (1 liter = 33.814 fl oz)
36. Returns null/error for incompatible conversions (e.g., oz → cups)

### Files to Change

| File | Change |
|------|--------|
| `src/Shared/DTOs/InventoryLogDto.cs` | Add `CostPaid` to `BulkRestockItem`, remove `Notes` |
| `src/Server/EndPoints/IngredientEndpoints.cs` | Update bulk-restock to handle weighted avg cost update |
| `src/next-client/src/app/admin/(dashboard)/ingredients/restock/page.tsx` | Full rewrite with new flow |
| `src/next-client/src/lib/unit-conversions.ts` | New — conversion logic |
| `src/next-client/src/components/admin/quantity-calculator.tsx` | New — calculator component |
| `src/next-client/src/lib/admin-api.ts` | Update restock types |
| `src/next-client/src/types/api.ts` | Update DTOs |
| `src/next-client/src/__tests__/components/restock-page.test.tsx` | Rewrite tests |
| `src/next-client/src/__tests__/lib/unit-conversions.test.ts` | New — conversion tests |
| `tests/MannaHp.Server.Tests/Endpoints/RestockEndpointTests.cs` | Add weighted avg cost tests |

### Implementation Notes

**Backend changes:**
- `src/Shared/DTOs/InventoryLogDto.cs`: Changed `BulkRestockItem` — replaced `Notes` field with `CostPaid` (decimal). Notes are now auto-generated from delivery context.
- `src/Server/EndPoints/IngredientEndpoints.cs`: Updated bulk-restock endpoint to compute weighted average cost: `newCostPerUnit = ((existingStock * oldCost) + (newQty * newCostPerUnit)) / (existingStock + newQty)`. Zero cost paid leaves existing cost unchanged. Zero existing stock sets cost to the new cost per unit. Auto-generates inventory log notes with quantity and cost info.

**Frontend changes:**
- `src/next-client/src/lib/unit-conversions.ts`: New — conversion library supporting oz/lb, fl oz/cups, tsp/tbsp with `convert()` and `getConvertibleUnits()` functions.
- `src/next-client/src/components/admin/quantity-calculator.tsx`: New — calculator component with count mode (items x size) and convert mode (unit conversion). Toggled via "Calculator" button.
- `src/next-client/src/app/admin/(dashboard)/ingredients/restock/page.tsx`: Full rewrite. Search-driven delivery flow: autocomplete search -> add-to-delivery card (quantity + calculator + cost) -> delivery list with edit/remove -> submit. Supports creating new ingredients inline. Submit creates new ingredients first, then calls bulk restock.
- `src/next-client/src/types/api.ts`: Updated `BulkRestockItem` — `notes` replaced with `costPaid`.

**Tests:**
- 12 unit conversion tests (all pass)
- 17 restock page component tests (all pass) — search/autocomplete, add-to-delivery card, new ingredient form, delivery list, submission
- 4 new backend integration tests for weighted average cost (all pass)

---

## G12: API Rate Limiting

**Source:** [undocumented-gaps.md](undocumented-gaps.md) (G12)
**Priority:** Tier 3 — Nice to Have (but important for security hardening)
**Dependencies:** None (standalone feature)

### Overview

All API endpoints are currently unprotected against abuse. Someone could spam order creation, brute-force staff login, or flood public endpoints like menu fetching. This feature adds rate limiting using ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware — no new packages needed.

### Threat Model

| Attack Vector | Endpoint | Risk | Impact |
|---------------|----------|------|--------|
| Order spam | `POST /api/orders` | Fake orders flood kitchen display | High — operational disruption |
| Login brute-force | `POST /api/auth/login` | Credential guessing | High — account compromise |
| Registration abuse | `POST /api/auth/register` | Account spam (if registration is opened to customers later) | Medium |
| Stripe webhook flooding | `POST /api/stripe/webhook` | Unnecessary processing (Stripe signs webhooks, so no data risk) | Low |
| Menu/settings scraping | `GET /api/menu-items`, `GET /api/categories`, `GET /api/settings/public` | Resource exhaustion | Low |
| Image upload abuse | `POST /api/menu-items/{id}/image` | Disk space exhaustion | Medium (already auth-gated) |

### Rate Limiting Strategy

Three tiers of rate limits, applied by endpoint sensitivity:

| Policy | Window | Limit | Applied To |
|--------|--------|-------|------------|
| `strict` | 1 minute | 5 requests per IP | Order creation, login, registration |
| `moderate` | 1 minute | 30 requests per IP | Authenticated admin write endpoints (CRUD operations) |
| `relaxed` | 1 minute | 60 requests per IP | Public read endpoints (menu, categories, settings) |

All rate-limited responses return `429 Too Many Requests` with a `Retry-After` header.

### Implementation Plan

#### Step 1: Configure Rate Limiting Services
- **Edit:** `src/Server/Program.cs`
- Add `builder.Services.AddRateLimiter(...)` with three named fixed-window policies:
  - `"strict"` — 5 requests/minute per IP
  - `"moderate"` — 30 requests/minute per IP
  - `"relaxed"` — 60 requests/minute per IP
- Configure the global `OnRejected` handler to return a JSON problem details response with `429` status and `Retry-After` header
- Add `app.UseRateLimiter()` after `app.UseCors()` and before `app.UseAuthentication()`
- Rate limits are per-IP using `HttpContext.Connection.RemoteIpAddress` as the partition key
- Respect `X-Forwarded-For` header when behind the Caddy reverse proxy (use `ForwardedHeadersOptions`)

#### Step 2: Apply Policies to Endpoints
- **Edit:** `src/Server/EndPoints/OrderEndpoints.cs`
  - `POST /api/orders` — `.RequireRateLimiting("strict")`
  - `POST /{id}/confirm-payment` — `.RequireRateLimiting("strict")`
- **Edit:** `src/Server/EndPoints/AuthEndpoints.cs`
  - `POST /api/auth/login` — `.RequireRateLimiting("strict")`
  - `POST /api/auth/register` — `.RequireRateLimiting("strict")`
- **Edit:** `src/Server/EndPoints/MenuItemEndpoints.cs`
  - `GET /api/menu-items` — `.RequireRateLimiting("relaxed")`
  - `GET /api/menu-items/{id}` — `.RequireRateLimiting("relaxed")`
  - `POST`, `PUT`, `DELETE` endpoints — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/CategoryEndpoints.cs`
  - `GET` endpoints — `.RequireRateLimiting("relaxed")`
  - `POST`, `PUT`, `DELETE` endpoints — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/IngredientEndpoints.cs`
  - All endpoints — `.RequireRateLimiting("moderate")` (all require auth)
- **Edit:** `src/Server/EndPoints/SettingsEndpoints.cs`
  - `GET /api/settings/public` — `.RequireRateLimiting("relaxed")`
  - `GET /api/settings` and `PUT /api/settings` — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/VariantEndpoints.cs` — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/AvailableIngredientEndpoints.cs` — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/RecipeIngredientEndpoints.cs` — `.RequireRateLimiting("moderate")`
- **Edit:** `src/Server/EndPoints/StripeWebhookEndpoints.cs` — `.RequireRateLimiting("moderate")`
  - Stripe webhooks are signature-verified so rate limiting is a secondary defense
- `GET /api/orders/{id}` (public, used by customers to check their order) — `.RequireRateLimiting("relaxed")`
- `GET /api/orders/active`, `PATCH /api/orders/{id}/status`, `GET /api/orders/today-revenue` — `.RequireRateLimiting("moderate")` (already auth-gated)

#### Step 3: Configure Forwarded Headers
- **Edit:** `src/Server/Program.cs`
- Add `builder.Services.Configure<ForwardedHeadersOptions>(...)` to trust `X-Forwarded-For` from Caddy
- Add `app.UseForwardedHeaders()` before `app.UseRateLimiter()`
- Without this, all requests behind the reverse proxy share the same IP (Caddy's internal IP) and would be rate-limited as a single client

#### Step 4: Make Limits Configurable (Optional)
- Read rate limit values from `appsettings.json` so they can be tuned per deployment:
```json
{
  "RateLimiting": {
    "Strict": { "Window": 60, "PermitLimit": 5 },
    "Moderate": { "Window": 60, "PermitLimit": 30 },
    "Relaxed": { "Window": 60, "PermitLimit": 60 }
  }
}
```
- Fall back to hardcoded defaults if config section is missing

### Frontend Handling

- **Edit:** `src/next-client/src/lib/api.ts`
  - Check for `429` status in the shared fetch wrapper
  - Parse `Retry-After` header (seconds)
  - Show a toast: "Too many requests. Please wait X seconds and try again."
  - Do NOT auto-retry — let the user trigger the action again manually

- **Edit:** `src/next-client/src/lib/admin-api.ts`
  - Same 429 handling for admin API calls

### Tests (Write Before Implementation)

#### Backend — `tests/MannaHp.Server.Tests/RateLimiting/RateLimitingTests.cs`
```
1. POST /api/orders returns 429 after 5 requests in 1 minute from same IP
2. POST /api/auth/login returns 429 after 5 requests in 1 minute from same IP
3. POST /api/auth/login — 429 response includes Retry-After header
4. POST /api/auth/login — 429 response body is a JSON problem details object
5. GET /api/menu-items allows 60 requests before returning 429
6. GET /api/menu-items — request 61 returns 429
7. Admin write endpoint returns 429 after 30 requests in 1 minute
8. Different IPs have independent rate limit counters
9. Rate limit resets after the window expires
10. Requests within the limit return normal status codes (200/201)
```

#### Frontend — `src/next-client/src/__tests__/lib/api-rate-limit.test.ts`
```
1. Shows toast with retry message when API returns 429
2. Toast message includes seconds from Retry-After header
3. Does not auto-retry on 429
4. Non-429 errors are handled normally (no rate limit toast)
```

### Manual Testing Checklist

#### Prerequisites
- App running via `docker compose up`
- A tool for rapid requests: `curl` loop, `ab` (Apache Bench), or `hey`
- Staff account credentials for authenticated endpoint testing

#### Strict Policy (Order + Auth)
- [ ] Send 5 `POST /api/auth/login` requests rapidly — all succeed (or return 401 for wrong creds, but not 429)
- [ ] Send a 6th request — returns `429 Too Many Requests`
- [ ] Response includes `Retry-After` header with seconds remaining
- [ ] Response body is JSON with error message
- [ ] Wait for the window to expire, send another request — succeeds
- [ ] Same test with `POST /api/orders` — 429 after 5 requests

#### Relaxed Policy (Public Reads)
- [ ] Send 60 `GET /api/menu-items` requests rapidly — all succeed
- [ ] Send a 61st request — returns 429
- [ ] Confirm `GET /api/categories` shares the same behavior

#### Moderate Policy (Admin Writes)
- [ ] Authenticate as staff, send 30 `POST /api/categories` requests — all succeed (or return validation errors, but not 429)
- [ ] Send a 31st request — returns 429

#### IP Independence
- [ ] From one IP, exhaust the strict limit on `/api/auth/login`
- [ ] From a different IP (or using `X-Forwarded-For` if behind proxy), send a request to the same endpoint — succeeds

#### Frontend Handling
- [ ] Trigger a 429 from the customer checkout (rapidly tap "Place Order") — see toast with retry message
- [ ] Trigger a 429 from admin panel — see toast with retry message
- [ ] Confirm normal errors (400, 401, 500) still show their regular error messages, not the rate limit toast

#### Reverse Proxy (Production-like)
- [ ] With Caddy in front, confirm rate limiting uses the client's real IP (not Caddy's IP)
- [ ] Two different clients behind Caddy have independent rate limits

### Files Changed (Summary)

#### Modified Files
| File | Change |
|------|--------|
| `src/Server/Program.cs` | Add rate limiter services, forwarded headers config, `UseRateLimiter()` middleware |
| `src/Server/EndPoints/OrderEndpoints.cs` | Add `.RequireRateLimiting("strict")` to POST endpoints |
| `src/Server/EndPoints/AuthEndpoints.cs` | Add `.RequireRateLimiting("strict")` to login/register |
| `src/Server/EndPoints/MenuItemEndpoints.cs` | Add `"relaxed"` to GETs, `"moderate"` to writes |
| `src/Server/EndPoints/CategoryEndpoints.cs` | Add `"relaxed"` to GETs, `"moderate"` to writes |
| `src/Server/EndPoints/IngredientEndpoints.cs` | Add `"moderate"` to all endpoints |
| `src/Server/EndPoints/SettingsEndpoints.cs` | Add `"relaxed"` to public GET, `"moderate"` to others |
| `src/Server/EndPoints/VariantEndpoints.cs` | Add `"moderate"` to all endpoints |
| `src/Server/EndPoints/AvailableIngredientEndpoints.cs` | Add `"moderate"` to all endpoints |
| `src/Server/EndPoints/RecipeIngredientEndpoints.cs` | Add `"moderate"` to all endpoints |
| `src/Server/EndPoints/StripeWebhookEndpoints.cs` | Add `"moderate"` to webhook endpoint |
| `src/next-client/src/lib/api.ts` | Add 429 handling with toast |
| `src/next-client/src/lib/admin-api.ts` | Add 429 handling with toast |

#### New Files
| File | Description |
|------|-------------|
| `tests/MannaHp.Server.Tests/RateLimiting/RateLimitingTests.cs` | Backend rate limiting tests |
| `src/next-client/src/__tests__/lib/api-rate-limit.test.ts` | Frontend 429 handling tests |

### Notes

- **No new NuGet packages required.** `Microsoft.AspNetCore.RateLimiting` is built into ASP.NET Core 7+.
- **No database changes.** Rate limiting is in-memory (fixed window counters). This is appropriate for a single-server deployment. If the app scales to multiple servers, switch to a distributed store (Redis) — but that's unlikely for this project.
- **SignalR connections are not rate-limited.** WebSocket upgrades happen once and then maintain a persistent connection. The `/hubs/orders` endpoint is excluded.
- **Stripe webhooks** are already authenticated via signature verification, so the `moderate` policy is a belt-and-suspenders measure against volume, not a security gate.

---

## F15: Email-Based Inventory Restocking

**Status:** Planned
**Complexity:** High
**Dependencies:** F13 (Inventory Check-In / Receiving - completed)

### Problem

Restocking inventory is fully manual. The owner receives supplier delivery receipts via email and must re-enter every item, quantity, and cost into the admin UI by hand. This is tedious, error-prone, and delays inventory accuracy.

### Solution

Automate the pipeline: poll a Gmail inbox for emails tagged with a specific label, parse receipt content using Claude API (LLM), store parsed items for admin review/mapping, then apply to inventory via existing bulk-restock logic.

### Architecture

```
Gmail (label: "supplier-orders")
  -> Hangfire recurring job (every 15 min)
    -> Gmail API reads new emails
      -> Claude API extracts line items (name, qty, unit, cost)
        -> Stored as SupplierReceipt (Pending) in DB
          -> Admin reviews in UI, maps items to ingredients
            -> Approve -> calls existing bulk-restock logic
```

### Key Design Decisions

- **Gmail API + OAuth2** for email access (owner's single Gmail account, refresh token stored in config)
- **Claude API (LLM) parsing** handles any receipt format (HTML, plain text, PDF) without brittle format-specific parsers
- **Admin review before applying** prevents LLM errors from corrupting inventory
- **Hangfire in-process** in the API container reuses existing PostgreSQL, no new infrastructure
- **Raw email body stored** for re-parsing without re-fetching from Gmail
- **Gmail label lifecycle** - label removed after processing; re-add label to re-process an email

### New Entities

#### `SupplierReceipt`

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| GmailMessageId | string | Unique index, prevents re-processing |
| EmailSubject | string? | |
| EmailFrom | string? | |
| EmailReceivedAt | DateTime | |
| RawEmailBody | string | Stored for re-parsing |
| Status | ReceiptStatus | Pending / Approved / Rejected |
| ParsedAt | DateTime | |
| ReviewedAt | DateTime? | |
| ReviewedBy | string? | Admin user ID |
| Notes | string? | |
| CreatedAt | DateTime | |

#### `SupplierReceiptItem`

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| SupplierReceiptId | Guid | FK -> SupplierReceipt |
| ParsedName | string | Raw name from LLM |
| ParsedQuantity | decimal | |
| ParsedUnit | string | Raw unit text from receipt |
| ParsedCostTotal | decimal | Total cost for this line |
| IngredientId | Guid? | FK -> Ingredient (nullable, admin maps) |
| MappedQuantity | decimal? | Admin-adjusted quantity |
| MappedCostPaid | decimal? | Admin-adjusted cost |
| Confidence | decimal? | LLM confidence score 0-1 |

#### `ReceiptStatus` enum

```
Pending = 0, Approved = 1, Rejected = 2
```

### New API Endpoints

All require Owner role.

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/supplier-receipts` | List receipts (filterable by status) |
| GET | `/api/supplier-receipts/{id}` | Get receipt with items |
| PATCH | `/api/supplier-receipts/{id}/items/{itemId}` | Update item mapping (ingredientId, qty, cost) |
| POST | `/api/supplier-receipts/{id}/approve` | Approve -> bulk-restock all mapped items |
| POST | `/api/supplier-receipts/{id}/reject` | Mark as Rejected |
| POST | `/api/supplier-receipts/poll-now` | Trigger immediate email poll |

### New Services

#### `GmailService` (`src/Server/Services/GmailService.cs`)
- OAuth2 refresh token auth via `Google.Apis.Gmail.v1`
- `GetUnreadMessagesAsync(label)` - fetch messages with configured label
- `GetMessageBodyAsync(messageId)` - extract text from MIME message
- `MarkAsProcessedAsync(messageId)` - remove label after processing

#### `ReceiptParsingService` (`src/Server/Services/ReceiptParsingService.cs`)
- Sends email body + existing ingredient names/units to Claude API
- Returns structured JSON: item name, quantity, unit, total cost, suggested ingredient match, confidence
- Uses `claude-sonnet-4-20250514` (configurable)

#### `InventoryService` (`src/Server/Services/InventoryService.cs`)
- **Refactor:** Extract weighted-average cost + restock logic from `IngredientEndpoints.cs` bulk-restock
- Shared by manual bulk-restock endpoint and receipt approve endpoint

#### `GmailPollingJob` (`src/Server/Jobs/GmailPollingJob.cs`)
- Hangfire recurring job, runs every 15 minutes
- Fetches emails with label, skips duplicates by `GmailMessageId`
- Calls `ReceiptParsingService`, creates `SupplierReceipt` + items as Pending
- Auto-populates `IngredientId` from LLM suggestion for admin to confirm

### Hangfire Setup

**NuGet packages:** `Hangfire.Core`, `Hangfire.AspNetCore`, `Hangfire.PostgreSql`

- Configured in `Program.cs` with PostgreSQL storage (reuses existing DB)
- Dashboard at `/hangfire` (Owner auth)
- Recurring job: `*/15 * * * *`

### Admin UI

#### Deliveries list page (`src/next-client/src/app/admin/(dashboard)/deliveries/page.tsx`)
- Table of receipts: subject, sender, date, status badge, item count
- Filter by status (Pending first)
- "Poll Now" button

#### Delivery detail/review page (`src/next-client/src/app/admin/(dashboard)/deliveries/[id]/page.tsx`)
- Receipt metadata (subject, sender, date)
- Table of parsed items:
  - Parsed name, qty, unit, cost (read-only from LLM)
  - Ingredient dropdown (pre-selected from LLM suggestion)
  - Editable mapped quantity and cost fields
  - Confidence badge (green/yellow/red)
  - Unit mismatch warning
- "Approve & Restock" button with confirmation dialog
- "Reject" button

#### Navigation
- Add "Deliveries" nav item in admin sidebar with Truck icon, after Ingredients

### Configuration (env vars)

```
GMAIL_CLIENT_ID
GMAIL_CLIENT_SECRET
GMAIL_REFRESH_TOKEN
GMAIL_LABEL_NAME (default: "supplier-orders")
GMAIL_EMAIL_ADDRESS
ANTHROPIC_API_KEY
ANTHROPIC_MODEL (default: "claude-sonnet-4-20250514")
```

Added to `docker-compose.yml` api service and `.env.example`. No new Docker containers needed.

### Implementation Phases

| Phase | Description | Depends On |
|-------|-------------|------------|
| 1 | New entities, enum, EF migration, DB schema | - |
| 2 | Extract `InventoryService` from bulk-restock endpoint (refactor) | Phase 1 |
| 3 | `SupplierReceiptEndpoints` CRUD + approve/reject | Phase 2 |
| 4 | Hangfire setup + stub polling job | Phase 3 |
| 5 | Gmail API integration (`GmailService`) | Phase 4 |
| 6 | Claude API parsing (`ReceiptParsingService`) | Phase 5 |
| 7 | Admin UI (deliveries list + detail/review pages) | Phase 3 |
| 8 | Docker/config changes, `.env.example` updates | Phase 5, 6 |

### Files Modified

| File | Change |
|------|--------|
| `src/Server/EndPoints/IngredientEndpoints.cs` | Extract restock logic to InventoryService |
| `src/Server/Data/MannaDbContext.cs` | Add DbSets + EF config for new entities |
| `src/Server/Program.cs` | Add Hangfire config, endpoint mapping |
| `src/next-client/src/lib/admin-api.ts` | Add supplier receipt API methods |
| `src/next-client/src/types/api.ts` | Add receipt DTOs/types |
| `src/next-client/src/components/admin/sidebar.tsx` | Add Deliveries nav item |
| `docker-compose.yml` | Add Gmail + Anthropic env vars |
| `.env.example` | Add placeholder entries |

### New Files

| File | Description |
|------|-------------|
| `src/Shared/Enums/ReceiptStatus.cs` | ReceiptStatus enum |
| `src/Shared/Entities/SupplierReceipt.cs` | SupplierReceipt entity |
| `src/Shared/Entities/SupplierReceiptItem.cs` | SupplierReceiptItem entity |
| `src/Shared/DTOs/SupplierReceiptDto.cs` | DTOs for receipt + items |
| `src/Server/Services/InventoryService.cs` | Extracted restock logic |
| `src/Server/Services/GmailService.cs` | Gmail API integration |
| `src/Server/Services/ReceiptParsingService.cs` | Claude API receipt parser |
| `src/Server/Jobs/GmailPollingJob.cs` | Hangfire recurring job |
| `src/Server/EndPoints/SupplierReceiptEndpoints.cs` | Receipt API endpoints |
| `src/next-client/src/app/admin/(dashboard)/deliveries/page.tsx` | Deliveries list page |
| `src/next-client/src/app/admin/(dashboard)/deliveries/[id]/page.tsx` | Delivery review page |

### Verification

1. **Unit tests:** Approve flow creates correct restock items; reject flow updates status; duplicate GmailMessageId is skipped
2. **Integration test:** Mock Gmail + Claude API responses, verify pipeline from email -> pending receipt -> approve -> inventory updated with correct weighted-average costs
3. **Manual E2E:** Label a test email in Gmail, click "Poll Now" or wait 15 min, review parsed items in admin UI, map to ingredients, approve, verify ingredient stock and inventory log updated
