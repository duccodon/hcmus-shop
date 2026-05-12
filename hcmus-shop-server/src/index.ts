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
import { dashboardResolver } from "./features/dashboard/dashboard.resolver";
import { promotionResolver } from "./features/promotion/promotion.resolver";
import { customerResolver } from "./features/customer/customer.resolver";
import { orderResolver } from "./features/order/order.resolver";
import { reportResolver } from "./features/report/report.resolver";
import { userResolver } from "./features/user/user.resolver";
import { uploadRouter } from "./features/upload/upload.routes";
import { backupRouter } from "./features/backup/backup.routes";
import { healthRouter } from "./features/health/health.routes";

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
  loadTypeDef("dashboard/dashboard.typeDef.graphql"),
  loadTypeDef("promotion/promotion.typeDef.graphql"),
  loadTypeDef("customer/customer.typeDef.graphql"),
  loadTypeDef("order/order.typeDef.graphql"),
  loadTypeDef("report/report.typeDef.graphql"),
  loadTypeDef("user/user.typeDef.graphql"),
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
  dashboardResolver,
  promotionResolver,
  customerResolver,
  orderResolver,
  reportResolver,
  userResolver
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

  // Enable CORS for all routes (uploads + static files + graphql)
  app.use(cors<cors.CorsRequest>());

  // REST: health check (pre-flight liveness probe used by the client)
  app.use(healthRouter);

  // REST: file upload endpoint (admin-only, see uploadRouter)
  app.use(uploadRouter);

  // REST: backup / restore (admin tooling)
  app.use(backupRouter);

  // Static: serve uploaded files
  app.use("/uploads", express.static("uploads"));

  // GraphQL endpoint
  app.use(
    "/graphql",
    express.json(),
    expressMiddleware(server, { context: buildContext })
  );

  app.listen(port, () => {
    console.log(`Server ready at http://localhost:${port}/graphql`);
  });
}

main().catch(console.error);
