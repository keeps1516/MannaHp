import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "TV Menu — Manna + HP",
};

export default function TvMenuLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // No sidebar, no header — full-screen TV display
  return <>{children}</>;
}
