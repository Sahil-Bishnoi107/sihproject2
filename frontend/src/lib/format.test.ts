import { describe, expect, it } from "vitest";
import { expressionToText, formatINR, formatINRShort } from "./format";

describe("formatting", () => {
  it("formats rupees with Indian digit grouping", () => {
    expect(formatINR(6000000)).toBe("₹60,00,000");
  });

  it("shortens large amounts to lakh and crore", () => {
    expect(formatINRShort(5000000)).toBe("₹50 lakh");
    expect(formatINRShort(25000000)).toBe("₹2.5 crore");
    expect(formatINRShort(90000)).toBe("₹90,000");
  });

  it("renders rule expressions as readable text", () => {
    expect(expressionToText({ field: "turnover.avg3y", op: ">=", value: 5000000 })).toBe("turnover.avg3y ≥ 50,00,000");
    expect(
      expressionToText({
        all: [
          { field: "gstin", op: "exists" },
          { field: "gstin", op: "verified", source: "GST" },
        ],
      }),
    ).toBe("gstin is present AND gstin verified with GST");
    expect(expressionToText({ not: { field: "x", op: "==", value: "y" } })).toBe("NOT (x = y)");
    expect(expressionToText({ field: "iso9001.expiry", op: "after", value: "$bidEndDate" })).toBe(
      "iso9001.expiry after bid end date",
    );
  });
});
