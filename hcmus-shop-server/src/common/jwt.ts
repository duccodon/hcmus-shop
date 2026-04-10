import jwt from "jsonwebtoken";

const JWT_SECRET = process.env.JWT_SECRET || "hcmus-shop-secret";
const JWT_EXPIRES_IN = process.env.JWT_EXPIRES_IN || "7d";

export interface JwtPayload {
  userId: string;
  username: string;
  role: string;
}

export function generateToken(payload: JwtPayload): string {
  return jwt.sign(
    { userId: payload.userId, username: payload.username, role: payload.role },
    JWT_SECRET,
    { expiresIn: JWT_EXPIRES_IN as jwt.SignOptions["expiresIn"] }
  );
}

export function verifyToken(token: string): JwtPayload {
  return jwt.verify(token, JWT_SECRET) as JwtPayload;
}
