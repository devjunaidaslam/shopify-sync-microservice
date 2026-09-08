using ShopifySync_DataAccessLayer.Entities.DTOs.UpdatePriceRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateVariantLocationPriceRequest;
using ShopifySync_DataAccessLayer.Entities.DTOs.UpdateInventoryRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
   public interface IShopifyUpdateService
    {
        Task<string> UpdateVariantPricesAsync(UpdateVariantPricesRequest request);

        Task<string> UpdateVariantLocationPriceAsync(UpdateVariantLocationPriceRequest request);

        Task<string> UpdateShopifyVariantMetaFieldAsync(object payload);

        Task<string> UpdateInventoryLevelsAsync(UpdateInventoryRequest request);
    }
}
