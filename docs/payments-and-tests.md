# Manna HP - Payment & Order Flow Documentation + Test Cases

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Data Layer - Entities & Schema](#data-layer)
3. [DTOs & Validators](#dtos--validators)
4. [Backend API Endpoints](#backend-api-endpoints)
5. [Stripe Service](#stripe-service)
6. [Stripe Webhooks](#stripe-webhooks)
7. [SignalR Real-Time Hub](#signalr-real-time-hub)
8. [Frontend - Cart State Management](#frontend---cart-state-management)
9. [Frontend - Checkout Flow (Card Payment)](#frontend---checkout-flow-card-payment)
10. [Frontend - In-Store Payment Flow](#frontend---in-store-payment-flow)
11. [Frontend - Admin Order Management](#frontend---admin-order-management)
12. [Frontend - Admin Submit Order](#frontend---admin-submit-order)
13. [End-to-End Flow Diagrams](#end-to-end-flow-diagrams)
14. [Environment Variables](#environment-variables)
15. [Test Suite Documentation](#test-suite-documentation)

---

## Architecture Overview

```
Customer Browser                    Server (ASP.NET Core)              External
+-----------------------+          +------------------------+         +--------+
| Next.js (Blazor PWA)  |  HTTP    | Minimal API Endpoints  |  SDK   | Stripe |
|                        | ------> | OrderEndpoints.cs      | -----> | API    |
| Cart -> Checkout       |         | StripeWebhook.cs       | <----- |        |
| Stripe Elements        |         | StripeService.cs       |         +--------+
|                        | SignalR  |                        |
| OrderHub connection    | <-----> | OrderHub.cs            |         +--------+
+-----------------------+          |                        |  EF    | Postgres|
                                   | MannaDbContext.cs       | -----> | DB     |
+-----------------------+          +------------------------+         +--------+
| Admin Dashboard        |
| Kitchen Display        |
| Submit Order           |
+-----------------------+
```

---

## Data Layer

### Order Entity

**File:** `src/Shared/Entities/Order.cs`

```csharp
// Line 5-26
public class Order
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }         // Auto-increment, starts at 1001
    public string? UserId { get; set; }           // Nullable - guest orders have no user
    public OrderStatus Status { get; set; }       // Received -> Preparing -> Ready -> Completed
    public PaymentMethod PaymentMethod { get; set; } // Card or InStore
    public PaymentStatus PaymentStatus { get; set; } // Pending -> Paid/Failed/Refunded
    public string? StripePaymentId { get; set; }  // Links to Stripe PaymentIntent ID
    public string? CardBrand { get; set; }        // "visa", "mastercard", etc.
    public string? CardLast4 { get; set; }        // "4242"
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal TaxRate { get; set; }          // e.g. 0.0825
    public decimal Total { get; set; }
    public bool Printed { get; set; }             // Receipt printed flag
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; }
}
```

### OrderItem Entity

**File:** `src/Shared/Entities/OrderItem.cs`

```csharp
// Line 3-18
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }          // Null for customizable bowls
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }        // Price snapshot at time of order
    public decimal TotalPrice { get; set; }       // UnitPrice * Quantity
    public string? Notes { get; set; }            // Special instructions
    public List<OrderItemIngredient> Ingredients { get; set; }
}
```

### OrderItemIngredient Entity

**File:** `src/Shared/Entities/OrderItemIngredient.cs`

```csharp
// Line 3-13
public class OrderItemIngredient
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal QuantityUsed { get; set; }     // e.g. 10.0 oz
    public decimal PriceCharged { get; set; }     // Price snapshot at time of order
}
```

### Enums

**File:** `src/Shared/Enums/OrderStatus.cs`

```csharp
// Lines 3-24
public enum OrderStatus
{
    Received = 0,      // Order just placed
    Preparing = 1,     // Kitchen started working on it
    Ready = 2,         // Ready for pickup
    Completed = 3,     // Customer picked up
    Cancelled = 4      // Order cancelled
}

public enum PaymentMethod
{
    Card = 0,          // Stripe card payment
    InStore = 1        // Pay at register
}

public enum PaymentStatus
{
    Pending = 0,       // Payment not yet confirmed
    Paid = 1,          // Payment confirmed by Stripe
    Failed = 2,        // Payment failed
    Refunded = 3       // Payment refunded
}
```

### Database Schema Configuration

**File:** `src/Server/Data/MannaDbContext.cs` (Lines 168-228)

Key schema details:
- `orders.order_number`: PostgreSQL sequence starting at 1001, auto-incrementing
- `orders.subtotal`, `tax`, `total`: `numeric(10,2)` precision
- `orders.tax_rate`: `numeric(5,4)` precision
- `order_items` cascade delete with parent order
- `order_item_ingredients` cascade delete with parent order item
- `stripe_payment_id`: varchar(255), nullable
- `card_brand`: varchar(20), `card_last4`: varchar(4)

---

## DTOs & Validators

### Request/Response DTOs

**File:** `src/Shared/DTOs/OrderDto.cs`

```csharp
// Lines 6-9 — What the frontend sends to create an order
public record CreateOrderRequest(
    PaymentMethod PaymentMethod,    // Card or InStore
    string? Notes,                  // Order-level notes
    List<CreateOrderItemRequest> Items
);

// Lines 11-16 — Individual item in the order
public record CreateOrderItemRequest(
    Guid MenuItemId,
    Guid? VariantId,                // Required for fixed items, null for bowls
    int Quantity,
    string? Notes,                  // "extra hot, oat milk"
    List<Guid>? SelectedIngredientIds  // For customizable items (bowls)
);

// Lines 50-53 — API response after creating an order
public record CreateOrderResponse(
    OrderDto Order,
    string? ClientSecret,           // Stripe PaymentIntent client_secret (card only)
    string? StripePublishableKey    // pk_test_... or pk_live_...
);

// Lines 55-56 — Kitchen staff status update
public record UpdateOrderStatusRequest(OrderStatus Status);
```

### Validators

**File:** `src/Shared/Validators/OrderValidators.cs`

```csharp
// Lines 6-18 — CreateOrderRequestValidator
// - PaymentMethod must be a valid enum value
// - Items list must not be empty
// - Each item validated by CreateOrderItemRequestValidator

// Lines 20-30 — CreateOrderItemRequestValidator
// - MenuItemId must not be empty GUID
// - Quantity must be > 0

// Lines 32-39 — UpdateOrderStatusRequestValidator
// - Status must be a valid OrderStatus enum value
```

### Validation Filter

**File:** `src/Server/Filters/ValidationFilter.cs`

```csharp
// Lines 10-22 — Generic endpoint filter
// - Resolves IValidator<T> from DI
// - Validates request body before endpoint executes
// - Returns 400 ValidationProblem if invalid
// - Passes through to endpoint if valid
```

---

## Backend API Endpoints

### POST /api/orders — Create Order

**File:** `src/Server/EndPoints/OrderEndpoints.cs` (Lines 20-151)

This is the core endpoint. Line-by-line:

```
Line 22:   Accept CreateOrderRequest from body
Line 23:   Tax rate hardcoded at 0.0825 (8.25%)

Lines 25-32: Create Order entity
  - Generate new GUID
  - Set PaymentMethod from request
  - Set PaymentStatus = Pending
  - Set Status = Received
  - Set Notes from request
  - Set CreatedAt/UpdatedAt = now

Lines 34-112: Process each item in request.Items
  Line 36:   Fetch MenuItem from DB (validate it exists)
  Line 38:   Return 400 if menu item not found

  Lines 40-42: Create OrderItem with MenuItemId, Quantity, Notes

  Lines 54-65: If VariantId provided (fixed-price items like coffee):
    - Fetch variant from DB
    - Validate variant belongs to this menu item
    - Set basePrice = variant.Price (e.g. $5.25 for 16oz Latte)

  Lines 68-100: If SelectedIngredientIds provided (customizable items like bowls):
    - Fetch available ingredients from DB
    - Validate each ingredient exists and is active
    - For each selected ingredient:
      - Create OrderItemIngredient record
      - Set PriceCharged = ingredient.CustomerPrice (snapshot)
      - Set QuantityUsed = ingredient.QuantityUsed
    - Add ingredientPrice to running total

  Lines 103-107: Validation
    - Must have either variant OR ingredients (not neither)
    - Return 400 if both missing

  Lines 109-111: Calculate prices
    - unitPrice = variantPrice + ingredientPrice
    - totalPrice = unitPrice * quantity
    - Add to order.Items

Lines 114-117: Calculate order totals
  - subtotal = sum of all item totalPrices
  - tax = round(subtotal * taxRate, 2)
  - total = subtotal + tax

Lines 120-127: STRIPE PAYMENT (if PaymentMethod == Card)
  - Call stripeService.CreatePaymentIntentAsync(order.Total)
  - This creates a PaymentIntent on Stripe's servers
  - Store the PaymentIntent ID on the order (StripePaymentId)
  - Capture the clientSecret for frontend

Lines 129-130: Save order to database

Lines 133-137: Re-fetch order with all navigation properties
  - Include Items -> MenuItems, Variants
  - Include Items -> Ingredients -> Ingredient

Lines 141-146: If InStore payment
  - Broadcast to kitchen immediately via SignalR
  - No waiting for payment — food prep starts now

Lines 148-150: Return 201 Created
  - Body: CreateOrderResponse { Order, ClientSecret, StripePublishableKey }
  - Card orders: clientSecret is set (frontend needs it for Stripe Elements)
  - InStore orders: clientSecret is null

Line 151: Apply ValidationFilter<CreateOrderRequest>
```

### POST /api/orders/{id}/confirm-payment — Confirm Card Payment

**File:** `src/Server/EndPoints/OrderEndpoints.cs` (Lines 154-200)

Called by the frontend after `stripe.confirmPayment()` succeeds on the client side.

```
Lines 157-161: Fetch order with all navigation properties
Lines 163-167: Validate:
  - Order exists
  - Not already marked as Paid
  - Has a StripePaymentId

Lines 169-170: Fetch PaymentIntent from Stripe API
  - Uses stripeService.GetPaymentIntentAsync(order.StripePaymentId)

Lines 171-190: If PaymentIntent status == "succeeded":
  - Set order.PaymentStatus = Paid
  Lines 176-181: Extract card details from Stripe charge:
    - Get latest charge ID from PaymentIntent
    - Fetch charge details
    - Set order.CardBrand (e.g. "visa")
    - Set order.CardLast4 (e.g. "4242")
  - Update order.UpdatedAt
  - Save to database
  - Broadcast OrderCreated to "kitchen" SignalR group
  - Return OrderDto

Lines 192-197: If status == "requires_payment_method" or "canceled":
  - Set order.PaymentStatus = Failed
  - Save to database

Line 199: Return current OrderDto regardless of outcome
```

### GET /api/orders/{id} — Get Order by ID

**File:** `src/Server/EndPoints/OrderEndpoints.cs` (Lines 203-213)

- Fetch order with full navigation (Items, Variants, Ingredients)
- Return 404 if not found
- Return OrderDto

### GET /api/orders/active — Get Active Orders (Kitchen)

**File:** `src/Server/EndPoints/OrderEndpoints.cs` (Lines 216-227)

- **Requires "Staff" authorization**
- Filter: Status != Completed AND Status != Cancelled
- Order by CreatedAt ascending (oldest first)
- Include all navigation properties
- Return List<OrderDto>

### PATCH /api/orders/{id}/status — Update Order Status

**File:** `src/Server/EndPoints/OrderEndpoints.cs` (Lines 230-248)

- **Requires "Staff" authorization**
- Fetch order, update Status field, save
- Broadcast to SignalR:
  - `"kitchen"` group: all staff see the status change
  - `"order-{id}"` group: customer watching their order sees the change
- Apply ValidationFilter<UpdateOrderStatusRequest>

---

## Stripe Service

**File:** `src/Server/Services/StripeService.cs`

### Constructor (Lines 9-17)

```csharp
// Reads from configuration:
//   Stripe:SecretKey   → sk_test_... or sk_live_...
//   Stripe:PublishableKey → pk_test_... or pk_live_...
// Sets StripeConfiguration.ApiKey globally for Stripe SDK
```

### CreatePaymentIntentAsync (Lines 21-36)

```csharp
// Parameters: amount (decimal in dollars), description (string?)
//
// Line 23-32: Build PaymentIntentCreateOptions
//   Amount = (long)(amount * 100)   // Convert $19.76 -> 1976 cents
//   Currency = "usd"
//   Description = description
//   AutomaticPaymentMethods.Enabled = true
//     ^ Supports card, Apple Pay, Google Pay, etc.
//
// Line 34: Create PaymentIntent via Stripe SDK
// Line 35: Return PaymentIntent object
//   - .Id = "pi_abc123..."
//   - .ClientSecret = "pi_abc123_secret_xyz..."
//   - .Status = "requires_payment_method"
```

### GetPaymentIntentAsync (Lines 38-42)

```csharp
// Fetches a PaymentIntent by ID from Stripe
// Used by confirm-payment endpoint and webhook handler
// Returns updated status: "succeeded", "requires_payment_method", "canceled"
```

### GetChargeAsync (Lines 44-48)

```csharp
// Fetches Charge details from Stripe
// Used to extract card brand and last 4 digits
// Returns charge with PaymentMethodDetails.Card.Brand and Last4
```

---

## Stripe Webhooks

**File:** `src/Server/EndPoints/StripeWebhookEndpoints.cs`

### POST /api/stripe/webhook (Lines 17-50)

Stripe sends HTTP POST requests to this endpoint when payment events occur. Must be publicly reachable.

```
Line 20:   Read raw request body as string
Line 21:   Get webhook secret from config (Stripe:WebhookSecret)

Lines 26-27: Validate webhook signature
  - EventUtility.ConstructEvent(body, signature_header, webhook_secret)
  - Prevents spoofed webhook calls

Lines 29-31: If signature invalid -> 400 Bad Request

Lines 34-39: Switch on event type:
  Case "payment_intent.succeeded":
    -> HandlePaymentSucceeded()

  Case "payment_intent.payment_failed":
    -> HandlePaymentFailed()

Line 49: Return 200 OK (acknowledge receipt to Stripe)
Line 50: DisableAntiforgery (Stripe POSTs from external origin)
```

### HandlePaymentSucceeded (Lines 53-80)

```
Line 56-60: Find order by StripePaymentId (with all nav properties)
Line 62:    Skip if order not found or already Paid

Line 64:    Set PaymentStatus = Paid

Lines 67-71: Extract card details:
  - Get latest charge ID from PaymentIntent
  - Fetch charge from Stripe
  - Set CardBrand and CardLast4

Lines 74-75: Save to DB

Lines 78-79: Broadcast to kitchen via SignalR
  - "kitchen" group receives "OrderCreated" event
  - Kitchen display shows the order
```

### HandlePaymentFailed (Lines 82-92)

```
Lines 84-85: Find order by StripePaymentId
Line 87:     Skip if not found or already Paid
Line 89:     Set PaymentStatus = Failed
Lines 90-91: Save to DB
```

### Why Both Webhook AND Client Confirmation?

The system has **two paths** to mark an order as paid:

1. **Webhook** (most reliable): Stripe directly notifies the server. Works even if the customer closes their browser.
2. **Client confirm-payment** (faster UX): Frontend calls after `stripe.confirmPayment()` resolves. Gives immediate feedback to the customer.

Whichever runs first marks the order as Paid. The second path sees `PaymentStatus == Paid` and skips.

---

## SignalR Real-Time Hub

**File:** `src/Server/Hubs/OrderHub.cs`

### Hub Methods

```csharp
// Lines 10-13: JoinKitchen()
// - Kitchen staff call this when opening the order display
// - Adds their SignalR connection to the "kitchen" group
// - They receive: OrderCreated, OrderStatusChanged broadcasts

// Lines 15-18: LeaveKitchen()
// - Called when staff navigates away from order display

// Lines 23-26: JoinOrder(string orderId)
// - Customer calls with their order ID after placing an order
// - Adds connection to "order-{orderId}" group
// - Receives status updates for their specific order

// Lines 28-31: LeaveOrder(string orderId)
// - Customer calls when done watching their order
```

### Server-Side Broadcasts (called from endpoints)

```
OrderCreated (broadcast to "kitchen"):
  - Sent when InStore order is created (immediately)
  - Sent when Card order is confirmed paid (via webhook or confirm-payment)

OrderStatusChanged (broadcast to "kitchen" + "order-{id}"):
  - Sent when staff advances order status
  - Both kitchen display AND customer see the update
```

### Frontend Hub Connection

**File:** `src/next-client/src/lib/order-hub.ts`

```typescript
// Lines 31-40: Disconnect any stale connection (React Strict Mode guard)

// Lines 43-47: Create HubConnectionBuilder
//   URL: ${API_BASE}/hubs/orders
//   Auto-reconnect delays: 0ms, 2s, 5s, 10s, 30s
//   Log level: Warning

// Lines 49-50: Register event listeners
//   "OrderCreated" -> onOrderCreated callback
//   "OrderStatusChanged" -> onOrderStatusChanged callback

// Lines 52-55: On reconnect -> re-invoke JoinKitchen, call onReconnected
// Lines 57-59: On close -> call onDisconnected

// Line 61: Start connection
// Lines 69-70: Invoke JoinKitchen to join kitchen group
```

---

## Frontend - Cart State Management

### Cart Types

**File:** `src/next-client/src/types/cart.ts`

```typescript
// CartItem type:
//   id: string              - Unique local ID (generated)
//   menuItem: MenuItemDto   - The menu item
//   variant: MenuItemVariantDto | null  - Selected size/variant
//   selectedIngredients: AvailableIngredientDto[] | null  - Bowl ingredients
//   quantity: number
//   notes: string | null    - Special instructions

// Helper functions (Lines 12-30):
// getUnitPrice(item): variant.price + sum(ingredient.customerPrice)
// getLineTotal(item): unitPrice * quantity
// getDisplayName(item): "Item Name (Variant)" or "Item Name"
```

### Cart Context (React Context + Reducer)

**File:** `src/next-client/src/store/cart-context.tsx`

```typescript
// Lines 15-17: CartState = { items: CartItem[] }

// Lines 25-55: CartReducer actions:
//   ADD_ITEM:        Append new CartItem with generated ID
//   REMOVE_ITEM:     Filter out by ID
//   UPDATE_QUANTITY: Update quantity, remove if <= 0
//   CLEAR:           Empty the cart

// Lines 74-77: Computed values exposed via context:
//   itemCount: sum of all item quantities
//   subtotal:  sum of all line totals
//   tax:       subtotal * 0.0825 (8.25% tax rate)
//   total:     subtotal + tax
```

---

## Frontend - Checkout Flow (Card Payment)

### Checkout Page

**File:** `src/next-client/src/app/(customer)/checkout/page.tsx`

```typescript
// Lines 18-25: Component state
//   stripePromise: Lazily loaded Stripe.js
//   clientSecret:  From API create-order response
//   orderId:       For tracking payment
//   loading, error: UI state

// Lines 35-80: useEffect — Create Order on mount
//   Line 37-38: Guard against duplicate creation (React Strict Mode)
//
//   Lines 42-56: Call api.createOrder({
//     paymentMethod: PaymentMethod.Card,
//     items: cart.items.map(item => ({
//       menuItemId: item.menuItem.id,
//       variantId: item.variant?.id,
//       quantity: item.quantity,
//       notes: item.notes,
//       selectedIngredientIds: item.selectedIngredients?.map(i => i.ingredientId)
//     }))
//   })
//
//   Lines 60-63: Extract clientSecret and publishableKey from response
//   Lines 65-67: Set orderId, load Stripe.js via loadStripe(publishableKey)
//   Line 78: Cleanup function prevents duplicate on Strict Mode re-mount

// Lines 82-87: handlePaymentSuccess()
//   - Clear cart
//   - Navigate to /order/{orderId}

// Lines 89-91: handlePaymentError(message)
//   - Show toast with error message

// Lines 97-231: Render
//   Lines 116-152: Order summary panel
//     - List cart items with variant names and prices
//     - Subtotal, Tax (8.25%), Total
//
//   Lines 155-228: Payment section
//     Lines 162-169: Loading state — spinner while creating PaymentIntent
//     Lines 171-183: Error state — error message with details
//     Lines 185-223: Stripe Elements
//       - Load Stripe.js with publishableKey
//       - Create <Elements> provider with clientSecret + appearance theme
//       - Render <CheckoutForm> inside the Elements provider
//     Lines 226-228: "Payments processed securely by Stripe" footer
```

### Checkout Form (Stripe Elements)

**File:** `src/next-client/src/components/checkout-form.tsx`

```typescript
// Lines 13-18: Props
//   orderId: string     - Order being paid
//   total: number       - Amount to display on button
//   onSuccess: () => void
//   onError: (msg) => void

// Lines 31-57: handleSubmit (form submission)
//
//   Line 39-42: Call stripe.confirmPayment({
//     elements,                    // Stripe Elements instance
//     redirect: "if_required"      // Don't redirect unless 3D Secure needed
//   })
//   // At this point, Stripe.js sends card info directly to Stripe servers.
//   // Our server NEVER sees raw card numbers.
//
//   Lines 44-48: If client-side error
//     - Call onError(error.message)
//     - Return early
//
//   Line 51: Call api.confirmPayment(orderId)
//     // POST /api/orders/{id}/confirm-payment
//     // Server checks PaymentIntent status with Stripe
//     // If succeeded: marks order Paid, extracts card details, notifies kitchen
//
//   Line 52: Call onSuccess()
//     // -> clears cart, navigates to confirmation page
//
//   Lines 53-56: Catch network/API errors

// Lines 59-85: Render
//   <PaymentElement layout="tabs" />
//     // Stripe-hosted iframe with card input
//     // Supports: Card, Apple Pay, Google Pay
//     // PCI compliant — card data never touches our servers
//
//   <Button type="submit">Pay ${total.toFixed(2)}</Button>
//     // Shows loading spinner while processing
```

### Order Confirmation Page

**File:** `src/next-client/src/app/(customer)/order/[id]/page.tsx`

```typescript
// Lines 18-27: Fetch order by ID on mount

// Lines 45-105: Render
//   Lines 47-52: Success checkmark icon + "Order Placed!" heading
//   Lines 55-93: Order details panel
//     - Each item with quantity, variant, ingredient list
//     - Subtotal, Tax rate (formatted as %), Total
//   Lines 96-102: "Order Again" button -> navigate to /
```

---

## Frontend - In-Store Payment Flow

### Cart Drawer (Customer)

**File:** `src/next-client/src/components/cart-drawer.tsx`

```typescript
// Lines 44-74: handlePlaceOrder() — In-Store payment
//
//   Lines 51-63: Call api.createOrder({
//     paymentMethod: PaymentMethod.InStore,
//     items: cart items mapped to request format
//   })
//   // Server creates order with PaymentStatus=Pending
//   // Server broadcasts to kitchen immediately (no payment wait)
//
//   Line 64: Clear cart
//   Line 66: Store orderId
//   Line 67: Show victory video overlay

// Lines 76-79: handlePayWithCard()
//   - Close drawer
//   - Navigate to /checkout page

// Lines 81-209: Render
//   Lines 145-187: Bottom bar
//     - Subtotal, Tax (8.25%), Total
//     - "Pay with Card" button -> /checkout
//     - "Pay In-Store" button -> handlePlaceOrder()
//   Lines 192-206: Victory video overlay on successful in-store order
```

---

## Frontend - Admin Order Management

### Kitchen Orders Page

**File:** `src/next-client/src/app/admin/(dashboard)/orders/page.tsx`

```typescript
// Lines 33-95: Initial load + SignalR setup
//
//   Line 23: Fetch active orders from GET /api/orders/active
//
//   Lines 39-78: Connect to OrderHub
//     onOrderCreated:      Add new order to state
//     onOrderStatusChanged: Update order status, filter out completed/cancelled
//     onReconnected:       Re-fetch orders, stop polling fallback
//     onDisconnected:      Start 15-second polling as fallback
//
//   Lines 84-86: If SignalR connection fails, fall back to polling

// Lines 97-118: handleAdvance(orderId)
//   Lines 101-109: Optimistic UI update (move order to next status instantly)
//   Line 112: Call adminApi.updateOrderStatus(orderId, nextStatus)
//     // PATCH /api/orders/{id}/status
//     // Server broadcasts to kitchen + customer groups
//   Line 116: If error, rollback optimistic update + refetch

// Lines 132-265: Render — Three Kanban columns
//   Received column:  New orders waiting to be started
//   Preparing column: Orders being made
//   Ready column:     Orders waiting for customer pickup
//
//   Each order rendered as <OrderCard> with tap-to-advance
```

### Order Card Component

**File:** `src/next-client/src/components/admin/order-card.tsx`

```typescript
// Lines 17-33: Status workflow mapping
//   Received  -> tap -> "Start Preparing"
//   Preparing -> tap -> "Mark Ready"
//   Ready     -> tap -> "Complete"

// Lines 57-186: Render
//   Lines 60-86: Header (always visible)
//     - Order ID (first 8 chars of GUID)
//     - Item count
//     - Time elapsed since created
//     - Expand/collapse chevron
//
//   Lines 89-183: Collapsible body
//     Lines 98-155: Item list
//       - Quantity x Name (Variant) ... $Price
//       - Bowl ingredients with measurements and prices
//       - Special notes in amber italic
//     Lines 159-181: Footer
//       - Total price
//       - Payment method badge ("Card" or "In-Store")
//       - Tap hint: "Tap -> [Next Action]"
```

---

## Frontend - Admin Submit Order

**File:** `src/next-client/src/app/admin/(dashboard)/submit-order/page.tsx`

For staff creating orders on behalf of walk-in customers.

```typescript
// Lines 73-101: handleSubmit()
//   Lines 78-89: Call api.createOrder({
//     paymentMethod: PaymentMethod.InStore,
//     notes: orderNotes,
//     items: orderItems mapped to request
//   })
//   // Identical to customer in-store flow
//   // Kitchen gets notified immediately via SignalR
//
//   Lines 92-95: Clear state, show success toast
```

---

## End-to-End Flow Diagrams

### Card Payment Flow

```
Customer                    Next.js App              ASP.NET API              Stripe
   |                           |                         |                      |
   |  Add items to cart        |                         |                      |
   |-------------------------->|                         |                      |
   |                           |                         |                      |
   |  Click "Pay with Card"    |                         |                      |
   |-------------------------->|                         |                      |
   |                           |                         |                      |
   |                           |  POST /api/orders       |                      |
   |                           |  {Card, items[]}        |                      |
   |                           |------------------------>|                      |
   |                           |                         |                      |
   |                           |                         |  CreatePaymentIntent |
   |                           |                         |  ($19.76 = 1976c)   |
   |                           |                         |--------------------->|
   |                           |                         |                      |
   |                           |                         |  pi_abc123...        |
   |                           |                         |  client_secret       |
   |                           |                         |<---------------------|
   |                           |                         |                      |
   |                           |  { order, clientSecret, |                      |
   |                           |    publishableKey }     |                      |
   |                           |<------------------------|                      |
   |                           |                         |                      |
   |  Show Stripe Elements     |                         |                      |
   |<--------------------------|                         |                      |
   |                           |                         |                      |
   |  Enter card details       |                         |                      |
   |  (in Stripe iframe)       |                         |                      |
   |                           |                         |                      |
   |  Submit payment           |                         |                      |
   |-------------------------->|                         |                      |
   |                           |                         |                      |
   |                           |  stripe.confirmPayment()|                      |
   |                           |  (card -> Stripe direct)|                      |
   |                           |------------------------------------------>--->|
   |                           |                         |                      |
   |                           |                         |                      |
   |                           |  Payment result         |                      |
   |                           |<------------------------------------------<---|
   |                           |                         |                      |
   |                           |  POST /confirm-payment  |                      |
   |                           |------------------------>|                      |
   |                           |                         |  GetPaymentIntent    |
   |                           |                         |--------------------->|
   |                           |                         |  status: succeeded   |
   |                           |                         |<---------------------|
   |                           |                         |                      |
   |                           |                         |  GetCharge           |
   |                           |                         |--------------------->|
   |                           |                         |  brand: visa         |
   |                           |                         |  last4: 4242         |
   |                           |                         |<---------------------|
   |                           |                         |                      |
   |                           |                         |  -> PaymentStatus=Paid
   |                           |                         |  -> SignalR: kitchen |
   |                           |                         |                      |
   |                           |  OrderDto (paid)        |                      |
   |                           |<------------------------|                      |
   |                           |                         |                      |
   |  Clear cart               |                         |                      |
   |  Show confirmation        |                         |                      |
   |<--------------------------|                         |                      |
   |                           |                         |                      |
   |          SIMULTANEOUSLY (webhook backup):           |                      |
   |                           |                         |  Webhook: succeeded  |
   |                           |                         |<---------------------|
   |                           |                         |  (skips - already    |
   |                           |                         |   marked Paid)       |
```

### In-Store Payment Flow

```
Customer                    Next.js App              ASP.NET API           Kitchen
   |                           |                         |                    |
   |  Add items to cart        |                         |                    |
   |-------------------------->|                         |                    |
   |                           |                         |                    |
   |  Click "Pay In-Store"     |                         |                    |
   |-------------------------->|                         |                    |
   |                           |                         |                    |
   |                           |  POST /api/orders       |                    |
   |                           |  {InStore, items[]}     |                    |
   |                           |------------------------>|                    |
   |                           |                         |                    |
   |                           |                         |  (No Stripe call)  |
   |                           |                         |  Save order        |
   |                           |                         |                    |
   |                           |                         |  SignalR broadcast  |
   |                           |                         |------------------->|
   |                           |                         |  "OrderCreated"    |
   |                           |                         |                    |
   |                           |  { order, null, null }  |                    |
   |                           |<------------------------|                    |
   |                           |                         |                    |
   |  Clear cart               |                         |                    |
   |  Show victory video       |                         |  Order appears     |
   |<--------------------------|                         |  in kitchen queue  |
   |                           |                         |                    |
   |  (Pays at register        |                         |                    |
   |   when picking up)        |                         |                    |
```

### Order Status Workflow

```
Kitchen Staff               ASP.NET API              SignalR              Customer
   |                           |                       |                    |
   |  Tap order card           |                       |                    |
   |  (Received -> Preparing)  |                       |                    |
   |-------------------------->|                       |                    |
   |                           |                       |                    |
   |  PATCH /api/orders/{id}   |                       |                    |
   |  { status: Preparing }    |                       |                    |
   |                           |                       |                    |
   |                           |  Broadcast to:        |                    |
   |                           |  "kitchen" group      |                    |
   |                           |  "order-{id}" group   |                    |
   |                           |---------------------->|                    |
   |                           |                       |                    |
   |  Order moves to           |                       |  Status update     |
   |  "Preparing" column       |                       |  notification      |
   |<--------------------------|                       |------------------->|
   |                           |                       |                    |
   |  (repeat for Ready,       |                       |                    |
   |   then Completed)         |                       |                    |
```

---

## Environment Variables

### Server (.env)

| Variable | Purpose | Example |
|----------|---------|---------|
| `Stripe:SecretKey` | Server-side Stripe API key | `sk_test_...` |
| `Stripe:PublishableKey` | Client-side Stripe key (returned to frontend) | `pk_test_...` |
| `Stripe:WebhookSecret` | Validates webhook signatures | `whsec_...` |
| `JWT_KEY` | Signs JWT tokens (32+ chars) | `change-me-to-a-secure-32-char-key` |
| `CORS_ORIGINS` | Allowed frontend origins | `http://localhost:3000` |

### Frontend (.env.local)

| Variable | Purpose | Example |
|----------|---------|---------|
| `NEXT_PUBLIC_API_URL` | API base URL for fetch calls | `http://localhost:5082` |

Note: The Stripe publishable key is **not** stored in the frontend `.env`. It's returned dynamically from the `CreateOrderResponse` so it always matches what the server is using.

---

## Test Suite Documentation

The project has **51 test files** with **500+ test cases** across 5 testing frameworks.

### Overview by Category

| Category | Files | Framework | Coverage Area |
|----------|-------|-----------|---------------|
| Next.js Components | 8 | Vitest + React Testing Library | UI rendering, interactions |
| Next.js Utilities | 6 | Vitest | Helpers, formatters, math |
| Next.js Pages | 2 | Vitest | Checkout, cart workflows |
| Next.js State | 2 | Vitest | Auth and cart contexts |
| .NET Endpoints | 11 | xUnit | REST API CRUD, auth, validation |
| .NET Database | 1 | xUnit | Seeds, sequences, concurrency |
| .NET Services | 2 | xUnit | JWT tokens, Stripe config |
| .NET SignalR | 1 | xUnit | Real-time order updates |
| Blazor Components | 3 | bUnit | Cart drawer, cart service |
| Shared Validators | 8 | xUnit + FluentValidation | DTO input validation |
| Shared Helpers | 1 | xUnit | Unit conversions |
| Print Agent | 2 | xUnit | Receipt PDF generation |
| E2E | 4 | Playwright | Full browser workflows |

---

### Next.js Component Tests

**Location:** `src/next-client/src/__tests__/`

#### `item-card.test.tsx`

Tests the menu item card component:
- Renders item name and description
- Shows price range for items with multiple variants
- Shows single price for single-variant items
- Displays variant summary text
- Generates correct navigation link

#### `quantity-selector.test.tsx`

Tests quantity increment/decrement controls:
- Renders current quantity
- Increment button increases value
- Decrement button decreases value
- Respects minimum value constraint
- Respects maximum value constraint

#### `category-card.test.tsx`

Tests category card display:
- Renders category name
- Shows item count
- Displays correct emoji for category
- Generates correct navigation link

#### `header.test.tsx`

Tests header component:
- Renders brand name
- Shows empty cart state (no badge when cart empty)

#### `auth-guard.test.tsx`

Tests admin authentication guard:
- Redirects unauthenticated users to login
- Shows protected content for authenticated users
- Shows loading state during auth check

#### `bowl-builder.test.tsx`

Tests burrito bowl customization:
- Renders ingredient groups (Bases, Proteins, Toppings)
- Shows ingredient names and prices
- Calculates running total as ingredients are selected
- Quick-start buttons pre-select ingredient combos
- Default ingredients auto-selected on load

#### `fixed-item-detail.test.tsx`

Tests fixed-price item detail page:
- Renders item name and description
- Shows variant options with prices
- Shows available add-ons
- Calculates total with add-ons
- Special request textarea works

#### `order-card.test.tsx`

Tests admin order card:
- Renders order number
- Shows correct status hint ("Tap -> Start Preparing")
- Displays payment method badge
- Lists ingredients with quantities

---

### Next.js Utility Tests

#### `time-ago.test.ts`

Tests relative time formatting:
- "just now" for recent times
- "X minutes ago" formatting
- "X hours ago" formatting
- "X days ago" formatting

#### `cart-helpers.test.ts`

Tests cart pricing calculations:
- `getUnitPrice`: variant price + sum of ingredient prices
- `getLineTotal`: unit price * quantity
- `getDisplayName`: "Item Name (Variant)" format

#### `category-meta.test.ts`

Tests category emoji and description lookup:
- Returns correct emoji for each category name
- Returns correct description for each category
- Falls back gracefully for unknown categories

#### `utils.test.ts`

Tests utility functions:
- `generateId`: creates unique IDs
- `cn`: merges Tailwind class names correctly

#### `unit-label.test.ts`

Tests unit measurement formatting:
- Abbreviates units correctly (oz, lb, shots)
- Handles pluralization (1 shot vs 2 shots)

#### `ingredient-emoji.test.ts`

Tests ingredient-to-emoji mapping:
- Returns correct emoji for known ingredients
- Falls back for unknown ingredients

---

### Next.js Page/Integration Tests

#### `checkout-page.test.tsx`

Tests the checkout flow:
- Calls `api.createOrder` with PaymentMethod.Card
- Passes correct cart items to API
- Handles empty cart state
- Shows loading state while creating PaymentIntent

#### `cart-drawer.test.tsx`

Tests cart drawer UI:
- Renders cart items with correct prices
- Shows subtotal, tax, total calculations
- "Pay with Card" button navigates to /checkout
- "Pay In-Store" button calls createOrder with PaymentMethod.InStore
- Debounces rapid button clicks (prevents duplicate orders)

---

### Next.js State Tests

#### `auth-context.test.tsx`

Tests admin auth state:
- Login stores token and user data
- Logout clears token
- Token persists across page reloads (localStorage)
- Hydration from stored token on mount

#### `cart-context.test.tsx`

Tests cart state management:
- ADD_ITEM adds item to cart
- REMOVE_ITEM removes by ID
- UPDATE_QUANTITY changes quantity
- UPDATE_QUANTITY with 0 removes item
- CLEAR empties cart
- `subtotal` computed correctly
- `tax` = subtotal * 0.0825
- `total` = subtotal + tax
- Tax rounding edge cases (e.g., $0.004 rounds correctly)

---

### .NET Endpoint Tests

**Location:** `tests/MannaHp.Server.Tests/Endpoints/`

#### `OrderEndpointsTests.cs` (30+ tests)

**Fixed item pricing:**
- Creates order with single fixed-price variant
- Correct subtotal, tax, and total for single item
- Handles multiple quantities of same item
- Multiple different items in one order

**Customizable item pricing:**
- Bowl with selected ingredients sums prices correctly
- Multiple ingredients in different groups
- Handles duplicate ingredient selections
- Calculates correct totals for mixed orders

**Tax calculation:**
- Tax rate applied correctly (8.25%)
- Tax rounding to 2 decimal places
- Mixed order tax calculation

**Validation:**
- Rejects empty items list
- Rejects invalid MenuItemId
- Rejects variant that doesn't belong to menu item
- Requires either variant or ingredients

**Order retrieval:**
- GET /api/orders/{id} returns full order with items
- GET /api/orders/active filters out completed/cancelled
- Returns 404 for nonexistent order

**Status updates:**
- PATCH /api/orders/{id}/status updates correctly
- Requires Staff role authorization

#### `OrderWorkflowTests.cs`

Tests the complete order lifecycle:
- Received -> Preparing -> Ready -> Completed (happy path)
- Cancel from Received state
- Cancel from Preparing state
- Cannot skip statuses (Received -> Ready blocked)
- Cannot re-open Completed orders
- Cannot re-open Cancelled orders
- Active orders filter excludes Completed and Cancelled

#### `OrderPricingEdgeCaseTests.cs`

Tests edge cases:
- Order-level notes preserved on created order
- Item-level notes preserved on order items
- InStore payment creates order without Stripe call
- Card payment returns clientSecret
- Triple duplicate ingredients handled
- Multi-item order with complex tax rounding

#### `AuthorizationTests.cs`

Tests role-based access control:
- Staff cannot manage categories
- Staff cannot manage ingredients
- Staff cannot manage menu items
- Anonymous cannot view active orders
- Anonymous cannot update order status
- Owner can access everything

#### `AuthEndpointsTests.cs`

Tests authentication:
- Login with valid credentials returns JWT
- Login with wrong password returns 401
- Login with nonexistent email returns 401
- Token contains correct claims (email, role, name)
- Register creates Staff account (Owner-only)
- Register rejects duplicate email

#### `CategoryEndpointsTests.cs`

Tests category CRUD:
- Get all categories ordered by sort order
- Get category by ID
- Create category with valid data
- Update category
- Soft-delete category (sets Active=false)
- Validation: name required, max 100 chars

#### `MenuItemEndpointsTests.cs`

Tests menu item CRUD:
- Get all menu items with categories
- Get customizable item includes available ingredients
- Get fixed item includes variants and add-ons
- Create menu item with validation

#### `IngredientEndpointsTests.cs`

Tests ingredient CRUD:
- Get all ingredients ordered by name
- Get ingredient by ID
- Create ingredient with all fields

#### `VariantEndpointsTests.cs`

Tests variant CRUD:
- Get variants for menu item
- Create variant
- Update variant
- Soft-delete variant

#### `AvailableIngredientEndpointsTests.cs`

Tests available ingredient management:
- Get available ingredients for menu item
- Create available ingredient link
- Update available ingredient
- Soft-delete available ingredient

#### `RecipeIngredientEndpointsTests.cs`

Tests recipe ingredient management:
- Get recipe ingredients for variant
- Create recipe ingredient
- Update recipe ingredient
- Hard-delete recipe ingredient

---

### .NET Database Tests

#### `DatabaseTests.cs`

- Seed data: verifies 5 categories seeded
- Seed data: verifies 34+ ingredients seeded
- Order number sequence starts at 1001
- Order number auto-increments
- Soft-delete: deleted items not returned by default queries
- Concurrent order creation: no sequence conflicts under load

---

### .NET Service Tests

#### `TokenServiceTests.cs`

Tests JWT token generation:
- Creates valid JWT token
- Token contains email claim
- Token contains role claim
- Token contains display name claim
- Token has correct expiration
- Falls back to email when DisplayName is null

#### `StripeServiceTests.cs`

Tests Stripe service initialization:
- Reads configuration correctly
- Throws when SecretKey missing
- Throws when PublishableKey missing

---

### .NET SignalR Tests

#### `SignalRTests.cs`

Tests real-time order notifications:
- Kitchen group receives "OrderCreated" when order placed
- Kitchen group receives "OrderStatusChanged" when status updates
- Individual order group receives status updates
- Isolation: order-A group doesn't receive order-B updates
- Join/leave kitchen group works
- Multiple kitchen clients all receive broadcasts

---

### Blazor Component Tests

**Location:** `tests/MannaHp.Client.Tests/`

#### `CartDrawerTests.cs`

Tests Blazor cart drawer (legacy client):
- Empty state shows "Your cart is empty"
- Items render with name and price
- Subtotal calculated correctly
- Tax calculated at 8.25%
- Total = subtotal + tax
- OnChange subscription triggers re-render
- Add/remove/update items with live UI updates

#### `CartItemTests.cs`

Tests cart item model:
- Unit price for customizable item = sum of ingredient prices
- Unit price for fixed item = variant price
- Line total = unit price * quantity
- Display name includes variant name

#### `CartServiceTests.cs`

Tests cart service:
- AddItem adds to collection
- RemoveItem removes by ID
- UpdateQuantity changes quantity
- Clear empties all items
- OnChange event fires on mutations
- Zero quantity removes item
- Negative quantity removes item

---

### Shared Validator Tests

**Location:** `tests/MannaHp.Shared.Tests/Validators/`

#### `OrderValidatorsTests.cs`

- Valid order request passes
- Empty items list fails
- Invalid PaymentMethod enum fails
- Valid item request passes
- Empty MenuItemId fails
- Zero quantity fails
- Negative quantity fails
- Valid status update passes
- Invalid OrderStatus enum fails

#### `CategoryValidatorsTests.cs`

- Valid category passes
- Empty name fails
- Name over 100 chars fails
- Negative sort order fails

#### `MenuItemValidatorsTests.cs`

- Valid menu item passes
- Empty CategoryId fails
- Empty name fails
- Name over 100 chars fails
- Negative sort order fails

#### `IngredientValidatorsTests.cs`

- Valid ingredient passes
- Empty name fails
- Name over 100 chars fails
- Invalid UnitOfMeasure fails
- Negative cost fails
- Negative stock fails
- Negative threshold fails

#### `VariantValidatorsTests.cs`

- Valid variant passes
- Empty name fails
- Name over 50 chars fails
- Negative price fails
- Zero price allowed (for customizable bowls)
- Negative sort order fails

#### `AvailableIngredientValidatorsTests.cs`

- Valid available ingredient passes
- Empty IngredientId fails
- Negative customer price fails
- Zero quantity used fails
- Empty group name fails
- Group name over 50 chars fails
- Negative sort order fails

#### `RecipeIngredientValidatorsTests.cs`

- Valid recipe ingredient passes
- Empty IngredientId fails
- Zero quantity fails
- Negative quantity fails

---

### Shared Helper Tests

#### `UnitConversionTests.cs`

- Measurement type detection (weight, volume, count)
- Unit abbreviation formatting
- Conversion compatibility (oz <-> lb, cups <-> fl oz)
- Actual unit conversion values (16 oz = 1 lb, etc.)
- Incompatible conversion throws exception

---

### Print Agent Tests

**Location:** `tests/MannaHp.PrintAgent.Tests/`

#### `WorkerTests.cs`

- Filters unprinted orders (Printed=false)
- Marks orders as Printed=true after processing
- Generates PDF without throwing exceptions
- Uses in-memory database for isolation

#### `ReceiptDocumentTests.cs`

- Generates non-empty PDF byte array
- PDF contains order number
- Fixed item receipt shows variant name and price
- Customizable item receipt shows ingredient breakdown
- Receipt shows subtotal, tax, total
- Tax percentage displayed correctly (8.25%)
- Card payment shows brand and last 4 digits
- In-store payment shows "Pay at Register"

---

### E2E Tests (Playwright)

**Location:** `tests/MannaHp.E2E.Tests/`

#### `CustomerBrowseTests.cs`

- Homepage loads and shows categories
- Click category navigates to item list
- Click item shows detail page with price
- Bowl builder shows ingredient groups
- Bowl builder calculates running total

#### `AdminFlowTests.cs`

- Admin login succeeds with owner credentials
- Dashboard loads after login
- Active orders page displays
- Can update order status through workflow
- Can create category via admin API
- Can create ingredient via admin API

#### `CheckoutTests.cs`

- Cart drawer shows both payment buttons
- Cart displays items with correct prices
- "Pay with Card" navigates to /checkout
- Checkout page shows order summary
- Checkout page shows Stripe payment section
- Back button returns to cart
- "Pay In-Store" creates order and shows confirmation
- Rapid double-click on "Pay In-Store" only creates one order (debounce test)

#### `PriceConsistencyTests.cs` (10-phase comprehensive test)

Verifies price integrity across all layers:

1. **Source of truth**: Admin-set prices in database match expected values
2. **Menu API**: GET /api/menu returns correct prices
3. **Bowl builder UI**: Ingredient prices display correctly in browser
4. **Running total**: Bowl total updates correctly as ingredients are selected
5. **Cart line totals**: Cart computes correct per-item totals
6. **Order API**: POST /api/orders returns correct price snapshots
7. **Database snapshots**: order_items.unit_price matches expected value
8. **Stripe amount**: PaymentIntent amount in cents matches order total * 100
9. **Receipt PDF**: Generated receipt shows correct ingredient prices and totals
10. **Confirmation page**: /order/{id} displays correct prices to customer

Also verifies tax math integrity: `tax == round(subtotal * 0.0825, 2)` across all layers.
