using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopifySync_DataAccess.Context;
using ShopifySync_BusinessLogicLayer.Repository.Interface;
using ShopifySync_DataAccessLayer.Entities.DTOs.CollectionSetDTO;
using ShopifySync_DataAccessLayer.Entities.DTOs.ProductDTO;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_BusinessLogicLayer.Repository.Implementation
{
    public class ProductRepository(ShopifySyncDbContext context) : IProductRepository
    {
        
        /// <summary>
        /// Retrieves a paginated list of products based on search, page number, and page size.
        /// </summary>
        /// <param name="search">Search term for filtering products.</param>
        /// <param name="pageNo">Page number for pagination.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>A list of products matching the criteria.</returns>
        public async Task<(int TotalCount, List<ProductDto> Data)> GetAllProductsPaginatedAsync(ProductFilterDto productFilter)
        {
            List<ProductDto> productDtos = new List<ProductDto>();
            IQueryable<Product> query = context.Products
                .Include(x => x.ProductType)
                .Include(x => x.Variants)
                //.ThenInclude(x => x.OemVariants)
                //.ThenInclude(x => x.OEM)
                //.Include(x => x.Variants)
                //.ThenInclude(x => x.VariantOptionValues)
                //.ThenInclude(x => x.OptionValue)
                //.ThenInclude(x => x.Option)
                //.Include(x => x.Variants)
                //.ThenInclude(x => x.VariantPrices)
                //.ThenInclude(x => x.Location)
                //.Include(x => x.Variants)
                //.ThenInclude(x => x.OEM)
                //.Include(x => x.Variants)
                //.ThenInclude(x => x.InventoryLevels)
                //.ThenInclude(x => x.Location)
                //.Include(x => x.ProductTags)
                //.ThenInclude(x => x.Tag)
                //.Include(x => x.ProductOptions)
                //.ThenInclude(x => x.Option)
                .Include(x => x.Vendor)
                //.Include(x => x.ProductCollections)
                //.ThenInclude(x => x.Collection)
                .Include(x => x.ProductImages).AsSplitQuery();

            // Apply the same filter to the data query - simplified to only product-level fields
            if (!string.IsNullOrEmpty(productFilter.search))
            {
                string searchTerm = productFilter.search.ToLower();
                query = query.Where(
                    p =>
                    EF.Functions.ILike(p.Title, $"%{searchTerm}%") ||
                p.Variants.Any(x => EF.Functions.ILike(x.SKU , $"%{searchTerm}%")) ||
                p.Variants.Any(x => EF.Functions.ILike(x.ShopifyId, $"%gid://shopify/ProductVariant/{searchTerm}%")) ||
                p.Variants.Any(x => EF.Functions.ILike(x.OEMMetaField, $"%{searchTerm}%")) ||
                EF.Functions.ILike(p.ShopifyId, $"%gid://shopify/Product/{searchTerm}%") ||
                EF.Functions.ILike(p.Vendor.Title, $"%{productFilter.search}%"));
                //p.ProductTags.Any(pt => pt.Tag.Title.Contains(searchTerm)) ||
                //p.ProductCollections.Any(pc => pc.Collection.Title.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
           var products = await query.Skip((productFilter.page_no - 1) * productFilter.page_size)
                                           .Take(productFilter.page_size).ToListAsync();


            foreach (var product in products)
            {
                ProductDto productDto = new ProductDto();
                productDto.Id = product.Id;
                productDto.ShopifyId = product.ShopifyId;
                productDto.Title = product.Title;
                productDto.Title_en = product.Title_en;
                productDto.DescriptionHtml = product.DescriptionHtml;
                productDto.DescriptionHtml_en = product.DescriptionHtml_en;
                productDto.VendorId = product.VendorId;
                productDto.Handle = product.Handle;
                productDto.Status = product.Status;
                productDto.CreatedAt = product.CreatedAt;
                productDto.UpdatedAt = product.UpdatedAt;
                productDto.CompareAtPrice = product.CompareAtPrice;
                productDto.CompareAtPriceMax = product.CompareAtPriceMax;
                productDto.CompareAtPriceMin = product.CompareAtPriceMin;
                productDto.CompareAtPriceVaries = product.CompareAtPriceVaries;
                productDto.Is_Piece = product.Is_Piece;
                productDto.Exact_Fit = product.Exact_Fit;
                productDto.LocalCreatedAt = product.LocalCreatedAt;
                productDto.LocalUpdatedAt = product.LocalUpdatedAt;
                productDto.ProductTypeId = product.ProductTypeId;
                productDto.ProductType = product.ProductTypeId != null ? new ProductTypeDto() { Id = product.ProductType.Id, Name = product.ProductType.Name } : null;
                productDto.Vendor = new VendorDto() { Id = product.Vendor.Id, Title = product.Vendor.Title, vendor = product.Vendor.vendor };
                productDto.ProductImages = product.ProductImages.Select(x => new ProductImageDto() { Id = x.Id, ImageShopifyId = x.ImageShopifyId, ProductId = x.ProductId, ImageSrc = x.ImageSrc, LocalCreatedAt = x.LocalCreatedAt }).ToList();

                productDtos.Add(productDto);
            }
            return (totalCount, productDtos);
        }

        /// <summary>
        /// Retrieves a product by its unique identifier, including related entities.
        /// </summary>
        /// <param name="productId">The unique identifier of the product.</param>
        /// <returns>The product entity or null if not found.</returns>
        public async Task<ProductByIdDto?> GetProductByIdAsync(int productId)
        {
            try
            {
                Product product = await context.Products
                 .Include(x => x.ProductType)
                .Include(x => x.Variants)
                .ThenInclude(x => x.OemVariants)
                .ThenInclude(x => x.OEM)
                .Include(x => x.Variants)
                .ThenInclude(x => x.VariantOptionValues)
                .ThenInclude(x => x.OptionValue)
                .ThenInclude(x => x.Option)
                .Include(x => x.Variants)
                .ThenInclude(x => x.OEM)
                .Include(x => x.Variants)
                .ThenInclude(x => x.VariantPrices)
                .ThenInclude(x => x.Location)
                .Include(x => x.Variants)
                .ThenInclude(x => x.InventoryLevels)
                .ThenInclude(x => x.Location)
                .Include(x => x.ProductTags)
                .ThenInclude(x => x.Tag)
                .Include(x => x.ProductOptions)
                .ThenInclude(x => x.Option)
                .Include(x => x.Vendor)
                .Include(x => x.ProductCollections)
                .ThenInclude(x => x.Collection)
                .Include(x => x.ProductImages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return null;

           var productDto = new ProductByIdDto();
            productDto.Id = product.Id;
            productDto.ShopifyId = product.ShopifyId;
            productDto.Title = product.Title;
            productDto.Title_en = product.Title_en;
            productDto.DescriptionHtml = product.DescriptionHtml;
            productDto.DescriptionHtml_en = product.DescriptionHtml_en;
            productDto.VendorId = product.VendorId;
            productDto.Handle = product.Handle;
            productDto.Status = product.Status;
            productDto.CreatedAt = product.CreatedAt;
            productDto.UpdatedAt = product.UpdatedAt;
            productDto.CompareAtPrice = product.CompareAtPrice;
            productDto.CompareAtPriceMax = product.CompareAtPriceMax;
            productDto.CompareAtPriceMin = product.CompareAtPriceMin;
            productDto.CompareAtPriceVaries = product.CompareAtPriceVaries;
            productDto.Is_Piece = product.Is_Piece;
            productDto.Exact_Fit = product.Exact_Fit;
            productDto.LocalCreatedAt = product.LocalCreatedAt;
            productDto.LocalUpdatedAt = product.LocalUpdatedAt;
            productDto.ProductTypeId = product.ProductTypeId;
            productDto.ProductType = product.ProductTypeId != null ? new ProductTypeDto() { Id = product.ProductType.Id, Name = product.ProductType.Name } : null;
                productDto.Variants = product.Variants.Select(x => new VariantDto()
                {
                    Id = x.Id,
                    ShopifyId = x.ShopifyId,
                    ProductId = x.ProductId,
                    Title = x.Title,
                    SKU = x.SKU,
                    Price = x.Price,
                    CompareAtPrice = x.CompareAtPrice,
                    Barcode = x.Barcode,
                    //Weight = x.Weight,
                    //WeightUnit = x.WeightUnit,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    OEM = x.OEMMetaField,
                    OemId = x.OEMId,
                    OfficialOem = x.OEM != null ? new OEMDto() { Id = x.OEM.Id, Name = x.OEM.Name } : null,
                    InventoryLevels = x.InventoryLevels.Select(x => new InventoryLevelDto()
                    {
                        Id = x.Id,
                        VariantId = x.VariantId,
                        LocationId = x.LocationId,
                        Available = x.Available,
                        UpdatedAt = x.UpdatedAt
                    }).ToList(),

                    VariantPrices = x.VariantPrices.Select(x => new VariantPriceDto()
                    {
                        Id = x.Id,
                        VariantId = x.VariantId,
                        LocationId = x.LocationId,
                        Price = x.Price,
                        CompareAtPrice = x.CompareAtPrice,
                        Currency = x.Currency,
                        UpdatedAt = x.UpdatedAt,
                        Location = new LocationDto() { Id = x.Location.Id, Name = x.Location.Name, City = x.Location.City, Province = x.Location.Province, Country = x.Location.Country , ShopifyId = x.Location.ShopifyId}
                    }).ToList(),

                    OemVariants = x.OemVariants.Select(x => new OemVariantDto()
                    {
                        Id = x.Id,
                        VariantId = x.VariantId,
                        OEMId = x.OEMId,
                        Name = x.OEM?.Name
                    }).ToList(),

                    VariantOptionValues = x.VariantOptionValues.Select(x => new VariantOptionValueDto()
                    {
                        Id = x.Id,
                        VariantId = x.VariantId,
                        Position = x.Position,
                        OptionValueId = x.OptionValueId,
                        CreatedAt = x.Variant.CreatedAt,
                        UpdatedAt = x.Variant.UpdatedAt,
                        OptionValue = new OptionValueDto() { Id = x.OptionValue.Id, Value = x.OptionValue.Value, OptionId = x.OptionValue.OptionId, Option = new OptionDto() { Id = x.OptionValue.Option.Id, Name = x.OptionValue.Option.Name } }
                    }).ToList()

                }).ToList();

                productDto.ProductTags = product.ProductTags.Select(x => new ProductTagDto()
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    TagId = x.TagId,
                    Tag = new TagDto() { Id =  x.Tag.Id, Title  = x.Tag.Title, Title_en = x.Tag.Title_en, CreatedAt = x.Tag.CreatedAt, UpdatedAt = x.Tag.UpdatedAt}
                }).ToList();

                productDto.Collections = product.ProductCollections.Select(x => new CollectionDto()
                {
                    Id = x.Collection.Id,
                    ShopifyId = x.Collection.ShopifyId,
                    Title = x.Collection.Title,
                    Title_en = x.Collection.Title_en,
                    Description = x.Collection.Description,
                    Image = x.Collection.Image,
                    CreatedAt = x.Collection.CreatedAt,

                }).ToList();

                productDto.ProductOptions = product.ProductOptions.Select(x => new ProductOptionDto()
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    OptionId = x.OptionId,
                    Position = x.Position,
                    Option = new OptionDto() { Id = x.Option.Id, Name = x.Option.Name}

                }).ToList();

                productDto.Vendor = new VendorDto() { Id = product.Vendor.Id, Title = product.Vendor.Title, vendor = product.Vendor.vendor };
                productDto.ProductImages = product.ProductImages.Select(x => new ProductImageDto() { Id = x.Id, ImageShopifyId = x.ImageShopifyId, ProductId = x.ProductId, ImageSrc = x.ImageSrc , LocalCreatedAt = x.LocalCreatedAt }).ToList();
               
                return productDto;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Retrieves the total count of products matching the search criteria.
        /// </summary>
        /// <param name="search">Search term for filtering products.</param>
        /// <returns>The total count of matching products.</returns>
        public async Task<int> GetTotalCountAsync(string search)
        {
            int count;
            if (string.IsNullOrEmpty(search))
                count = await context.Products.CountAsync();
            else
                count = await context.Products.Where(p => p.Title.Contains(search)).CountAsync();
            return count;

        }
    }
}
