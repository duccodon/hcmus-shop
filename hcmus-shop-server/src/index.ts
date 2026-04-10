import "dotenv/config";
import express from "express";
import cors from "cors";
import { ApolloServer } from "@apollo/server";
import { expressMiddleware } from "@apollo/server/express4";
import { PrismaClient } from "@prisma/client";
import { readFileSync } from "fs";
import { join } from "path";
import { Context, getUser } from "./middleware/auth";
import { authResolvers } from "./schema/resolvers/auth";
import { brandResolvers } from "./schema/resolvers/brand";
import { categoryResolvers } from "./schema/resolvers/category";
import { seriesResolvers } from "./schema/resolvers/series";
import { productResolvers } from "./schema/resolvers/product";
import GraphQLJSON from "./utils/jsonScalar";

// Load .graphql files
function loadTypeDef(filename: string): string {
  return readFileSync(
    join(__dirname, "schema", "typeDefs", filename),
    "utf-8"
  );
}

const typeDefs = [
  loadTypeDef("auth.graphql"),
  loadTypeDef("brand.graphql"),
  loadTypeDef("category.graphql"),
  loadTypeDef("series.graphql"),
  loadTypeDef("product.graphql"),
].join("\n");

const prisma = new PrismaClient();

// Merge resolvers
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
  authResolvers(prisma),
  brandResolvers(prisma),
  categoryResolvers(prisma),
  seriesResolvers(prisma),
  productResolvers(prisma)
);

async function main() {
  const app = express();
  const port = Number(process.env.PORT) || 4000;

  const server = new ApolloServer<Context>({
    typeDefs,
    resolvers,
  });

  await server.start();

  app.use(
    "/graphql",
    cors<cors.CorsRequest>(),
    express.json(),
    expressMiddleware(server, {
      context: async ({ req }) => ({
        user: getUser(req.headers.authorization),
      }),
    })
  );

  app.listen(port, () => {
    console.log(`Server ready at http://localhost:${port}/graphql`);
  });
}

main().catch(console.error);
