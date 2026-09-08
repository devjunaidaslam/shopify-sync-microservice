using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Model
{
    public class ErrorLog
    {
        [Key]
        public int LogId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? UserLoginLogId { get; set; }


        [Column(TypeName = "nvarchar(MAX)")]
        public string Stacktrace { get; set; }
        public int Type { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string Message { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string Request { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string LogError { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
