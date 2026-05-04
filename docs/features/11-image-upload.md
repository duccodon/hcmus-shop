# Feature 11 — Image Upload Endpoint

**Owner**: Dev A
**Status**: Planned
**Phase**: 1 (priority 0 — unblocks Dev B's AddProduct)

## Summary

A REST endpoint on the GraphQL server that accepts file uploads (multipart/form-data) and saves them to the server's local filesystem. Returns a URL that can be stored in `ProductImage.imageUrl` and displayed by the WinUI client over HTTP.

## Why REST instead of GraphQL?

GraphQL doesn't natively handle binary uploads well. A simple REST endpoint is the standard pattern, even in GraphQL-first APIs. The server already exposes Express, so this is one extra route.

## User-visible behavior

When the shop owner adds a new product:
1. User picks one or more image files via the WinUI file picker
2. Client uploads each file to `POST http://localhost:4000/uploads`
3. Server returns `{ url: "/uploads/products/<unique-id>.jpg" }`
4. Client attaches those URLs to the `createProduct` mutation
5. Later, anyone viewing the product fetches `http://localhost:4000/uploads/products/<unique-id>.jpg` directly

## Architecture

```
┌────────────────────┐     POST /uploads (multipart)       ┌─────────────────┐
│  WinUI AddProduct  │ ──────────────────────────────────► │  Express server │
│  (file picker)     │ ◄────────────────────────────────── │  multer + fs    │
└────────────────────┘     { url: "/uploads/xxx.jpg" }     └────────┬────────┘
                                                                    │ saves to
                                                                    ▼
                                                            uploads/products/

Later display:
┌────────────────────┐     GET /uploads/xxx.jpg            ┌─────────────────┐
│  WinUI Image ctrl  │ ──────────────────────────────────► │ express.static  │
│                    │ ◄────────────────────────────────── │                 │
└────────────────────┘     <image bytes>                   └─────────────────┘
```

## Files

| File | Purpose |
|------|---------|
| `hcmus-shop-server/src/index.ts` | Adds the upload route + static serving (existing file, edit) |
| `hcmus-shop-server/uploads/` | Directory created at startup; gitignored |
| `hcmus-shop-server/uploads/products/` | Where uploaded product images land |
| `hcmus-shop-server/.gitignore` | Add `uploads/` to it |

## Data flow

### Upload
1. Client builds `multipart/form-data` with field name `file` and a binary blob
2. POSTs to `http://<server>/uploads`
3. `multer` middleware parses the multipart, saves the file to `uploads/products/<random-uuid>.<ext>`, and attaches `req.file`
4. Route handler returns JSON: `{ url: "/uploads/products/<random-uuid>.<ext>" }`

### Display
1. Client sets `Image.Source = "http://<server-base>/uploads/products/xxx.jpg"`
2. `express.static('uploads')` middleware serves the file directly from disk
3. WinUI's `BitmapImage` fetches over HTTP and renders

## Implementation details

### multer config
- Storage: `multer.diskStorage` with custom filename to prevent collisions (use `uuid` or timestamp + random)
- Destination: `uploads/products/`
- Limits: max 5 MB per file, accept only `image/*` MIME types

### Code outline (added to `src/index.ts`)
```typescript
import multer from "multer";
import { v4 as uuid } from "uuid";

const storage = multer.diskStorage({
  destination: (_req, _file, cb) => cb(null, "uploads/products"),
  filename: (_req, file, cb) => {
    const ext = path.extname(file.originalname);
    cb(null, `${uuid()}${ext}`);
  },
});

const upload = multer({
  storage,
  limits: { fileSize: 5 * 1024 * 1024 },
  fileFilter: (_req, file, cb) => {
    cb(null, file.mimetype.startsWith("image/"));
  },
});

app.post("/uploads", upload.single("file"), (req, res) => {
  if (!req.file) return res.status(400).json({ error: "No file" });
  res.json({ url: `/uploads/products/${req.file.filename}` });
});

app.use("/uploads", express.static("uploads"));
```

## Business rules

- Max upload size: 5 MB
- Only `image/*` MIME types allowed (jpg, png, webp, gif)
- Filenames are randomized — no risk of overwrite, no path traversal
- No authentication required for upload (simplification — could be added later)

## Edge cases

| Case | Behavior |
|------|----------|
| No file in request | 400 with `{ error: "No file" }` |
| File > 5 MB | 413 (multer rejects automatically) |
| Non-image MIME type | File silently dropped by fileFilter, response 400 |
| Disk full | Express returns 500 |
| Race condition (two uploads same instant) | UUIDs prevent collision |

## Verification

```bash
# Test upload
curl -F "file=@/path/to/test.jpg" http://localhost:4000/uploads
# Expect: {"url":"/uploads/products/abc-123.jpg"}

# Test display
curl http://localhost:4000/uploads/products/abc-123.jpg --output downloaded.jpg
# Should download the same file

# Test rejection
curl -F "file=@/path/to/test.txt" http://localhost:4000/uploads
# Expect: 400

# Test size limit
curl -F "file=@/path/to/huge-10mb.jpg" http://localhost:4000/uploads
# Expect: 413
```

## Extension points

- Add JWT auth check (decode token from header, require valid user)
- Add image resizing/compression on the fly (sharp library)
- Move storage to S3 / Cloudinary for production
- Add background cleanup job to delete orphaned images (no product references them)
