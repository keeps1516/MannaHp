"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { Loader2, Wifi, WifiOff } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { connectOrderHub, disconnectOrderHub } from "@/lib/order-hub";
import { OrderCard } from "@/components/admin/order-card";
import { OrderStatus } from "@/types/api";
import type { OrderDto } from "@/types/api";

export default function OrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [connected, setConnected] = useState(false);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchOrders = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getActiveOrders(token);
      setOrders(data);
    } catch {
      // silent — will retry
    } finally {
      setLoading(false);
    }
  }, [token]);

  // Initial load + SignalR setup
  useEffect(() => {
    if (!token) return;

    fetchOrders();

    // Connect SignalR
    connectOrderHub(
      // onOrderCreated
      (order: OrderDto) => {
        setOrders((prev) => {
          // Avoid duplicates
          if (prev.some((o) => o.id === order.id)) return prev;
          return [...prev, order];
        });
        toast.success("New order received!");
      },
      // onOrderStatusChanged
      (update: { id: string; status: OrderStatus }) => {
        setOrders((prev) =>
          prev
            .map((o) => (o.id === update.id ? { ...o, status: update.status } : o))
            .filter(
              (o) =>
                o.status !== OrderStatus.Completed &&
                o.status !== OrderStatus.Cancelled
            )
        );
      },
      // onReconnected
      () => {
        setConnected(true);
        fetchOrders(); // Re-sync after reconnect
        if (pollRef.current) {
          clearInterval(pollRef.current);
          pollRef.current = null;
        }
      },
      // onDisconnected
      () => {
        setConnected(false);
        // Start polling as fallback
        if (!pollRef.current) {
          pollRef.current = setInterval(fetchOrders, 15000);
        }
      }
    )
      .then(() => setConnected(true))
      .catch(() => {
        setConnected(false);
        // If SignalR fails to connect, use polling
        if (!pollRef.current) {
          pollRef.current = setInterval(fetchOrders, 15000);
        }
      });

    return () => {
      disconnectOrderHub();
      if (pollRef.current) {
        clearInterval(pollRef.current);
        pollRef.current = null;
      }
    };
  }, [token, fetchOrders]);

  async function handleAdvance(orderId: string, nextStatus: OrderStatus) {
    if (!token) return;

    // Optimistic update
    setOrders((prev) =>
      prev
        .map((o) => (o.id === orderId ? { ...o, status: nextStatus } : o))
        .filter(
          (o) =>
            o.status !== OrderStatus.Completed &&
            o.status !== OrderStatus.Cancelled
        )
    );

    try {
      await adminApi.updateOrderStatus(token, orderId, nextStatus);
    } catch (err) {
      toast.error("Failed to update order status");
      // Rollback: refetch
      fetchOrders();
    }
  }

  const received = orders.filter((o) => o.status === OrderStatus.Received);
  const preparing = orders.filter((o) => o.status === OrderStatus.Preparing);
  const ready = orders.filter((o) => o.status === OrderStatus.Ready);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Orders</h1>
          <p className="text-[#7a9bb5] mt-1">
            {orders.length} active order{orders.length !== 1 && "s"}
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm">
          {connected ? (
            <div className="flex items-center gap-1.5 text-emerald-400">
              <Wifi className="h-4 w-4" />
              <span>Live</span>
            </div>
          ) : (
            <div className="flex items-center gap-1.5 text-amber-400">
              <WifiOff className="h-4 w-4" />
              <span>Polling</span>
            </div>
          )}
        </div>
      </div>

      {/* Kanban columns */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Received */}
        <KanbanColumn
          title="Received"
          count={received.length}
          color="amber"
          orders={received}
          onAdvance={handleAdvance}
        />

        {/* Preparing */}
        <KanbanColumn
          title="Preparing"
          count={preparing.length}
          color="cyan"
          orders={preparing}
          onAdvance={handleAdvance}
        />

        {/* Ready */}
        <KanbanColumn
          title="Ready"
          count={ready.length}
          color="emerald"
          orders={ready}
          onAdvance={handleAdvance}
        />
      </div>
    </div>
  );
}

function KanbanColumn({
  title,
  count,
  color,
  orders,
  onAdvance,
}: {
  title: string;
  count: number;
  color: "amber" | "cyan" | "emerald";
  orders: OrderDto[];
  onAdvance: (orderId: string, nextStatus: OrderStatus) => Promise<void>;
}) {
  const colorMap = {
    amber: {
      dot: "bg-amber-400",
      text: "text-amber-400",
      border: "border-amber-400/20",
    },
    cyan: {
      dot: "bg-[#00e5ff]",
      text: "text-[#00e5ff]",
      border: "border-[#00e5ff]/20",
    },
    emerald: {
      dot: "bg-emerald-400",
      text: "text-emerald-400",
      border: "border-emerald-400/20",
    },
  };

  const c = colorMap[color];

  return (
    <div className="space-y-3">
      <div
        className={`flex items-center gap-2 pb-2 border-b ${c.border}`}
      >
        <div className={`h-2.5 w-2.5 rounded-full ${c.dot}`} />
        <h2 className={`text-sm font-semibold ${c.text}`}>{title}</h2>
        <span className="text-xs text-[#7a9bb5] ml-auto">{count}</span>
      </div>

      <div className="space-y-3">
        {orders.length === 0 ? (
          <p className="text-sm text-[#4a6a85] text-center py-8">
            No orders
          </p>
        ) : (
          orders.map((order) => (
            <OrderCard key={order.id} order={order} onAdvance={onAdvance} />
          ))
        )}
      </div>
    </div>
  );
}
