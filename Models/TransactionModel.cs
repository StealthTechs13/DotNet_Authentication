using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixWebApi.Models
{
    public class TransactionModel:BaseModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int SportsId { get; set; }
        public string EventId { get; set; }
        public string MarketId { get; set; }
        public string SelectionId { get; set; }
        public string Discription { get; set; }
        public string MarketName { get; set; }
        public string Remark { get; set; }
        public double Amount { get; set; }
        public double Balance { get; set; }
        public int ParentId { get; set; }
        public int MasterId { get; set; }
        public int AdminId { get; set; }
        public int SuperId { get; set; }
        public int Parent { get; set; }
    }
}