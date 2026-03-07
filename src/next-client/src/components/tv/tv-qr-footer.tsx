"use client";

import { QRCodeSVG } from "qrcode.react";

interface TvQrFooterProps {
  storeTokenUrl: string | null;
  orderOnlineUrl: string;
}

export function TvQrFooter({ storeTokenUrl, orderOnlineUrl }: TvQrFooterProps) {
  const hasInStore = !!storeTokenUrl;
  const hasOnline = !!orderOnlineUrl;

  if (!hasInStore && !hasOnline) return null;

  return (
    <div className="fixed bottom-6 right-6 flex flex-col gap-4 z-10">
      {hasInStore && (
        <div className="bg-white rounded-2xl p-4 flex flex-col items-center shadow-lg">
          <QRCodeSVG value={storeTokenUrl!} size={140} level="H" />
          <p className="mt-2 text-sm font-semibold text-gray-800 text-center">
            Scan to Order
          </p>
        </div>
      )}
      {hasOnline && (
        <div className="bg-white rounded-2xl p-4 flex flex-col items-center shadow-lg">
          <QRCodeSVG value={orderOnlineUrl} size={140} level="H" />
          <p className="mt-2 text-sm font-semibold text-gray-800 text-center">
            Order Online
          </p>
        </div>
      )}
    </div>
  );
}
