import "dotenv/config";
import express from "express";
import cors from "cors";
import { ApolloServer } from "@apollo/server";
import { expressMiddleware } from "@apollo/server/express4";
import { readFileSync } from "fs";
import { join } from "path";
import { Context, buildContext } from "./common/context";
import { authPlugin } from "./common/authPlugin";
import GraphQLJSON from "./common/jsonScalar";
import { authResolver } from "./features/auth/auth.resolver";
import { brandResolver } from "./features/brand/brand.resolver";
import { categoryResolver } from "./features/category/category.resolver";
import { seriesResolver } from "./features/series/series.resolver";
import { productResolver } from "./features/product/product.resolver";

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
  productResolver
);

async function main() {
  const app = express();
  const port = Number(process.env.PORT) || 4000;

  const server = new ApolloServer<Context>({
    typeDefs,
    resolvers,
    plugins: [authPlugin],
  });

  await server.start();

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
