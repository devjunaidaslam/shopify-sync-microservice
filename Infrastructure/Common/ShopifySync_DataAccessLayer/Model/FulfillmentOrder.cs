namespace ShopifySync_DataAccessLayer.Model
{
    public class FulfillmentOrder
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public Order? Order { get; set; }

        public string? ShopifyFulfillmentOrderGid { get; set; }  // gid://shopify/FulfillmentOrder/...
        public string? Status { get; set; }                      // OPEN/IN_PROGRESS/CLOSED

        public long? AssignedLocationId { get; set; }           // numeric (from GID tail) - ESSENTIAL for routing logic
        public string? AssignedLocationName { get; set; }        // ESSENTIAL for supplier check

        // Destination snapshot - all nullable
        public string? DestinationFirstName { get; set; }
        public string? DestinationLastName { get; set; }
        public string? DestinationEmail { get; set; }
        public string? DestinationPhone { get; set; }
        public string? DestinationAddress1 { get; set; }
        public string? DestinationAddress2 { get; set; }
        public string? DestinationCity { get; set; }
        public string? DestinationProvince { get; set; }
        public string? DestinationCountryCode { get; set; }
        public string? DestinationZip { get; set; }

        public string? RawPayload { get; set; }                  // JSON snapshot

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FulfillmentOrderLineItem>? LineItems { get; set; }
    }
}
