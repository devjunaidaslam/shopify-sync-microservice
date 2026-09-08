namespace ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO
{
    /// <summary>
    /// Response DTO for Shopify order import operation
    /// </summary>
    public class ShopifyOrderImportResponseDTO
    {
        /// <summary>
        /// The created/updated Order entity
        /// </summary>
        public OrderDTO Order { get; set; }

        /// <summary>
        /// List of actions created for this order (Transfer, Drop Ship, etc.)
        /// </summary>
        public List<OrderActionDTO> ActionsCreated { get; set; }

        /// <summary>
        /// Indicates if the order was newly created (true) or already existed (false)
        /// </summary>
        public bool IsNewOrder { get; set; }

        /// <summary>
        /// Summary message describing what was processed
        /// </summary>
        public string Summary { get; set; }
    }
}
