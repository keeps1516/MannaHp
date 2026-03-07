"use client";

import { useEffect, useState, useCallback } from "react";
import { QRCodeSVG } from "qrcode.react";
import { Loader2, RefreshCw, Trash2, Maximize2, Copy, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import type { StoreTokenResponse } from "@/types/api";
import { toast } from "sonner";

const PUBLIC_BASE_URL =
  process.env.NEXT_PUBLIC_BASE_URL ?? "http://localhost:3000";

export default function QrCodePage() {
  const { token: authToken } = useAuth();
  const [storeToken, setStoreToken] = useState<StoreTokenResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [durationDays, setDurationDays] = useState(7);
  const [fullScreen, setFullScreen] = useState(false);
  const [copied, setCopied] = useState(false);
  const [customerMessage, setCustomerMessage] = useState("");
  const [savingMessage, setSavingMessage] = useState(false);

  const fetchToken = useCallback(async () => {
    if (!authToken) return;
    try {
      const current = await adminApi.getCurrentStoreToken(authToken);
      setStoreToken(current);
    } catch {
      setStoreToken(null);
    } finally {
      setLoading(false);
    }
  }, [authToken]);

  const fetchMessage = useCallback(async () => {
    if (!authToken) return;
    try {
      const settings = await adminApi.getSettings(authToken);
      const msg = settings.find((s) => s.key === "StoreTokenRequiredMessage");
      if (msg) setCustomerMessage(msg.value);
    } catch { /* ignore */ }
  }, [authToken]);

  useEffect(() => {
    fetchToken();
    fetchMessage();
  }, [fetchToken, fetchMessage]);

  async function handleGenerate() {
    if (!authToken) return;
    setGenerating(true);
    try {
      const newToken = await adminApi.generateStoreToken(authToken, { durationDays });
      setStoreToken(newToken);
      toast.success("QR code generated");
    } catch {
      toast.error("Failed to generate QR code");
    } finally {
      setGenerating(false);
    }
  }

  async function handleSaveMessage() {
    if (!authToken) return;
    setSavingMessage(true);
    try {
      await adminApi.updateSettings(authToken, [
        { key: "StoreTokenRequiredMessage", value: customerMessage },
      ]);
      toast.success("Customer message saved");
    } catch {
      toast.error("Failed to save message");
    } finally {
      setSavingMessage(false);
    }
  }

  async function handleRevoke() {
    if (!authToken || !storeToken) return;
    try {
      await adminApi.revokeStoreToken(authToken, storeToken.id);
      setStoreToken(null);
      toast.success("QR code revoked");
    } catch {
      toast.error("Failed to revoke QR code");
    }
  }

  const qrUrl = storeToken ? `${PUBLIC_BASE_URL}?token=${storeToken.token}` : "";
  const expiresAt = storeToken ? new Date(storeToken.expiresAt) : null;
  const daysRemaining = expiresAt
    ? Math.max(0, Math.ceil((expiresAt.getTime() - Date.now()) / (1000 * 60 * 60 * 24)))
    : 0;

  if (fullScreen && storeToken) {
    return (
      <div
        className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-white cursor-pointer"
        onClick={() => setFullScreen(false)}
      >
        <QRCodeSVG value={qrUrl} size={320} level="H" />
        <p className="mt-6 text-lg font-semibold text-gray-800">
          Scan to order
        </p>
        <p className="text-sm text-gray-500 mt-2">Tap anywhere to exit</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">QR Code</h1>
        <p className="text-sm text-[#7a9bb5] mt-1">
          In-store ordering QR code for customers
        </p>
      </div>

      {/* Customer Message */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white text-sm">
          Customer Message
        </h2>
        <Separator className="bg-[#1e3a5f]" />
        <p className="text-xs text-[#7a9bb5]">
          Shown to customers who try to order without scanning the QR code.
        </p>
        <Textarea
          value={customerMessage}
          onChange={(e) => setCustomerMessage(e.target.value)}
          placeholder="Please scan the QR code displayed on the TV before placing your order."
          rows={2}
          className="bg-[#0a1628] border-white/10 text-white placeholder:text-[#7a9bb5]/50 resize-none"
        />
        <div className="flex justify-end">
          <Button
            onClick={handleSaveMessage}
            disabled={savingMessage}
            size="sm"
            className="bg-[#00e5ff] text-[#0a1628] hover:bg-[#00e5ff]/80"
          >
            {savingMessage ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : null}
            Save Message
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-[#00e5ff]" />
        </div>
      ) : storeToken ? (
        <div className="space-y-6">
          {/* Active QR Code */}
          <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-6 flex flex-col items-center space-y-4">
            <div className="bg-white rounded-xl p-4">
              <QRCodeSVG value={qrUrl} size={200} level="H" />
            </div>

            <div className="text-center space-y-1">
              <p className="text-sm text-[#7a9bb5]">
                Expires {expiresAt?.toLocaleDateString()} ({daysRemaining} day
                {daysRemaining !== 1 ? "s" : ""} remaining)
              </p>
            </div>

            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  navigator.clipboard.writeText(qrUrl);
                  setCopied(true);
                  setTimeout(() => setCopied(false), 2000);
                  toast.success("Link copied to clipboard");
                }}
                className="border-[#1e3a5f] text-[#7a9bb5] hover:text-white hover:bg-white/5"
              >
                {copied ? <Check className="h-4 w-4 mr-1" /> : <Copy className="h-4 w-4 mr-1" />}
                {copied ? "Copied" : "Copy Link"}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setFullScreen(true)}
                className="border-[#1e3a5f] text-[#7a9bb5] hover:text-white hover:bg-white/5"
              >
                <Maximize2 className="h-4 w-4 mr-1" />
                Full Screen
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleRevoke}
                className="border-[#ff4757]/40 text-[#ff4757] hover:bg-[#ff4757]/10"
              >
                <Trash2 className="h-4 w-4 mr-1" />
                Revoke
              </Button>
            </div>
          </div>

          {/* Generate replacement */}
          <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
            <h2 className="font-semibold text-white text-sm">
              Replace QR Code
            </h2>
            <Separator className="bg-[#1e3a5f]" />
            <p className="text-xs text-[#7a9bb5]">
              Generating a new code will automatically revoke the current one.
            </p>
            <div className="flex items-center gap-3">
              <label className="text-sm text-[#7a9bb5] shrink-0">
                Duration (days):
              </label>
              <Input
                type="number"
                min={1}
                max={365}
                value={durationDays}
                onChange={(e) => setDurationDays(Number(e.target.value))}
                className="w-24 bg-[#0a1628] border-white/10 text-white"
              />
              <Button
                onClick={handleGenerate}
                disabled={generating}
                size="sm"
                className="bg-[#00e5ff] text-[#0a1628] hover:bg-[#00e5ff]/80"
              >
                {generating ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4 mr-1" />
                )}
                Generate New
              </Button>
            </div>
          </div>
        </div>
      ) : (
        /* Empty state */
        <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-6 space-y-4">
          <div className="text-center space-y-2">
            <p className="text-white font-medium">No active QR code</p>
            <p className="text-sm text-[#7a9bb5]">
              Generate a QR code for customers to scan and place in-store orders.
            </p>
          </div>
          <Separator className="bg-[#1e3a5f]" />
          <div className="flex items-center justify-center gap-3">
            <label className="text-sm text-[#7a9bb5] shrink-0">
              Duration (days):
            </label>
            <Input
              type="number"
              min={1}
              max={365}
              value={durationDays}
              onChange={(e) => setDurationDays(Number(e.target.value))}
              className="w-24 bg-[#0a1628] border-white/10 text-white"
            />
            <Button
              onClick={handleGenerate}
              disabled={generating}
              className="bg-[#00e5ff] text-[#0a1628] hover:bg-[#00e5ff]/80"
            >
              {generating ? (
                <Loader2 className="h-4 w-4 animate-spin mr-1" />
              ) : null}
              Generate QR Code
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
