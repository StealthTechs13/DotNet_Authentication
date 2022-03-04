using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class BalanceDetailDTO
    {
        public double DownLineBal { get; set; }
        public double DownLineExp { get; set; }
        public double DownLineAvailBal { get; set; }
        public double OwnBal { get; set; }
        public double TotalBal { get; set; }
        public dynamic usrObj { get; set; }
    }
   
}