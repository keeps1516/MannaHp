interface CategoryMeta {
  emoji: string;
  description: string;
}

const categoryMap: Record<string, CategoryMeta> = {
  "burrito bowls": {
    emoji: "\uD83C\uDF2F",
    description: "Build your perfect bowl",
  },
  "traditional drinks": {
    emoji: "\u2615",
    description: "Classic coffee & espresso",
  },
  "seasonal specials": {
    emoji: "\u2728",
    description: "Limited-time favorites",
  },
  "sides & drinks": {
    emoji: "\uD83E\uDD64",
    description: "Refreshments & extras",
  },
  sides: {
    emoji: "\uD83C\uDF7F",
    description: "Sides & snacks",
  },
  drinks: {
    emoji: "\uD83E\uDD64",
    description: "Refreshing beverages",
  },
  coffee: {
    emoji: "\u2615",
    description: "Freshly brewed coffee",
  },
  tea: {
    emoji: "\uD83C\uDF75",
    description: "Hot & iced teas",
  },
  smoothies: {
    emoji: "\uD83E\uDD5D",
    description: "Blended fruit smoothies",
  },
  desserts: {
    emoji: "\uD83C\uDF70",
    description: "Sweet treats",
  },
};

const fallback: CategoryMeta = {
  emoji: "\uD83C\uDF7D\uFE0F",
  description: "Explore our menu",
};

export function getCategoryMeta(categoryName: string): CategoryMeta {
  const key = categoryName.toLowerCase().trim();

  // Exact match
  if (categoryMap[key]) return categoryMap[key];

  // Partial match
  for (const [mapKey, meta] of Object.entries(categoryMap)) {
    if (key.includes(mapKey) || mapKey.includes(key)) return meta;
  }

  return fallback;
}
