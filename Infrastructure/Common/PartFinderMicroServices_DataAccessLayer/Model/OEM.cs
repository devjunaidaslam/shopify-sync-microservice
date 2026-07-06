namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class OEM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<OemVariant> OemVariants { get; set; }
       // public List<Variant> Variants { get; set; }
    }
}
