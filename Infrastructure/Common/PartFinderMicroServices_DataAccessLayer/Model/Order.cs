namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class Order
    {
        public long Id { get; set; }
        public long ShopifyOrderId { get; set; }       // numeric
        public string? ShopifyOrderGid { get; set; }    // gid://...
        public string? Name { get; set; }               // "#1083"
        public string? SourceName { get; set; }         // "pos" | "web"
        public long? SaleLocationId { get; set; }      // POS location_id
        public string? SaleLocationName { get; set; }

        public long? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime? CreatedAtShopify { get; set; }
        public string? Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? RawPayload { get; set; }         // JSON (string or JsonDocument)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderLineItem>? LineItems { get; set; }
        public ICollection<FulfillmentOrder>? FulfillmentOrders { get; set; }
        public ICollection<OrderAction>? Actions { get; set; }
    }
}
