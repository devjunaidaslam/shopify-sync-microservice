namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class ProductCollection
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public int CollectionId { get; set; }

        public Product Product { get; set; }
        public Collection Collection { get; set; }
    }

}
