using PartFinderMicroServices_DataAccessLayer.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Model
{
    public class ShopifyDataQueue
    {
        public int Id { get; set; }

        public string? Cursor { get; set; }

        public int RetryCount { get; set; } = 0;

        public string? LastError { get; set; }

        public string? ProductShopifyId { get; set; }

        public string? Filter { get; set; }

        public QueueStatus Status { get; set; } = QueueStatus.Failed;

        public DateTime? LastAttemptAt { get; set; }
    }
}