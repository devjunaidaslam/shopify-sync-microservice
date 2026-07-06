using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.RabbitMQ
{
    public class RabbitMQSetting
    {
        public string HostName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; } = 5672;
        public bool UseSsl { get; set; }
    }
}