import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { QuantitySelector } from "@/components/quantity-selector";

describe("QuantitySelector", () => {
  it("renders current quantity value", () => {
    render(<QuantitySelector value={3} onChange={vi.fn()} />);
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("calls onChange with decremented value on minus click", () => {
    const onChange = vi.fn();
    render(<QuantitySelector value={3} onChange={onChange} />);

    const buttons = screen.getAllByRole("button");
    fireEvent.click(buttons[0]); // minus button
    expect(onChange).toHaveBeenCalledWith(2);
  });

  it("calls onChange with incremented value on plus click", () => {
    const onChange = vi.fn();
    render(<QuantitySelector value={3} onChange={onChange} />);

    const buttons = screen.getAllByRole("button");
    fireEvent.click(buttons[1]); // plus button
    expect(onChange).toHaveBeenCalledWith(4);
  });

  it("disables minus button when value equals min", () => {
    render(<QuantitySelector value={1} onChange={vi.fn()} min={1} />);

    const buttons = screen.getAllByRole("button");
    expect(buttons[0]).toBeDisabled();
  });

  it("disables plus button when value equals max", () => {
    render(<QuantitySelector value={20} onChange={vi.fn()} max={20} />);

    const buttons = screen.getAllByRole("button");
    expect(buttons[1]).toBeDisabled();
  });

  it("respects custom min and max values", () => {
    render(<QuantitySelector value={5} onChange={vi.fn()} min={5} max={5} />);

    const buttons = screen.getAllByRole("button");
    expect(buttons[0]).toBeDisabled(); // at min
    expect(buttons[1]).toBeDisabled(); // at max
  });
});
