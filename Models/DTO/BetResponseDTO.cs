using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class BetResponseDTO
    {
        public bool Status { get; set; }
        public double FreeChips { get; set; }
        public double Exp { get; set; }
        public string Result { get; set; }
    }
}