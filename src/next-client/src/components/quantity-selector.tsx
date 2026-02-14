"use client";

import { Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";

interface QuantitySelectorProps {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
}

export function QuantitySelector({
  value,
  onChange,
  min = 1,
  max = 20,
}: QuantitySelectorProps) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-sm text-[#7a9bb5]">Qty</span>
      <Button
        variant="outline"
        size="icon"
        className="h-8 w-8 border-[#1e3a5f] bg-[#1a3550] text-[#7a9bb5] hover:text-white hover:bg-[#1e3a5f]"
        disabled={value <= min}
        onClick={() => onChange(value - 1)}
      >
        <Minus className="h-4 w-4" />
      </Button>
      <span className="w-8 text-center font-medium text-white">{value}</span>
      <Button
        variant="outline"
        size="icon"
        className="h-8 w-8 border-[#1e3a5f] bg-[#1a3550] text-[#7a9bb5] hover:text-white hover:bg-[#1e3a5f]"
        disabled={value >= max}
        onClick={() => onChange(value + 1)}
      >
        <Plus className="h-4 w-4" />
      </Button>
    </div>
  );
}
