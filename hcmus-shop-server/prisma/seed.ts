import { PrismaClient } from "@prisma/client";
import bcrypt from "bcrypt";

const prisma = new PrismaClient();

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

  console.log("Seed completed!");
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
