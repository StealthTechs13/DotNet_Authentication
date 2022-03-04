using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class ResponseDTO
    {
        public bool Status { get; set; }
        public dynamic Result { get; set; }
        public double Count { get; set; }
    }
}