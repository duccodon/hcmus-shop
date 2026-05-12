ALTER TABLE "orders" DROP CONSTRAINT "orders_customerId_fkey";

ALTER TABLE "orders" ALTER COLUMN "customerId" DROP NOT NULL;

ALTER TABLE "orders"
ADD CONSTRAINT "orders_customerId_fkey"
FOREIGN KEY ("customerId") REFERENCES "customers"("customerId")
ON DELETE SET NULL
ON UPDATE CASCADE;
