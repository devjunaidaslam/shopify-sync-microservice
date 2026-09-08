using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_DataAccess.Context
{
    public class ShopifySyncDbContext : IdentityDbContext<AspNetUser>
    {
        public ShopifySyncDbContext()
        {
            
        }
        public ShopifySyncDbContext(DbContextOptions options) : base(options)
        {
        }
        
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        public DbSet<ImportFitment> ImportFitment { get; set; }
        public DbSet<TempVehicleImports> TempVehicleImports { get; set; }


        public DbSet<OemVehicle> OemVehicles { get; set; }
        public DbSet<Suppliers> Suppliers { get; set; }

        public DbSet<VehicleMakes> VehicleMakes { get; set; }
        public DbSet<VehicleModels> VehicleModels { get; set; }
        public DbSet<Vehicles> Vehicles { get; set; }

        public DbSet<VehicleTypes> VehicleTypes { get; set; }

        public DbSet<VehicleYears> VehicleYears { get; set; }
        public DbSet<ExpiredTokens> ExpiredTokens { get; set; }


        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<ProductCollection> ProductCollections { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<InventoryLevel> InventoryLevels { get; set; }
        public DbSet<VariantPrice> VariantPrices { get; set; }
        public DbSet<OemVariant> OemVariants { get; set; }
        public DbSet<OEM> OEMs { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<MetaFields> MetaFields { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<OptionValue> OptionValues { get; set; }
        public DbSet<VariantOptionValue> VariantOptionValues { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<HistoryInventory> HistoryInventory { get; set; }
        public DbSet<ShopifyDataQueue> ShopifyDataQueues { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ShopifyTransactionHistory> ShopifyTransactionHistories { get; set; }
        
        // Order Management
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLineItem> OrderLineItems { get; set; }
        public DbSet<FulfillmentOrder> FulfillmentOrders { get; set; }
        public DbSet<FulfillmentOrderLineItem> FulfillmentOrderLineItems { get; set; }
        public DbSet<OrderAction> OrderActions { get; set; }
    }
}