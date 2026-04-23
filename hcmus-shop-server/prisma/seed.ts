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

  for (const seed of products) {
    const brandId = brandMap.get(seed.brandName);
    if (!brandId) {
      throw new Error(`Brand not found for product ${seed.sku}: ${seed.brandName}`);
    }

    const product = await prisma.product.upsert({
      where: { sku: seed.sku },
      update: {
        name: seed.name,
        brandId,
        importPrice: seed.importPrice,
        sellingPrice: seed.sellingPrice,
        stockQuantity: 3,
        specifications: seed.specifications,
        description: seed.description,
        warrantyMonths: seed.warrantyMonths,
        isActive: true,
      },
      create: {
        sku: seed.sku,
        name: seed.name,
        brandId,
        importPrice: seed.importPrice,
        sellingPrice: seed.sellingPrice,
        stockQuantity: 3,
        specifications: seed.specifications,
        description: seed.description,
        warrantyMonths: seed.warrantyMonths,
        isActive: true,
      },
    });

    for (const categoryName of seed.categoryNames) {
      const categoryId = categoryMap.get(categoryName);
      if (!categoryId) {
        continue;
      }

      await prisma.productCategory.upsert({
        where: {
          productId_categoryId: { productId: product.productId, categoryId },
        },
        update: {},
        create: {
          productId: product.productId,
          categoryId,
        },
      });
    }

    for (let i = 1; i <= 3; i++) {
      const imageUrl = `/uploads/products/${seed.sku.toLowerCase()}-${i}.jpg`;
      const existingImage = await prisma.productImage.findFirst({
        where: { productId: product.productId, imageUrl },
      });

      if (!existingImage) {
        await prisma.productImage.create({
          data: {
            productId: product.productId,
            imageUrl,
            displayOrder: i - 1,
          },
        });
      }
    }

    for (let i = 1; i <= 3; i++) {
      const serialNumber = `${seed.sku}-SN-${String(i).padStart(3, "0")}`;
      await prisma.productInstance.upsert({
        where: { serialNumber },
        update: {
          productId: product.productId,
          status: "Available",
        },
        create: {
          productId: product.productId,
          serialNumber,
          status: "Available",
        },
      });
    }
  }

  console.log(`Products seeded: ${products.length}`);

  console.log("Seed completed!");
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
