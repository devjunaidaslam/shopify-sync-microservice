using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.PredikoDTO
{
    /// <summary>
    /// Message DTO for sending order data to Prediko microservice via RabbitMQ
    /// Contains enriched order information with variants, products, tags, and suppliers
    /// </summary>
    public class PredikoOrderMessageDTO
    {
        /// <summary>
        /// The complete order with all relationships (customer, line items, fulfillment orders, actions)
        /// </summary>
        public PartFinderMicroServices_DataAccessLayer.Entities.DTOs.OrderDTO.OrderDTO Order { get; set; } = null!;

        /// <summary>
        /// Enriched line items with product and variant details
        /// </summary>
        public List<PredikoOrderLineItemDTO> EnrichedLineItems { get; set; } = new List<PredikoOrderLineItemDTO>();

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        public DateTime MessageCreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source of the order (e.g., "pos", "online")
        /// </summary>
        public string? SourceName { get; set; }

        /// <summary>
        /// Sale location ID if POS order
        /// </summary>
        public long? SaleLocationId { get; set; }

        /// <summary>
        /// Sale location name if POS order
        /// </summary>
        public string? SaleLocationName { get; set; }
    }

    /// <summary>
    /// Enriched line item with product, variant, tags, and supplier information
    /// </summary>
    public class PredikoOrderLineItemDTO
    {
        /// <summary>
        /// Order line item ID
        /// </summary>
        public long OrderLineItemId { get; set; }

        /// <summary>
        /// Shopify line item ID
        /// </summary>
        public long ShopifyLineItemId { get; set; }

        /// <summary>
        /// SKU
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Product title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Quantity ordered
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product information
        /// </summary>
        public PredikoProductInfoDTO? Product { get; set; }

        /// <summary>
        /// Variant information
        /// </summary>
        public PredikoVariantInfoDTO? Variant { get; set; }
    }

    /// <summary>
    /// Product information for Prediko
    /// </summary>
    public class PredikoProductInfoDTO
    {
        public int ProductId { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Variant information for Prediko
    /// </summary>
    public class PredikoVariantInfoDTO
    {
        public int VariantId { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? SKU { get; set; }
        public string? Price { get; set; }
        public string? Barcode { get; set; }
        public float Weight { get; set; }
        public string? WeightUnit { get; set; }
        public int? OEMId { get; set; }
        public string? OEMName { get; set; }
        public List<PredikoSupplierInfoDTO> Suppliers { get; set; } = new List<PredikoSupplierInfoDTO>();
    }

    /// <summary>
    /// Supplier information for Prediko
    /// </summary>
    public class PredikoSupplierInfoDTO
    {
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierCode { get; set; }
        public string? PredikoId { get; set; }
    }
}
