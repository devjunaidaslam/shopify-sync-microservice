using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopifySync_DataAccessLayer.Model
{
    [Table("ProductImages")]
    public class ProductImage
    {
      
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ImageShopifyId { get; set; }

        public string ImageSrc { get; set; }

        public DateTime LocalCreatedAt { get; set; }

        public Product Product { get; set; }
    }
}
