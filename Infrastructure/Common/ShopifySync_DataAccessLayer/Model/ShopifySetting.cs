namespace ShopifySync_DataAccessLayer.Model
{
    public class ShopifySetting
    {
        public string Token { get; set; }
        public string ShopName { get; set; }
        public string Version { get; set; }
        public int VariantPageSize { get; set; }
        public int CollectionPageSize { get; set; }
        public int ProductPageSize { get; set; }
        public int InventoryLevelPageSize { get; set; }
        public int VariantMetaFieldPageSize { get; set; }
        public int LocationPageSize { get; set; }

        public int VendorPageSize { get; set; }
        public int ConcurrencyLimit { get; set; }
        public string WebHookSecret { get; set; }
    }

}
