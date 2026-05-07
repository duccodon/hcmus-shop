import { Kind } from "graphql";

/**
 * Lightweight test of the auth plugin's whitelist logic.
 * The plugin's effect: queries pass through, mutations require auth UNLESS
 * all top-level fields are in PUBLIC_MUTATIONS.
 *
 * We re-create the field-extraction logic here to verify the rule is correct.
 */

const PUBLIC_MUTATIONS = ["login"];

function shouldRequireAuth(operation: "query" | "mutation", topLevelFields: string[], hasUser: boolean) {
  if (operation === "query") return false; // queries are public
  const allPublic = topLevelFields.every((f) => PUBLIC_MUTATIONS.includes(f));
  if (allPublic) return false;
  return !hasUser;
}

describe("Auth plugin whitelist logic", () => {
  it("allows any query without auth", () => {
    expect(shouldRequireAuth("query", ["products"], false)).toBe(false);
  });

  it("allows the public login mutation without auth", () => {
    expect(shouldRequireAuth("mutation", ["login"], false)).toBe(false);
  });

  it("blocks createProduct without auth", () => {
    expect(shouldRequireAuth("mutation", ["createProduct"], false)).toBe(true);
  });

  it("permits createProduct with auth", () => {
    expect(shouldRequireAuth("mutation", ["createProduct"], true)).toBe(false);
  });

  it("blocks if any field is non-public", () => {
    // mixed mutation: login + createProduct
    expect(shouldRequireAuth("mutation", ["login", "createProduct"], false)).toBe(true);
  });
});
