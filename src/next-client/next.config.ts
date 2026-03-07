import type { NextConfig } from "next";
import { withSentryConfig } from "@sentry/nextjs";

const nextConfig: NextConfig = {
  devIndicators: false,
  output: 'standalone',
};

export default withSentryConfig(nextConfig, {
  silent: true,
  disableLogger: true,
});
