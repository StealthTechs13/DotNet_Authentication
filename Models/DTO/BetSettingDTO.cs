using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class BetSettingDTO
    {
        public int ParentId { get; set; }
        public int MasterId { get; set; }
        public int AdminId { get; set; }
        public int SuperId { get; set; }
        public double Stake { get; set; }
    }
}