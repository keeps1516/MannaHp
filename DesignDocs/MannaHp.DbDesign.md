## Database Schema
### Tables

```sql
-- =============================================
-- CATEGORIES
-- =============================================
CREATE TABLE categories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL,       -- "Bowls", "Coffee", "Sides"
    sort_order      INT NOT NULL DEFAULT 0,
    active          BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================
-- INGREDIENTS (raw inventory)
-- =============================================
-- UnitOfMeasure enum stored as int:
--   0=Oz, 1=Lb, 2=Cups, 3=FlOz, 4=Tsp, 5=Tbsp, 6=Each, 7=Shot
-- MeasurementType groups (for conversion validation):
--   Weight: Oz, Lb
--   Volume: Cups, FlOz, Tsp, Tbsp
--   Count:  Each, Shot
CREATE TABLE ingredients (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                VARCHAR(100) NOT NULL,       -- "Chicken", "White Rice", "Oat Milk"
    unit                INT NOT NULL,                 -- UnitOfMeasure enum (0=Oz, 1=Lb, etc.)
    cost_per_unit       DECIMAL(10,4) NOT NULL,       -- what the restaurant pays
    stock_quantity      DECIMAL(10,2) NOT NULL DEFAULT 0,
    low_stock_threshold DECIMAL(10,2),
    active              BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================
-- MENU ITEMS
-- =============================================
CREATE TABLE menu_items (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id       UUID NOT NULL REFERENCES categories(id),
    name              VARCHAR(100) NOT NULL,       -- "Burrito Bowl", "Coffee"
    description       TEXT,
    is_customizable   BOOLEAN NOT NULL DEFAULT FALSE,  -- TRUE = customer picks ingredients
    active            BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order        INT NOT NULL DEFAULT 0,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================
-- MENU ITEM VARIANTS (sizes)
-- Every menu item has at least one variant.
-- Fixed items: Small $3.00, Medium $4.00, Large $5.00
-- Customizable items: "Regular" variant (price comes from ingredients)
-- =============================================
CREATE TABLE menu_item_variants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    menu_item_id    UUID NOT NULL REFERENCES menu_items(id),
    name            VARCHAR(50) NOT NULL,          -- "Small", "Medium", "Large", "Regular"
    price           DECIMAL(10,2) NOT NULL DEFAULT 0, -- selling price (0 for customizable)
    sort_order      INT NOT NULL DEFAULT 0,
    active          BOOLEAN NOT NULL DEFAULT TRUE
);

-- =============================================
-- RECIPE INGREDIENTS (fixed recipes for non-customizable items)
-- Tied to variant so "Medium Coffee" and "Large Coffee" use different amounts.
-- Used for inventory decrement on fixed items.
-- =============================================
CREATE TABLE recipe_ingredients (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    variant_id      UUID NOT NULL REFERENCES menu_item_variants(id),
    ingredient_id   UUID NOT NULL REFERENCES ingredients(id),
    quantity        DECIMAL(10,4) NOT NULL          -- how much of the ingredient per item
);

-- =============================================
-- MENU ITEM AVAILABLE INGREDIENTS (for customizable items)
-- Defines what a customer can pick and what they pay.
-- Grouped by category (Protein, Rice, Toppings, Extras).
-- No selection limits — customer can pick whatever they want.
-- =============================================
CREATE TABLE menu_item_available_ingredients (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    menu_item_id    UUID NOT NULL REFERENCES menu_items(id),
    ingredient_id   UUID NOT NULL REFERENCES ingredients(id),
    customer_price  DECIMAL(10,2) NOT NULL,        -- what the customer pays for this ingredient
    quantity_used   DECIMAL(10,4) NOT NULL,         -- how much stock to decrement (e.g., 4oz chicken)
    is_default      BOOLEAN NOT NULL DEFAULT FALSE, -- pre-selected in the UI
    group_name      VARCHAR(50) NOT NULL,            -- "Protein", "Rice", "Toppings", "Extras"
    sort_order      INT NOT NULL DEFAULT 0,
    active          BOOLEAN NOT NULL DEFAULT TRUE
);

-- =============================================
-- ORDERS
-- =============================================
CREATE TABLE orders (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           VARCHAR(450) REFERENCES asp_net_users(id),  -- nullable for guest/QR orders
    status            VARCHAR(20) NOT NULL DEFAULT 'received'
                        CHECK (status IN ('received','preparing','ready','completed','cancelled')),
    payment_method    VARCHAR(20) NOT NULL
                        CHECK (payment_method IN ('card','in_store')),
    payment_status    VARCHAR(20) NOT NULL DEFAULT 'pending'
                        CHECK (payment_status IN ('pending','paid','failed','refunded')),
    stripe_payment_id VARCHAR(255),
    subtotal          DECIMAL(10,2) NOT NULL,
    tax_rate          DECIMAL(5,4) NOT NULL,        -- snapshot of rate at time of order (e.g., 0.0825)
    tax               DECIMAL(10,2) NOT NULL,
    total             DECIMAL(10,2) NOT NULL,
    printed           BOOLEAN NOT NULL DEFAULT FALSE,
    notes             TEXT,                          -- customer special instructions
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================
-- ORDER ITEMS
-- =============================================
CREATE TABLE order_items (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id            UUID NOT NULL REFERENCES orders(id),
    menu_item_id        UUID NOT NULL REFERENCES menu_items(id),
    variant_id          UUID REFERENCES menu_item_variants(id),
    quantity            INT NOT NULL DEFAULT 1,
    unit_price          DECIMAL(10,2) NOT NULL,       -- snapshot: price at time of order
    total_price         DECIMAL(10,2) NOT NULL,       -- unit_price * quantity
    notes               TEXT                           -- "no ice", "extra hot"
);

-- =============================================
-- ORDER ITEM INGREDIENTS (customer selections for customizable items)
-- Only populated for customizable menu items (is_customizable = TRUE).
-- This is what prints on the receipt with per-ingredient costs.
-- =============================================
CREATE TABLE order_item_ingredients (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_item_id   UUID NOT NULL REFERENCES order_items(id),
    ingredient_id   UUID NOT NULL REFERENCES ingredients(id),
    quantity_used   DECIMAL(10,4) NOT NULL,         -- snapshot: for inventory decrement
    price_charged   DECIMAL(10,2) NOT NULL          -- snapshot: what customer paid
);

-- =============================================
-- STORE TOKENS (QR code in-store ordering)
-- =============================================
CREATE TABLE store_tokens (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    token       VARCHAR(64) NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================
-- APP SETTINGS (key-value config)
-- =============================================
CREATE TABLE app_settings (
    key         VARCHAR(50) PRIMARY KEY,
    value       TEXT NOT NULL
);

INSERT INTO app_settings (key, value) VALUES ('tax_rate', '0.0825');
INSERT INTO app_settings (key, value) VALUES ('store_name', 'Manna');
INSERT INTO app_settings (key, value) VALUES ('store_address', '123 Main St');  -- update with actual address
```

### Indexes

```sql
-- Order queries
CREATE INDEX idx_orders_status ON orders(status) WHERE status != 'completed';
CREATE INDEX idx_orders_user ON orders(user_id);
CREATE INDEX idx_orders_created ON orders(created_at);
CREATE INDEX idx_orders_unprinted ON orders(printed) WHERE printed = FALSE;

-- Order item lookups
CREATE INDEX idx_order_items_order ON order_items(order_id);
CREATE INDEX idx_order_item_ingredients_order_item ON order_item_ingredients(order_item_id);

-- Menu lookups
CREATE INDEX idx_menu_items_category ON menu_items(category_id);
CREATE INDEX idx_menu_item_variants_menu_item ON menu_item_variants(menu_item_id);
CREATE INDEX idx_recipe_ingredients_variant ON recipe_ingredients(variant_id);
CREATE INDEX idx_available_ingredients_menu_item ON menu_item_available_ingredients(menu_item_id);

-- Store tokens
CREATE INDEX idx_store_tokens_token ON store_tokens(token);
CREATE INDEX idx_store_tokens_expires ON store_tokens(expires_at);

-- Inventory alerts
CREATE INDEX idx_ingredients_low_stock ON ingredients(stock_quantity, low_stock_threshold)
    WHERE low_stock_threshold IS NOT NULL;
```

### Pricing Logic

**Customizable item (Burrito Bowl):**

```
Customer picks: 10oz Jasmine Rice ($3.00), 8oz Chicken ($3.00),
                Lettuce ($0.50), Fresh Salsa ($0.50), Shredded Cheese ($0.50)

unit_price = SUM(customer_price for each selected ingredient)
           = $3.00 + $3.00 + $0.50 + $0.50 + $0.50
           = $7.50

order_items row:       unit_price = $7.50, quantity = 1, total_price = $7.50
order_item_ingredients: one row per ingredient with price_charged snapshot
```

**Fixed item with variant (16oz Latte):**

```
unit_price = menu_item_variants.price = $5.25

order_items row:       unit_price = $5.25, quantity = 1, total_price = $5.25
order_item_ingredients: NOT populated (fixed recipe)
inventory decrement:    uses recipe_ingredients tied to the 16oz Latte variant
```

**Add-on items (Espresso Shot, Flavor Shot):**

```
These are standalone menu items in the "Add-Ons" category.
Customer adds them as separate order_items alongside their drink.

unit_price = menu_item_variants.price = $1.00 (Espresso Shot)
```

**Order totals:**

```
subtotal = SUM(order_items.total_price)
tax      = subtotal * tax_rate
total    = subtotal + tax
```

### Inventory Decrement Logic

When order status → `completed`:

```
FOR EACH order_item:
  IF menu_item.is_customizable:
    -- Use what the customer actually picked
    FOR EACH order_item_ingredient:
      ingredients.stock_quantity -= order_item_ingredient.quantity_used * order_item.quantity

  ELSE:
    -- Use the fixed recipe
    FOR EACH recipe_ingredient WHERE variant_id = order_item.variant_id:
      ingredients.stock_quantity -= recipe_ingredient.quantity * order_item.quantity
```

### Sample Data (Manna Menu)

```sql
-- =============================================
-- CATEGORIES
-- =============================================
INSERT INTO categories (id, name, sort_order) VALUES
  ('cat-bowls',     'Bowls',              1),
  ('cat-drinks',    'Traditional Drinks', 2),
  ('cat-seasonal',  'Seasonal Specials',  3),
  ('cat-sides',     'Sides & Drinks',     4),
  ('cat-addons',    'Add-Ons',            5);

-- =============================================
-- INGREDIENTS (raw inventory with estimated costs)
-- =============================================
INSERT INTO ingredients (id, name, unit, cost_per_unit, stock_quantity, low_stock_threshold) VALUES
  -- Bowl: Bases
  ('ing-jrice',      'Jasmine Rice',      'oz', 0.0400, 500, 80),    -- ~$0.64/lb
  ('ing-beans',      'Beans',             'oz', 0.0350, 400, 64),    -- ~$0.56/lb

  -- Bowl: Proteins
  ('ing-gbeef',      'Ground Beef',       'oz', 0.3750, 300, 48),    -- ~$6/lb
  ('ing-chicken',    'Chicken',           'oz', 0.3125, 400, 64),    -- ~$5/lb
  ('ing-sausqueso',  'Sausage Queso',     'oz', 0.3500, 200, 32),    -- ~$5.60/lb

  -- Bowl: Fresh Toppings
  ('ing-lettuce',    'Lettuce',           'oz', 0.0625, 200, 32),    -- ~$1/lb
  ('ing-tomatoes',   'Tomatoes',          'oz', 0.1250, 200, 32),    -- ~$2/lb
  ('ing-jalapenos',  'Fresh Jalapenos',   'oz', 0.0750, 150, 24),    -- ~$1.20/lb
  ('ing-salsa',      'Fresh Salsa',       'oz', 0.1000, 200, 32),    -- ~$1.60/lb
  ('ing-shrcheese',  'Shredded Cheese',   'oz', 0.2500, 200, 32),    -- ~$4/lb

  -- Sides
  ('ing-chips',      'Tortilla Chips',    'oz', 0.0800, 300, 48),
  ('ing-lgqueso',    'Large Queso',       'oz', 0.1500, 200, 32),

  -- Drinks: Base Ingredients
  ('ing-coffee',     'Coffee (brewed)',   'oz', 0.0500, 600, 96),
  ('ing-espresso',   'Espresso',          'shot', 0.3000, 500, 80),
  ('ing-milk',       'Whole Milk',        'oz', 0.0350, 500, 80),    -- ~$0.56/lb
  ('ing-altmilk',    'Alternative Milk',  'oz', 0.0625, 300, 48),    -- oat/almond
  ('ing-chocolate',  'Chocolate Syrup',   'oz', 0.1500, 200, 32),
  ('ing-whtchoc',    'White Choc Syrup',  'oz', 0.1500, 150, 24),
  ('ing-caramel',    'Caramel Syrup',     'oz', 0.1500, 200, 32),
  ('ing-flvsyrup',   'Flavored Syrup',    'oz', 0.1200, 200, 32),
  ('ing-chai',       'Chai Concentrate',  'oz', 0.1000, 200, 32),
  ('ing-smoothie',   'Smoothie Base',     'oz', 0.0800, 200, 32),
  ('ing-hotchocmix', 'Hot Chocolate Mix', 'oz', 0.1200, 200, 32),
  ('ing-tea',        'Tea (sachet)',       'each', 0.2000, 300, 50),
  ('ing-whipcream',  'Whipped Cream',     'oz', 0.1000, 150, 24),
  ('ing-icecream',   'Vanilla Ice Cream', 'oz', 0.1500, 100, 16),
  ('ing-cider',      'Apple Cider',       'oz', 0.0800, 200, 32),
  ('ing-cinnamon',   'Cinnamon',          'tsp', 0.0500, 100, 16),
  ('ing-pumpkin',    'Pumpkin Spice Syrup','oz', 0.1500, 100, 16),
  ('ing-maple',      'Maple Brown Sugar Syrup','oz', 0.1500, 100, 16),
  ('ing-marshmallow','Marshmallow Syrup', 'oz', 0.1500, 100, 16),
  ('ing-peppermint', 'Peppermint Syrup',  'oz', 0.1500, 100, 16),
  ('ing-gingerbread','Gingerbread Syrup', 'oz', 0.1500, 100, 16),
  ('ing-ice',        'Ice',               'oz', 0.0100, 9999, NULL);

-- =============================================
-- MENU ITEMS
-- =============================================
INSERT INTO menu_items (id, category_id, name, description, is_customizable, sort_order) VALUES
  -- Bowls (customizable)
  ('mi-bowl',           'cat-bowls',    'Burrito Bowl',           'Build your own bowl',                    TRUE,  1),

  -- Traditional Drinks (fixed)
  ('mi-drip',           'cat-drinks',   'Drip Coffee',            'Fresh batch brew',                       FALSE, 1),
  ('mi-aulait',         'cat-drinks',   'Café au Lait',           '½ drip + ½ steamed milk',                FALSE, 2),
  ('mi-espresso',       'cat-drinks',   'Espresso',               'Shot of espresso',                       FALSE, 3),
  ('mi-americano',      'cat-drinks',   'Americano',              'Espresso + hot water',                   FALSE, 4),
  ('mi-caramac',        'cat-drinks',   'Caramel Macchiato',      'Layered caramel espresso drink',         FALSE, 5),
  ('mi-cortado',        'cat-drinks',   'Cortado',                '1:1 espresso : steamed milk',            FALSE, 6),
  ('mi-cappuccino',     'cat-drinks',   'Cappuccino',             'Espresso + airy foam',                   FALSE, 7),
  ('mi-latte',          'cat-drinks',   'Latte',                  'Espresso + steamed milk',                FALSE, 8),
  ('mi-mocha',          'cat-drinks',   'Mocha',                  'Latte + chocolate syrup',                FALSE, 9),
  ('mi-whtmocha',       'cat-drinks',   'White Mocha',            'Latte + white chocolate syrup',          FALSE, 10),
  ('mi-flvlatte',       'cat-drinks',   'Flavored Latte',         'Latte + flavored syrup',                 FALSE, 11),
  ('mi-caralatte',      'cat-drinks',   'Caramel Latte',          'Latte + caramel syrup',                  FALSE, 12),
  ('mi-icedcoffee',     'cat-drinks',   'Iced Coffee',            'Chilled drip over ice',                  FALSE, 13),
  ('mi-coldbrew',       'cat-drinks',   'Cold Brew',              '16-18 hr brew, over ice',                FALSE, 14),
  ('mi-icedlatte',      'cat-drinks',   'Iced Latte',             'Espresso + milk over ice',               FALSE, 15),
  ('mi-affogato',       'cat-drinks',   'Affogato',               'Espresso over vanilla ice cream',        FALSE, 16),
  ('mi-blendmocha',     'cat-drinks',   'Blended Mocha',          'Espresso, milk, chocolate, ice',         FALSE, 17),
  ('mi-blendcaramel',   'cat-drinks',   'Blended Caramel',        'Espresso, milk, caramel, ice',           FALSE, 18),
  ('mi-smoothie',       'cat-drinks',   'Smoothie',               'Smoothie base + fruit flavor',           FALSE, 19),
  ('mi-hotchoc',        'cat-drinks',   'Hot Chocolate',          'Steamed milk + chocolate',               FALSE, 20),
  ('mi-steamer',        'cat-drinks',   'Steamer',                'Steamed milk + syrup',                   FALSE, 21),
  ('mi-tea',            'cat-drinks',   'Tea',                    'Premium sachet or pot',                  FALSE, 22),
  ('mi-chailatte',      'cat-drinks',   'Chai Latte',             'Chai + steamed milk',                    FALSE, 23),

  -- Seasonal Specials (fixed)
  ('mi-pumpkin',        'cat-seasonal', 'Pumpkin Spice Latte',    'Latte + pumpkin spice syrup + whipped cream',         FALSE, 1),
  ('mi-maple',          'cat-seasonal', 'Maple Brown Sugar Latte','Latte + pumpkin spice syrup + whipped cream',         FALSE, 2),
  ('mi-marshmallow',    'cat-seasonal', 'Toasted Marshmallow Mocha','Mocha + toasted marshmallow syrup',                 FALSE, 3),
  ('mi-pepmocha',       'cat-seasonal', 'Peppermint Mocha',       'Mocha + peppermint syrup + whipped cream',            FALSE, 4),
  ('mi-gingerbread',    'cat-seasonal', 'Gingerbread Latte',      'Latte + gingerbread syrup',                           FALSE, 5),
  ('mi-applecider',     'cat-seasonal', 'Hot Apple Cider',        'Steamed cider + cinnamon',                            FALSE, 6),

  -- Sides & Drinks (fixed)
  ('mi-chips',          'cat-sides',    'Side of Chips',          NULL,                                     FALSE, 1),
  ('mi-chipsqueso',     'cat-sides',    'Chips & Salsa with 13oz Large Queso', NULL,                        FALSE, 2),

  -- Add-Ons (fixed, single variant each)
  ('mi-flvshot',        'cat-addons',   'Flavor Shot',            NULL,                                     FALSE, 1),
  ('mi-espshot',        'cat-addons',   'Espresso Shot',          NULL,                                     FALSE, 2),
  ('mi-whipcream',      'cat-addons',   'Whipped Cream',          NULL,                                     FALSE, 3),
  ('mi-altmilk',        'cat-addons',   'Alternative Milk',       NULL,                                     FALSE, 4);

-- =============================================
-- MENU ITEM VARIANTS (sizes and prices)
-- =============================================
INSERT INTO menu_item_variants (id, menu_item_id, name, price, sort_order) VALUES
  -- Bowl
  ('var-bowl-reg',         'mi-bowl',          'Regular',   0.00,  1),

  -- Drip Coffee: 12oz $2.25, 16oz $2.75, Pot $4.00
  ('var-drip-12',          'mi-drip',          '12oz',      2.25,  1),
  ('var-drip-16',          'mi-drip',          '16oz',      2.75,  2),
  ('var-drip-pot',         'mi-drip',          'Pot',       4.00,  3),

  -- Café au Lait: 12oz $3.50, 16oz $4.00
  ('var-aulait-12',        'mi-aulait',        '12oz',      3.50,  1),
  ('var-aulait-16',        'mi-aulait',        '16oz',      4.00,  2),

  -- Espresso: Single $2.00, Double $3.00
  ('var-espresso-single',  'mi-espresso',      'Single',    2.00,  1),
  ('var-espresso-double',  'mi-espresso',      'Double',    3.00,  2),

  -- Americano: 12oz $3.25, 16oz $3.75
  ('var-americano-12',     'mi-americano',     '12oz',      3.25,  1),
  ('var-americano-16',     'mi-americano',     '16oz',      3.75,  2),

  -- Caramel Macchiato: 12oz $5.50, 16oz $6.00
  ('var-caramac-12',       'mi-caramac',       '12oz',      5.50,  1),
  ('var-caramac-16',       'mi-caramac',       '16oz',      6.00,  2),

  -- Cortado: 12oz $2.25, 8oz $2.75
  ('var-cortado-12',       'mi-cortado',       '12oz',      2.25,  1),
  ('var-cortado-8',        'mi-cortado',       '8oz',       2.75,  2),

  -- Cappuccino: 6oz $4.25, 8oz $4.50
  ('var-cappuccino-6',     'mi-cappuccino',    '6oz',       4.25,  1),
  ('var-cappuccino-8',     'mi-cappuccino',    '8oz',       4.50,  2),

  -- Latte: 12oz $4.75, 16oz $5.25
  ('var-latte-12',         'mi-latte',         '12oz',      4.75,  1),
  ('var-latte-16',         'mi-latte',         '16oz',      5.25,  2),

  -- Mocha: 12oz $5.25, 16oz $5.75
  ('var-mocha-12',         'mi-mocha',         '12oz',      5.25,  1),
  ('var-mocha-16',         'mi-mocha',         '16oz',      5.75,  2),

  -- White Mocha: 12oz $5.25, 16oz $5.75
  ('var-whtmocha-12',      'mi-whtmocha',      '12oz',      5.25,  1),
  ('var-whtmocha-16',      'mi-whtmocha',      '16oz',      5.75,  2),

  -- Flavored Latte: 12oz $5.00, 16oz $5.50
  ('var-flvlatte-12',      'mi-flvlatte',      '12oz',      5.00,  1),
  ('var-flvlatte-16',      'mi-flvlatte',      '16oz',      5.50,  2),

  -- Caramel Latte: 12oz $5.25, 16oz $5.75
  ('var-caralatte-12',     'mi-caralatte',     '12oz',      5.25,  1),
  ('var-caralatte-16',     'mi-caralatte',     '16oz',      5.75,  2),

  -- Iced Coffee: 12oz $2.25, 16oz $2.75
  ('var-icedcoffee-12',    'mi-icedcoffee',    '12oz',      2.25,  1),
  ('var-icedcoffee-16',    'mi-icedcoffee',    '16oz',      2.75,  2),

  -- Cold Brew: 12oz $4.25, 16oz $4.75
  ('var-coldbrew-12',      'mi-coldbrew',      '12oz',      4.25,  1),
  ('var-coldbrew-16',      'mi-coldbrew',      '16oz',      4.75,  2),

  -- Iced Latte: 12oz $5.25, 16oz $5.75
  ('var-icedlatte-12',     'mi-icedlatte',     '12oz',      5.25,  1),
  ('var-icedlatte-16',     'mi-icedlatte',     '16oz',      5.75,  2),

  -- Affogato: 8oz $6.50
  ('var-affogato-8',       'mi-affogato',      '8oz',       6.50,  1),

  -- Blended Mocha: 16oz $5.75, 24oz $6.50
  ('var-blendmocha-16',    'mi-blendmocha',    '16oz',      5.75,  1),
  ('var-blendmocha-24',    'mi-blendmocha',    '24oz',      6.50,  2),

  -- Blended Caramel: 16oz $5.75, 24oz $6.50
  ('var-blendcaramel-16',  'mi-blendcaramel',  '16oz',      5.75,  1),
  ('var-blendcaramel-24',  'mi-blendcaramel',  '24oz',      6.50,  2),

  -- Smoothie: 16oz $5.50, 24oz $6.25
  ('var-smoothie-16',      'mi-smoothie',      '16oz',      5.50,  1),
  ('var-smoothie-24',      'mi-smoothie',      '24oz',      6.25,  2),

  -- Hot Chocolate: 4oz $1.50, 12oz $3.50, 16oz $4.00
  ('var-hotchoc-4',        'mi-hotchoc',       '4oz',       1.50,  1),
  ('var-hotchoc-12',       'mi-hotchoc',       '12oz',      3.50,  2),
  ('var-hotchoc-16',       'mi-hotchoc',       '16oz',      4.00,  3),

  -- Steamer: 12oz $3.25, 16oz $3.75
  ('var-steamer-12',       'mi-steamer',       '12oz',      3.25,  1),
  ('var-steamer-16',       'mi-steamer',       '16oz',      3.75,  2),

  -- Tea: 12oz $2.25, 16oz $2.75, Pot $4.00
  ('var-tea-12',           'mi-tea',           '12oz',      2.25,  1),
  ('var-tea-16',           'mi-tea',           '16oz',      2.75,  2),
  ('var-tea-pot',          'mi-tea',           'Pot',       4.00,  3),

  -- Chai Latte: 12oz $4.75, 16oz $5.25
  ('var-chailatte-12',     'mi-chailatte',     '12oz',      4.75,  1),
  ('var-chailatte-16',     'mi-chailatte',     '16oz',      5.25,  2),

  -- Seasonal: Pumpkin Spice Latte: 12oz $5.75, 16oz $6.25
  ('var-pumpkin-12',       'mi-pumpkin',       '12oz',      5.75,  1),
  ('var-pumpkin-16',       'mi-pumpkin',       '16oz',      6.25,  2),

  -- Seasonal: Maple Brown Sugar Latte: 12oz $5.75, 16oz $6.25
  ('var-maple-12',         'mi-maple',         '12oz',      5.75,  1),
  ('var-maple-16',         'mi-maple',         '16oz',      6.25,  2),

  -- Seasonal: Toasted Marshmallow Mocha: 12oz $5.75, 16oz $6.25
  ('var-marshmallow-12',   'mi-marshmallow',   '12oz',      5.75,  1),
  ('var-marshmallow-16',   'mi-marshmallow',   '16oz',      6.25,  2),

  -- Seasonal: Peppermint Mocha: 12oz $5.75, 16oz $6.25
  ('var-pepmocha-12',      'mi-pepmocha',      '12oz',      5.75,  1),
  ('var-pepmocha-16',      'mi-pepmocha',      '16oz',      6.25,  2),

  -- Seasonal: Gingerbread Latte: 12oz $5.75, 16oz $6.25
  ('var-gingerbread-12',   'mi-gingerbread',   '12oz',      5.75,  1),
  ('var-gingerbread-16',   'mi-gingerbread',   '16oz',      6.25,  2),

  -- Seasonal: Hot Apple Cider: 12oz $4.50, 16oz $5.00
  ('var-applecider-12',    'mi-applecider',    '12oz',      4.50,  1),
  ('var-applecider-16',    'mi-applecider',    '16oz',      5.00,  2),

  -- Sides
  ('var-chips',            'mi-chips',         'Regular',   1.50,  1),
  ('var-chipsqueso',       'mi-chipsqueso',    'Regular',   6.00,  1),

  -- Add-Ons
  ('var-flvshot',          'mi-flvshot',       'Regular',   0.75,  1),
  ('var-espshot',          'mi-espshot',       'Regular',   1.00,  1),
  ('var-whipcream',        'mi-whipcream',     'Regular',   0.50,  1),
  ('var-altmilk',          'mi-altmilk',       'Regular',   0.75,  1);

-- =============================================
-- AVAILABLE INGREDIENTS FOR BURRITO BOWL
-- =============================================
INSERT INTO menu_item_available_ingredients
  (menu_item_id, ingredient_id, customer_price, quantity_used, group_name, sort_order) VALUES
  -- Bases
  ('mi-bowl', 'ing-jrice',     3.00, 10.0, 'Bases',          1),   -- 10oz Jasmine Rice
  ('mi-bowl', 'ing-beans',     2.00,  5.0, 'Bases',          2),   -- 5oz Beans

  -- Proteins
  ('mi-bowl', 'ing-gbeef',     3.00,  6.0, 'Proteins',       1),   -- 6oz Ground Beef
  ('mi-bowl', 'ing-chicken',   3.00,  8.0, 'Proteins',       2),   -- 8oz Chicken
  ('mi-bowl', 'ing-sausqueso', 3.00,  6.0, 'Proteins',       3),   -- 6oz Sausage Queso

  -- Fresh Toppings
  ('mi-bowl', 'ing-lettuce',   0.50,  2.0, 'Fresh Toppings', 1),
  ('mi-bowl', 'ing-tomatoes',  0.50,  2.0, 'Fresh Toppings', 2),
  ('mi-bowl', 'ing-jalapenos', 0.25,  1.0, 'Fresh Toppings', 3),
  ('mi-bowl', 'ing-salsa',     0.50,  2.0, 'Fresh Toppings', 4),
  ('mi-bowl', 'ing-shrcheese', 0.50,  1.5, 'Fresh Toppings', 5);

-- =============================================
-- RECIPE INGREDIENTS (fixed recipes for drinks)
-- Representative recipes — full set would be defined via admin panel
-- =============================================

-- Drip Coffee
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-drip-12',  'ing-coffee', 12.0),
  ('var-drip-16',  'ing-coffee', 16.0),
  ('var-drip-pot', 'ing-coffee', 48.0);

-- Café au Lait
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-aulait-12', 'ing-coffee', 6.0),
  ('var-aulait-12', 'ing-milk',   6.0),
  ('var-aulait-16', 'ing-coffee', 8.0),
  ('var-aulait-16', 'ing-milk',   8.0);

-- Espresso
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-espresso-single', 'ing-espresso', 1.0),
  ('var-espresso-double', 'ing-espresso', 2.0);

-- Americano
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-americano-12', 'ing-espresso', 2.0),
  ('var-americano-16', 'ing-espresso', 3.0);

-- Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-latte-12', 'ing-espresso', 2.0),
  ('var-latte-12', 'ing-milk',    10.0),
  ('var-latte-16', 'ing-espresso', 2.0),
  ('var-latte-16', 'ing-milk',    14.0);

-- Mocha
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-mocha-12', 'ing-espresso',  2.0),
  ('var-mocha-12', 'ing-milk',      8.0),
  ('var-mocha-12', 'ing-chocolate', 2.0),
  ('var-mocha-16', 'ing-espresso',  2.0),
  ('var-mocha-16', 'ing-milk',     12.0),
  ('var-mocha-16', 'ing-chocolate', 2.0);

-- White Mocha
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-whtmocha-12', 'ing-espresso', 2.0),
  ('var-whtmocha-12', 'ing-milk',     8.0),
  ('var-whtmocha-12', 'ing-whtchoc',  2.0),
  ('var-whtmocha-16', 'ing-espresso', 2.0),
  ('var-whtmocha-16', 'ing-milk',    12.0),
  ('var-whtmocha-16', 'ing-whtchoc',  2.0);

-- Caramel Macchiato
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-caramac-12', 'ing-espresso', 2.0),
  ('var-caramac-12', 'ing-milk',     8.0),
  ('var-caramac-12', 'ing-caramel',  2.0),
  ('var-caramac-16', 'ing-espresso', 2.0),
  ('var-caramac-16', 'ing-milk',    12.0),
  ('var-caramac-16', 'ing-caramel',  2.0);

-- Caramel Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-caralatte-12', 'ing-espresso', 2.0),
  ('var-caralatte-12', 'ing-milk',     8.0),
  ('var-caralatte-12', 'ing-caramel',  2.0),
  ('var-caralatte-16', 'ing-espresso', 2.0),
  ('var-caralatte-16', 'ing-milk',    12.0),
  ('var-caralatte-16', 'ing-caramel',  2.0);

-- Flavored Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-flvlatte-12', 'ing-espresso', 2.0),
  ('var-flvlatte-12', 'ing-milk',     8.0),
  ('var-flvlatte-12', 'ing-flvsyrup', 1.0),
  ('var-flvlatte-16', 'ing-espresso', 2.0),
  ('var-flvlatte-16', 'ing-milk',    12.0),
  ('var-flvlatte-16', 'ing-flvsyrup', 1.5);

-- Iced Coffee
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-icedcoffee-12', 'ing-coffee', 8.0),
  ('var-icedcoffee-12', 'ing-ice',    4.0),
  ('var-icedcoffee-16', 'ing-coffee', 12.0),
  ('var-icedcoffee-16', 'ing-ice',    4.0);

-- Cold Brew
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-coldbrew-12', 'ing-coffee', 12.0),
  ('var-coldbrew-12', 'ing-ice',    4.0),
  ('var-coldbrew-16', 'ing-coffee', 16.0),
  ('var-coldbrew-16', 'ing-ice',    4.0);

-- Iced Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-icedlatte-12', 'ing-espresso', 2.0),
  ('var-icedlatte-12', 'ing-milk',     8.0),
  ('var-icedlatte-12', 'ing-ice',      4.0),
  ('var-icedlatte-16', 'ing-espresso', 2.0),
  ('var-icedlatte-16', 'ing-milk',    12.0),
  ('var-icedlatte-16', 'ing-ice',      4.0);

-- Affogato
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-affogato-8', 'ing-espresso', 2.0),
  ('var-affogato-8', 'ing-icecream', 4.0);

-- Blended Mocha
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-blendmocha-16', 'ing-espresso',  2.0),
  ('var-blendmocha-16', 'ing-milk',      8.0),
  ('var-blendmocha-16', 'ing-chocolate', 2.0),
  ('var-blendmocha-16', 'ing-ice',       6.0),
  ('var-blendmocha-24', 'ing-espresso',  3.0),
  ('var-blendmocha-24', 'ing-milk',     12.0),
  ('var-blendmocha-24', 'ing-chocolate', 3.0),
  ('var-blendmocha-24', 'ing-ice',       8.0);

-- Blended Caramel
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-blendcaramel-16', 'ing-espresso', 2.0),
  ('var-blendcaramel-16', 'ing-milk',     8.0),
  ('var-blendcaramel-16', 'ing-caramel',  2.0),
  ('var-blendcaramel-16', 'ing-ice',      6.0),
  ('var-blendcaramel-24', 'ing-espresso', 3.0),
  ('var-blendcaramel-24', 'ing-milk',    12.0),
  ('var-blendcaramel-24', 'ing-caramel',  3.0),
  ('var-blendcaramel-24', 'ing-ice',      8.0);

-- Smoothie
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-smoothie-16', 'ing-smoothie', 12.0),
  ('var-smoothie-16', 'ing-ice',       6.0),
  ('var-smoothie-24', 'ing-smoothie', 18.0),
  ('var-smoothie-24', 'ing-ice',       8.0);

-- Hot Chocolate
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-hotchoc-4',  'ing-milk',       3.0),
  ('var-hotchoc-4',  'ing-hotchocmix', 1.0),
  ('var-hotchoc-12', 'ing-milk',      10.0),
  ('var-hotchoc-12', 'ing-hotchocmix', 2.0),
  ('var-hotchoc-16', 'ing-milk',      14.0),
  ('var-hotchoc-16', 'ing-hotchocmix', 2.0);

-- Steamer
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-steamer-12', 'ing-milk',      10.0),
  ('var-steamer-12', 'ing-flvsyrup',   1.0),
  ('var-steamer-16', 'ing-milk',      14.0),
  ('var-steamer-16', 'ing-flvsyrup',   1.5);

-- Tea
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-tea-12',  'ing-tea', 1.0),
  ('var-tea-16',  'ing-tea', 1.0),
  ('var-tea-pot', 'ing-tea', 3.0);

-- Chai Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-chailatte-12', 'ing-chai', 6.0),
  ('var-chailatte-12', 'ing-milk', 6.0),
  ('var-chailatte-16', 'ing-chai', 8.0),
  ('var-chailatte-16', 'ing-milk', 8.0);

-- Cortado
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-cortado-12', 'ing-espresso', 2.0),
  ('var-cortado-12', 'ing-milk',     2.0),
  ('var-cortado-8',  'ing-espresso', 2.0),
  ('var-cortado-8',  'ing-milk',     2.0);

-- Cappuccino
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-cappuccino-6', 'ing-espresso', 2.0),
  ('var-cappuccino-6', 'ing-milk',     4.0),
  ('var-cappuccino-8', 'ing-espresso', 2.0),
  ('var-cappuccino-8', 'ing-milk',     6.0);

-- Seasonal: Pumpkin Spice Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-pumpkin-12', 'ing-espresso',  2.0),
  ('var-pumpkin-12', 'ing-milk',      8.0),
  ('var-pumpkin-12', 'ing-pumpkin',   1.5),
  ('var-pumpkin-12', 'ing-whipcream', 1.0),
  ('var-pumpkin-16', 'ing-espresso',  2.0),
  ('var-pumpkin-16', 'ing-milk',     12.0),
  ('var-pumpkin-16', 'ing-pumpkin',   2.0),
  ('var-pumpkin-16', 'ing-whipcream', 1.5);

-- Seasonal: Maple Brown Sugar Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-maple-12', 'ing-espresso',  2.0),
  ('var-maple-12', 'ing-milk',      8.0),
  ('var-maple-12', 'ing-maple',     1.5),
  ('var-maple-12', 'ing-whipcream', 1.0),
  ('var-maple-16', 'ing-espresso',  2.0),
  ('var-maple-16', 'ing-milk',     12.0),
  ('var-maple-16', 'ing-maple',     2.0),
  ('var-maple-16', 'ing-whipcream', 1.5);

-- Seasonal: Toasted Marshmallow Mocha
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-marshmallow-12', 'ing-espresso',    2.0),
  ('var-marshmallow-12', 'ing-milk',        8.0),
  ('var-marshmallow-12', 'ing-chocolate',   2.0),
  ('var-marshmallow-12', 'ing-marshmallow', 1.0),
  ('var-marshmallow-16', 'ing-espresso',    2.0),
  ('var-marshmallow-16', 'ing-milk',       12.0),
  ('var-marshmallow-16', 'ing-chocolate',   2.0),
  ('var-marshmallow-16', 'ing-marshmallow', 1.5);

-- Seasonal: Peppermint Mocha
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-pepmocha-12', 'ing-espresso',   2.0),
  ('var-pepmocha-12', 'ing-milk',       8.0),
  ('var-pepmocha-12', 'ing-chocolate',  2.0),
  ('var-pepmocha-12', 'ing-peppermint', 1.0),
  ('var-pepmocha-12', 'ing-whipcream',  1.0),
  ('var-pepmocha-16', 'ing-espresso',   2.0),
  ('var-pepmocha-16', 'ing-milk',      12.0),
  ('var-pepmocha-16', 'ing-chocolate',  2.0),
  ('var-pepmocha-16', 'ing-peppermint', 1.5),
  ('var-pepmocha-16', 'ing-whipcream',  1.5);

-- Seasonal: Gingerbread Latte
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-gingerbread-12', 'ing-espresso',    2.0),
  ('var-gingerbread-12', 'ing-milk',        8.0),
  ('var-gingerbread-12', 'ing-gingerbread', 1.5),
  ('var-gingerbread-16', 'ing-espresso',    2.0),
  ('var-gingerbread-16', 'ing-milk',       12.0),
  ('var-gingerbread-16', 'ing-gingerbread', 2.0);

-- Seasonal: Hot Apple Cider
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-applecider-12', 'ing-cider',    12.0),
  ('var-applecider-12', 'ing-cinnamon',  0.5),
  ('var-applecider-16', 'ing-cider',    16.0),
  ('var-applecider-16', 'ing-cinnamon',  0.5);

-- Sides
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-chips',      'ing-chips',   4.0),
  ('var-chipsqueso', 'ing-chips',   4.0),
  ('var-chipsqueso', 'ing-salsa',   4.0),
  ('var-chipsqueso', 'ing-lgqueso', 13.0);

-- Add-Ons
INSERT INTO recipe_ingredients (variant_id, ingredient_id, quantity) VALUES
  ('var-flvshot',   'ing-flvsyrup',  0.5),
  ('var-espshot',   'ing-espresso',  1.0),
  ('var-whipcream', 'ing-whipcream', 1.5),
  ('var-altmilk',   'ing-altmilk',   2.0);
```
### Entity Relationship Overview

```
Categories
  └── MenuItems
        ├── MenuItemVariants (Small/Med/Large coffee, or "Regular" for single-size items)
        │     └── RecipeIngredients (fixed recipe for inventory tracking)
        │           └── Ingredients
        └── MenuItemAvailableIngredients (what can go in a customizable item)
              └── Ingredients

Orders
  └── OrderItems
        ├── MenuItemVariant (which size, if applicable)
        └── OrderItemIngredients (what the customer picked for customizable items)
              └── Ingredients

StoreTokens (QR code flow)
AppSettings (tax rate, store info)
AppUsers (managed by ASP.NET Core Identity)
```