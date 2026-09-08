using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IWebHookService
    {
        Task ProcessCollectionCreatedOrUpdatedAsync(JsonElement jsonBody);
        Task UpdateInventoryLevel(JsonElement jsonBody);
        Task ProcessOrderCreatedOrUpdatedAsync(JsonElement jsonBody);
        bool IsValidWebhook(string requestBody, string shopifyHmacHeader, string webhookSecret);
    }
}