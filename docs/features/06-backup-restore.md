# Feature 06 — Backup / Restore Database

**Owner**: Dev A
**Status**: ✅ Implemented (commit `0f82a8a`)
**Bonus**: +0.25 pts
**Phase**: 5
**Files**:
- Backend: `hcmus-shop-server/src/features/backup/backup.routes.ts`
- Client service: `hcmus-shop/Services/Backup/BackupService.cs`, `Contracts/Services/IBackupService.cs`
- UI: Backup section in `hcmus-shop/Views/Pages/Settings/SettingsPage.xaml`
- Commands: `DownloadBackupAsync`, `RestoreAsync` in `SettingsViewModel`
- Flow trace: see [FLOWS.md](../FLOWS.md) Flow 9

## Summary

Two REST endpoints on the server that wrap `pg_dump` and `psql` to export and re-import the entire PostgreSQL database. Triggered from the Settings page.

## User-visible behavior

### Backup
1. User opens Settings → Backup section
2. Clicks "Download Backup"
3. Server runs `pg_dump`, streams the SQL file
4. Client saves to user-chosen path
5. Toast: "Backup saved to <path>"

### Restore
1. User opens Settings → Backup section
2. Clicks "Restore from File"
3. File picker opens → user selects an `.sql` file
4. Confirmation dialog: "This will replace ALL current data. Continue?"
5. Client uploads file to server
6. Server runs `psql` to apply
7. Toast: "Restore complete"

## Architecture

```
Client                                 Server
──────                                 ──────
SettingsPage
   │
   │ Click "Backup"
   ▼
GET /backup
   │ ─────────────────────────────► Express route
   │                                    │
   │                                    ▼
   │                                 spawn `pg_dump`
   │                                    │
   │ ◄───────────── stream sql ──────  pipes stdout to response
   │
Saves to file via FilePicker
```

## Files

### Backend (new)
| File | Purpose |
|------|---------|
| `src/features/backup/backup.routes.ts` | Express route handlers |

### Backend (modified)
| File | Change |
|------|--------|
| `src/index.ts` | Mount backup routes |

### Client (new)
| File | Purpose |
|------|---------|
| `Services/Backup/BackupService.cs` | HTTP client for backup endpoints |
| `Contracts/Services/IBackupService.cs` | Interface |

### Client (modified)
| File | Change |
|------|--------|
| `Views/Pages/Settings/SettingsPage.xaml` | Backup/Restore section |
| `ViewModels/Settings/SettingsViewModel.cs` | Backup/Restore commands |

## Implementation outline

### Backend
```typescript
// backup.routes.ts
import { Router } from "express";
import { spawn } from "child_process";
import multer from "multer";

export const backupRouter = Router();

backupRouter.get("/backup", (_req, res) => {
  res.setHeader("Content-Type", "application/sql");
  res.setHeader("Content-Disposition", `attachment; filename=backup-${Date.now()}.sql`);

  const dump = spawn("pg_dump", [process.env.DATABASE_URL!]);
  dump.stdout.pipe(res);
  dump.stderr.on("data", (d) => console.error(d.toString()));
  dump.on("error", (err) => res.status(500).json({ error: err.message }));
});

const upload = multer({ storage: multer.memoryStorage() });
backupRouter.post("/restore", upload.single("file"), (req, res) => {
  if (!req.file) return res.status(400).json({ error: "No file" });

  const psql = spawn("psql", [process.env.DATABASE_URL!]);
  psql.stdin.write(req.file.buffer);
  psql.stdin.end();
  psql.on("close", (code) =>
    code === 0 ? res.json({ ok: true }) : res.status(500).json({ error: "Restore failed" })
  );
});
```

### Client
```csharp
public async Task<string?> DownloadBackupAsync()
{
    using var resp = await _http.GetAsync($"{baseUrl}/backup");
    var bytes = await resp.Content.ReadAsByteArrayAsync();
    var savePicker = new FileSavePicker { ... };
    var file = await savePicker.PickSaveFileAsync();
    if (file != null) await FileIO.WriteBytesAsync(file, bytes);
    return file?.Path;
}
```

## Business rules

- Backup is admin-only (check role in route handler)
- Restore is admin-only
- Server must have `pg_dump` and `psql` in PATH (Docker postgres container has them)
- No size limit on backup file (streams)
- Restore drops + recreates current data

## Edge cases

| Case | Behavior |
|------|----------|
| `pg_dump` not in PATH | 500 error with setup help message |
| Backup file too big | Streams, no memory issue |
| Restore SQL is malformed | psql exits non-zero → 500 returned |
| Restore mid-operation interrupted | DB may be in partial state — manual recovery needed |

## Verification

```bash
# Test backup
curl http://localhost:4000/backup -o backup.sql
# Verify SQL file contains CREATE TABLE statements

# Test restore (after dropping some data)
curl -F "file=@backup.sql" http://localhost:4000/restore
# Verify data is back
```

## Extension points

- Add scheduled automatic backups (cron)
- Encrypt backup file
- Compress (gzip) for smaller files
- Backup history page (list previous backups)
- Cloud storage upload (S3, Google Drive)
