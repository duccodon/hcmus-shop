# Feature 12 — Data Seeding

**Owner**: Dev A
**Status**: ✅ Implemented (commit `a470088`) — procedural variants generate 25+ per category
**Files**: `hcmus-shop-server/prisma/seed.ts`

## Summary

Generate realistic seed data: 5 categories, 25 products per category, 3 images per product, 5 ProductInstances (serial numbers) per product. Total: 125 products, 375 images, 625 instances.

## Why 125+ products

Instructor requirement: "Mỗi loại sản phẩm có tối thiểu 22 sản phẩm" (minimum 22 products per category).

## Data plan

### Brands (already seeded — 7)
ASUS, Dell, HP, Lenovo, Acer, MSI, Apple

### Categories (already seeded — 5)
Gaming, Business, Student, Ultrabook, Workstation

### Products (need to expand from 12 → 125)

Strategy: instead of hand-typing 125 entries, use procedural generation.

```typescript
const productsPerCategory = 25;
const categories = ["Gaming", "Business", "Student", "Ultrabook", "Workstation"];

const productTemplates = {
  Gaming: [
    { brand: "ASUS", model: "ROG Strix G16", basePrice: 32990000 },
    { brand: "Acer", model: "Predator Helios", basePrice: 34990000 },
    // ... etc
  ],
  Business: [...],
  // ...
};

for (const category of categories) {
  for (let i = 0; i < productsPerCategory; i++) {
    const template = pickTemplate(category, i);
    const variation = i; // for SKU uniqueness
    products.push({
      sku: `${template.brand.toUpperCase()}-${template.model.replace(/\s/g, "-")}-V${variation}`,
      name: `${template.model} ${variation > 0 ? `Edition ${variation}` : ""}`,
      brandName: template.brand,
      importPrice: Math.round(template.basePrice * 0.85),
      sellingPrice: template.basePrice + variation * 1000000,
      // ... specs
    });
  }
}
```

### Images
For each product, attach 3 placeholder URLs:
```typescript
const placeholderImages = [
  `/uploads/products/seed-${product.sku}-1.jpg`,
  `/uploads/products/seed-${product.sku}-2.jpg`,
  `/uploads/products/seed-${product.sku}-3.jpg`,
];
```

We'll need to copy actual placeholder image files into `uploads/products/` (3 generic laptop images named after each SKU). Or use a shared placeholder URL and document that "real images need to be uploaded by users."

**Simpler approach**: have ONE placeholder file `uploads/products/placeholder.jpg` and reference it 3× per product. Real images uploaded via the AddProduct flow.

### ProductInstances
For each product, create 5 instances:
```typescript
for (let i = 1; i <= 5; i++) {
  instances.push({
    productId: product.productId,
    serialNumber: `${product.sku}-SN${String(i).padStart(4, "0")}`,
    status: "Available",
  });
}
```

## Files

### Modified
| File | Change |
|------|--------|
| `prisma/seed.ts` | Replace hardcoded 12 products with procedural generation |

## Run

```bash
cd hcmus-shop-server
npx ts-node prisma/seed.ts
```

Output:
```
Admin user created: admin
Sale user created: sale
Brands seeded
Categories seeded
Products seeded: 125
Images seeded: 375
Instances seeded: 625
```

## Verification

```sql
-- Run in psql or via Prisma Studio
SELECT COUNT(*) FROM products WHERE is_active = true;  -- Expect 125
SELECT COUNT(*) FROM product_images;                   -- Expect 375
SELECT COUNT(*) FROM product_instances;                -- Expect 625
SELECT category.name, COUNT(*) AS product_count
FROM products p
JOIN product_categories pc ON p.product_id = pc.product_id
JOIN categories c ON c.category_id = pc.category_id
GROUP BY c.name;
-- Expect: each category >= 25
```

## Idempotency

Seed script should be safe to re-run:
- Use `findFirst` then create/skip pattern (or `upsert` where keys allow)
- For products, key by `sku` which has unique constraint
- For instances, key by `serialNumber`

## Edge cases

| Case | Behavior |
|------|----------|
| Seed run on populated DB | Products with same SKU → skipped (or error) |
| Brand/category missing | Skip that template, log warning |
| Placeholder image file missing | Image URLs still saved; client shows broken image — acceptable for demo |

## Extension points

- Generate orders too (mock sales for dashboard testing)
- Vary stock quantities (some products with < 5 to populate Low Stock table)
- Realistic timestamps spread across last 6 months (for revenue chart)
