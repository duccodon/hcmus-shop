import { PrismaClient } from "@prisma/client";
import bcrypt from "bcrypt";

const prisma = new PrismaClient();

type ProductSeed = {
  sku: string;
  name: string;
  brandName: string;
  importPrice: number;
  sellingPrice: number;
  warrantyMonths: number;
  description: string;
  specifications: Record<string, string>;
  categoryNames: string[];
};

async function main() {
  // Seed admin user
  const adminPassword = await bcrypt.hash("admin123", 10);
  const admin = await prisma.user.upsert({
    where: { username: "admin" },
    update: {},
    create: {
      username: "admin",
      passwordHash: adminPassword,
      fullName: "Administrator",
      role: "Admin",
    },
  });
  console.log("Admin user created:", admin.username);

  // Seed sale user
  const salePassword = await bcrypt.hash("sale123", 10);
  const sale = await prisma.user.upsert({
    where: { username: "sale" },
    update: {},
    create: {
      username: "sale",
      passwordHash: salePassword,
      fullName: "Sales Staff",
      role: "Sale",
    },
  });
  console.log("Sale user created:", sale.username);

  // Seed brands
  const existingBrands = await prisma.brand.findMany();
  if (existingBrands.length === 0) {
    await prisma.brand.createMany({
      data: [
        { name: "ASUS", description: "Taiwan-based multinational" },
        { name: "Dell", description: "American technology company" },
        { name: "HP", description: "Hewlett-Packard" },
        { name: "Lenovo", description: "Chinese technology company" },
        { name: "Acer", description: "Taiwanese electronics company" },
        { name: "MSI", description: "Micro-Star International" },
        { name: "Apple", description: "American technology company" },
      ],
    });
    console.log("Brands seeded");
  }

  // Seed categories
  const existingCategories = await prisma.category.findMany();
  if (existingCategories.length === 0) {
    await prisma.category.createMany({
      data: [
        { name: "Gaming", description: "High-performance gaming laptops" },
        { name: "Business", description: "Professional business laptops" },
        { name: "Student", description: "Budget-friendly student laptops" },
        {
          name: "Ultrabook",
          description: "Thin and lightweight premium laptops",
        },
        { name: "Workstation", description: "Heavy-duty workstation laptops" },
      ],
    });
    console.log("Categories seeded");
  }

  // Seed products (idempotent)
  const brandMap = new Map(
    (await prisma.brand.findMany()).map((brand) => [brand.name, brand.brandId])
  );
  const categoryMap = new Map(
    (await prisma.category.findMany()).map((category) => [
      category.name,
      category.categoryId,
    ])
  );

  const products: ProductSeed[] = [
    {
      sku: "ASUS-ROG-G16-2026",
      name: "ASUS ROG Strix G16",
      brandName: "ASUS",
      importPrice: 28000000,
      sellingPrice: 32990000,
      warrantyMonths: 24,
      description: "Gaming laptop with RTX graphics and high refresh display.",
      specifications: {
        cpu: "Intel Core i7-14650HX",
        ram: "16GB DDR5",
        gpu: "RTX 4060 8GB",
        storage: "1TB SSD",
        screen: "16-inch FHD+ 165Hz",
      },
      categoryNames: ["Gaming", "Workstation"],
    },
    {
      sku: "ASUS-TUF-A15-2026",
      name: "ASUS TUF A15",
      brandName: "ASUS",
      importPrice: 20500000,
      sellingPrice: 23990000,
      warrantyMonths: 24,
      description: "Durable gaming laptop with Ryzen and RTX GPU.",
      specifications: {
        cpu: "AMD Ryzen 7 8845HS",
        ram: "16GB DDR5",
        gpu: "RTX 4050 6GB",
        storage: "512GB SSD",
        screen: "15.6-inch FHD 144Hz",
      },
      categoryNames: ["Gaming", "Student"],
    },
    {
      sku: "DELL-XPS-13-PLUS-2026",
      name: "Dell XPS 13 Plus",
      brandName: "Dell",
      importPrice: 30500000,
      sellingPrice: 35990000,
      warrantyMonths: 24,
      description: "Premium ultrabook for productivity and portability.",
      specifications: {
        cpu: "Intel Core Ultra 7 155H",
        ram: "16GB LPDDR5x",
        gpu: "Intel Arc Graphics",
        storage: "1TB SSD",
        screen: "13.4-inch 3K OLED",
      },
      categoryNames: ["Ultrabook", "Business"],
    },
    {
      sku: "DELL-LATITUDE-5440",
      name: "Dell Latitude 5440",
      brandName: "Dell",
      importPrice: 21400000,
      sellingPrice: 25990000,
      warrantyMonths: 24,
      description: "Business laptop focused on security and battery life.",
      specifications: {
        cpu: "Intel Core i5-1345U",
        ram: "16GB DDR4",
        gpu: "Intel Iris Xe",
        storage: "512GB SSD",
        screen: "14-inch FHD",
      },
      categoryNames: ["Business", "Student"],
    },
    {
      sku: "HP-OMEN-16-2026",
      name: "HP Omen 16",
      brandName: "HP",
      importPrice: 24600000,
      sellingPrice: 28990000,
      warrantyMonths: 24,
      description: "High-performance gaming laptop with strong cooling.",
      specifications: {
        cpu: "Intel Core i7-14700HX",
        ram: "16GB DDR5",
        gpu: "RTX 4060 8GB",
        storage: "1TB SSD",
        screen: "16.1-inch QHD 165Hz",
      },
      categoryNames: ["Gaming"],
    },
    {
      sku: "HP-ELITEBOOK-840-G10",
      name: "HP EliteBook 840 G10",
      brandName: "HP",
      importPrice: 23800000,
      sellingPrice: 27990000,
      warrantyMonths: 24,
      description: "Enterprise business laptop with premium build quality.",
      specifications: {
        cpu: "Intel Core i7-1365U",
        ram: "16GB DDR5",
        gpu: "Intel Iris Xe",
        storage: "512GB SSD",
        screen: "14-inch WUXGA",
      },
      categoryNames: ["Business", "Ultrabook"],
    },
    {
      sku: "LENOVO-LEGION-5I-2026",
      name: "Lenovo Legion 5i",
      brandName: "Lenovo",
      importPrice: 25800000,
      sellingPrice: 30490000,
      warrantyMonths: 24,
      description: "Gaming laptop with balanced thermals and strong keyboard.",
      specifications: {
        cpu: "Intel Core i7-14650HX",
        ram: "16GB DDR5",
        gpu: "RTX 4060 8GB",
        storage: "1TB SSD",
        screen: "16-inch WQXGA 165Hz",
      },
      categoryNames: ["Gaming", "Workstation"],
    },
    {
      sku: "LENOVO-THINKPAD-X1-CARBON-G12",
      name: "Lenovo ThinkPad X1 Carbon Gen 12",
      brandName: "Lenovo",
      importPrice: 33200000,
      sellingPrice: 38990000,
      warrantyMonths: 36,
      description: "Premium business ultrabook with lightweight chassis.",
      specifications: {
        cpu: "Intel Core Ultra 7 165U",
        ram: "32GB LPDDR5x",
        gpu: "Intel Graphics",
        storage: "1TB SSD",
        screen: "14-inch 2.8K OLED",
      },
      categoryNames: ["Business", "Ultrabook"],
    },
    {
      sku: "ACER-PREDATOR-HELIOS-16",
      name: "Acer Predator Helios 16",
      brandName: "Acer",
      importPrice: 29600000,
      sellingPrice: 34990000,
      warrantyMonths: 24,
      description: "Gaming powerhouse with high refresh rate display.",
      specifications: {
        cpu: "Intel Core i9-14900HX",
        ram: "32GB DDR5",
        gpu: "RTX 4070 8GB",
        storage: "1TB SSD",
        screen: "16-inch QHD+ 240Hz",
      },
      categoryNames: ["Gaming", "Workstation"],
    },
    {
      sku: "ACER-SWIFT-GO-14",
      name: "Acer Swift Go 14",
      brandName: "Acer",
      importPrice: 16500000,
      sellingPrice: 19990000,
      warrantyMonths: 24,
      description: "Compact student ultrabook with OLED panel.",
      specifications: {
        cpu: "Intel Core Ultra 5 125H",
        ram: "16GB LPDDR5",
        gpu: "Intel Arc Graphics",
        storage: "512GB SSD",
        screen: "14-inch 2.8K OLED",
      },
      categoryNames: ["Student", "Ultrabook"],
    },
    {
      sku: "MSI-STEALTH-16-AI",
      name: "MSI Stealth 16 AI Studio",
      brandName: "MSI",
      importPrice: 35400000,
      sellingPrice: 41990000,
      warrantyMonths: 24,
      description: "Creator and gaming laptop with strong AI-ready hardware.",
      specifications: {
        cpu: "Intel Core Ultra 9 185H",
        ram: "32GB DDR5",
        gpu: "RTX 4070 8GB",
        storage: "1TB SSD",
        screen: "16-inch UHD+ Mini LED",
      },
      categoryNames: ["Workstation", "Gaming"],
    },
    {
      sku: "APPLE-MACBOOK-AIR-M3-13",
      name: "MacBook Air M3 13",
      brandName: "Apple",
      importPrice: 25600000,
      sellingPrice: 28990000,
      warrantyMonths: 12,
      description: "Thin and light laptop with Apple M3 chip.",
      specifications: {
        cpu: "Apple M3",
        ram: "16GB Unified",
        gpu: "10-core GPU",
        storage: "512GB SSD",
        screen: "13.6-inch Liquid Retina",
      },
      categoryNames: ["Ultrabook", "Student", "Business"],
    },
  ];

  // ---- Procedural generation: ensure ≥25 products per category ----
  const baseTemplates = [...products];
  const PRODUCTS_PER_CATEGORY = 25;
  const allCategoryNames = ["Gaming", "Business", "Student", "Ultrabook", "Workstation"];
  const variantSuffixes = [
    { suffix: "Pro", priceDelta: 4000000, ramOverride: "32GB" },
    { suffix: "Lite", priceDelta: -3500000, ramOverride: "8GB" },
    { suffix: "Plus", priceDelta: 2000000, ramOverride: "32GB" },
    { suffix: "Max", priceDelta: 6500000, ramOverride: "64GB" },
    { suffix: "SE", priceDelta: -1500000, ramOverride: "16GB" },
    { suffix: "Edition 2025", priceDelta: -800000, ramOverride: undefined },
    { suffix: "Edition 2026", priceDelta: 1500000, ramOverride: undefined },
    { suffix: "Slim", priceDelta: 800000, ramOverride: undefined },
    { suffix: "Performance", priceDelta: 5500000, ramOverride: "32GB" },
    { suffix: "Studio", priceDelta: 4200000, ramOverride: "32GB" },
    { suffix: "Touch", priceDelta: 2200000, ramOverride: undefined },
    { suffix: "OLED", priceDelta: 3500000, ramOverride: undefined },
    { suffix: "X", priceDelta: 7000000, ramOverride: "64GB" },
    { suffix: "Black", priceDelta: 600000, ramOverride: undefined },
    { suffix: "Silver", priceDelta: 600000, ramOverride: undefined },
  ];

  // Count how many products belong to each category from the base templates
  const categoryCounts = new Map<string, number>(
    allCategoryNames.map((name) => [name, 0])
  );
  for (const product of baseTemplates) {
    for (const cat of product.categoryNames) {
      categoryCounts.set(cat, (categoryCounts.get(cat) || 0) + 1);
    }
  }

  // For each category that's under target, generate variants from existing
  // templates that belong to that category until the count reaches the target.
  // Safety: if a full pass through all templates produces no new SKU (because
  // every combination already exists from another category's pass), break out
  // to avoid an infinite loop.
  for (const category of allCategoryNames) {
    const templatesInCategory = baseTemplates.filter((p) =>
      p.categoryNames.includes(category)
    );
    if (templatesInCategory.length === 0) continue;
    let suffixIndex = 0;
    let safetyMaxAttempts = templatesInCategory.length * variantSuffixes.length * 2;
    while ((categoryCounts.get(category) || 0) < PRODUCTS_PER_CATEGORY) {
      const countBefore = categoryCounts.get(category) || 0;
      for (const template of templatesInCategory) {
        if ((categoryCounts.get(category) || 0) >= PRODUCTS_PER_CATEGORY) break;
        const variant = variantSuffixes[suffixIndex % variantSuffixes.length];
        suffixIndex++;
        const sku = `${template.sku}-${variant.suffix.toUpperCase().replace(/\s+/g, "-")}`;
        // Skip if we already created this variant
        if (products.some((p) => p.sku === sku)) continue;

        const newImportPrice = Math.max(
          5000000,
          template.importPrice + Math.round(variant.priceDelta * 0.85)
        );
        const newSellingPrice = Math.max(
          6500000,
          template.sellingPrice + variant.priceDelta
        );
        const specs = { ...template.specifications };
        if (variant.ramOverride) {
          const oldRam = specs.ram || "";
          specs.ram = oldRam.includes("DDR") || oldRam.includes("LPDDR")
            ? `${variant.ramOverride} ${oldRam.split(" ").slice(1).join(" ")}`
            : variant.ramOverride;
        }

        products.push({
          sku,
          name: `${template.name} ${variant.suffix}`,
          brandName: template.brandName,
          importPrice: newImportPrice,
          sellingPrice: newSellingPrice,
          warrantyMonths: template.warrantyMonths,
          description: `${template.description} (${variant.suffix} variant)`,
          specifications: specs,
          categoryNames: template.categoryNames,
        });

        // Update counts for ALL categories this new product belongs to
        for (const cat of template.categoryNames) {
          categoryCounts.set(cat, (categoryCounts.get(cat) || 0) + 1);
        }
      }

      // Anti-livelock: if a full pass through templates produced zero new
      // SKUs, every combination is already taken by previous category passes.
      // Stop trying for this category — we've done our best.
      const countAfter = categoryCounts.get(category) || 0;
      if (countAfter === countBefore) break;

      safetyMaxAttempts--;
      if (safetyMaxAttempts <= 0) break;
    }
  }

  console.log(`Generated ${products.length} total products`);

  // ---- Batched seeding ----
  // Strategy: 4 bulk INSERTs (products, categories, images, instances) instead of
  // ~1500 sequential upserts. Uses skipDuplicates for idempotency on unique cols.
  // ProductImage has no unique constraint on (productId, imageUrl) so we filter
  // existing rows in code before bulk insert.

  // Validate brands present
  for (const seed of products) {
    if (!brandMap.has(seed.brandName)) {
      throw new Error(`Brand not found for product ${seed.sku}: ${seed.brandName}`);
    }
  }

  // 1. Bulk insert products (skip ones already seeded by SKU)
  const productCreateRows = products.map((seed) => ({
    sku: seed.sku,
    name: seed.name,
    brandId: brandMap.get(seed.brandName)!,
    importPrice: seed.importPrice,
    sellingPrice: seed.sellingPrice,
    stockQuantity: 5,
    specifications: seed.specifications,
    description: seed.description,
    warrantyMonths: seed.warrantyMonths,
    isActive: true,
  }));

  const productResult = await prisma.product.createMany({
    data: productCreateRows,
    skipDuplicates: true,
  });
  console.log(`Inserted ${productResult.count} new products (skipped ${products.length - productResult.count} existing)`);

  // 2. Fetch all products (need IDs for relations)
  const allProducts = await prisma.product.findMany({ select: { productId: true, sku: true } });
  const skuToId = new Map(allProducts.map((p) => [p.sku, p.productId]));

  // 3. Bulk insert ProductCategory rows
  const categoryRows: { productId: number; categoryId: number }[] = [];
  for (const seed of products) {
    const productId = skuToId.get(seed.sku);
    if (productId == null) continue;
    for (const categoryName of seed.categoryNames) {
      const categoryId = categoryMap.get(categoryName);
      if (categoryId != null) categoryRows.push({ productId, categoryId });
    }
  }
  const categoryResult = await prisma.productCategory.createMany({
    data: categoryRows,
    skipDuplicates: true, // composite PK (productId, categoryId)
  });
  console.log(`Inserted ${categoryResult.count} new product-category links`);

  // 4. Bulk insert ProductImage rows
  // No unique constraint on (productId, imageUrl) — must dedupe in code.
  const existingImages = await prisma.productImage.findMany({
    select: { productId: true, imageUrl: true },
  });
  const existingImageKeys = new Set(
    existingImages.map((img) => `${img.productId}|${img.imageUrl}`)
  );
  const imageRows: { productId: number; imageUrl: string; displayOrder: number }[] = [];
  for (const seed of products) {
    const productId = skuToId.get(seed.sku);
    if (productId == null) continue;
    for (let i = 1; i <= 3; i++) {
      const imageUrl = `/uploads/products/${seed.sku.toLowerCase()}-${i}.jpg`;
      if (existingImageKeys.has(`${productId}|${imageUrl}`)) continue;
      imageRows.push({ productId, imageUrl, displayOrder: i - 1 });
    }
  }
  const imageResult = await prisma.productImage.createMany({ data: imageRows });
  console.log(`Inserted ${imageResult.count} new product images`);

  // 5. Bulk insert ProductInstance rows (serialNumber is unique)
  const instanceRows: { productId: number; serialNumber: string; status: string }[] = [];
  for (const seed of products) {
    const productId = skuToId.get(seed.sku);
    if (productId == null) continue;
    for (let i = 1; i <= 5; i++) {
      const serialNumber = `${seed.sku}-SN-${String(i).padStart(3, "0")}`;
      instanceRows.push({ productId, serialNumber, status: "Available" });
    }
  }
  const instanceResult = await prisma.productInstance.createMany({
    data: instanceRows,
    skipDuplicates: true,
  });
  console.log(`Inserted ${instanceResult.count} new product instances`);

  console.log(`Products seeded: ${products.length}`);

  const promotions = [
    {
      code: "WELCOME10",
      discountPercent: 10,
      discountAmount: null,
      startDate: new Date("2026-01-01T00:00:00.000Z"),
      endDate: new Date("2026-12-31T23:59:59.999Z"),
      isActive: true,
    },
    {
      code: "SPRING500K",
      discountPercent: null,
      discountAmount: 500000,
      startDate: new Date("2026-03-01T00:00:00.000Z"),
      endDate: new Date("2026-06-30T23:59:59.999Z"),
      isActive: true,
    },
    {
      code: "EXPIRED25",
      discountPercent: 25,
      discountAmount: null,
      startDate: new Date("2025-01-01T00:00:00.000Z"),
      endDate: new Date("2025-12-31T23:59:59.999Z"),
      isActive: false,
    },
  ];

  for (const seed of promotions) {
    await prisma.promotion.upsert({
      where: { code: seed.code },
      update: {
        discountPercent: seed.discountPercent,
        discountAmount: seed.discountAmount,
        startDate: seed.startDate,
        endDate: seed.endDate,
        isActive: seed.isActive,
      },
      create: {
        code: seed.code,
        discountPercent: seed.discountPercent,
        discountAmount: seed.discountAmount,
        startDate: seed.startDate,
        endDate: seed.endDate,
        isActive: seed.isActive,
      },
    });
  }

  console.log(`Promotions seeded: ${promotions.length}`);

  console.log("Seed completed!");
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
