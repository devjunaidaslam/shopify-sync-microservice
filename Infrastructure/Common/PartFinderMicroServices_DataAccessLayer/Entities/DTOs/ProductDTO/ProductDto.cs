using Microsoft.VisualBasic;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.CollectionSetDTO;
using PartFinderMicroServices_DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ProductDTO
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public string? DescriptionHtml { get; set; }
        public string? DescriptionHtml_en { get; set; }
        public int VendorId { get; set; }
        public int? ProductTypeId { get; set; }

        // public string? Vendor { get; set; }

        public string? Handle { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CompareAtPrice { get; set; }
        public decimal CompareAtPriceMax { get; set; }
        public decimal CompareAtPriceMin { get; set; }
        public bool CompareAtPriceVaries { get; set; }
        public bool Is_Piece { get; set; }
        public bool Exact_Fit { get; set; }
        public DateTime? LocalCreatedAt { get; set; }
        public DateTime? LocalUpdatedAt { get; set; }
        public List<ProductImageDto> ProductImages { get; set; }
        public VendorDto? Vendor { get; set; }
        public ProductTypeDto? ProductType { get; set; }
      
    }


    public class ProductByIdDto
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public string? DescriptionHtml { get; set; }
        public string? DescriptionHtml_en { get; set; }
        public int VendorId { get; set; }
        public int? ProductTypeId { get; set; }

        // public string? Vendor { get; set; }

        public string? Handle { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CompareAtPrice { get; set; }
        public decimal CompareAtPriceMax { get; set; }
        public decimal CompareAtPriceMin { get; set; }
        public bool CompareAtPriceVaries { get; set; }
        public bool Is_Piece { get; set; }
        public bool Exact_Fit { get; set; }
        public DateTime? LocalCreatedAt { get; set; }
        public DateTime? LocalUpdatedAt { get; set; }

        public List<ProductTagDto> ProductTags { get; set; }
        public List<CollectionDto> Collections { get; set; }
        public List<VariantDto> Variants { get; set; }
        public List<ProductImageDto> ProductImages { get; set; }
        public VendorDto? Vendor { get; set; }
        public ProductTypeDto? ProductType { get; set; }
        public List<ProductOptionDto> ProductOptions { get; set; }
    }

    public class ProductImageDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageShopifyId { get; set; }
        public string ImageSrc { get; set; }
        public DateTime LocalCreatedAt { get; set; }
    }

    public class ProductTagDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int TagId { get; set; }
        public TagDto Tag { get; set; }
    }

    public class VariantDto
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public int ProductId { get; set; }
        public string? Title { get; set; }
        public string? SKU { get; set; }
        public string? Price { get; set; }
        public string? CompareAtPrice { get; set; }
        public string? Barcode { get; set; }
        //public float Weight { get; set; }
        //public string? WeightUnit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? OEM { get; set; }
        public int? OemId { get; set; }
        public OEMDto OfficialOem { get; set; }
        public List<InventoryLevelDto> InventoryLevels { get; set; }
        public List<VariantPriceDto> VariantPrices { get; set; }
        public List<OemVariantDto> OemVariants { get; set; }
        public List<VariantOptionValueDto> VariantOptionValues { get; set; }

    }
    public class OEMDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class VendorDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int vendor { get; set; }
    }

    public class ProductOptionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int OptionId { get; set; }
        public int Position { get; set; }
        public OptionDto Option { get; set; }
    }

    public class TagDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Title_en { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OptionDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class InventoryLevelDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public int Available { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }

    public class VariantPriceDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public string? Price { get; set; }
        public int? CompareAtPrice { get; set; }
        public int Currency { get; set; }
        public DateTime UpdatedAt { get; set; }
        public LocationDto Location { get; set; }
    }

    public class LocationDto
    {
        public int Id { get; set; }
        public string? ShopifyId { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? PredikoId { get; set; }
    }
    public class OemVariantDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int OEMId { get; set; }
        public string Name { get; set; }
    }

    public class VariantOptionValueDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int OptionValueId { get; set; }
        public string? Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public OptionValueDto OptionValue { get; set; }
    }

    public class OptionValueDto
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int OptionId { get; set; }
        public OptionDto Option { get; set; }

    }

    public class ProductTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}