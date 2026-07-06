using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartFinder_DataAccess.Context;
using PartFinderMicroServices_BusinessLogicLayer.Functions;
using PartFinderMicroServices_BusinessLogicLayer.Service.Interface;
using PartFinderMicroServices_DataAccessLayer.Model;

namespace PartFinderMicroServices_BusinessLogicLayer.Service.Implementation
{
    public class CommonService : ICommonService
    {
        private readonly PartFinderDbContext _context;

        public CommonService(PartFinderDbContext context)
        {
            _context = context;
        }

        public void ErrorLogs(string StackTrace, string Request, int Type, string Message, string LogError)
        {
            try
            {
                //var UserId = CommonFunction.GetUserIdFromToken();
                // var UserLoginLogId = "";

                // Getting UserLoginLog Id From Table
                // var UserLoginLogIdContext = _context.UserLoginLogs.Where(x => x.UserId == UserId && x.IsLogout == false).OrderByDescending(x => x.LoginDateTime).FirstOrDefault();

                // if (UserLoginLogIdContext != null)
                // {
                //     UserLoginLogId = UserLoginLogIdContext.UserLoginLogId;
                // }
                ErrorLog _error = new ErrorLog()
                {
                    Stacktrace = StackTrace,
                    //UserLoginLogId = UserLoginLogId,
                    Request = Request,
                    Type = Type,
                    Message = Message,
                    LogError = LogError,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ErrorLogs.Add(_error);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
               // ErrorLogs(ex.StackTrace ?? "No stack trace available", "ErrorLog", 1, ex.Message, ex.ToString());
            }
        }

        public async Task<bool> CheckLogout()
        {
            var _access = true;
            var (token, expiry) = CommonFunction.GetToken();
            try
            {
                var _getExpireToken = _context.ExpiredTokens.Any(x => x.AccessToken == token);

                if (_getExpireToken)
                {
                    _access = false;
                }
            }
            catch (Exception ex)
            {
                ErrorLogs(ex.StackTrace ?? "No stack trace available","CheckLogout", 1, ex.Message, ex.ToString());
            }
            return _access;
        }

    }
}
