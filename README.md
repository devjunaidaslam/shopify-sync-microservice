# shopify-sync-microservice
# Shopify Integration — Resume Project

A .NET 8 microservices system for bidirectional Shopify synchronization using Admin GraphQL API, RabbitMQ message queues, and PostgreSQL.

## Architecture

Built as a .NET microservices system with two separate APIs:

| Service | Role |
|---------|------|
| **ShopifyService_API** | Data import & outbound updates to Shopify |
| **ShopifyConnector** | Receives inbound webhooks from Shopify and processes them via message queues |

### Shopify GraphQL API Integration

Integrated with Shopify Admin GraphQL API for products, variants, collections, locations, vendors, inventory levels, orders, metafields, and product images. Uses cursor-based pagination with configurable page sizes and concurrency limits.

### Inbound Flow (Shopify → System)

1. Webhook Controller receives real-time events (Product, Collection, Inventory Level, Order)
2. Each webhook is validated using HMAC-SHA256 signature verification
3. Validated payloads are pushed to RabbitMQ queues for async processing
4. `WebHookRMQService` consumes queues and delegates to the appropriate service
5. OEM change detection compares existing variant OEM metafields with incoming data and auto-triggers FitmentSync when changes are detected

### Outbound Flow (System → Shopify)

1. REST API endpoints accept update requests (variant prices, location-based prices via metafields, inventory levels)
2. Requests are serialized and published to RabbitMQ outbound queues
3. `ShopifyUpdateRMQService` consumes and executes updates against Shopify GraphQL API
4. Supports Variant Price Update, Variant Location Price Update, Inventory Level Update, and Fitment Sync

### RabbitMQ Message Broker

- 8 named queues (4 inbound, 4 outbound) with a dedicated enum for queue names
- Shared long-lived connection with thread-safe double-check locking pattern
- Dead Letter Queues (DLQ) with 14-day TTL for failed messages
- Smart retry mechanism (up to 5 retries) with transient vs permanent error classification (5xx, 429, 408, timeouts)
- Manual acknowledgment (BasicAck / BasicNack) for reliable message processing
- Dedicated channels per queue consumer

### Background Jobs

| Job | Description |
|-----|-------------|
| `WebHookRMQJob` | Long-running BackgroundService consuming inbound webhook queues |
| `ShopifyUpdateRMQJob` | Long-running BackgroundService consuming outbound update queues |
| `ImportProductsBackgroundService` | Triggers bulk product import with optional date filtering |

### Data Sync & Import

- Bulk import of Products, Variants, Collections, Locations, Vendors, and Product Images from Shopify
- Single product import by ID
- Failed page retry queue (`ShopifyDataQueue`) for resilient imports
- Transaction history logging for every queue message processed
- Local database sync for variant prices and inventory levels after successful Shopify updates

### Unit Tests (xUnit + Moq)

- **ShopifyServiceTests** — ImportCollections, ImportLocations, ImportVendors, ImportProductById (success + not found), GetHistoryStatus (success + exception)
- **ShopifyUpdateServiceTests** — UpdateVariantPrices (not found + success), UpdateInventoryLevels (empty + null), UpdateVariantLocationPrice, UpdateShopifyVariantMetaField, GetInventoryItemId

## Key Skills Demonstrated

C# .NET · Microservices Architecture · Shopify Admin GraphQL API · RabbitMQ · Webhook Processing · HMAC Signature Verification · Dead Letter Queues & Retry Patterns · Background Services · Entity Framework Core · Repository Pattern · Dependency Injection · Unit Testing (xUnit, Moq) · Async/Concurrent Programming · Structured Logging

## Solution Structure

```
ResumeShopifySyncProject/
├── ShopifyService_API/          # Import & outbound update API
├── ShopifyConnector/            # Webhook receiver + RMQ consumers
├── ShopifyService_Test/         # xUnit tests
└── Infrastructure/Common/
    ├── PartFinderMicroServices_BusinessLogicLayer/
    └── PartFinderMicroServices_DataAccessLayer/
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- RabbitMQ
- Shopify Admin API access token and webhook secret

## Getting Started

1. Clone the repository
2. Copy `appsettings.json` values for your environment (never commit real secrets)
3. Apply EF migrations:

```bash
dotnet ef database update --project Infrastructure/Common/PartFinderMicroServices_DataAccessLayer
```

4. Build and test:

```bash
dotnet build
dotnet test
```

5. Run APIs:

```bash
dotnet run --project ShopifyService_API
dotnet run --project ShopifyConnector
```

## Configuration

Set these in `appsettings.json` or environment variables:

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Shopify:Token` | Shopify Admin API access token |
| `Shopify:ShopName` | Shopify shop subdomain |
| `Shopify:WebHookSecret` | HMAC secret for webhook validation |
| `RabbitMQ:*` | RabbitMQ host, credentials, port (`UseSsl: true` for AWS Amazon MQ on port 5671) |

## License

Portfolio / resume demonstration project. Extracted from a larger production codebase.
