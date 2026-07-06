namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class ProductOption
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int OptionId { get; set; }
        public int Position { get; set; }

        public Product Product { get; set; }
        public Option Option { get; set; }
    }
}
