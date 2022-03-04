using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models.DTO
{
    public class TransactionDTO
    {
        public string UserName { get; set; }
        public string EventId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public string Discription { get; set; }
        public string MarketName { get; set; }
        public string Remark { get; set; }
        public double Amount { get; set; }
        public double Balance { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}