import { ApolloServerPlugin } from "@apollo/server";
import { Context } from "./context";
import { GraphQLError, Kind, FieldNode } from "graphql";

// Mutation fields that don't require authentication
const PUBLIC_MUTATIONS = ["login"];

export const authPlugin: ApolloServerPlugin<Context> = {
  async requestDidStart() {
    return {
      async didResolveOperation(requestContext) {
        const operation = requestContext.operation;
        if (!operation || operation.operation === "query") return;

        // For mutations, check if all requested fields are public
        const fields = operation.selectionSet.selections
          .filter((s): s is FieldNode => s.kind === Kind.FIELD)
          .map((s) => s.name.value);

        const allPublic = fields.every((f) => PUBLIC_MUTATIONS.includes(f));
        if (allPublic) return;

        if (!requestContext.contextValue.user) {
          throw new GraphQLError("Authentication required", {
            extensions: { code: "UNAUTHENTICATED" },
          });
        }
      },
    };
  },
};
