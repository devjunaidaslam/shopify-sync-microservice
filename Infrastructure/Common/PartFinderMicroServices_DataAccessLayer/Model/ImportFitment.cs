using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class ImportFitment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ImportFitmentId { get; set; }

        [Required]
        public long SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Suppliers? Supplier { get; set; }

        public string FileLink { get; set; }

        public long? ProcessedLines { get; set; }

        [Required]
        public string UserId { get; set; }

        public string Status { get; set; }

        public DateTime? CleanedAt { get; set; }

        public string[] Category { get; set; }

        public bool? IsDefault { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
