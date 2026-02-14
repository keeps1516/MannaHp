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
let connectId = 0; // monotonic counter to handle Strict Mode double-mount

export function getOrderHubConnection(): HubConnection | null {
  return connection;
}

/**
 * Connect to the OrderHub SignalR endpoint and join the kitchen group.
 * Handles React Strict Mode double-mount by tracking a connect generation ID.
 * If disconnect is called before connect finishes, the stale connect is ignored.
 */
export async function connectOrderHub(
  onOrderCreated: (order: OrderDto) => void,
  onOrderStatusChanged: (update: { id: string; status: OrderStatus }) => void,
  onReconnected?: () => void,
  onDisconnected?: () => void
): Promise<HubConnection> {
  // Bump the generation so any in-flight connect from a previous mount is stale
  const thisConnectId = ++connectId;

  // If a previous connection exists, tear it down first
  if (connection) {
    try {
      await connection.stop();
    } catch {
      // ignore
    }
    connection = null;
  }

  const conn = new HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/orders`)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  conn.on("OrderCreated", onOrderCreated);
  conn.on("OrderStatusChanged", onOrderStatusChanged);

  conn.onreconnected(() => {
    conn.invoke("JoinKitchen").catch(() => {});
    onReconnected?.();
  });

  conn.onclose(() => {
    onDisconnected?.();
  });

  await conn.start();

  // If disconnect was called while we were starting, abort
  if (thisConnectId !== connectId) {
    await conn.stop();
    throw new Error("Connection aborted (component unmounted)");
  }

  connection = conn;
  await conn.invoke("JoinKitchen");

  return conn;
}

/**
 * Disconnect from the OrderHub and clean up.
 */
export async function disconnectOrderHub(): Promise<void> {
  // Bump generation to cancel any in-flight connect
  connectId++;

  const conn = connection;
  connection = null;

  if (conn && conn.state !== HubConnectionState.Disconnected) {
    try {
      await conn.invoke("LeaveKitchen");
    } catch {
      // ignore if already disconnected
    }
    await conn.stop();
  }
}
