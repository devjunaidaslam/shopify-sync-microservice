using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Model
{
    public class ExpiredTokens
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ExpiredTokenId { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string AccessToken { get; set; }

        public DateTime ExpiredDate { get; set; }
    }
}
