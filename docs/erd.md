# Entity Relationship Diagram

## ERD Overview (Mermaid)

```mermaid
erDiagram
    USER ||--o{ ORDER : "creates"
    USER ||--o{ INVENTORY_LOG : "performs"
    CUSTOMER ||--o{ ORDER : "places"
    PROMOTION |o--o{ ORDER : "applied to"

    BRAND ||--o{ SERIES : "has"
    BRAND ||--o{ PRODUCT : "manufactures"
    SERIES |o--o{ PRODUCT : "belongs to"

    PRODUCT ||--o{ PRODUCT_IMAGE : "has"
    PRODUCT ||--o{ PRODUCT_INSTANCE : "has"
    PRODUCT ||--o{ PRODUCT_CATEGORY : "tagged"
    PRODUCT ||--o{ INVENTORY_LOG : "tracked"
    CATEGORY ||--o{ PRODUCT_CATEGORY : "contains"

    ORDER ||--|{ ORDER_ITEM : "contains"
    PRODUCT_INSTANCE ||--o{ ORDER_ITEM : "sold in"
    PRODUCT_INSTANCE |o--o{ INVENTORY_LOG : "logged"

    USER {
        uuid userId PK
        string username "unique"
        string passwordHash
        string fullName
        string role
        datetime createdAt
        datetime updatedAt
    }

    CUSTOMER {
        uuid customerId PK
        string name
        string phone
        string email
        int loyaltyPoints
        datetime createdAt
        datetime updatedAt
    }

    BRAND {
        int brandId PK
        string name
        string description
        string logoUrl
        datetime createdAt
        datetime updatedAt
    }

    SERIES {
        int seriesId PK
        int brandId FK
        string name
        string description
        string targetSegment
        datetime createdAt
        datetime updatedAt
    }

    CATEGORY {
        int categoryId PK
        string name
        string description
        datetime createdAt
        datetime updatedAt
    }

    PRODUCT {
        int productId PK
        string sku "unique"
        string name
        int brandId FK
        int seriesId FK
        decimal importPrice
        decimal sellingPrice
        int stockQuantity
        json specifications
        string description
        int warrantyMonths
        bool isActive
        datetime createdAt
        datetime updatedAt
    }

    PRODUCT_CATEGORY {
        int productId PK
        int categoryId PK
    }

    PRODUCT_IMAGE {
        int imageId PK
        int productId FK
        string imageUrl
        int displayOrder
        datetime createdAt
        datetime updatedAt
    }

    PRODUCT_INSTANCE {
        int instanceId PK
        int productId FK
        string serialNumber "unique"
        string status
        datetime createdAt
        datetime updatedAt
    }

    ORDER {
        uuid orderId PK
        uuid customerId FK
        uuid userId FK
        int promotionId FK
        decimal subtotal
        decimal discountAmount
        decimal finalAmount
        string status
        string notes
        datetime createdAt
        datetime updatedAt
    }

    ORDER_ITEM {
        int orderItemId PK
        uuid orderId FK
        int instanceId FK
        decimal unitSalePrice
        int quantity
    }

    INVENTORY_LOG {
        int logId PK
        int productId FK
        int instanceId FK
        uuid userId FK
        int quantityChange
        string changeType
        string reason
        datetime createdAt
        datetime updatedAt
    }

    PROMOTION {
        int promotionId PK
        string code "unique"
        decimal discountPercent
        decimal discountAmount
        datetime startDate
        datetime endDate
        bool isActive
        datetime createdAt
        datetime updatedAt
    }
```

## Relationship Summary

### Master Data (static/reference)
| Entity | Description |
|--------|-------------|
| **User** | Shop staff. Role = Admin (full access) or Sale (limited: can't see import price) |
| **Brand** | Laptop manufacturer: ASUS, Dell, HP, Lenovo, Acer, MSI, Apple |
| **Series** | Product line within a brand: ROG, TUF, ThinkPad, Legion, etc. |
| **Category** | Product classification: Gaming, Business, Student, Ultrabook, Workstation |
| **Promotion** | Discount codes with date range and percent/amount off |

### Transaction Data (generated over time)
| Entity | Description |
|--------|-------------|
| **Product** | A laptop model with specs, prices, stock count |
| **ProductInstance** | Individual physical laptop with unique serial number |
| **ProductImage** | Product photos (min 3 per product per requirements) |
| **ProductCategory** | Many-to-many join: one product can be in multiple categories |
| **Customer** | Buyer info + loyalty points |
| **Order** | Sale transaction. Status flow: Created → Paid / Cancelled |
| **OrderItem** | Line item linking order to a specific serial (ProductInstance) |
| **InventoryLog** | Audit trail for stock changes (import, export, adjust, return) |

## Key Design Decisions

1. **Serial-based sales**: Each laptop sold is tracked by serial number (ProductInstance), not just quantity. OrderItem references a specific instance.

2. **Soft delete on Product**: `isActive = false` instead of physical delete, preserving order history.

3. **JSONB specifications**: Flexible schema for laptop specs (CPU, RAM, GPU, screen size, storage) — avoids rigid columns for varying attributes.

4. **Price types**: `importPrice` (cost) + `sellingPrice` (retail). Admin sees both, Sale role sees only sellingPrice.

5. **Composite PK**: ProductCategory uses (productId, categoryId) composite key with cascade deletes.

6. **UUID for people/orders**: User, Customer, Order use UUID. Auto-increment int for products/brands/categories (simpler queries).

## Order Status Flow

```mermaid
stateDiagram-v2
    [*] --> Created
    Created --> Paid : Payment confirmed
    Created --> Cancelled : Order cancelled
    Paid --> [*]
    Cancelled --> [*]
```

## Inventory Flow

```mermaid
flowchart LR
    A[Excel Import] -->|Import| B[InventoryLog +N]
    C[Manual Add] -->|Import| B
    D[Sale Order Paid] -->|Export| E[InventoryLog -N]
    F[Stock Adjust] -->|Adjust| G[InventoryLog ±N]
    H[Customer Return] -->|Return| I[InventoryLog +N]
    B --> J[Product.stockQuantity updated]
    E --> J
    G --> J
    I --> J
```
