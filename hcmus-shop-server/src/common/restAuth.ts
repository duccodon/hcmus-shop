import { Request, Response, NextFunction } from "express";
import { verifyToken, JwtPayload } from "./jwt";

declare module "express-serve-static-core" {
  interface Request {
    user?: JwtPayload;
  }
}

/**
 * Express middleware that mirrors the GraphQL authPlugin policy for REST routes.
 * Verifies the Bearer token in Authorization header; on success sets req.user.
 * On failure responds 401 and stops the chain.
 */
export function requireAuthRest(req: Request, res: Response, next: NextFunction) {
  const header = req.headers.authorization;
  if (!header) {
    return res.status(401).json({ error: "Authentication required" });
  }
  const token = header.startsWith("Bearer ") ? header.slice(7) : header;
  try {
    req.user = verifyToken(token);
    next();
  } catch {
    return res.status(401).json({ error: "Invalid or expired token" });
  }
}

/**
 * Requires an authenticated user with the given role.
 * Composes: auth check first (401 on miss), then role check (403 on miss).
 */
export function requireRoleRest(role: string) {
  return (req: Request, res: Response, next: NextFunction) => {
    requireAuthRest(req, res, () => {
      if (req.user?.role !== role) {
        return res.status(403).json({ error: `Role '${role}' required` });
      }
      next();
    });
  };
}
