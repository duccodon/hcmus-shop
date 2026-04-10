import { GraphQLScalarType, Kind, ValueNode } from "graphql";

function parseLiteralValue(ast: ValueNode): unknown {
  switch (ast.kind) {
    case Kind.STRING:
      return ast.value;
    case Kind.INT:
      return parseInt(ast.value, 10);
    case Kind.FLOAT:
      return parseFloat(ast.value);
    case Kind.BOOLEAN:
      return ast.value;
    case Kind.OBJECT: {
      const obj: Record<string, unknown> = {};
      ast.fields.forEach((field) => {
        obj[field.name.value] = parseLiteralValue(field.value);
      });
      return obj;
    }
    case Kind.LIST:
      return ast.values.map((v) => parseLiteralValue(v));
    case Kind.NULL:
      return null;
    default:
      return null;
  }
}

const GraphQLJSON: GraphQLScalarType = new GraphQLScalarType({
  name: "JSON",
  description: "Arbitrary JSON value",
  serialize(value: unknown) {
    return value;
  },
  parseValue(value: unknown) {
    return value;
  },
  parseLiteral(ast: ValueNode) {
    return parseLiteralValue(ast);
  },
});

export default GraphQLJSON;
