import type { NextConfig } from "next";
import { withSentryConfig } from "@sentry/nextjs";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082";
const apiHostname = new URL(apiUrl).hostname;

const nextConfig: NextConfig = {
  devIndicators: false,
  output: 'standalone',
  images: {
    remotePatterns: [
      {
        protocol: apiUrl.startsWith("https") ? "https" : "http",
        hostname: apiHostname,
      },
    ],
  },
};

export default withSentryConfig(nextConfig, {
  silent: true,
  disableLogger: true,
});
