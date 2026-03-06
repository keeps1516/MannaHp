# Browser Bug Review - 2026-03-06

Environment:
- Tested against `http://localhost:3000/`
- Observed through a real headless browser session, using the site as a customer and admin user

## Findings

### 1. High: many menu thumbnails are broken in the customer menu

Observed behavior:
- The `Traditional Drinks` category renders repeated broken image requests while the page loads.
- The page still shows the items, but many thumbnails are missing because Next image optimization is returning `400`.

Observed failing requests:
- `/_next/image?url=%2Fmenu%2Fwhite-mocha.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fflavored-latte.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fcaramel-latte.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Ficed-coffee.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fcold-brew.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Ficed-latte.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Faffogato.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fblended-mocha.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fblended-caramel.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fsmoothie.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fhot-chocolate.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fsteamer.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Ftea.jpg&w=64&q=75`
- `/_next/image?url=%2Fmenu%2Fchai-latte.jpg&w=64&q=75`

Impact:
- Customers see missing product thumbnails in the menu.
- The page emits a large number of `400` responses during normal browsing.

### 2. Medium: tax labels are inconsistent across the customer flow

Observed behavior for the same Latte order:
- Cart drawer showed `Tax (8.00%)` and total `$5.13`
- Order confirmation showed `Tax (8.00%)`
- Checkout showed `Tax (8.25%)` for the same order and same dollar amount

Observed API response:
- `GET http://localhost:5082/api/settings/public` returned `{"taxRate":0.08}`

Impact:
- Customers are shown conflicting tax rates during a single order flow.
- The dollar amount stayed the same, so the UI is internally inconsistent rather than clearly wrong in one place.

Likely root cause:
- Checkout appears to hardcode `8.25%` while the rest of the app is using the live `0.08` setting.

### 3. Medium: card checkout is presented as available, then fails with an unhelpful message

Observed behavior:
- The cart drawer shows a normal `Pay with Card` button.
- Clicking it navigates to checkout.
- Checkout eventually fails with `Failed to create order. Please try again.`

Observed API response:
- `POST http://localhost:5082/api/orders`
- Response: `422`
- Body: `{"error":"Card payments are not yet available. Stripe is not configured."}`

Impact:
- Customers are allowed into a dead-end checkout path even though card payments are unavailable.
- The UI hides the actionable server message and replaces it with a generic failure string.

Recommended fix:
- Either disable/hide `Pay with Card` when Stripe is unavailable, or surface the server’s actual `422` message in checkout.

## Flows exercised

- Homepage load
- Category browsing
- Item detail page for Latte
- Add to cart
- Cart drawer
- Card checkout attempt
- In-store checkout and order confirmation
- Admin login and dashboard load

## Notes

- Admin login and dashboard loaded successfully during this pass.
- In-store ordering completed successfully during this pass.
