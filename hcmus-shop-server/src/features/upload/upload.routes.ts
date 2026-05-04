import { Router, Request, Response } from "express";
import multer from "multer";
import { v4 as uuid } from "uuid";
import path from "path";
import fs from "fs";

const UPLOAD_DIR = "uploads/products";
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB

// Ensure upload directory exists
if (!fs.existsSync(UPLOAD_DIR)) {
  fs.mkdirSync(UPLOAD_DIR, { recursive: true });
}

const storage = multer.diskStorage({
  destination: (_req, _file, cb) => cb(null, UPLOAD_DIR),
  filename: (_req, file, cb) => {
    const ext = path.extname(file.originalname).toLowerCase();
    cb(null, `${uuid()}${ext}`);
  },
});

const upload = multer({
  storage,
  limits: { fileSize: MAX_FILE_SIZE },
  fileFilter: (_req, file, cb) => {
    if (file.mimetype.startsWith("image/")) {
      cb(null, true);
    } else {
      cb(new Error("Only image files are allowed"));
    }
  },
});

export const uploadRouter = Router();

/**
 * POST /uploads
 * Multipart form-data with field "file" (single image, <= 5MB).
 * Returns: { url: "/uploads/products/<uuid>.<ext>" }
 */
uploadRouter.post("/uploads", (req: Request, res: Response) => {
  upload.single("file")(req, res, (err) => {
    if (err) {
      return res.status(400).json({ error: err.message });
    }
    if (!req.file) {
      return res.status(400).json({ error: "No file uploaded" });
    }
    res.json({ url: `/uploads/products/${req.file.filename}` });
  });
});
