import { generateToken, verifyToken } from "../src/common/jwt";

describe("JWT", () => {
  it("encodes and decodes a payload", () => {
    const payload = { userId: "abc-123", username: "admin", role: "Admin" };
    const token = generateToken(payload);
    expect(typeof token).toBe("string");
    expect(token.split(".").length).toBe(3); // header.body.signature

    const decoded = verifyToken(token);
    expect(decoded.userId).toBe(payload.userId);
    expect(decoded.username).toBe(payload.username);
    expect(decoded.role).toBe(payload.role);
  });

  it("throws on a tampered token", () => {
    const token = generateToken({ userId: "x", username: "y", role: "Sale" });
    const tampered = token.slice(0, -2) + "AA";
    expect(() => verifyToken(tampered)).toThrow();
  });

  it("throws on a malformed token", () => {
    expect(() => verifyToken("not-a-jwt")).toThrow();
  });
});
