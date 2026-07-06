using PartFinderMicroServices_DataAccessLayer.Entities;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IFitmentService
    {
        /// <summary>
        /// Sends fitment data to Shopify for a specific variant.
        /// </summary>
        /// <param name="variantId">The Shopify variant ID.</param>
        /// <returns>A response indicating success or failure.</returns>
       // Task<Response> SendFitmentDataToShopifyAsync(string variantId);

        /// <summary>
        /// Upserts fitment data to Shopify based on the specified mode.
        /// </summary>
        /// <param name="request">The fitment upsert request containing mode and optional variant ID.</param>
        /// <returns>A response indicating success or failure.</returns>
        Task<Response> UpsertFitmentDataAsync(FitmentUpsertDTO request);
    }
}
