import { Router, Request, Response } from "express";

/**
 * GET /health
 * Lightweight liveness probe used by the WinUI client to verify the server
 * is reachable BEFORE the user tries to log in. Returns 200 with a minimal
 * JSON payload. No auth required.
 */
export const healthRouter = Router();

healthRouter.get("/health", (_req: Request, res: Response) => {
  res.json({
    ok: true,
    timestamp: new Date().toISOString(),
    service: "hcmus-shop-server",
  });
});
