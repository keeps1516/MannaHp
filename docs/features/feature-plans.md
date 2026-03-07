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

## F13: Inventory Check-In / Receiving

**Priority:** P13
**Mobile Impact:** High - restocking happens in the kitchen/storage, likely on a phone

### Current State
- Stock levels can only be changed by editing an ingredient via `PUT /api/ingredients/{id}`, which **overwrites** all fields including `StockQuantity`
- No additive "received X units" operation — admin must mentally add the delivery amount to the current stock and type the new total
- No audit trail — no record of when stock was added, by whom, or how much
- No batch receiving — each ingredient must be edited one at a time
- Stock **does** auto-decrement when orders are marked Completed (commit `1f67b72`)

### Plan

#### Step 1: Create inventory log table
- **File:** `src/Shared/Entities/InventoryLog.cs` (new)
  - `Id` (Guid), `IngredientId` (FK), `ChangeType` (enum: Received, OrderDecrement, Adjustment), `QuantityChange` (decimal, positive for additions, negative for decrements), `NewStockQuantity` (decimal, snapshot after change), `Notes` (string, optional — e.g., "Weekly delivery"), `CreatedBy` (string, user ID), `CreatedAt` (DateTime)
- **Migration** to add `inventory_logs` table

#### Step 2: Add restock endpoint
- **File:** `src/Server/EndPoints/IngredientEndpoints.cs`
- `POST /api/ingredients/{id}/restock`
  - Accepts `{ quantity: decimal, notes?: string }`
  - **Adds** `quantity` to the ingredient's current `StockQuantity` (not overwrite)
  - Creates an `InventoryLog` entry with `ChangeType = Received`
  - Returns updated ingredient DTO
  - Owner authorization

#### Step 3: Add bulk restock endpoint
- **File:** `src/Server/EndPoints/IngredientEndpoints.cs`
- `POST /api/ingredients/bulk-restock`
  - Accepts array of `{ ingredientId, quantity, notes? }`
  - Adds quantities in a single transaction, creates log entries for each
  - Owner authorization

#### Step 4: Log order decrements
- **File:** `src/Server/EndPoints/OrderEndpoints.cs`
- When an order is marked Completed and stock is decremented, also write `InventoryLog` entries with `ChangeType = OrderDecrement`

#### Step 5: Add receiving UI
- **File:** `src/next-client/src/app/admin/(dashboard)/ingredients/restock/page.tsx` (new)
- Shows all ingredients in a list with current stock and an "Add" input field
- Enter quantities received for each ingredient (leave blank to skip)
- Optional notes field (e.g., "Sysco delivery 3/10")
- "Submit Delivery" button sends bulk restock request
- Success toast with summary: "Restocked 8 ingredients"
- Link from ingredients page header: "Check In Delivery" button

#### Step 6: Add inventory history view
- **File:** `src/next-client/src/app/admin/(dashboard)/ingredients/[id]/history/page.tsx` (new)
- Shows chronological log of all stock changes for a single ingredient
- Each entry: date, change type, quantity change (+/-), resulting stock, notes, user
- Accessible from ingredient detail/edit sheet via "View History" link

### Tests (Write Before Implementation)

#### Unit Tests — `src/next-client/src/__tests__/components/restock-page.test.tsx`
```
1. renders all ingredients with current stock levels
2. "Add" input accepts numeric values
3. ingredients with no input are excluded from submission
4. "Submit Delivery" sends only modified ingredients to bulk-restock endpoint
5. shows success toast with count of restocked ingredients
6. optional notes field is included in the request
7. form resets after successful submission
```

#### Unit Tests — `src/next-client/src/__tests__/components/ingredient-history.test.tsx`
```
1. renders chronological list of inventory changes
2. shows change type badge (Received / Order / Adjustment)
3. positive changes show green "+X", negative show red "-X"
4. each entry shows resulting stock level
5. shows "No history" when log is empty
```

#### Integration Tests (Backend) — `tests/MannaHp.Server.Tests/Endpoints/RestockEndpointTests.cs`
```
1. POST /api/ingredients/{id}/restock adds to current stock (not overwrites)
2. restock creates an InventoryLog entry with ChangeType = Received
3. bulk-restock updates multiple ingredients in a single transaction
4. restock rejects negative quantities
5. requires Owner authorization
6. completing an order creates InventoryLog entries with ChangeType = OrderDecrement
7. inventory log records the correct NewStockQuantity snapshot
```

### Relationship to F9 (Bulk Stock Update)
F9 covers a "restock mode" with inline editing that **overwrites** stock values. F13 replaces that concept with **additive** receiving and an audit trail. If F13 is implemented, F9's inline overwrite mode becomes less necessary — consider merging F9's UI ideas (inline inputs on the ingredients table) into F13's receiving flow.

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
