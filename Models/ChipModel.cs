using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class ChipModel:BaseModel
    {
        public int UserId { get; set; }
        public double ChipName { get; set; }
        public double ChipValue { get; set; }
    }
}