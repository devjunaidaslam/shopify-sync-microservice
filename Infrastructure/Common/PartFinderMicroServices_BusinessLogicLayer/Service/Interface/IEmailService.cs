using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartFinderMicroServices_DataAccessLayer.Entities.Email;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Interface
{
    public interface IEmailService
    {
        void SendEmail(Message message);
    }
}
