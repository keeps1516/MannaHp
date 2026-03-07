"use client";

import { useState } from "react";
import { Calculator } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { convert, getConvertibleUnits } from "@/lib/unit-conversions";
import { unitShortLabel } from "@/lib/unit-options";
import { UnitOfMeasure } from "@/types/api";

interface QuantityCalculatorProps {
  unit: UnitOfMeasure;
  onCalculated: (total: number) => void;
}

export function QuantityCalculator({ unit, onCalculated }: QuantityCalculatorProps) {
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<"count" | "convert">("count");

  // Count mode
  const [count, setCount] = useState("");
  const [sizePerItem, setSizePerItem] = useState("");

  // Convert mode
  const [convertValue, setConvertValue] = useState("");
  const [convertFrom, setConvertFrom] = useState<UnitOfMeasure | null>(null);

  const convertibleUnits = getConvertibleUnits(unit);

  const countTotal = Number(count) * Number(sizePerItem);
  const convertResult =
    convertFrom !== null && convertValue
      ? convert(Number(convertValue), convertFrom, unit)
      : null;

  function applyCount() {
    if (countTotal > 0) {
      onCalculated(countTotal);
      setCount("");
      setSizePerItem("");
    }
  }

  function applyConvert() {
    if (convertResult && convertResult.value > 0) {
      onCalculated(convertResult.value);
      setConvertValue("");
    }
  }

  if (!open) {
    return (
      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={() => setOpen(true)}
        className="text-[#7a9bb5] hover:text-[#00e5ff] gap-1"
        data-testid="calculator-toggle"
      >
        <Calculator className="h-3.5 w-3.5" />
        Calculator
      </Button>
    );
  }

  return (
    <div className="rounded-lg border border-[#1e3a5f] bg-[#0a1628] p-3 space-y-3" data-testid="calculator-panel">
      {/* Mode tabs */}
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => setMode("count")}
          className={`text-xs px-2 py-1 rounded ${
            mode === "count" ? "bg-[#00e5ff]/20 text-[#00e5ff]" : "text-[#7a9bb5]"
          }`}
        >
          Count
        </button>
        {convertibleUnits.length > 0 && (
          <button
            type="button"
            onClick={() => {
              setMode("convert");
              if (!convertFrom) setConvertFrom(convertibleUnits[0]);
            }}
            className={`text-xs px-2 py-1 rounded ${
              mode === "convert" ? "bg-[#00e5ff]/20 text-[#00e5ff]" : "text-[#7a9bb5]"
            }`}
          >
            Convert
          </button>
        )}
        <button
          type="button"
          onClick={() => setOpen(false)}
          className="text-xs text-[#7a9bb5] ml-auto hover:text-white"
        >
          Close
        </button>
      </div>

      {mode === "count" ? (
        <div className="space-y-2">
          <div className="flex items-center gap-2 text-sm">
            <Input
              type="number"
              min="0"
              placeholder="Count"
              value={count}
              onChange={(e) => setCount(e.target.value)}
              className="bg-[#0d1f3c] border-[#1e3a5f] text-white w-20 text-center"
              inputMode="decimal"
              data-testid="calc-count"
            />
            <span className="text-[#7a9bb5]">x</span>
            <Input
              type="number"
              min="0"
              placeholder="Size"
              value={sizePerItem}
              onChange={(e) => setSizePerItem(e.target.value)}
              className="bg-[#0d1f3c] border-[#1e3a5f] text-white w-20 text-center"
              inputMode="decimal"
              data-testid="calc-size"
            />
            <span className="text-[#7a9bb5]">{unitShortLabel(unit)}</span>
          </div>
          {countTotal > 0 && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-[#00e5ff]" data-testid="calc-total">
                = {countTotal} {unitShortLabel(unit)}
              </span>
              <Button
                type="button"
                size="sm"
                onClick={applyCount}
                className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] text-xs"
              >
                Apply
              </Button>
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-2">
          <div className="flex items-center gap-2 text-sm">
            <Input
              type="number"
              min="0"
              placeholder="Value"
              value={convertValue}
              onChange={(e) => setConvertValue(e.target.value)}
              className="bg-[#0d1f3c] border-[#1e3a5f] text-white w-24 text-center"
              inputMode="decimal"
              data-testid="calc-convert-value"
            />
            <select
              value={convertFrom ?? ""}
              onChange={(e) => setConvertFrom(Number(e.target.value) as UnitOfMeasure)}
              className="bg-[#0d1f3c] border border-[#1e3a5f] text-white rounded px-2 py-1 text-sm"
              data-testid="calc-convert-unit"
            >
              {convertibleUnits.map((u) => (
                <option key={u} value={u}>
                  {unitShortLabel(u)}
                </option>
              ))}
            </select>
            <span className="text-[#7a9bb5]">to {unitShortLabel(unit)}</span>
          </div>
          {convertResult && convertResult.value > 0 && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-[#00e5ff]" data-testid="calc-convert-result">
                = {parseFloat(convertResult.value.toFixed(4))} {unitShortLabel(unit)}
              </span>
              <Button
                type="button"
                size="sm"
                onClick={applyConvert}
                className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] text-xs"
              >
                Apply
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
