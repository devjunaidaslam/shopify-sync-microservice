namespace ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO
{
    public class OrderDTO
    {
        public long Id { get; set; }
        public long ShopifyOrderId { get; set; }
        public string ShopifyOrderGid { get; set; }
        public string Name { get; set; }
        public string SourceName { get; set; }
        public long? SaleLocationId { get; set; }
        public string SaleLocationName { get; set; }
        public long? CustomerId { get; set; }
        public CustomerDTO Customer { get; set; }
        public DateTime? CreatedAtShopify { get; set; }
        public string Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RawPayload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<OrderLineItemDTO> LineItems { get; set; }
        public List<FulfillmentOrderDTO> FulfillmentOrders { get; set; }
        public List<OrderActionDTO> Actions { get; set; }
    }

    public class CustomerDTO
    {
        public long Id { get; set; }
        public long? ShopifyCustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string CountryCode { get; set; }
        public string Zip { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderLineItemDTO
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ShopifyLineItemId { get; set; }
        public long? ProductId { get; set; }
        public long? VariantId { get; set; }
        public string Sku { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
    }

    public class FulfillmentOrderDTO
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string ShopifyFulfillmentOrderGid { get; set; }
        public string Status { get; set; }
        public long? AssignedLocationId { get; set; }
        public string AssignedLocationName { get; set; }
        public string DestinationFirstName { get; set; }
        public string DestinationLastName { get; set; }
        public string DestinationEmail { get; set; }
        public string DestinationPhone { get; set; }
        public string DestinationAddress1 { get; set; }
        public string DestinationAddress2 { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationProvince { get; set; }
        public string DestinationCountryCode { get; set; }
        public string DestinationZip { get; set; }
        public string RawPayload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<FulfillmentOrderLineItemDTO> LineItems { get; set; }
    }

    public class FulfillmentOrderLineItemDTO
    {
        public long Id { get; set; }
        public long FulfillmentOrderId { get; set; }
        public string ShopifyFoLineItemGid { get; set; }
        public long ShopifyOrderLineItemId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        
        // Enriched data for Prediko
        public PredikoDTO.PredikoProductInfoDTO? Product { get; set; }
        public PredikoDTO.PredikoVariantInfoDTO? Variant { get; set; }
    }

    public class OrderActionDTO
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long? FulfillmentOrderId { get; set; }
        public string ActionType { get; set; }
        public string Reason { get; set; }
        public long? FromLocationId { get; set; }
        public long? ToLocationId { get; set; }
        public string Payload { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
