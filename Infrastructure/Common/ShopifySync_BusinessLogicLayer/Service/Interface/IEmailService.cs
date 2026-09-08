using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopifySync_DataAccessLayer.Entities.Email;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface IEmailService
    {
        void SendEmail(Message message);
    }
}
