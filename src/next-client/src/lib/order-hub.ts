import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { OrderDto, OrderStatus } from "@/types/api";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082";

let connection: HubConnection | null = null;

export function getOrderHubConnection(): HubConnection | null {
  return connection;
}

/**
 * Connect to the OrderHub SignalR endpoint and join the kitchen group.
 * Returns the HubConnection instance.
 */
export async function connectOrderHub(
  onOrderCreated: (order: OrderDto) => void,
  onOrderStatusChanged: (update: { id: string; status: OrderStatus }) => void,
  onReconnected?: () => void,
  onDisconnected?: () => void
): Promise<HubConnection> {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    return connection;
  }

  connection = new HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/orders`)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on("OrderCreated", onOrderCreated);
  connection.on("OrderStatusChanged", onOrderStatusChanged);

  connection.onreconnected(() => {
    // Re-join the kitchen group after reconnect
    connection?.invoke("JoinKitchen").catch(() => {});
    onReconnected?.();
  });

  connection.onclose(() => {
    onDisconnected?.();
  });

  await connection.start();
  await connection.invoke("JoinKitchen");

  return connection;
}

/**
 * Disconnect from the OrderHub and clean up.
 */
export async function disconnectOrderHub(): Promise<void> {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    try {
      await connection.invoke("LeaveKitchen");
    } catch {
      // ignore if already disconnected
    }
    await connection.stop();
  }
  connection = null;
}
