import { Request } from "express";
import { verifyToken, JwtPayload } from "./jwt";

export interface Context {
  user: JwtPayload | null;
}

function getUserFromToken(token?: string): JwtPayload | null {
  if (!token) return null;

  try {
    const cleaned = token.startsWith("Bearer ") ? token.slice(7) : token;
    return verifyToken(cleaned);
  } catch {
    return null;
  }
}

export async function buildContext({
  req,
}: {
  req: Request;
}): Promise<Context> {
  return {
    user: getUserFromToken(req.headers.authorization),
  };
}

export function requireAuth(context: Context): JwtPayload {
  if (!context.user) {
    throw new Error("Authentication required");
  }
  return context.user;
}

export function requireRole(context: Context, role: string): JwtPayload {
  const user = requireAuth(context);
  if (user.role !== role) {
    throw new Error(`Role '${role}' required`);
  }
  return user;
}
