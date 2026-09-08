namespace ShopifySync_DataAccessLayer.Model
{
    public class OrderLineItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public Order? Order { get; set; }

        public long ShopifyLineItemId { get; set; }

        public long? ProductId { get; set; }
        public long? VariantId { get; set; }

        public string? Sku { get; set; }
        public string? Title { get; set; }
        public int Quantity { get; set; }
    }
}
