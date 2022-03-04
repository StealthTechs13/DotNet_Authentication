using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class OfferModel:BaseModel
    {
        [Column(TypeName = "varchar(MAX)")]
        public string Offer { get; set; }
    }
}