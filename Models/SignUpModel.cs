using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class SignUpModel: BaseModel
    {
        public string UserId { get; set; }

        public int ParentId { get; set; }

        public int MasterId { get; set; }

        public int AdminId { get; set; }

        public int SuperId { get; set; }

        public string UserName { get; set; }

        public string Role { get; set; }

        public bool BetStatus { get; set; }

        public bool FancyBetStatus { get; set; }

        public bool CasinoStatus { get; set; }

        public bool TableStatus { get; set; }

        public double ExposureLimit { get; set; }

        public string IpAddress { get; set; }

        public double Balance { get; set; }

        public double Exposure { get; set; }

        public double ProfitLoss { get; set; }

        public double CreditLimit { get; set; }

        public double Share { get; set; }

        public string Password { get; set; }

        public string MobileNumber { get; set; }
    }
}