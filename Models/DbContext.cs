using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace FixWebApi.Models
{
    public class FixDbContext: DbContext
    {
        public FixDbContext() : base("DbContext")
        {
        }
        public DbSet<SignUpModel> SignUp { get; set; }
        public DbSet<ChipModel> Chip { get; set; }
        public DbSet<NewsModel> News { get; set; }
        public DbSet<OfferModel> Offer { get; set; }
        public DbSet<TakeRecord> TakeRecord { get; set; }
        public DbSet<TransactionModel> Transaction { get; set; }

    }
}