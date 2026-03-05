"use client";

import {
  createContext,
  useContext,
  useEffect,
  useReducer,
  type ReactNode,
} from "react";
import type { CartItem } from "@/types/cart";
import { getLineTotal } from "@/types/cart";
import { generateId } from "@/lib/utils";

const TAX_RATE = 0.0825;
const CART_STORAGE_KEY = "manna-cart";

interface CartState {
  items: CartItem[];
}

type CartAction =
  | { type: "ADD_ITEM"; payload: Omit<CartItem, "id"> }
  | { type: "REMOVE_ITEM"; payload: string }
  | { type: "UPDATE_QUANTITY"; payload: { id: string; quantity: number } }
  | { type: "CLEAR" };

function cartReducer(state: CartState, action: CartAction): CartState {
  switch (action.type) {
    case "ADD_ITEM":
      return {
        items: [
          ...state.items,
          { ...action.payload, id: generateId() },
        ],
      };
    case "REMOVE_ITEM":
      return { items: state.items.filter((i) => i.id !== action.payload) };
    case "UPDATE_QUANTITY": {
      if (action.payload.quantity <= 0) {
        return {
          items: state.items.filter((i) => i.id !== action.payload.id),
        };
      }
      return {
        items: state.items.map((i) =>
          i.id === action.payload.id
            ? { ...i, quantity: action.payload.quantity }
            : i
        ),
      };
    }
    case "CLEAR":
      return { items: [] };
    default:
      return state;
  }
}

interface CartContextValue {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  tax: number;
  total: number;
  addItem: (item: Omit<CartItem, "id">) => void;
  removeItem: (id: string) => void;
  updateQuantity: (id: string, quantity: number) => void;
  clear: () => void;
}

function loadCartFromStorage(): CartState {
  if (typeof window === "undefined") return { items: [] };
  try {
    const stored = localStorage.getItem(CART_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed.items)) return { items: parsed.items };
    }
  } catch {
    // Corrupted data — start fresh
  }
  return { items: [] };
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(cartReducer, undefined, loadCartFromStorage);

  useEffect(() => {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify({ items: state.items }));
    } catch {
      // Storage full or unavailable — ignore
    }
  }, [state.items]);

  const itemCount = state.items.reduce((sum, i) => sum + i.quantity, 0);
  const subtotal = state.items.reduce((sum, i) => sum + getLineTotal(i), 0);
  const tax = Math.round(subtotal * TAX_RATE * 100) / 100;
  const total = subtotal + tax;

  const value: CartContextValue = {
    items: state.items,
    itemCount,
    subtotal,
    tax,
    total,
    addItem: (item) => dispatch({ type: "ADD_ITEM", payload: item }),
    removeItem: (id) => dispatch({ type: "REMOVE_ITEM", payload: id }),
    updateQuantity: (id, quantity) =>
      dispatch({ type: "UPDATE_QUANTITY", payload: { id, quantity } }),
    clear: () => dispatch({ type: "CLEAR" }),
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used within CartProvider");
  return ctx;
}
