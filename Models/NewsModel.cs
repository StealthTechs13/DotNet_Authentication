using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class NewsModel:BaseModel
    {
        [Column(TypeName = "varchar(MAX)")]
        [Required]
        public string News { get; set; }
    }
}