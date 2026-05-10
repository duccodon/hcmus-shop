namespace hcmus_shop.GraphQL.Operations
{
    public static class OrderQueries
    {
        public const string GetOrders = @"
            query Orders(
                $status: String
                $fromDate: String
                $toDate: String
                $search: String
                $page: Int
                $pageSize: Int
            ) {
                orders(
                    status: $status
                    fromDate: $fromDate
                    toDate: $toDate
                    search: $search
                    page: $page
                    pageSize: $pageSize
                ) {
                    items {
                        orderId
                        subtotal
                        discountAmount
                        finalAmount
                        status
                        notes
                        createdAt
                        updatedAt
                        customer {
                            customerId
                            name
                            phone
                            email
                            loyaltyPoints
                        }
                        user {
                            userId
                            username
                            fullName
                            role
                            createdAt
                        }
                        promotion {
                            promotionId
                            code
                            discountPercent
                            discountAmount
                            startDate
                            endDate
                            isActive
                            createdAt
                            updatedAt
                        }
                        orderItems {
                            orderItemId
                            unitSalePrice
                            quantity
                            instance {
                                instanceId
                                serialNumber
                                status
                                createdAt
                                updatedAt
                                product {
                                    productId
                                    sku
                                    name
                                    sellingPrice
                                    stockQuantity
                                }
                            }
                        }
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

        public const string GetOrderById = @"
            query Order($orderId: ID!) {
                order(orderId: $orderId) {
                    orderId
                    subtotal
                    discountAmount
                    finalAmount
                    status
                    notes
                    createdAt
                    updatedAt
                    customer {
                        customerId
                        name
                        phone
                        email
                        loyaltyPoints
                    }
                    user {
                        userId
                        username
                        fullName
                        role
                        createdAt
                    }
                    promotion {
                        promotionId
                        code
                        discountPercent
                        discountAmount
                        startDate
                        endDate
                        isActive
                        createdAt
                        updatedAt
                    }
                    orderItems {
                        orderItemId
                        unitSalePrice
                        quantity
                        instance {
                            instanceId
                            serialNumber
                            status
                            createdAt
                            updatedAt
                            product {
                                productId
                                sku
                                name
                                sellingPrice
                                stockQuantity
                            }
                        }
                    }
                }
            }";

        public const string GetAvailableProductInstances = @"
            query AvailableProductInstances(
                $search: String
                $productId: Int
                $page: Int
                $pageSize: Int
            ) {
                availableProductInstances(
                    search: $search
                    productId: $productId
                    page: $page
                    pageSize: $pageSize
                ) {
                    items {
                        instanceId
                        serialNumber
                        status
                        createdAt
                        updatedAt
                        product {
                            productId
                            sku
                            name
                            sellingPrice
                            stockQuantity
                        }
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

        public const string CreateOrder = @"
            mutation CreateOrder($input: CreateOrderInput!) {
                createOrder(input: $input) {
                    orderId
                    subtotal
                    discountAmount
                    finalAmount
                    status
                    notes
                    createdAt
                    updatedAt
                }
            }";

        public const string UpdateOrder = @"
            mutation UpdateOrder($orderId: ID!, $input: UpdateOrderInput!) {
                updateOrder(orderId: $orderId, input: $input) {
                    orderId
                    subtotal
                    discountAmount
                    finalAmount
                    status
                    notes
                    createdAt
                    updatedAt
                }
            }";

        public const string UpdateOrderStatus = @"
            mutation UpdateOrderStatus($orderId: ID!, $status: String!) {
                updateOrderStatus(orderId: $orderId, status: $status) {
                    orderId
                    status
                    subtotal
                    discountAmount
                    finalAmount
                    updatedAt
                }
            }";

        public const string DeleteOrder = @"
            mutation DeleteOrder($orderId: ID!) {
                deleteOrder(orderId: $orderId)
            }";
    }
}
