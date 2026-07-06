using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.NewFolder
{
   public class CollectionSetDTO
    {
       public List<Collection> Collections { get; set; }
      public List<ProductCollection> ProductCollections { get; set; }
    }
}
