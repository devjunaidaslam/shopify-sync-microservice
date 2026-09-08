# shopify-sync-microservice

.NET 8 services for bidirectional Shopify synchronization via Admin GraphQL, RabbitMQ, and PostgreSQL.

## Architecture

| Service | Role |
|---------|------|
| **ShopifyService_API** | Catalog import and outbound updates to Shopify |
| **ShopifyConnector** | Inbound Shopify webhooks and RabbitMQ consumers |

### Inbound (Shopify → system)

1. Webhook endpoints receive Product, Collection, Inventory Level, and Order events
2. Payloads are validated with HMAC-SHA256
3. Messages are published to RabbitMQ for async handling
4. `WebHookRMQService` consumes queues and updates the local database
5. OEM metafield changes on variants can trigger FitmentSync

### Outbound (system → Shopify)

1. REST endpoints enqueue variant price, location price, inventory, and fitment updates
2. `ShopifyUpdateRMQService` consumes outbound queues and calls Shopify GraphQL
3. Successful updates sync related fields back to PostgreSQL

### Messaging

- Named inbound/outbound queues with a dedicated queue enum
- Shared RabbitMQ connection; dedicated channels per consumer
- Dead-letter queues (14-day TTL) and retry (up to 5) with transient vs permanent classification
- Manual ack (`BasicAck` / `BasicNack`)

### Background work

| Job | Description |
|-----|-------------|
| `WebHookRMQJob` | Consumes inbound webhook queues |
| `ShopifyUpdateRMQJob` | Consumes outbound update queues |
| `ImportProductsBackgroundService` / Quartz job | Bulk product import (optional date filter) |

## Solution layout

```
shopify-sync-microservice/
├── ShopifyService_API/
├── ShopifyConnector/
├── ShopifyService_Test/
└── Infrastructure/Common/
    ├── ShopifySync_BusinessLogicLayer/
    └── ShopifySync_DataAccessLayer/
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL
- RabbitMQ
- Shopify Admin API access token and webhook secret

## Getting started

1. Clone the repository
2. Set configuration in `appsettings.json` or environment variables (never commit real secrets)
3. Apply EF migrations:

```bash
dotnet ef database update --project Infrastructure/Common/ShopifySync_DataAccessLayer
```

4. Build and test:

```bash
dotnet build ShopifySync.sln
dotnet test ShopifySync.sln
```

5. Run:

```bash
dotnet run --project ShopifyService_API
dotnet run --project ShopifyConnector
```

Default path bases: `/shopify` (API) and `/shopifyconnector` (Connector). Swagger UI is at `/swagger` under each path base.

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Shopify:Token` | Shopify Admin API access token |
| `Shopify:ShopName` | Shop subdomain |
| `Shopify:WebHookSecret` | HMAC secret for webhook validation |
| `JwtSettings:*` | JWT issuer/audience/secret (ShopifyService_API) |
| `RabbitMQ:*` | Host, credentials, port (`UseSsl: true` for Amazon MQ on 5671) |
| `Quartz:ImportShopifyJob:Cron` | Cron for scheduled import |
| `AppSettings:AWS_*` / `AWS:*` | Optional CloudWatch logging |

## Tests

xUnit + Moq coverage includes import/update happy paths and failure cases in `ShopifyService_Test`.

## License

MIT — portfolio extraction from a larger production codebase.
