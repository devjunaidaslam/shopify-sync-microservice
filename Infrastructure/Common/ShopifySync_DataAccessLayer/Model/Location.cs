using System.ComponentModel.DataAnnotations.Schema;

namespace ShopifySync_DataAccessLayer.Model
{
    public class Location
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }

        [Column("prediko_id")]
        public string? PredikoId { get; set; }

        //public List<InventoryLevel> InventoryLevels { get; set; }
        //public List<VariantPrice> VariantPrices { get; set; }
    }
}
