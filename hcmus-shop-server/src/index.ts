import "dotenv/config";
import express from "express";
import cors from "cors";
import multer from "multer";
import { ApolloServer } from "@apollo/server";
import { expressMiddleware } from "@apollo/server/express4";
import { mkdirSync, readFileSync } from "fs";
import { extname, join } from "path";
import { Context, buildContext } from "./common/context";
import { authPlugin } from "./common/authPlugin";
import GraphQLJSON from "./common/jsonScalar";
import { authResolver } from "./features/auth/auth.resolver";
import { brandResolver } from "./features/brand/brand.resolver";
import { categoryResolver } from "./features/category/category.resolver";
import { seriesResolver } from "./features/series/series.resolver";
import { productResolver } from "./features/product/product.resolver";
import { promotionResolver } from "./features/promotion/promotion.resolver";

// Load .graphql type definitions
function loadTypeDef(featurePath: string): string {
  return readFileSync(join(__dirname, "features", featurePath), "utf-8");
}

const typeDefs = [
  loadTypeDef("auth/auth.typeDef.graphql"),
  loadTypeDef("brand/brand.typeDef.graphql"),
  loadTypeDef("category/category.typeDef.graphql"),
  loadTypeDef("series/series.typeDef.graphql"),
  loadTypeDef("product/product.typeDef.graphql"),
  loadTypeDef("promotion/promotion.typeDef.graphql"),
].join("\n");

// Merge resolvers by type (Query, Mutation, etc.)
function mergeResolvers(...resolversList: Record<string, unknown>[]) {
  const merged: Record<string, Record<string, unknown>> = {};
  for (const resolvers of resolversList) {
    for (const [type, fields] of Object.entries(resolvers)) {
      if (!merged[type]) merged[type] = {};
      Object.assign(merged[type], fields as Record<string, unknown>);
    }
  }
  return merged;
}

const resolvers = mergeResolvers(
  { JSON: GraphQLJSON },
  authResolver,
  brandResolver,
  categoryResolver,
  seriesResolver,
  productResolver,
  promotionResolver
);

async function main() {
  const app = express();
  const port = Number(process.env.PORT) || 4000;
  const uploadRoot = join(process.cwd(), "uploads");
  const productUploadDir = join(uploadRoot, "products");

  mkdirSync(productUploadDir, { recursive: true });

  const upload = multer({
    storage: multer.diskStorage({
      destination: (_req, _file, cb) => cb(null, productUploadDir),
      filename: (_req, file, cb) => {
        const safeExt = extname(file.originalname).toLowerCase() || ".jpg";
        const uniqueName = `${Date.now()}-${Math.round(Math.random() * 1e9)}${safeExt}`;
        cb(null, uniqueName);
      },
    }),
    limits: { fileSize: 5 * 1024 * 1024 },
    fileFilter: (_req, file, cb) => {
      const allowed = new Set([
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
      ]);

      if (!allowed.has(file.mimetype)) {
        cb(new Error("Invalid file type. Only png, jpg, jpeg, webp are allowed."));
        return;
      }

      cb(null, true);
    },
  });

  const server = new ApolloServer<Context>({
    typeDefs,
    resolvers,
    plugins: [authPlugin],
  });

  await server.start();

  app.use("/uploads", express.static(uploadRoot));

  app.post("/uploads", cors<cors.CorsRequest>(), (req, res) => {
    upload.single("file")(req, res, (err) => {
      if (err instanceof multer.MulterError) {
        console.error("[Uploads] multer error", {
          code: err.code,
          message: err.message,
        });
        if (err.code === "LIMIT_FILE_SIZE") {
          res.status(400).json({ message: "File too large. Max size is 5MB." });
          return;
        }

        res.status(400).json({ message: err.message });
        return;
      }

      if (err) {
        console.error("[Uploads] upload error", err);
        res.status(400).json({ message: err.message });
        return;
      }

      if (!req.file) {
        console.error("[Uploads] no file uploaded");
        res.status(400).json({ message: "No file uploaded." });
        return;
      }

      console.log("[Uploads] upload success", {
        fileName: req.file.filename,
        originalName: req.file.originalname,
        mimeType: req.file.mimetype,
        size: req.file.size,
      });
      res.status(200).json({ url: `/uploads/products/${req.file.filename}` });
    });
  });

  app.use(
    "/graphql",
    cors<cors.CorsRequest>(),
    express.json(),
    expressMiddleware(server, { context: buildContext })
  );

  app.listen(port, () => {
    console.log(`Server ready at http://localhost:${port}/graphql`);
  });
}

main().catch(console.error);
