import { describe, it, expect, vi, afterEach } from "vitest";
import { timeAgo } from "@/lib/time-ago";

describe("timeAgo", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns "Just now" for less than 1 minute ago', () => {
    const now = new Date();
    expect(timeAgo(now.toISOString())).toBe("Just now");
  });

  it('returns "1 min ago" for exactly 1 minute ago', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-15T12:01:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("1 min ago");
  });

  it('returns "N min ago" for minutes less than 60', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-15T12:30:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("30 min ago");
  });

  it('returns "1 hr ago" for exactly 1 hour ago', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-15T13:00:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("1 hr ago");
  });

  it('returns "N hr ago" for hours less than 24', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-15T17:00:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("5 hr ago");
  });

  it('returns "1 day ago" for exactly 1 day ago', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-16T12:00:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("1 day ago");
  });

  it('returns "N days ago" for multiple days', () => {
    vi.useFakeTimers();
    const now = new Date("2026-01-20T12:00:00Z");
    vi.setSystemTime(now);
    expect(timeAgo("2026-01-15T12:00:00Z")).toBe("5 days ago");
  });
});
