using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities
{
    public class Response
    {
        public string Message { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public int StatusCode { get; set; }

        public string? LoggedUserRole { get; set; }

        public object? Data { get; set; }


    }
}
