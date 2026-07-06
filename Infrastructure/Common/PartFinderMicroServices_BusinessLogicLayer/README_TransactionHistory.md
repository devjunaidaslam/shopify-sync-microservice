# Shopify Transaction History System

## Overview

The Shopify Transaction History system provides comprehensive tracking and logging of all Shopify-related transactions across microservices. This system enables effective debugging, monitoring, and auditing of all interactions with Shopify APIs.

## Features

- **Complete Transaction Tracking**: Logs all Shopify API calls, webhooks, and data operations
- **Multi-Service Support**: Works across all microservices (ShopifyService, ShopifyConnector, PartService, AuthService)
- **Flexible Filtering**: Search transactions by product ID, variant ID, event type, status, and more
- **Automatic Cleanup**: Configurable retention policy (default: 30 days)
- **Statistics & Analytics**: Get insights into transaction patterns and success rates
- **Error Tracking**: Detailed error logging for failed transactions
- **Background Processing**: Automated cleanup jobs via Quartz scheduler

## Database Schema

### ShopifyTransactionHistory Table

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| ProductId | string | Shopify Product ID |
| VariantId | string | Shopify Variant ID (if applicable) |
| CollectionId | string | Shopify Collection ID (if applicable) |
| InventoryItemId | string | Shopify Inventory Item ID (if applicable) |
| LocationId | string | Shopify Location ID (if applicable) |
| TransactionType | string | "send" or "receive" |
| EventType | string | "product", "variant", "inventory", "fitment", "collection", "price", "webhook" |
| Status | string | "success", "failed", "pending", "retry" |
| Microservice | string | Service name that initiated the transaction |
| Payload | text | JSON payload sent/received |
| Response | text | JSON response from Shopify |
| ErrorMessage | text | Error details if failed |
| SentAt | datetime | When transaction was initiated |
| ReceivedBy | datetime | When received (for webhooks) |
| SentToShopifyAt | datetime | When sent to Shopify API |
| ResponseReceivedAt | datetime | When response received from Shopify |
| RequestId | string | Unique request identifier |
| CorrelationId | string | For tracking related transactions |
| UserId | string | User who initiated (if applicable) |
| WebhookId | string | Shopify webhook ID (if applicable) |
| RetryCount | int | Number of retries attempted |
| HttpMethod | string | GET, POST, PUT, DELETE |
| Endpoint | string | Shopify API endpoint called |
| HttpStatusCode | int | HTTP status code received |
| CreatedAt | datetime | Record creation timestamp |
| UpdatedAt | datetime | Record update timestamp |

## API Endpoints

### Transaction History Controller

Base URL: `/api/TransactionHistory`

#### Get Transactions by Product ID
```
GET /api/TransactionHistory/product/{productId}?pageNumber=1&pageSize=50&fromDate=2024-01-01&toDate=2024-12-31
```

#### Get Transactions by Variant ID
```
GET /api/TransactionHistory/variant/{variantId}?pageNumber=1&pageSize=50
```

#### Get Transactions by Event Type
```
GET /api/TransactionHistory/event-type/{eventType}?pageNumber=1&pageSize=50
```

#### Get Failed Transactions
```
GET /api/TransactionHistory/failed?pageNumber=1&pageSize=50
```

#### Get Transaction Statistics
```
GET /api/TransactionHistory/statistics?fromDate=2024-01-01&toDate=2024-12-31
```

#### Get Transaction by ID
```
GET /api/TransactionHistory/{id}
```

#### Create Transaction
```
POST /api/TransactionHistory
Content-Type: application/json

{
  "productId": "gid://shopify/Product/123",
  "eventType": "fitment",
  "status": "success",
  "microservice": "ShopifyService",
  "payload": "{\"fitment\": \"data\"}",
  "transactionType": "send"
}
```

#### Cleanup Old Records
```
DELETE /api/TransactionHistory/cleanup?retentionDays=30
```

## Usage Examples

### 1. Basic Service Integration

```csharp
public class MyService
{
    private readonly TransactionHistoryHelper _transactionHistoryHelper;

    public MyService(TransactionHistoryHelper transactionHistoryHelper)
    {
        _transactionHistoryHelper = transactionHistoryHelper;
    }

    public async Task UpdateProductAsync(string productId, object productData)
    {
        try
        {
            // Your Shopify API call here
            var response = await shopifyClient.UpdateProductAsync(productId, productData);
            
            // Log successful transaction
            await _transactionHistoryHelper.LogSuccessAsync(
                productId, 
                "product", 
                "MyService", 
                JsonSerializer.Serialize(productData), 
                JsonSerializer.Serialize(response),
                "PUT",
                "/admin/api/2023-04/products/{id}.json",
                200);
        }
        catch (Exception ex)
        {
            // Log failed transaction
            await _transactionHistoryHelper.LogFailureAsync(
                productId, 
                "product", 
                "MyService", 
                ex.Message, 
                JsonSerializer.Serialize(productData),
                "PUT",
                "/admin/api/2023-04/products/{id}.json",
                400);
            
            throw;
        }
    }
}
```

### 2. Webhook Processing

```csharp
public async Task ProcessWebhookAsync(string webhookPayload, string webhookId)
{
    try
    {
        var webhookData = JsonSerializer.Deserialize<WebhookData>(webhookPayload);
        
        // Process webhook
        await ProcessWebhookDataAsync(webhookData);
        
        // Log webhook received
        await _transactionHistoryHelper.LogWebhookReceivedAsync(
            webhookData.ProductId,
            "product_updated",
            "ShopifyConnector",
            webhookPayload,
            webhookId);
    }
    catch (Exception ex)
    {
        // Log webhook processing failure
        await _transactionHistoryHelper.LogFailureAsync(
            null,
            "webhook",
            "ShopifyConnector",
            ex.Message,
            webhookPayload);
    }
}
```

### 3. Fitment Data Updates

```csharp
public async Task UpdateFitmentDataAsync(string variantId, string fitmentData)
{
    try
    {
        // Update fitment data in Shopify
        await UpdateShopifyFitmentAsync(variantId, fitmentData);
        
        // Log successful fitment update
        await _transactionHistoryHelper.LogFitmentUpdateAsync(
            variantId,
            productId,
            "ShopifyService",
            fitmentData,
            true);
    }
    catch (Exception ex)
    {
        // Log failed fitment update
        await _transactionHistoryHelper.LogFitmentUpdateAsync(
            variantId,
            productId,
            "ShopifyService",
            fitmentData,
            false,
            ex.Message);
    }
}
```

### 4. Inventory Updates

```csharp
public async Task UpdateInventoryAsync(string inventoryItemId, string locationId, int quantity)
{
    try
    {
        // Update inventory in Shopify
        await UpdateShopifyInventoryAsync(inventoryItemId, locationId, quantity);
        
        // Log successful inventory update
        await _transactionHistoryHelper.LogInventoryUpdateAsync(
            inventoryItemId,
            locationId,
            "ShopifyService",
            JsonSerializer.Serialize(new { quantity }),
            true);
    }
    catch (Exception ex)
    {
        // Log failed inventory update
        await _transactionHistoryHelper.LogInventoryUpdateAsync(
            inventoryItemId,
            locationId,
            "ShopifyService",
            JsonSerializer.Serialize(new { quantity }),
            false,
            ex.Message);
    }
}
```

## Configuration

### appsettings.json

```json
{
  "TransactionHistory": {
    "RetentionDays": 30,
    "EnableLogging": true,
    "LogPayload": true,
    "LogResponse": true,
    "MaxPayloadSize": 10000,
    "CleanupSchedule": "0 0 2 * * ?"
  }
}
```

### Configuration Options

- **RetentionDays**: Number of days to retain transaction records (default: 30)
- **EnableLogging**: Enable/disable transaction logging (default: true)
- **LogPayload**: Log request payloads (default: true)
- **LogResponse**: Log response data (default: true)
- **MaxPayloadSize**: Maximum payload size to log (default: 10000 characters)
- **CleanupSchedule**: Cron expression for cleanup job (default: daily at 2 AM)

## Service Registration

### Program.cs

```csharp
// Register transaction history services
builder.Services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
builder.Services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();
builder.Services.AddScoped<TransactionHistoryHelper>();

// Register cleanup job
builder.Services.AddQuartz(q =>
{
    var cleanupJobKey = new JobKey("TransactionHistoryCleanupQuartzJob");
    q.AddJob<TransactionHistoryCleanupQuartzJob>(opts => opts.WithIdentity(cleanupJobKey));
    q.AddTrigger(opts => opts
        .ForJob(cleanupJobKey)
        .WithIdentity("TransactionHistoryCleanupQuartzJob-trigger")
        .WithCronSchedule(builder.Configuration["TransactionHistory:CleanupSchedule"] ?? "0 0 2 * * ?"));
});
```

## Database Migration

After adding the new entity, run the following commands to create the database migration:

```bash
# Add migration
dotnet ef migrations add AddShopifyTransactionHistory --project Infrastructure/Common/PartFinderMicroServices_DataAccessLayer

# Update database
dotnet ef database update --project Infrastructure/Common/PartFinderMicroServices_DataAccessLayer
```

## Best Practices

### 1. Consistent Logging
- Always log both successful and failed transactions
- Include relevant context data (product ID, variant ID, etc.)
- Use appropriate event types for different operations

### 2. Error Handling
- Log detailed error messages for failed transactions
- Include HTTP status codes and endpoint information
- Use correlation IDs for tracking related transactions

### 3. Performance Considerations
- Be mindful of payload size limits
- Use async/await for all logging operations
- Consider batching for high-volume operations

### 4. Security
- Avoid logging sensitive data in payloads
- Use appropriate access controls for transaction history endpoints
- Consider data retention policies for compliance

## Monitoring and Debugging

### 1. Transaction Search
Use the API endpoints to search for specific transactions:
- By product ID for product-related issues
- By variant ID for variant-specific problems
- By status to find failed transactions
- By event type to analyze specific operations

### 2. Statistics and Analytics
Monitor transaction patterns:
- Success rates by microservice
- Common failure points
- Performance trends
- Volume patterns

### 3. Error Analysis
Use failed transaction logs to:
- Identify recurring issues
- Debug integration problems
- Monitor API rate limits
- Track retry patterns

## Troubleshooting

### Common Issues

1. **Missing Transaction Logs**
   - Check if TransactionHistoryHelper is properly injected
   - Verify logging is enabled in configuration
   - Check for exceptions in transaction logging

2. **Performance Issues**
   - Review payload size limits
   - Check database indexes
   - Monitor cleanup job performance

3. **Cleanup Job Not Running**
   - Verify Quartz configuration
   - Check cron expression format
   - Review job execution logs

### Support

For issues or questions about the transaction history system:
1. Check the application logs for error details
2. Review the transaction history records for patterns
3. Use the statistics endpoints for system health monitoring
4. Contact the development team for complex issues

## Future Enhancements

Potential improvements for the transaction history system:
- Real-time transaction monitoring dashboard
- Advanced analytics and reporting
- Integration with external monitoring tools
- Automated alerting for failure patterns
- Transaction replay capabilities
- Enhanced search and filtering options
