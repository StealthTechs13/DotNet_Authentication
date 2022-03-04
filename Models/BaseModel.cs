using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class BaseModel
    {
       
        public int id { get; set; }
        public bool deleted { get; set;}
        public bool status { get; set; }
        public DateTime createdOn { get; set; }
    }
}