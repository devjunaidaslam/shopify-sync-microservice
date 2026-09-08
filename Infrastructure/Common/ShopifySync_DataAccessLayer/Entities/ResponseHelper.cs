using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifySync_DataAccessLayer.Entities
{
    public class ResponseHelper
    {
        public static Response Success(string message, object data = null,string loggedUserRole = null,PaginationInfo pagination = null)
        {
            if (pagination != null)
            {
                var paginatedData = new
                {
                    pagination.TotalItemCount,
                    pagination.PageNo,
                    pagination.PerPage,
                    pagination.TotalPages,
                    pagination.NextPage,
                    pagination.PrevPage,
                    Result = data
                };

                return new Response
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = message,
                    LoggedUserRole = loggedUserRole,
                    Data = paginatedData
                };
            }
            else
            {
                return new Response
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = message,
                    LoggedUserRole = loggedUserRole,
                    Data = data
                };
            }
        }
        public static Response BadRequest(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = message,
                Data = null
            };
        }

        public static Response InternalServerError(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = message,
                Data = null
            };
        }

        public static Response NotFound(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = message,
                Data = null
            };
        }

        public static Response Unauthorized(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 401,
                Message = message,
                Data = null
            };
        }

        public static Response Forbidden(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 403,
                Message = message,
                Data = null
            };
        }

        public static Response Conflict(string message)
        {
            return new Response
            {
                IsSuccess = false,
                StatusCode = 409,
                Message = message,
                Data = null
            };
        }
    }
}
