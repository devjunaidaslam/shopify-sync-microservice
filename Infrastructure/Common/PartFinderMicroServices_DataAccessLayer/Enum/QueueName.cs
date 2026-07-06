using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Enum
{
    public enum QueueName
    {
        // INBOUND QUEUES - Receiving FROM Shopify (Webhooks)
        ProductWebhook,
        CollectionWebhook,
        InventoryLevelWebhook,
        OrderWebhook,
        
        // OUTBOUND QUEUES - Sending TO Shopify (Updates)
        VariantPriceUpdate,
        VariantLocationUpdate,
        InventoryLevelUpdate,
        FitmentSync,
        
        // PREDIKO QUEUES - Order data for Prediko microservice
        PredikoOrderQueue
    }
}