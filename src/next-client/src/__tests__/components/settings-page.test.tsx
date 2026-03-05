import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import SettingsPage from "@/app/admin/(dashboard)/settings/page";

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({
    user: { email: "owner@manna.local", displayName: "Owner", role: "Owner" },
    token: "fake-token",
  }),
}));

const registerMock = vi.fn().mockResolvedValue({ email: "test@test.com" });
const getSettingsMock = vi.fn().mockResolvedValue([
  { key: "StoreName", value: "Manna + HP" },
  { key: "StoreAddress", value: "317 S Main St" },
  { key: "StoreCity", value: "Lindsay, OK 73052" },
  { key: "StorePhone", value: "(405) 208-2271" },
  { key: "DefaultTaxRate", value: "0.0825" },
  { key: "ReceiptFooter", value: "Our pleasure to serve you!" },
]);
const updateSettingsMock = vi.fn().mockResolvedValue(undefined);

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    register: (...args: unknown[]) => registerMock(...args),
    getSettings: (...args: unknown[]) => getSettingsMock(...args),
    updateSettings: (...args: unknown[]) => updateSettingsMock(...args),
  },
}));

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}));

describe("SettingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("displays store settings form instead of placeholder text", async () => {
    render(<SettingsPage />);

    await waitFor(() => {
      expect(
        screen.queryByText(/will be configurable here in a future update/)
      ).not.toBeInTheDocument();
    });
  });

  it("loads and displays the store name from settings", async () => {
    render(<SettingsPage />);

    await waitFor(() => {
      const input = screen.getByDisplayValue("Manna + HP");
      expect(input).toBeInTheDocument();
    });
  });

  it("loads and displays the tax rate from settings", async () => {
    render(<SettingsPage />);

    await waitFor(() => {
      const input = screen.getByDisplayValue("8.25");
      expect(input).toBeInTheDocument();
    });
  });
});
