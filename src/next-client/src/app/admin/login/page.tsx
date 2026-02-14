"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { adminApi } from "@/lib/admin-api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Loader2 } from "lucide-react";

export default function AdminLoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const res = await adminApi.login({ email, password });
      localStorage.setItem("admin_token", res.token);
      router.push("/admin");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Login failed. Please try again."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#0a1628] px-4">
      <div className="w-full max-w-sm space-y-8">
        {/* Brand */}
        <div className="text-center space-y-2">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-[#00e5ff]/10 text-[#00e5ff] font-bold text-xl">
            M
          </div>
          <h1 className="text-2xl font-bold text-white">Manna + HP</h1>
          <p className="text-sm text-[#7a9bb5]">Admin Login</p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="rounded-lg border border-[#ff4757]/30 bg-[#ff4757]/10 p-3 text-sm text-[#ff4757]">
              {error}
            </div>
          )}

          <div className="space-y-2">
            <label
              htmlFor="email"
              className="text-sm font-medium text-[#7a9bb5]"
            >
              Email
            </label>
            <Input
              id="email"
              type="email"
              placeholder="owner@manna.local"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="bg-[#0d1f3c] border-white/10 text-white placeholder:text-[#7a9bb5]/50 focus-visible:ring-[#00e5ff]"
            />
          </div>

          <div className="space-y-2">
            <label
              htmlFor="password"
              className="text-sm font-medium text-[#7a9bb5]"
            >
              Password
            </label>
            <Input
              id="password"
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="bg-[#0d1f3c] border-white/10 text-white placeholder:text-[#7a9bb5]/50 focus-visible:ring-[#00e5ff]"
            />
          </div>

          <Button
            type="submit"
            disabled={loading}
            className="w-full bg-[#00e5ff] text-[#0a1628] font-semibold hover:bg-[#00e5ff]/90"
          >
            {loading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              "Sign In"
            )}
          </Button>
        </form>
      </div>
    </div>
  );
}
