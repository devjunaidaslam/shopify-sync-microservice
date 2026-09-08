namespace ShopifySync_DataAccessLayer.Model
{
    public class OemVariant
    {
        public int Id { get; set; }
        public int VariantId {  get; set; }
        public int OEMId { get; set; }
        public Variant Variant { get; set; }
        public OEM OEM { get; set; }
    }
}
