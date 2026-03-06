"use client";

import {
  createContext,
  useContext,
  useEffect,
  useReducer,
  useState,
  useCallback,
  type ReactNode,
} from "react";
import type { CartItem } from "@/types/cart";
import { getLineTotal } from "@/types/cart";
import { generateId } from "@/lib/utils";

const DEFAULT_TAX_RATE = 0.0825;
const CART_STORAGE_KEY = "manna-cart";

interface CartState {
  items: CartItem[];
  editingItem: CartItem | null;
}

type CartAction =
  | { type: "ADD_ITEM"; payload: Omit<CartItem, "id"> }
  | { type: "REMOVE_ITEM"; payload: string }
  | { type: "UPDATE_QUANTITY"; payload: { id: string; quantity: number } }
  | { type: "UPDATE_ITEM"; payload: { id: string; data: Omit<CartItem, "id"> } }
  | { type: "SET_EDITING_ITEM"; payload: CartItem }
  | { type: "CLEAR_EDITING_ITEM" }
  | { type: "CLEAR" };

function cartReducer(state: CartState, action: CartAction): CartState {
  switch (action.type) {
    case "ADD_ITEM":
      return {
        ...state,
        items: [
          ...state.items,
          { ...action.payload, id: generateId() },
        ],
      };
    case "REMOVE_ITEM":
      return { ...state, items: state.items.filter((i) => i.id !== action.payload) };
    case "UPDATE_QUANTITY": {
      if (action.payload.quantity <= 0) {
        return {
          ...state,
          items: state.items.filter((i) => i.id !== action.payload.id),
        };
      }
      return {
        ...state,
        items: state.items.map((i) =>
          i.id === action.payload.id
            ? { ...i, quantity: action.payload.quantity }
            : i
        ),
      };
    }
    case "UPDATE_ITEM":
      return {
        ...state,
        items: state.items.map((i) =>
          i.id === action.payload.id
            ? { ...action.payload.data, id: i.id }
            : i
        ),
      };
    case "SET_EDITING_ITEM":
      return { ...state, editingItem: action.payload };
    case "CLEAR_EDITING_ITEM":
      return { ...state, editingItem: null };
    case "CLEAR":
      return { ...state, items: [], editingItem: null };
    default:
      return state;
  }
}

interface CartContextValue {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  tax: number;
  taxRate: number;
  total: number;
  editingItem: CartItem | null;
  addItem: (item: Omit<CartItem, "id">) => void;
  removeItem: (id: string) => void;
  updateQuantity: (id: string, quantity: number) => void;
  updateItem: (id: string, data: Omit<CartItem, "id">) => void;
  setEditingItem: (item: CartItem) => void;
  clearEditingItem: () => void;
  clear: () => void;
}

function loadCartFromStorage(): CartState {
  if (typeof window === "undefined") return { items: [], editingItem: null };
  try {
    const stored = localStorage.getItem(CART_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      if (Array.isArray(parsed.items)) return { items: parsed.items, editingItem: null };
    }
  } catch {
    // Corrupted data — start fresh
  }
  return { items: [], editingItem: null };
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children, initialTaxRate }: { children: ReactNode; initialTaxRate?: number }) {
  const [state, dispatch] = useReducer(cartReducer, undefined, loadCartFromStorage);
  const [taxRate, setTaxRate] = useState(initialTaxRate ?? DEFAULT_TAX_RATE);

  const fetchTaxRate = useCallback(async () => {
    try {
      const { api } = await import("@/lib/api");
      const settings = await api.getPublicSettings();
      setTaxRate(settings.taxRate);
    } catch {
      // Use default if fetch fails
    }
  }, []);

  useEffect(() => {
    if (initialTaxRate === undefined) {
      fetchTaxRate();
    }
  }, [fetchTaxRate, initialTaxRate]);

  useEffect(() => {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify({ items: state.items }));
    } catch {
      // Storage full or unavailable — ignore
    }
  }, [state.items]);

  const itemCount = state.items.reduce((sum, i) => sum + i.quantity, 0);
  const subtotal = state.items.reduce((sum, i) => sum + getLineTotal(i), 0);
  const tax = Math.round(subtotal * taxRate * 100) / 100;
  const total = subtotal + tax;

  const value: CartContextValue = {
    items: state.items,
    itemCount,
    subtotal,
    tax,
    taxRate,
    total,
    editingItem: state.editingItem,
    addItem: (item) => dispatch({ type: "ADD_ITEM", payload: item }),
    removeItem: (id) => dispatch({ type: "REMOVE_ITEM", payload: id }),
    updateQuantity: (id, quantity) =>
      dispatch({ type: "UPDATE_QUANTITY", payload: { id, quantity } }),
    updateItem: (id, data) =>
      dispatch({ type: "UPDATE_ITEM", payload: { id, data } }),
    setEditingItem: (item) => dispatch({ type: "SET_EDITING_ITEM", payload: item }),
    clearEditingItem: () => dispatch({ type: "CLEAR_EDITING_ITEM" }),
    clear: () => dispatch({ type: "CLEAR" }),
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used within CartProvider");
  return ctx;
}
