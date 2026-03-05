"use client";

import { useState, useEffect, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Loader2, UserPlus, Settings } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";

export default function SettingsPage() {
  const { token } = useAuth();

  // Staff registration
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [registering, setRegistering] = useState(false);

  // Store settings
  const [storeName, setStoreName] = useState("");
  const [storeAddress, setStoreAddress] = useState("");
  const [storeCity, setStoreCity] = useState("");
  const [storePhone, setStorePhone] = useState("");
  const [taxRate, setTaxRate] = useState("");
  const [receiptFooter, setReceiptFooter] = useState("");
  const [loadingSettings, setLoadingSettings] = useState(true);
  const [savingSettings, setSavingSettings] = useState(false);

  const loadSettings = useCallback(async () => {
    if (!token) return;
    try {
      const settings = await adminApi.getSettings(token);
      const map = new Map(settings.map((s) => [s.key, s.value]));
      setStoreName(map.get("StoreName") ?? "");
      setStoreAddress(map.get("StoreAddress") ?? "");
      setStoreCity(map.get("StoreCity") ?? "");
      setStorePhone(map.get("StorePhone") ?? "");
      const rate = parseFloat(map.get("DefaultTaxRate") ?? "0");
      setTaxRate((rate * 100).toString());
      setReceiptFooter(map.get("ReceiptFooter") ?? "");
    } catch {
      // silent
    } finally {
      setLoadingSettings(false);
    }
  }, [token]);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setRegistering(true);

    try {
      const result = await adminApi.register(token, {
        email,
        password,
        displayName: displayName || null,
      });
      toast.success(`Staff account created for ${result.email}`);
      setEmail("");
      setPassword("");
      setDisplayName("");
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to create staff account"
      );
    } finally {
      setRegistering(false);
    }
  }

  async function handleSaveSettings(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSavingSettings(true);

    try {
      await adminApi.updateSettings(token, [
        { key: "StoreName", value: storeName },
        { key: "StoreAddress", value: storeAddress },
        { key: "StoreCity", value: storeCity },
        { key: "StorePhone", value: storePhone },
        { key: "DefaultTaxRate", value: (parseFloat(taxRate) / 100).toString() },
        { key: "ReceiptFooter", value: receiptFooter },
      ]);
      toast.success("Settings saved");
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to save settings"
      );
    } finally {
      setSavingSettings(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Settings</h1>
        <p className="text-[#7a9bb5] mt-1">
          Manage staff accounts and store settings.
        </p>
      </div>

      {/* Store Settings */}
      <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-6 space-y-5">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-emerald-400/10 flex items-center justify-center">
            <Settings className="h-5 w-5 text-emerald-400" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-white">
              Store Settings
            </h2>
            <p className="text-sm text-[#7a9bb5]">
              Tax rate, store name, address, and receipt details.
            </p>
          </div>
        </div>

        <Separator className="bg-white/10" />

        {loadingSettings ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-5 w-5 animate-spin text-[#7a9bb5]" />
          </div>
        ) : (
          <form onSubmit={handleSaveSettings} className="space-y-4 max-w-md">
            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Store Name</Label>
              <Input
                value={storeName}
                onChange={(e) => setStoreName(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Address</Label>
              <Input
                value={storeAddress}
                onChange={(e) => setStoreAddress(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">City / State / ZIP</Label>
              <Input
                value={storeCity}
                onChange={(e) => setStoreCity(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Phone</Label>
              <Input
                value={storePhone}
                onChange={(e) => setStorePhone(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Tax Rate (%)</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                max="100"
                value={taxRate}
                onChange={(e) => setTaxRate(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Receipt Footer Text</Label>
              <Input
                value={receiptFooter}
                onChange={(e) => setReceiptFooter(e.target.value)}
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
              />
            </div>

            <Button
              type="submit"
              disabled={savingSettings}
              className="bg-emerald-500 text-white hover:bg-emerald-600 font-semibold"
            >
              {savingSettings && (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              )}
              Save Settings
            </Button>
          </form>
        )}
      </div>

      {/* Staff Registration */}
      <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-6 space-y-5">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-[#00e5ff]/10 flex items-center justify-center">
            <UserPlus className="h-5 w-5 text-[#00e5ff]" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-white">
              Register Staff Account
            </h2>
            <p className="text-sm text-[#7a9bb5]">
              Create a new staff account with order management access.
            </p>
          </div>
        </div>

        <Separator className="bg-white/10" />

        <form onSubmit={handleRegister} className="space-y-4 max-w-md">
          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Email</Label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="staff@example.com"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Password</Label>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="Minimum 8 characters"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Display Name</Label>
            <Input
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="Optional"
            />
          </div>

          <Button
            type="submit"
            disabled={registering}
            className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
          >
            {registering && (
              <Loader2 className="h-4 w-4 animate-spin mr-2" />
            )}
            Create Staff Account
          </Button>
        </form>
      </div>
    </div>
  );
}
