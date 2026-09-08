using System.ComponentModel.DataAnnotations;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.OrderDTO
{
    public class OrderCreateDTO
    {
        [Required]
        public long ShopifyOrderId { get; set; }
        
        [Required]
        public string ShopifyOrderGid { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        public string SourceName { get; set; }
        public long? SaleLocationId { get; set; }
        public string SaleLocationName { get; set; }
        public long? CustomerId { get; set; }
        public DateTime? CreatedAtShopify { get; set; }
        public string Currency { get; set; }
        public decimal? TotalPrice { get; set; }
        public string RawPayload { get; set; }
        
        public CustomerCreateDTO Customer { get; set; }
        public List<OrderLineItemCreateDTO> LineItems { get; set; }
        public List<FulfillmentOrderCreateDTO> FulfillmentOrders { get; set; }
    }

    public class CustomerCreateDTO
    {
        public long? ShopifyCustomerId { get; set; }
        
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        public string Phone { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string CountryCode { get; set; }
        public string Zip { get; set; }
    }

    public class OrderLineItemCreateDTO
    {
        [Required]
        public long ShopifyLineItemId { get; set; }
        
        public long? ProductId { get; set; }
        public long? VariantId { get; set; }
        
        [Required]
        public string Sku { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class FulfillmentOrderCreateDTO
    {
        [Required]
        public string ShopifyFulfillmentOrderGid { get; set; }
        
        [Required]
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
        
        public List<FulfillmentOrderLineItemCreateDTO> LineItems { get; set; }
    }

    public class FulfillmentOrderLineItemCreateDTO
    {
        [Required]
        public string ShopifyFoLineItemGid { get; set; }
        
        [Required]
        public long ShopifyOrderLineItemId { get; set; }
        
        [Required]
        public string Sku { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
