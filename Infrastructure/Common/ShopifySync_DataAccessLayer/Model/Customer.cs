namespace ShopifySync_DataAccessLayer.Model
{
    public class Customer
    {
        public long Id { get; set; }
        public long? ShopifyCustomerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? CountryCode { get; set; }
        public string? Zip { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Order>? Orders { get; set; }
    }
}
