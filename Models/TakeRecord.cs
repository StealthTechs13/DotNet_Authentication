using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class TakeRecord:BaseModel
    {
        public int UserId { get; set; }
        public int Records { get; set; }
    }
}