namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class FulfillmentOrderLineItem
    {
        public long Id { get; set; }
        public long FulfillmentOrderId { get; set; }
        public FulfillmentOrder? FulfillmentOrder { get; set; }

        public string? ShopifyFoLineItemGid { get; set; }
        public long ShopifyOrderLineItemId { get; set; }     // join back to OrderLineItem

        public string? Sku { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
    }
}
