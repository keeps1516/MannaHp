/** Maps ingredient names (lowercased) to emoji for visual display */
const EMOJI_MAP: Record<string, string> = {
  // Bases
  "rice": "🍚",
  "jasmine rice": "🍚",
  "10oz jasmine rice": "🍚",
  "beans": "🫘",
  "5oz beans": "🫘",
  "black beans": "🫘",
  "pinto beans": "🫘",

  // Proteins
  "chicken": "🍗",
  "8oz chicken": "🍗",
  "ground beef": "🥩",
  "6oz ground beef": "🥩",
  "steak": "🥩",
  "beef": "🥩",
  "carnitas": "🥓",
  "pork": "🥓",
  "sausage queso": "🧀",
  "6oz sausage queso": "🧀",
  "tofu": "🫛",
  "shrimp": "🦐",

  // Fresh toppings
  "lettuce": "🥬",
  "tomatoes": "🍅",
  "tomato": "🍅",
  "fresh jalapenos": "🌶️",
  "jalapenos": "🌶️",
  "fresh salsa": "🫙",
  "salsa": "🫙",
  "shredded cheese": "🧀",
  "cheese": "🧀",
  "corn": "🌽",
  "onions": "🧅",
  "cilantro": "🌿",
  "avocado": "🥑",
  "guacamole": "🥑",
  "sour cream": "🍶",
  "lime": "🍋",
  "pico de gallo": "🍅",

  // Sides & extras
  "chips": "🫓",
  "side of chips": "🫓",
  "chips & salsa": "🫓",
  "queso": "🧀",
  "tortilla": "🫓",

  // Drinks
  "water": "💧",
  "soda": "🥤",
  "lemonade": "🍋",
  "tea": "🍵",
  "coffee": "☕",
};

export function getIngredientEmoji(name: string): string {
  const lower = name.toLowerCase();

  // Exact match
  if (EMOJI_MAP[lower]) return EMOJI_MAP[lower];

  // Partial match — check if any key is contained in the name
  for (const [key, emoji] of Object.entries(EMOJI_MAP)) {
    if (lower.includes(key) || key.includes(lower)) return emoji;
  }

  // Fallback
  return "🍽️";
}
