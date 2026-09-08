using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Model;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.ImportFitment
{
    public class ImportFitmentDTO
    {
        public long ImportFitmentId { get; set; }

        public long SupplierId { get; set; }

        public Suppliers? Supplier { get; set; }

        [Required]
        public required string FileLink { get; set; }

        public long? ProcessedLines { get; set; }

        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string Status { get; set; }

        public DateTime? CleanedAt { get; set; }

        [Required]
        public required string[] Category { get; set; }

        public bool? IsDefault { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
