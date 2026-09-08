using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities.DTOs.ImportFitment
{
    public class ImportFitmentUpdateDTO
    {
        public long ImportFitmentId { get; set; }

        public long SupplierId { get; set; }

        public string FileLink { get; set; }

        public long? ProcessedLines { get; set; }

        public string UserId { get; set; }

        public string Status { get; set; }

        public DateTime? CleanedAt { get; set; }


        public string[] Category { get; set; }

        public bool? IsDefault { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
