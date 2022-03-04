using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using FixWebApi.Authentication;
using FixWebApi.Models;
using FixWebApi.Models.DTO;

namespace FixWebApi.Controllers
{
    [RoutePrefix("Transaction")]
    public class TransactionModelsController : ApiController
    {
        private FixDbContext db = new FixDbContext();
        ResponseDTO responseDTO = new ResponseDTO();
        Runtime run_time = new Runtime();


        [HttpGet]
        [Route("ParentTrans")]
        public async Task<IHttpActionResult> ParentTrans(double amount)
        {
            try
            {
                int id = Convert.ToInt32(run_time.RunTimeUserId());
                var signObj = await db.SignUp.Where(x => x.id == id).FirstOrDefaultAsync();
                signObj.Balance = Math.Round(signObj.Balance + amount, 2);
                TransactionModel trnObj = new TransactionModel()
                {
                    UserId = id,
                    UserName = signObj.UserName,
                    SportsId = 0,
                    EventId = "00000",
                    MarketId = "0000",
                    SelectionId = "000",
                    Discription = "Credit To Own",
                    MarketName = "Cash",
                    Remark = "Self Deposit",
                    Amount = amount,




                    Balance = signObj.Balance,
                    status = false,
                    deleted = false,
                    createdOn = DateTime.Now,
                };
                db.Transaction.Add(trnObj);
                await db.SaveChangesAsync();
                responseDTO.Status = true;
                responseDTO.Result = "Done";
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }

        [HttpGet]
        [Route("Transactions")]
        public async Task<IHttpActionResult> Transactions(int userId, double amount, string type, string remark)
        {
            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    if (amount > 0)
                    {
                        int flag = 0;
                        int id = Convert.ToInt32(run_time.RunTimeUserId());
                        var parentObj = await db.SignUp.Where(x => x.id == id && !x.deleted && !x.status).FirstOrDefaultAsync();
                        var childObj = await db.SignUp.Where(x => x.id == userId && !x.deleted && !x.status).FirstOrDefaultAsync();
                        double proitLoss = 0;
                        switch (childObj.Role)
                        {
                            case "SubAdmin":
                                proitLoss = Math.Round(childObj.Balance + db.SignUp.Where(x => x.AdminId == userId).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - childObj.CreditLimit, 2);
                                break;

                            case "Master":
                                proitLoss = Math.Round(childObj.Balance + db.SignUp.Where(x => x.MasterId == userId).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - childObj.CreditLimit, 2);
                                break;
                            case "Agent":
                                proitLoss = Math.Round(childObj.Balance + db.SignUp.Where(x => x.ParentId == userId).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - childObj.CreditLimit, 2);
                                break;
                            case "Client":
                                proitLoss = Math.Round(db.Transaction.Where(x => x.UserId == userId).Select(x => x.Amount).DefaultIfEmpty(0).Sum() - childObj.CreditLimit, 2);
                                break;
                        }
                        if (proitLoss < 0)
                        {
                            proitLoss = -proitLoss;
                        }
                        switch (type)
                        {
                            case "Deposit":
                                if (parentObj.Balance >= amount)
                                {
                                    parentObj.Balance = Math.Round(parentObj.Balance - amount, 2);
                                    childObj.Balance = Math.Round(childObj.Balance + amount, 2);
                                    childObj.ExposureLimit = childObj.Balance;
                                    if (proitLoss != 0)
                                    {
                                        if (amount > proitLoss)
                                        {
                                            double diff = amount - proitLoss;
                                            childObj.CreditLimit = Math.Round(childObj.CreditLimit + diff, 2);
                                        }
                                    }
                                    else
                                    {
                                        childObj.CreditLimit = Math.Round(childObj.CreditLimit + amount, 2);
                                    }
                                    flag = 1;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = "Low Balance.";
                                }
                                break;
                            case "Withdraw":
                                double balance = 0;
                                if (childObj.Role == "Client")
                                {
                                    balance = Math.Round(db.Transaction.Where(x => x.UserId == userId).Select(x => x.Amount).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.UserId == userId).Select(x => x.Exposure).DefaultIfEmpty(0).Sum(), 2);
                                }
                                else
                                {
                                    balance = childObj.Balance;
                                }

                                if (balance >= amount)
                                {
                                    parentObj.Balance = Math.Round(parentObj.Balance + amount, 2);
                                    childObj.Balance = Math.Round(childObj.Balance - amount, 2);
                                    if (proitLoss != 0)
                                    {
                                        if (amount > proitLoss)
                                        {
                                            double diff = amount - proitLoss;
                                            childObj.CreditLimit = Math.Round(childObj.CreditLimit - diff, 2);
                                        }
                                    }
                                    else
                                    {
                                        childObj.CreditLimit = Math.Round(childObj.CreditLimit - amount, 2);
                                    }
                                    flag = 1;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = "Low Balance.";
                                }
                                break;
                        }
                        if (flag == 1)
                        {
                            TransactionModel trnChildObj = new TransactionModel()
                            {
                                UserId = userId,
                                UserName = childObj.UserId,
                                SportsId = 0,
                                EventId = "00000",
                                MarketId = "0000",
                                SelectionId = "000",
                                Discription = "Credit By Parent",
                                MarketName = "Cash",
                                Remark = remark,
                                Amount = amount,
                                Balance = childObj.Balance,
                                ParentId = childObj.ParentId,
                                MasterId = childObj.MasterId,
                                AdminId = childObj.AdminId,
                                SuperId = childObj.SuperId,
                                status = false,
                                deleted = false,
                                createdOn = DateTime.Now,
                                Parent = 1,
                            };
                            db.Transaction.Add(trnChildObj);
                            TransactionModel trnPrntObj = new TransactionModel()
                            {
                                UserId = id,
                                UserName = parentObj.UserId,
                                SportsId = 0,
                                EventId = "00000",
                                MarketId = "0000",
                                SelectionId = "000",
                                Discription = "Credit To " + childObj.UserId,
                                MarketName = "Cash",
                                Remark = remark,
                                Amount = -amount,
                                Balance = parentObj.Balance,
                                ParentId = parentObj.ParentId,
                                MasterId = parentObj.MasterId,
                                AdminId = parentObj.AdminId,
                                SuperId = parentObj.SuperId,
                                status = false,
                                deleted = false,
                                createdOn = DateTime.Now,
                            };
                            db.Transaction.Add(trnPrntObj);
                            await db.SaveChangesAsync();
                            dbContextTransaction.Commit();
                            responseDTO.Status = true;
                            responseDTO.Result = "Done";
                        }
                        else
                        {
                            if (responseDTO.Result != "Low Balance.")
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Undefined Case.";
                            }
                        }
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "Amount Must be Greater than 0.";
                    }
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    responseDTO.Status = false;
                    responseDTO.Result = ex.Message;
                }
            }
            return Ok(responseDTO);
        }

        [HttpGet]
        [Route("TranHistory")]
        public async Task<IHttpActionResult> GetTransactions(string role, int userId, int skipRec, int take, string type, int sportsId, string marketName, DateTime sDate, DateTime eDate)
        {
            try
            {
                List<TransactionDTO> transObjList = new List<TransactionDTO>();
                var usrObj = await db.SignUp.Where(x => x.id == userId).FirstOrDefaultAsync();
                switch (role)
                {
                    case "Client":
                        switch (type)
                        {
                            case "All":
                                var tranObj = await db.Transaction.AsNoTracking().Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (tranObj.Count > 0)
                                {
                                    foreach (var obj in tranObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Cash":
                                var cashObj = await db.Transaction.AsNoTracking().Where(x => x.UserId == userId && x.MarketName == "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (cashObj.Count > 0)
                                {
                                    foreach (var obj in cashObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Sports":
                                if (sportsId == 0)
                                {
                                    var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.UserId == userId && x.MarketName != "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                    if (sportsObj.Count > 0)
                                    {
                                        foreach (var obj in sportsObj)
                                        {
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = obj.EventId,
                                                MarketId = obj.MarketId,
                                                SelectionId = obj.SelectionId,
                                                Discription = obj.Discription,
                                                MarketName = obj.MarketName,
                                                Remark = "",
                                                Amount = obj.Amount,
                                                Balance = obj.Balance,
                                                CreatedOn = obj.createdOn,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                    }

                                }
                                else
                                {
                                    if (marketName == "All")
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.UserId == userId && x.SportsId == sportsId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = obj.Amount,
                                                    Balance = obj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }

                                    }
                                    else
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.UserId == userId && x.SportsId == sportsId && x.MarketName == marketName && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = obj.Amount,
                                                    Balance = obj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    case "Agent":
                        switch (type)
                        {
                            case "All":
                                var tranAgObj = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                if (tranAgObj.Count > 0)
                                {
                                    foreach (var item in tranAgObj)
                                    {
                                        if (item.MarketName == "Cash")
                                        {
                                            var agCashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName).OrderBy(x => x.id).ToListAsync();
                                            if (agCashObj.Count > 0)
                                            {
                                                foreach (var cash in agCashObj)
                                                {
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = item.EventId,
                                                        MarketId = item.MarketId,
                                                        SelectionId = item.SelectionId,
                                                        Discription = item.Discription,
                                                        MarketName = item.MarketName,
                                                        Remark = "",
                                                        Amount = cash.Amount,
                                                        Balance = cash.Balance,
                                                        CreatedOn = item.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                        else if (item.MarketName != "Fancy")
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = -profitLoss,
                                                CreatedOn = item.createdOn,
                                                Balance = usrObj.Balance,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                        else
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId && x.SelectionId == item.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = -profitLoss,
                                                Balance = usrObj.Balance,
                                                CreatedOn = item.createdOn,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                    }
                                }
                                break;
                            case "Cash":
                                var cashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == "Cash").OrderBy(x => x.id).ToListAsync();
                                if (cashObj.Count > 0)
                                {
                                    foreach (var obj in cashObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Sports":
                                if (sportsId == 0)
                                {
                                    var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.ParentId == userId && x.MarketName != "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                    if (sportsObj.Count > 0)
                                    {
                                        foreach (var obj in sportsObj)
                                        {
                                            if (obj.MarketName != "Fancy")
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = -profitLoss,
                                                    CreatedOn = obj.createdOn,
                                                    Balance = usrObj.Balance,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                            else
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = -profitLoss,
                                                    Balance = usrObj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (marketName == "All")
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.ParentId == userId && x.SportsId == sportsId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.ParentId == userId && x.SportsId == sportsId && x.MarketName==marketName && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId}).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.ParentId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    case "Master":
                        switch (type)
                        {
                            case "All":
                                var tranAgObj = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                if (tranAgObj.Count > 0)
                                {
                                    foreach (var item in tranAgObj)
                                    {
                                        if (item.MarketName == "Cash")
                                        {
                                            var agCashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName).OrderBy(x => x.id).ToListAsync();
                                            if (agCashObj.Count > 0)
                                            {
                                                foreach (var cash in agCashObj)
                                                {
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = item.EventId,
                                                        MarketId = item.MarketId,
                                                        SelectionId = item.SelectionId,
                                                        Discription = item.Discription,
                                                        MarketName = item.MarketName,
                                                        Remark = "",
                                                        Amount = cash.Amount,
                                                        Balance = cash.Balance,
                                                        CreatedOn = item.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                        else if (item.MarketName != "Fancy")
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = Math.Round(usrObj.Share/100 * -profitLoss,2),
                                                CreatedOn = item.createdOn,
                                                Balance = usrObj.Balance,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                        else
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId && x.SelectionId == item.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                Balance = usrObj.Balance,
                                                CreatedOn = item.createdOn,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                    }
                                }
                                break;
                            case "Cash":
                                var cashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == "Cash").OrderBy(x => x.id).ToListAsync();
                                if (cashObj.Count > 0)
                                {
                                    foreach (var obj in cashObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Sports":
                                if (sportsId == 0)
                                {
                                    var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.MasterId == userId && x.MarketName != "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                    if (sportsObj.Count > 0)
                                    {
                                        foreach (var obj in sportsObj)
                                        {
                                            if (obj.MarketName != "Fancy")
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                    CreatedOn = obj.createdOn,
                                                    Balance = usrObj.Balance,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                            else
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                    Balance = usrObj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (marketName == "All")
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.MasterId == userId && x.SportsId == sportsId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.MasterId == userId && x.SportsId == sportsId && x.MarketName == marketName && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.MasterId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    case "SubAdmin":
                        switch (type)
                        {
                            case "All":
                                var tranAgObj = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                if (tranAgObj.Count > 0)
                                {
                                    foreach (var item in tranAgObj)
                                    {
                                        if (item.MarketName == "Cash")
                                        {
                                            var agCashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName).OrderBy(x => x.id).ToListAsync();
                                            if (agCashObj.Count > 0)
                                            {
                                                foreach (var cash in agCashObj)
                                                {
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = item.EventId,
                                                        MarketId = item.MarketId,
                                                        SelectionId = item.SelectionId,
                                                        Discription = item.Discription,
                                                        MarketName = item.MarketName,
                                                        Remark = "",
                                                        Amount = cash.Amount,
                                                        Balance = cash.Balance,
                                                        CreatedOn = item.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                        else if (item.MarketName != "Fancy")
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                CreatedOn = item.createdOn,
                                                Balance = usrObj.Balance,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                        else
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId && x.SelectionId == item.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                Balance = usrObj.Balance,
                                                CreatedOn = item.createdOn,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                    }
                                }
                                break;
                            case "Cash":
                                var cashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == "Cash").OrderBy(x => x.id).ToListAsync();
                                if (cashObj.Count > 0)
                                {
                                    foreach (var obj in cashObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Sports":
                                if (sportsId == 0)
                                {
                                    var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.AdminId == userId && x.MarketName != "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                    if (sportsObj.Count > 0)
                                    {
                                        foreach (var obj in sportsObj)
                                        {
                                            if (obj.MarketName != "Fancy")
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                    CreatedOn = obj.createdOn,
                                                    Balance = usrObj.Balance,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                            else
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                    Balance = usrObj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (marketName == "All")
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.AdminId == userId && x.SportsId == sportsId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.AdminId == userId && x.SportsId == sportsId && x.MarketName == marketName && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.AdminId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = Math.Round(usrObj.Share / 100 * -profitLoss, 2),
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    case "Admin":
                        switch (type)
                        {
                            case "All":
                                var tranAgObj = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                if (tranAgObj.Count > 0)
                                {
                                    foreach (var item in tranAgObj)
                                    {
                                        if (item.MarketName == "Cash")
                                        {
                                            var agCashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName).OrderBy(x => x.id).ToListAsync();
                                            if (agCashObj.Count > 0)
                                            {
                                                foreach (var cash in agCashObj)
                                                {
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = item.EventId,
                                                        MarketId = item.MarketId,
                                                        SelectionId = item.SelectionId,
                                                        Discription = item.Discription,
                                                        MarketName = item.MarketName,
                                                        Remark = "",
                                                        Amount = cash.Amount,
                                                        Balance = cash.Balance,
                                                        CreatedOn = item.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                        else if (item.MarketName != "Fancy")
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = -profitLoss,
                                                CreatedOn = item.createdOn,
                                                Balance = usrObj.Balance,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                        else
                                        {
                                            double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == item.MarketName && x.MarketId == item.MarketId && x.SelectionId == item.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                            TransactionDTO transDTO = new TransactionDTO()
                                            {
                                                UserName = usrObj.UserName,
                                                EventId = item.EventId,
                                                MarketId = item.MarketId,
                                                SelectionId = item.SelectionId,
                                                Discription = item.Discription,
                                                MarketName = item.MarketName,
                                                Remark = "",
                                                Amount = -profitLoss,
                                                Balance = usrObj.Balance,
                                                CreatedOn = item.createdOn,
                                            };
                                            transObjList.Add(transDTO);
                                        }
                                    }
                                }
                                break;
                            case "Cash":
                                var cashObj = await db.Transaction.Where(x => x.UserId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == "Cash").OrderBy(x => x.id).ToListAsync();
                                if (cashObj.Count > 0)
                                {
                                    foreach (var obj in cashObj)
                                    {
                                        TransactionDTO transDTO = new TransactionDTO()
                                        {
                                            UserName = usrObj.UserName,
                                            EventId = obj.EventId,
                                            MarketId = obj.MarketId,
                                            SelectionId = obj.SelectionId,
                                            Discription = obj.Discription,
                                            MarketName = obj.MarketName,
                                            Remark = "",
                                            Amount = obj.Amount,
                                            Balance = obj.Balance,
                                            CreatedOn = obj.createdOn,
                                        };
                                        transObjList.Add(transDTO);
                                    }
                                }
                                break;
                            case "Sports":
                                if (sportsId == 0)
                                {
                                    var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.SuperId == userId && x.MarketName != "Cash" && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                    if (sportsObj.Count > 0)
                                    {
                                        foreach (var obj in sportsObj)
                                        {
                                            if (obj.MarketName != "Fancy")
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = -profitLoss,
                                                    CreatedOn = obj.createdOn,
                                                    Balance = usrObj.Balance,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                            else
                                            {
                                                double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                TransactionDTO transDTO = new TransactionDTO()
                                                {
                                                    UserName = usrObj.UserName,
                                                    EventId = obj.EventId,
                                                    MarketId = obj.MarketId,
                                                    SelectionId = obj.SelectionId,
                                                    Discription = obj.Discription,
                                                    MarketName = obj.MarketName,
                                                    Remark = "",
                                                    Amount = -profitLoss,
                                                    Balance = usrObj.Balance,
                                                    CreatedOn = obj.createdOn,
                                                };
                                                transObjList.Add(transDTO);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (marketName == "All")
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.SuperId == userId && x.SportsId == sportsId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId, x.MarketName }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var sportsObj = await db.Transaction.AsNoTracking().Where(x => x.SuperId == userId && x.SportsId == sportsId && x.MarketName == marketName && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate).OrderBy(x => x.id).GroupBy(x => new { x.MarketId }).Select(x => x.FirstOrDefault()).ToListAsync();
                                        if (sportsObj.Count > 0)
                                        {
                                            foreach (var obj in sportsObj)
                                            {
                                                if (obj.MarketName != "Fancy")
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        CreatedOn = obj.createdOn,
                                                        Balance = usrObj.Balance,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                                else
                                                {
                                                    double profitLoss = await db.Transaction.Where(x => x.SuperId == userId && !x.deleted && x.createdOn >= sDate && x.createdOn <= eDate && x.MarketName == obj.MarketName && x.MarketId == obj.MarketId && x.SelectionId == obj.SelectionId).Select(x => x.Amount).DefaultIfEmpty(0).SumAsync();
                                                    TransactionDTO transDTO = new TransactionDTO()
                                                    {
                                                        UserName = usrObj.UserName,
                                                        EventId = obj.EventId,
                                                        MarketId = obj.MarketId,
                                                        SelectionId = obj.SelectionId,
                                                        Discription = obj.Discription,
                                                        MarketName = obj.MarketName,
                                                        Remark = "",
                                                        Amount = -profitLoss,
                                                        Balance = usrObj.Balance,
                                                        CreatedOn = obj.createdOn,
                                                    };
                                                    transObjList.Add(transDTO);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                }
                if (transObjList.Count > 0)
                {
                    responseDTO.Status = true;
                    responseDTO.Result = transObjList;
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Result = transObjList;
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }
    }
}