"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Loader2, UserPlus } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";

export default function SettingsPage() {
  const { token } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [registering, setRegistering] = useState(false);

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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Settings</h1>
        <p className="text-[#7a9bb5] mt-1">
          Manage staff accounts and store settings.
        </p>
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

      {/* Placeholder for future settings */}
      <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-6 space-y-4">
        <h2 className="text-lg font-semibold text-white">Store Settings</h2>
        <p className="text-sm text-[#4a6a85]">
          Tax rate, store name, address, and other settings will be configurable
          here in a future update.
        </p>
      </div>
    </div>
  );
}
