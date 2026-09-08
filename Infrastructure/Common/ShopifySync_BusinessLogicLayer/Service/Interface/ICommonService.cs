using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_BusinessLogicLayer.Service.Interface
{
    public interface ICommonService
    {
        void ErrorLogs(string StackTrace, string Request, int Type, string Message, string LogError);
        Task<bool> CheckLogout();
    }
}
