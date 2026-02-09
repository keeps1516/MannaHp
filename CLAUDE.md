# Manna — Ordering App Technical Architecture

## Overview

A full-stack ordering system for Manna — a coffee and burrito bowl restaurant. Supports customer ordering (remote and in-store), real-time order queue management, credit card payments with saved cards, QR code in-store ordering, inventory tracking, menu management with ingredient-level costing, and automated receipt printing.

---

## Final Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WASM (PWA) |
| API | ASP.NET Core Minimal APIs |
| Auth | ASP.NET Core Identity + Google OAuth |
| Real-time | SignalR |
| Validation | FluentValidation (shared between client and server) |
| ORM | EF Core |
| Database | PostgreSQL |
| Payments | Stripe (saved cards via Customer objects) |
| PDF / Printing | QuestPDF + local Worker Service |
| UI Components | MudBlazor |
| Hosting | Docker Compose (self-hosted) |
| Hangfire | Docker Compose (self-hosted) |

---

## Project Structure

```
/src
  /Shared              ← Models, DTOs, Validators (referenced by both Client and Server)
  /Server              ← ASP.NET Core API, EF Core, SignalR Hub, Stripe, Identity
  /Client              ← Blazor WASM (customer, kitchen, admin views)
  /PrintAgent          ← Worker Service (QuestPDF, polls DB, prints receipts)

docker-compose.yml     ← PostgreSQL + Server + Client + PrintAgent
```

---

## Key Features & Architecture Decisions

### 1. Customer Ordering

- Blazor WASM served as a PWA — works on mobile browsers, no app store needed
- Customers can create an account via Google OAuth or order as a guest (in-store QR code)
- Menu displays items with descriptions and prices (calculated from ingredient costs + owner-defined margin)

### 2. Payment — Stripe

- **Stripe Elements** embedded in the Blazor app for card entry
- **Stripe Customer objects** store payment methods — returning customers don't re-enter card info
- When a user signs in for the first time, a Stripe Customer is created and linked to their app account
- PCI compliance is handled by Stripe — the app never touches raw card numbers
- Fees: ~2.9% + $0.30 per transaction (industry standard)

#### Payment Flow

1. Customer places order → API creates order and Stripe PaymentIntent
2. API returns `client_secret` to frontend
3. Frontend uses Stripe Elements to confirm payment (card info goes directly to Stripe)
4. Stripe sends webhook to API → order updated to `paid`
5. SignalR pushes update to kitchen display

### 3. QR Code In-Store Ordering (No Card Required)

- QR code posted at the counter/tables contains a URL: `https://manna.example.com/order?token=abc123`
- Token is a short-lived GUID stored in the database with an expiration (30–60 minutes)
- Orders placed via QR code are flagged as `PaymentMethod = InStore` — no card required
- Customer pays at the register when picking up (cash, card, whatever)
- Kitchen queue treats in-store and prepaid orders identically
- Implemented via a custom ASP.NET Core authorization handler that accepts either a valid JWT or a valid store token

### 4. Real-Time Order Queue — SignalR

**Order Status Flow:**

```
Received → Preparing → Ready → Completed
```

- Customer places order → API inserts order with status `Received`
- SignalR broadcasts to kitchen display
- Kitchen staff taps to move to `Preparing` → SignalR notifies customer
- Staff marks `Ready` → customer gets notified
- Customer picks up → staff marks `Completed`
- All connected clients (customer app, kitchen display, admin) stay in sync without polling

**SignalR Hub Groups:**

- `kitchen` — all kitchen staff subscribe to all orders
- `order-{orderId}` — individual customers track their specific order

### 5. Menu & Pricing Management

- **Menu items** have a name, description, and either a percentage margin or flat markup
- **Ingredients** have a name, unit of measurement, cost per unit, and current stock quantity
- **Recipes** link menu items to ingredients with quantities (e.g., burrito bowl uses 0.5 lb rice, 0.25 lb chicken)
- Selling price is calculated: `baseCost (sum of ingredient costs) + margin`
- Owner can set margin as percentage (e.g., 40%) or flat rate (e.g., +$3.00)

### 6. Inventory Tracking

- When an order is marked `Completed`, ingredient stock quantities are automatically decremented based on the recipe
- Low-stock thresholds can be set per ingredient — owner gets alerted when stock drops below threshold
- Owner manages ingredient costs and stock levels through the admin panel

### 7. Receipt Printing — QuestPDF + Worker Service

- A C# Worker Service runs on the shop machine (outside Docker for easy printer access)
- Polls the database every 3–5 seconds for orders where `Printed = false`
- Generates a PDF receipt using QuestPDF with full ingredient breakdown for burrito bowls
- Sends PDF to the local printer via `lpr` (Linux) or `System.Drawing.Printing` (Windows)
- Marks the order as `Printed = true` after successful print
- Recovers from downtime — picks up any unprinted orders on restart

#### Receipt Layout

```
================================
    MANNA
    123 Main St
    Date: 2/6/2026 10:30 AM
    Order #1042
================================

Burrito Bowl
  10oz Jasmine Rice         $3.00
  8oz Chicken               $3.00
  Lettuce                   $0.50
  Fresh Salsa               $0.50
  Shredded Cheese           $0.50
                    Item:   $7.50

Latte (16oz)                $5.25

Espresso Shot               $1.00

--------------------------------
Subtotal:                  $13.75
Tax (8.25%):                $1.13
Total:                     $14.88
Payment: Visa ***4242
================================
```

---

## Authentication & Authorization

### Authentication

- **ASP.NET Core Identity** for user management
- **Google OAuth** for customer sign-in (easy, most people have a Google account)
- **Facebook OAuth** available if needed (same NuGet pattern)
- **Guest/QR code flow** for in-store customers with no account
- JWT tokens issued after authentication

### Authorization

Three roles, three policies:

| Role | Access |
|---|---|
| Owner | Full access — menu, inventory, pricing, orders, reports |
| Staff | Order management — view/update order status, generate QR tokens |
| Customer | Place orders, view own order history |

**Policies:**

```
Endpoint                          Policy
─────────────────────────────────────────────
GET  /api/menu                    (anonymous)
POST /api/orders                  CanOrder (authenticated OR valid store token)
GET  /api/orders/mine             (authenticated)
GET  /api/orders/active           Staff
PATCH /api/orders/{id}/status     Staff
POST /api/menu-items              Owner
PUT  /api/ingredients             Owner
GET  /api/inventory               Owner
POST /api/generate-store-token    Staff
```

**Custom Authorization Handler** for the QR code flow: accepts either a valid JWT (logged-in user) or a valid store token header (`X-Store-Token`) for in-store guest ordering.

---

## Docker Compose Deployment

```yaml
services:
  postgres:
    image: postgres:16
    volumes:
      - pgdata:/var/lib/postgresql/data
    environment:
      POSTGRES_DB: restaurant
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  server:
    build: ./src/Server
    depends_on:
      - postgres
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=restaurant;Username=app;Password=${DB_PASSWORD}
      - Stripe__SecretKey=${STRIPE_SECRET_KEY}
      - Stripe__WebhookSecret=${STRIPE_WEBHOOK_SECRET}
      - Auth__Google__ClientId=${GOOGLE_CLIENT_ID}
      - Auth__Google__ClientSecret=${GOOGLE_CLIENT_SECRET}

  frontend:
    build: ./src/Client
    # Nginx serving the Blazor WASM build

  reverse-proxy:
    image: caddy
    ports:
      - "80:80"
      - "443:443"
    # Routes traffic to frontend, API, and handles TLS

volumes:
  pgdata:
```

**Print Agent** runs directly on the host machine (not in Docker) for easy printer access. Connects to PostgreSQL on `localhost:5432`.

---

## Infrastructure Considerations

### Backups

- PostgreSQL: `pg_dump` on a cron job to a separate drive or offsite location
- Stripe: All payment records are in Stripe's dashboard as a secondary source of truth

### Uptime

- UPS for the shop machine
- Docker containers set to `restart: unless-stopped`
- Print agent configured as a systemd service (Linux) or Windows Service for auto-start on boot

### DNS / TLS

- Required if customers order remotely from their phones
- Caddy handles Let's Encrypt certificate provisioning automatically
- Domain pointed to the shop's network (static IP or dynamic DNS)

### Stripe (Cloud Dependency)

- Stripe is the one service that always requires internet access
- Webhook endpoint must be reachable by Stripe (exposed through reverse proxy)
- In development, use Stripe CLI's `listen` command to forward webhooks locally

---

## Documentation Links

| Technology | URL |
|---|---|
| Blazor WASM | https://learn.microsoft.com/en-us/aspnet/core/blazor/ |
| ASP.NET Core Minimal APIs | https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis |
| ASP.NET Core Identity | https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity |
| Google OAuth (ASP.NET) | https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins |
| Facebook OAuth (ASP.NET) | https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/facebook-logins |
| ASP.NET Core Authorization | https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction |
| SignalR | https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction |
| FluentValidation | https://docs.fluentvalidation.net/ |
| EF Core | https://learn.microsoft.com/en-us/ef/core/ |
| EF Core + PostgreSQL (Npgsql) | https://www.npgsql.org/efcore/ |
| PostgreSQL | https://www.postgresql.org/docs/ |
| Stripe .NET SDK | https://docs.stripe.com/api?lang=dotnet |
| Stripe Elements | https://docs.stripe.com/payments/elements |
| Stripe Customer / Saved Cards | https://docs.stripe.com/payments/save-and-reuse |
| Stripe Webhooks | https://docs.stripe.com/webhooks |
| QuestPDF | https://www.questpdf.com/documentation/getting-started.html |
| MudBlazor | https://mudblazor.com/docs/overview |
| Docker Compose | https://docs.docker.com/compose/ |
| Caddy (reverse proxy) | https://caddyserver.com/docs/ |
| HangFire Background Processing (reverse proxy) | https://github.com/hangfire-postgres/Hangfire.PostgreSql |

---


### Design Decisions

- **Two pricing models in one app:**
  - **Customizable items (bowls):** price = sum of selected ingredient prices (no base price)
  - **Fixed items (coffee):** price set on the variant (Small $3, Medium $4, Large $5)
- **Ingredient groups:** customizable items organize ingredients into groups (Protein, Rice, Toppings, Extras) with no selection limits
- **Variants:** every menu item has at least one variant (even if just "Regular") — this keeps pricing and inventory logic consistent
- **Price snapshots:** `order_items.unit_price` and `order_item_ingredients.price_charged` capture the price at time of order so historical orders stay accurate when menu prices change
- **Tax:** single flat rate stored in `app_settings`, captured on each order
- **Inventory:** customizable items decrement from `order_item_ingredients.quantity_used`; fixed items decrement from `recipe_ingredients.quantity`


## Build Priority

1. **Menu / ingredient management** (admin CRUD) — foundation for everything
2. **Order placement API + basic frontend** — get orders flowing
3. **Stripe integration** — accept payments
4. **SignalR queue display** — kitchen and customer real-time status
5. **QR code in-store flow** — the pay-later path
6. **Inventory tracking** — auto-decrement on order completion, low-stock alerts
7. **Receipt printing** — QuestPDF + Worker Service
8. **Google OAuth** — streamlined customer sign-in
