import { Router, Request, Response } from "express";
import multer from "multer";
import { spawn } from "child_process";
import { requireRoleRest } from "../../common/restAuth";

/**
 * Backup / Restore the entire PostgreSQL database via pg_dump and psql.
 * We invoke them inside the postgres Docker container (`docker exec`)
 * so devs don't need to install the postgres CLI tools on the host.
 *
 * Admin-only: a backup is a full DB dump; a restore wipes & replaces
 * the entire database. Both require Admin role.
 */
export const backupRouter = Router();

backupRouter.use(requireRoleRest("Admin"));

const PG_CONTAINER = process.env.POSTGRES_CONTAINER ?? "hcmus-shop-db";
const PG_USER = process.env.POSTGRES_USER ?? "postgres";
const PG_DB = process.env.POSTGRES_DB ?? "MyShop2026";

backupRouter.get("/backup", (_req: Request, res: Response) => {
  res.setHeader("Content-Type", "application/sql");
  res.setHeader(
    "Content-Disposition",
    `attachment; filename=hcmus-shop-backup-${Date.now()}.sql`
  );

  const dump = spawn("docker", [
    "exec",
    PG_CONTAINER,
    "pg_dump",
    "-U", PG_USER,
    "--clean",
    "--if-exists",
    PG_DB,
  ]);
  dump.stdout.pipe(res);
  dump.stderr.on("data", (chunk) => {
    console.error("[pg_dump stderr]", chunk.toString());
  });
  dump.on("error", (err) => {
    console.error("[pg_dump error]", err);
    if (!res.headersSent) {
      res.status(500).json({ error: err.message });
    }
  });
  dump.on("close", (code) => {
    if (code !== 0 && !res.headersSent) {
      res.status(500).json({ error: `pg_dump exited with code ${code}` });
    }
  });
});

const uploadMemory = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 200 * 1024 * 1024 }, // 200 MB
});

backupRouter.post(
  "/restore",
  uploadMemory.single("file"),
  (req: Request, res: Response) => {
    if (!req.file) {
      return res.status(400).json({ error: "No file uploaded" });
    }

    const psql = spawn("docker", [
      "exec",
      "-i",
      PG_CONTAINER,
      "psql",
      "-U", PG_USER,
      PG_DB,
    ]);
    let stderr = "";
    psql.stderr.on("data", (chunk) => (stderr += chunk.toString()));
    psql.on("error", (err) => {
      console.error("[psql error]", err);
      res.status(500).json({ error: err.message });
    });
    psql.on("close", (code) => {
      if (code === 0) {
        res.json({ ok: true });
      } else {
        res.status(500).json({ error: stderr || `psql exited ${code}` });
      }
    });

    psql.stdin.write(req.file.buffer);
    psql.stdin.end();
  }
);
