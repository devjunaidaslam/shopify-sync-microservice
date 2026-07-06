# Fitment API Documentation

## Overview

The Fitment API allows you to send vehicle fitment data to Shopify for specific product variants. This API checks if a product has the required flags set and then finds compatible vehicles based on the OEM number.

## API Endpoint

### Send Fitment Data to Shopify

**POST** `/api/Fitment/SendFitmentData/{variantId}`

Sends fitment data to Shopify for a specific variant.

#### Parameters

- `variantId` (long, required): The Shopify variant ID

#### Request Example

```http
POST /api/Fitment/SendFitmentData/12345
```

#### Response Examples

**Success Response (200 OK)**
```json
{
  "isSuccess": true,
  "message": "Fitment data successfully sent to Shopify for variant 12345. Found 5 compatible vehicles.",
  "data": null,
  "statusCode": 200
}
```

**Bad Request (400 Bad Request)**
```json
{
  "isSuccess": false,
  "message": "Product must have both Is_Piece and Exact_Fit flags set to true.",
  "data": null,
  "statusCode": 400
}
```

**Not Found (404 Not Found)**
```json
{
  "isSuccess": false,
  "message": "Variant with ID 12345 not found.",
  "data": null,
  "statusCode": 404
}
```

## Business Logic

The API performs the following steps:

1. **Variant Validation**: Checks if the variant exists in the database
2. **Product Flag Check**: Verifies that the associated product has both `Is_Piece = true` and `Exact_Fit = true`
3. **OEM Validation**: Ensures the variant has an OEM number
4. **Vehicle Search**: Searches for compatible vehicles using the OEM number from the `OEMParts` table
5. **Data Formatting**: Formats the vehicle data according to the required format: `type|year|make|model^^type|year|make|model`
6. **Shopify Update**: Updates the `global.ymm` metafield on the Shopify variant

## Data Format

The fitment data is formatted as follows:
- Each vehicle entry: `type|year|make|model`
- Multiple vehicles separated by: `^^`
- Example: `ATV|2020|Honda|Rancher^^Snowmobile|2019|Yamaha|SRX`

## Error Handling

The API handles various error scenarios:

- **Invalid Variant ID**: Returns 400 Bad Request
- **Variant Not Found**: Returns 404 Not Found
- **Product Flags Not Set**: Returns 400 Bad Request
- **No OEM Number**: Returns 400 Bad Request
- **No Compatible Vehicles**: Returns 404 Not Found
- **Shopify API Errors**: Returns 400 Bad Request with error details
- **Internal Errors**: Returns 400 Bad Request with generic error message

## Dependencies

The API requires the following services to be registered in the dependency injection container:

- `IFitmentService`: Main service for fitment operations
- `IShopifyRepository`: Repository for Shopify data access
- `IVehicleRepository`: Repository for vehicle data access
- `ICommonService`: Service for logging and common operations

## Configuration

The API uses the following configuration from `appsettings.json`:

```json
{
  "Shopify": {
    "Token": "your-shopify-token",
    "ShopName": "your-shop-name",
    "Version": "2023-10"
  }
}
```

## Testing

Unit tests are available in `ShopifyService_Test/Controllers/FitmentControllerTests.cs` covering:

- Valid variant ID scenarios
- Invalid variant ID validation
- Service error responses
- Exception handling

## Usage Example

```csharp
// Using HttpClient
var client = new HttpClient();
var response = await client.PostAsync("/api/Fitment/SendFitmentData/12345", null);
var result = await response.Content.ReadAsStringAsync();
```

## Notes

- The API requires proper authentication and authorization
- All operations are logged for debugging purposes
- The API follows the existing codebase patterns and standards
- Error messages are user-friendly and provide actionable information
