namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class OrderAction
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public Order? Order { get; set; }

        public long? FulfillmentOrderId { get; set; }
        public FulfillmentOrder? FulfillmentOrder { get; set; }

        // "SUPPLIER_PRODUCT_ORDER" | "TRANSFER" | "DROP_SHIP" - ESSENTIAL for business logic
        public string? ActionType { get; set; }
        public string? Reason { get; set; }

        public long? FromLocationId { get; set; }  // for Transfer - ESSENTIAL for routing logic
        public long? ToLocationId { get; set; }    // for Transfer (e.g., POS store) - ESSENTIAL for routing logic

        public string? Payload { get; set; }        // JSON with lines [{sku, qty, name}], etc.
        public string? IdempotencyKey { get; set; } // e.g., $"{orderId}:{foGid}:{actionType}"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
