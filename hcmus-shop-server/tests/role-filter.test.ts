import { Prisma } from "@prisma/client";
import { Context } from "../src/common/context";
import { productResolver } from "../src/features/product/product.resolver";

/**
 * Tests the role-based filter on Product.importPrice.
 * Pure unit test — no database or HTTP needed.
 */
describe("Product.importPrice role filter", () => {
  const decimalPrice = new Prisma.Decimal(28000000);
  const parent = { importPrice: decimalPrice };

  function callResolver(role: string | undefined) {
    const context: Context = {
      user: role ? { userId: "x", username: "u", role } : null,
    };
    return (productResolver.Product.importPrice as Function)(parent, undefined, context);
  }

  it("returns the price for Admin", () => {
    expect(callResolver("Admin")).toBe(28000000);
  });

  it("returns null for Sale", () => {
    expect(callResolver("Sale")).toBeNull();
  });

  it("returns null for unauthenticated user", () => {
    expect(callResolver(undefined)).toBeNull();
  });

  it("returns null for unknown role", () => {
    expect(callResolver("Manager")).toBeNull();
  });
});
