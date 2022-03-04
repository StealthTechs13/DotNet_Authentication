using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using FixWebApi.Authentication;
using FixWebApi.Models;
using FixWebApi.Models.DTO;

namespace FixWebApi.Controllers
{
    [RoutePrefix("SignUp")]
    public class SignUpController : ApiController
    {
        ResponseDTO responseDTO = new ResponseDTO();
        private FixDbContext db = new FixDbContext();
        Runtime run_time = new Runtime();
        CheckTokenExpire _check = new CheckTokenExpire();

        // GET: api/SignUpModels/CheckToken
        [HttpGet]
        [Route("Token_Refresh")]
        public async Task<IHttpActionResult> tokenRefresh(int userId)
        {
            try
            {
                var signUpModel = await db.SignUp.Where(x => x.id == userId && !x.deleted).FirstOrDefaultAsync();
                if (signUpModel != null)
                {
                    string token = _check.refreshToken(run_time.RunTimeToken(), signUpModel);
                    if (!string.IsNullOrEmpty(token))
                    {
                        responseDTO.Status = true;
                        responseDTO.Result = token;
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = HttpStatusCode.Forbidden;
                    }
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Result = HttpStatusCode.Forbidden;
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }


        // Post: api/SignUpModels/Login
        [HttpPost]
        [Route("Valid_Login")]
        public async Task<IHttpActionResult> ValidLogin(LoginDTO loginDto)
        {
            try
            {
                string ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(ip))
                {
                    ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                SignUpModel signUpModel = new SignUpModel();
                signUpModel = db.SignUp.Where(x => x.UserId == loginDto.LoginID).FirstOrDefault();
                if (signUpModel != null)
                {
                    if (signUpModel.Password == loginDto.Password)
                    {
                        if (signUpModel.deleted == true)
                        {
                            responseDTO.Status = false;
                            responseDTO.Result = "Account Closed";
                        }
                        else
                        {
                            if (signUpModel.status == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Account Blocked";
                            }
                            else
                            {
                                string token = TokenManager.generateToken(signUpModel);
                                if (!string.IsNullOrEmpty(token))
                                {
                                    signUpModel.IpAddress = ip;
                                    await db.SaveChangesAsync();
                                    responseDTO.Status = true;
                                    responseDTO.Result = token;
                                }
                            }
                        }
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "Wrong Password";
                    }
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Result = "InValid UserName";
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }

        // GET: api/SignUpModels
        [HttpGet]
        [Route("GetBy_ParentId")]
        public async Task<IHttpActionResult> GetSignUp(string type, int take, int referById, int skipRec, string value,string role)
        {
            try
            {
                BalanceDetailDTO balObj = new BalanceDetailDTO();
                switch (role)
                {
                    case "Admin":
                        balObj.DownLineBal = await db.SignUp.Where(x => x.SuperId == referById && !x.deleted).Select(x => x.Balance).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineExp = await db.SignUp.Where(x => x.SuperId == referById && !x.deleted).Select(x => x.Exposure).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineAvailBal = Math.Round(balObj.DownLineBal - balObj.DownLineExp, 2);
                        balObj.OwnBal = (await db.SignUp.Where(x => x.id == referById && !x.deleted).FirstOrDefaultAsync()).Balance;
                        balObj.TotalBal = Math.Round(balObj.OwnBal + balObj.DownLineAvailBal, 2);

                        switch (type)
                        {
                            case "All":
                            case "Inner":

                                var signObj = await (from s in db.SignUp
                                                     where
                                 s.ParentId.Equals(referById) && !s.deleted
                                                     select new
                                                     {
                                                         s.id,
                                                         s.UserId,
                                                         s.Role,
                                                         s.Balance,
                                                         s.CreditLimit,
                                                         profitLoss= s.Balance + db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum()-s.CreditLimit,
                                                         DownBal = db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         DownExp =db.Exposure.Where(x=>x.AdminId==s.id && !x.deleted).Select(l=>l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         DownAvailBal = db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.AdminId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         TotalBal = s.Balance + db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                         s.createdOn,
                                                         Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                         s.BetStatus,
                                                         s.FancyBetStatus,
                                                         s.status,
                                                         s.deleted,
                                                         s.CasinoStatus,
                                                         s.TableStatus,
                                                         s.Password,
                                                         s.MobileNumber,
                                                         s.Share,
                                                     }).AsNoTracking().OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (signObj.Count > 0)
                                {
                                    balObj.usrObj = signObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                    responseDTO.Count = await db.SignUp.CountAsync(x => x.ParentId == referById && !x.deleted);
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                            case "Search":
                                var searchObj = await (from s in db.SignUp
                                                       where
                                   s.ParentId.Equals(referById) && s.UserId.ToLower().Contains(value.ToLower()) && !s.deleted
                                                       select new
                                                       {
                                                           s.id,
                                                           s.UserId,
                                                           s.Role,
                                                           s.Balance,
                                                           s.CreditLimit,
                                                           profitLoss = s.Balance + db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                           DownBal = db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           DownExp = db.Exposure.Where(x => x.AdminId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           DownAvailBal = db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.AdminId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           TotalBal = s.Balance + db.SignUp.Where(x => x.AdminId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                           s.createdOn,
                                                           Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                           s.BetStatus,
                                                           s.FancyBetStatus,
                                                           s.status,
                                                           s.deleted,
                                                           s.CasinoStatus,
                                                           s.TableStatus,
                                                           s.Password,
                                                           s.MobileNumber,
                                                           s.Share,
                                                       }).AsNoTracking().ToListAsync();
                                if (searchObj.Count > 0)
                                {
                                    balObj.usrObj = searchObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                        }
                        break;
                    case "SubAdmin":
                        balObj.DownLineBal = await db.SignUp.Where(x => x.AdminId == referById && !x.deleted).Select(x => x.Balance).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineExp = await db.SignUp.Where(x => x.AdminId == referById && !x.deleted).Select(x => x.Exposure).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineAvailBal = Math.Round(balObj.DownLineBal - balObj.DownLineExp, 2);
                        balObj.OwnBal = (await db.SignUp.Where(x => x.id == referById && !x.deleted).FirstOrDefaultAsync()).Balance;
                        balObj.TotalBal = Math.Round(balObj.OwnBal + balObj.DownLineAvailBal, 2);

                        switch (type)
                        {
                            case "All":
                            case "Inner":

                                var signObj = await (from s in db.SignUp
                                                     where
                                 s.ParentId.Equals(referById) && !s.deleted
                                                     select new
                                                     {
                                                         s.id,
                                                         s.UserId,
                                                         s.Role,
                                                         s.Balance,
                                                         s.CreditLimit,
                                                         profitLoss = s.Balance + db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                         DownBal = db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         DownExp = db.Exposure.Where(x => x.MasterId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         DownAvailBal = db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.MasterId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         TotalBal = s.Balance + db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                         s.createdOn,
                                                         Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                         s.BetStatus,
                                                         s.FancyBetStatus,
                                                         s.status,
                                                         s.deleted,
                                                         s.CasinoStatus,
                                                         s.TableStatus,
                                                         s.Password,
                                                         s.MobileNumber,
                                                         s.Share,
                                                     }).AsNoTracking().OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (signObj.Count > 0)
                                {
                                    balObj.usrObj = signObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                    responseDTO.Count = await db.SignUp.CountAsync(x => x.ParentId == referById && !x.deleted);
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                            case "Search":
                                var searchObj = await (from s in db.SignUp
                                                       where
                                   s.ParentId.Equals(referById) && s.UserId.ToLower().Contains(value.ToLower()) && !s.deleted
                                                       select new
                                                       {
                                                           s.id,
                                                           s.UserId,
                                                           s.Role,
                                                           s.Balance,
                                                           s.CreditLimit,
                                                           profitLoss = s.Balance + db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                           DownBal = db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           DownExp = db.Exposure.Where(x => x.MasterId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           DownAvailBal = db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.MasterId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           TotalBal = s.Balance + db.SignUp.Where(x => x.MasterId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                           s.createdOn,
                                                           Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                           s.BetStatus,
                                                           s.FancyBetStatus,
                                                           s.status,
                                                           s.deleted,
                                                           s.CasinoStatus,
                                                           s.TableStatus,
                                                           s.Password,
                                                           s.MobileNumber,
                                                           s.Share,
                                                       }).AsNoTracking().ToListAsync();
                                if (searchObj.Count > 0)
                                {
                                    balObj.usrObj = searchObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                        }
                        break;
                    case "Master":
                        balObj.DownLineBal = await db.SignUp.Where(x => x.MasterId == referById && !x.deleted).Select(x => x.Balance).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineExp = await db.SignUp.Where(x => x.MasterId == referById && !x.deleted).Select(x => x.Exposure).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineAvailBal = Math.Round(balObj.DownLineBal - balObj.DownLineExp, 2);
                        balObj.OwnBal = (await db.SignUp.Where(x => x.id == referById && !x.deleted).FirstOrDefaultAsync()).Balance;
                        balObj.TotalBal = Math.Round(balObj.OwnBal + balObj.DownLineAvailBal, 2);

                        switch (type)
                        {
                            case "All":
                            case "Inner":

                                var signObj = await (from s in db.SignUp
                                                     where
                                 s.ParentId.Equals(referById) && !s.deleted
                                                     select new
                                                     {
                                                         s.id,
                                                         s.UserId,
                                                         s.Role,
                                                         s.Balance,
                                                         s.CreditLimit,
                                                         profitLoss = s.Balance + db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                         DownBal = db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         DownExp = db.Exposure.Where(x => x.ParentId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         DownAvailBal = db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.ParentId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         TotalBal = s.Balance + db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                         ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                         s.createdOn,
                                                         Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                         s.BetStatus,
                                                         s.FancyBetStatus,
                                                         s.status,
                                                         s.deleted,
                                                         s.CasinoStatus,
                                                         s.TableStatus,
                                                         s.Password,
                                                         s.MobileNumber,
                                                         s.Share,
                                                     }).AsNoTracking().OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (signObj.Count > 0)
                                {
                                    balObj.usrObj = signObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                    responseDTO.Count = await db.SignUp.CountAsync(x => x.ParentId == referById && !x.deleted);
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                            case "Search":
                                var searchObj = await (from s in db.SignUp
                                                       where
                                   s.ParentId.Equals(referById) && s.UserId.ToLower().Contains(value.ToLower()) && !s.deleted
                                                       select new
                                                       {
                                                           s.id,
                                                           s.UserId,
                                                           s.Role,
                                                           s.Balance,
                                                           s.CreditLimit,
                                                           profitLoss = s.Balance + db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                           DownBal = db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           DownExp = db.Exposure.Where(x => x.ParentId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           DownAvailBal = db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.ParentId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           TotalBal = s.Balance + db.SignUp.Where(x => x.ParentId == s.id).Select(x => x.Balance).DefaultIfEmpty(0).Sum(),
                                                           ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                           s.createdOn,
                                                           Count = db.SignUp.Count(x => x.ParentId == s.id && !x.deleted),
                                                           s.BetStatus,
                                                           s.FancyBetStatus,
                                                           s.status,
                                                           s.deleted,
                                                           s.CasinoStatus,
                                                           s.TableStatus,
                                                           s.Password,
                                                           s.MobileNumber,
                                                           s.Share,
                                                       }).AsNoTracking().ToListAsync();
                                if (searchObj.Count > 0)
                                {
                                    balObj.usrObj = searchObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                        }
                        break;
                    case "Agent":
                        balObj.DownLineBal = await db.SignUp.Where(x => x.ParentId == referById && !x.deleted).Select(x => x.Balance).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineExp = await db.SignUp.Where(x => x.ParentId == referById && !x.deleted).Select(x => x.Exposure).DefaultIfEmpty(0).SumAsync();
                        balObj.DownLineAvailBal = Math.Round(balObj.DownLineBal - balObj.DownLineExp, 2);
                        balObj.OwnBal = (await db.SignUp.Where(x => x.id == referById && !x.deleted).FirstOrDefaultAsync()).Balance;
                        balObj.TotalBal = Math.Round(balObj.OwnBal + balObj.DownLineAvailBal, 2);

                        switch (type)
                        {
                            case "All":
                            case "Inner":

                                var signObj = await (from s in db.SignUp
                                                     where
                                 s.ParentId.Equals(referById) && !s.deleted
                                                     select new
                                                     {
                                                         s.id,
                                                         s.UserId,
                                                         s.Role,
                                                         s.Balance,
                                                         s.CreditLimit,
                                                         profitLoss = db.Transaction.Where(x=>x.UserId==s.id).Select(l=>l.Amount).DefaultIfEmpty(0).Sum()-s.CreditLimit,
                                                         DownExp = db.Exposure.Where(x => x.UserId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         DownAvailBal = db.Transaction.Where(x => x.UserId == s.id).Select(l => l.Amount).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.UserId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                         
                                                         ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                         s.createdOn,
                                                         s.BetStatus,
                                                         s.FancyBetStatus,
                                                         s.status,
                                                         s.deleted,
                                                         s.CasinoStatus,
                                                         s.TableStatus,
                                                         s.Password,
                                                         s.MobileNumber,
                                                         s.Share,
                                                     }).AsNoTracking().OrderBy(x => x.id).Skip(skipRec).Take(take).ToListAsync();
                                if (signObj.Count > 0)
                                {
                                    balObj.usrObj = signObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                    responseDTO.Count = await db.SignUp.CountAsync(x => x.ParentId == referById && !x.deleted);
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                            case "Search":
                                var searchObj = await (from s in db.SignUp
                                                       where
                                   s.ParentId.Equals(referById) && s.UserId.ToLower().Contains(value.ToLower()) && !s.deleted
                                                       select new
                                                       {
                                                           s.id,
                                                           s.UserId,
                                                           s.Role,
                                                           s.Balance,
                                                           s.CreditLimit,
                                                           profitLoss = db.Transaction.Where(x => x.UserId == s.id).Select(l => l.Amount).DefaultIfEmpty(0).Sum() - s.CreditLimit,
                                                           DownExp = db.Exposure.Where(x => x.UserId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),
                                                           DownAvailBal = db.Transaction.Where(x => x.UserId == s.id).Select(l => l.Amount).DefaultIfEmpty(0).Sum() + db.Exposure.Where(x => x.UserId == s.id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).Sum(),

                                                           ParentName = db.SignUp.Where(x => x.id == s.ParentId).FirstOrDefault().UserName,
                                                           s.createdOn,
                                                           s.BetStatus,
                                                           s.FancyBetStatus,
                                                           s.status,
                                                           s.deleted,
                                                           s.CasinoStatus,
                                                           s.TableStatus,
                                                           s.Password,
                                                           s.MobileNumber,
                                                           s.Share,
                                                       }).AsNoTracking().ToListAsync();
                                if (searchObj.Count > 0)
                                {
                                    balObj.usrObj = searchObj;
                                    responseDTO.Status = true;
                                    responseDTO.Result = balObj;
                                }
                                else
                                {
                                    responseDTO.Status = false;
                                    responseDTO.Result = balObj;
                                }
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }

        // POST: api/SignUpModels
        [HttpPost]
        [Route("Create_User")]
        public async Task<IHttpActionResult> PostSignUpModel(SignUpModel SignObj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                int user_Id = Convert.ToInt32(run_time.RunTimeUserId());
                bool userName = ExistsUserName(SignObj.UserId);
                bool m_Number = ExistsMnumber(SignObj.MobileNumber);

                if ((!userName) && (!m_Number))
                {
                    var ParentData = await db.SignUp.Where(x => x.id == user_Id && x.deleted == false).FirstOrDefaultAsync();
                    if (ParentData != null)
                    {
                        string role = ParentData.Role == "Admin" ? "SubAdmin" :
                                 ParentData.Role == "SubAdmin" ? "Master" :
                                 ParentData.Role == "Master" ? "Agent" : " Client";
                        if (role != "Client")
                        {
                            if (ParentData.Share < SignObj.Share)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Share must be equal or less than parent share";
                                return Ok(responseDTO);
                            }
                        }
                        switch (ParentData.Role)
                        {
                            case "Admin":
                                SignObj.ParentId = ParentData.id;
                                SignObj.MasterId = ParentData.MasterId;
                                SignObj.AdminId = ParentData.AdminId;
                                SignObj.SuperId = ParentData.id;
                                break;
                            case "SubAdmin":
                                SignObj.ParentId = ParentData.id;
                                SignObj.MasterId = ParentData.MasterId;
                                SignObj.AdminId = ParentData.id;
                                SignObj.SuperId = ParentData.SuperId;
                                break;
                            case "Master":
                                SignObj.ParentId = ParentData.id;
                                SignObj.MasterId = ParentData.id;
                                SignObj.AdminId = ParentData.AdminId;
                                SignObj.SuperId = ParentData.SuperId;
                                break;
                            case "Agent":
                                SignObj.ParentId = ParentData.id;
                                SignObj.MasterId = ParentData.MasterId;
                                SignObj.AdminId = ParentData.AdminId;
                                SignObj.SuperId = ParentData.SuperId;
                                break;

                        }

                        SignObj.Role = role;
                        SignObj.BetStatus = ParentData.BetStatus;
                        SignObj.FancyBetStatus = ParentData.FancyBetStatus;
                        SignObj.CasinoStatus = ParentData.CasinoStatus;
                        SignObj.TableStatus = ParentData.TableStatus;
                        SignObj.ExposureLimit = 0;
                        SignObj.IpAddress = "";
                        SignObj.Exposure = 0;
                        SignObj.ProfitLoss = 0;
                        SignObj.CreditLimit = 0;
                        SignObj.createdOn = DateTime.Now;
                        SignObj.deleted = false;
                        SignObj.status = ParentData.status;
                        db.SignUp.Add(SignObj);
                        int returnValue = await db.SaveChangesAsync();
                        if (returnValue > 0)
                        {
                            TakeRecord takeRec = new TakeRecord()
                            {
                                UserId = SignObj.id,
                                Records = 10,
                                createdOn = DateTime.Now,
                            };
                            db.TakeRecord.Add(takeRec);
                            var chipModel = await db.Chip.Where(x => x.UserId == 1 && !x.deleted).ToListAsync();
                            if (chipModel.Count > 0)
                            {
                                foreach (var item in chipModel)
                                {
                                    ChipModel obj = new ChipModel()
                                    {
                                        UserId = SignObj.id,
                                        ChipName = item.ChipName,
                                        ChipValue = item.ChipValue,
                                        status = item.status,
                                        deleted = item.deleted,
                                        createdOn = DateTime.Now,
                                    };
                                    db.Chip.Add(obj);
                                }
                            }
                            await db.SaveChangesAsync();
                            responseDTO.Status = true;
                            responseDTO.Result = "Successfully Created!";
                        }
                        else
                        {
                            responseDTO.Status = false;
                            responseDTO.Result = "DataBase Exception At the time of Create data";
                        }
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "No Parent Found";
                    }

                }
                else
                {
                    if (userName)
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "Username All ready Exist";
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "Mobile No All ready Exist";
                    }

                }

            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;

            }
            return Ok(responseDTO);
        }

        [HttpPost]
        [Route("Create_Master")]
        public async Task<IHttpActionResult> CreateMaster(string type, SignUpModel signObj)
        {
            try
            {
                if (type == "SecurityForPost")
                {
                    signObj.BetStatus = false;
                    signObj.status = false;
                    signObj.deleted = false;
                    signObj.FancyBetStatus = false;
                    signObj.CasinoStatus = false;
                    signObj.TableStatus = false;
                    signObj.ExposureLimit = 0;
                    signObj.IpAddress = "";
                    signObj.Exposure = 0;
                    signObj.ProfitLoss = 0;
                    signObj.CreditLimit = 0;
                    signObj.createdOn = DateTime.Now;
                    db.SignUp.Add(signObj);
                    await db.SaveChangesAsync();
                    responseDTO.Status = true;
                    responseDTO.Result = "Done";
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Result = "Type Error";
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }

        // PUT : api/SignUpModels
        [HttpGet]
        [Route("Update_User")]
        public async Task<IHttpActionResult> PutSignUpModel(string Type, string Value, int userId)
        {
            try
            {
                SignUpModel SignObj = new SignUpModel();
                List<SignUpModel> parentObj = new List<SignUpModel>();
                SignObj = await db.SignUp.Where(x => x.id == userId).FirstOrDefaultAsync();
                var checkParent = await db.SignUp.Where(x => x.id == SignObj.ParentId).FirstOrDefaultAsync();

                if (SignObj != null)
                {
                    switch (SignObj.Role)
                    {
                        case "SubAdmin":
                            parentObj = await db.SignUp.Where(x => x.AdminId == userId).ToListAsync();
                            break;
                        case "Master":
                            parentObj = await db.SignUp.Where(x => x.MasterId == userId).ToListAsync();
                            break;
                        case "Agent":
                            parentObj = await db.SignUp.Where(x => x.ParentId == userId).ToListAsync();
                            break;
                    }
                    switch (Type)
                    {
                        case "BetStatus":
                            if (checkParent.BetStatus == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent BetStatus Is Block So You Can't Do Any Thing Without your UpperLine Permission";
                                return Ok(responseDTO);
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.BetStatus = true;
                                }
                                else
                                {
                                    SignObj.BetStatus = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.BetStatus = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.BetStatus = false);
                                    }
                                }
                            }
                            break;
                        case "FancyBetStatus":
                            if (checkParent.FancyBetStatus == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent FancyBetStatus Is Block So You Can't Do Any Thing Without your UpperLine Permission";
                                return Ok(responseDTO);
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.FancyBetStatus = true;
                                }
                                else
                                {
                                    SignObj.FancyBetStatus = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.FancyBetStatus = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.FancyBetStatus = false);
                                    }
                                }
                            }
                            break;
                        case "CasinoStatus":
                            if (checkParent.CasinoStatus == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent CasinoStatus Is Block So You Can't Do Any Things. Without your UpperLine Permission";
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.CasinoStatus = true;
                                }
                                else
                                {
                                    SignObj.CasinoStatus = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.CasinoStatus = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.CasinoStatus = false);
                                    }

                                }
                            }
                            break;
                        case "TableStatus":
                            if (checkParent.TableStatus == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent VirtualStatus Is Block So You Can't Do Any Things. Without your UpperLine Permission";
                                return Ok(responseDTO);
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.TableStatus = true;
                                }
                                else
                                {
                                    SignObj.TableStatus = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.TableStatus = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.TableStatus = false);
                                    }
                                }
                            }
                            break;

                        case "Status":
                            if (checkParent.status == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent Status Is Block So You Can't Do Any Things. Without your UpperLine Permission";
                                return Ok(responseDTO);
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.status = true;
                                }
                                else
                                {
                                    SignObj.status = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.status = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.status = false);
                                    }
                                }
                            }
                            break;
                        case "deleted":
                            if (checkParent.deleted == true)
                            {
                                responseDTO.Status = false;
                                responseDTO.Result = "Parent Status Is Closed So You Can't Do Any Things. Without your UpperLine Permission";
                                return Ok(responseDTO);
                            }
                            else
                            {
                                if (Value == "true")
                                {
                                    SignObj.deleted = true;
                                }
                                else
                                {
                                    SignObj.deleted = false;
                                }
                                if (parentObj.Count > 0)
                                {
                                    if (Value == "true")
                                    {
                                        parentObj.ForEach(x => x.deleted = true);
                                    }
                                    else
                                    {
                                        parentObj.ForEach(x => x.deleted = false);
                                    }
                                }
                            }
                            break;
                        case "Password":
                            SignObj.Password = Value;
                            break;
                    }
                    int returnValue = await db.SaveChangesAsync();
                    if (returnValue > 0)
                    {
                        responseDTO.Status = true;
                        responseDTO.Result = "Executed Successfully";
                    }
                    else
                    {
                        responseDTO.Status = false;
                        responseDTO.Result = "DataBase Exception At the time of Update data";
                    }
                }
                else
                {
                    responseDTO.Status = false;
                    responseDTO.Result = "NO Data Found";
                }

            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }



        [HttpGet]
        [Route("GetUserDetails")]
        public async Task<IHttpActionResult> getUserDetails()
        {
            try
            {
                int id = Convert.ToInt32(run_time.RunTimeUserId());
                switch (run_time.RunTimeRole())
                {
                    case "Client":
                        double Exposure = await db.Exposure.AsNoTracking().Where(x => x.UserId == id && !x.deleted).Select(l => l.Exposure).DefaultIfEmpty(0).SumAsync();
                        double bal = await db.Transaction.AsNoTracking().Where(x => x.UserId == id && !x.deleted).Select(l => l.Amount).DefaultIfEmpty(0).SumAsync();
                        var objData = await (from s in db.SignUp
                                             where s.id.Equals(id)
                                             select new
                                             {
                                                 s.UserId,
                                                 Bal = bal + Exposure,
                                                 ProfitLoss=bal-s.CreditLimit,
                                                 Exp = Exposure,
                                                 s.UserName,
                                                 s.MobileNumber,
                                                 s.Password,
                                                 s.CasinoStatus,
                                                 s.BetStatus,
                                                 s.FancyBetStatus,
                                                 s.TableStatus,
                                                 s.status,
                                             }).AsNoTracking().FirstOrDefaultAsync();

                        if (objData != null)
                        {
                            responseDTO.Status = true;
                            responseDTO.Result = objData;
                        }
                        else
                        {
                            responseDTO.Status = false;
                            responseDTO.Result = objData;
                        }
                        break;
                    default:
                        var adData = await (from s in db.SignUp
                                            where s.id.Equals(id)
                                            select new
                                            {
                                                s.UserId,
                                                Bal = s.Balance,
                                                Exp = 0,
                                                s.Share,
                                                News = db.News.Where(x => !x.deleted).Select(n => new
                                                {
                                                    n.createdOn,
                                                    n.News,
                                                }).ToList(),
                                                TopEvents = db.Event.Where(x => !x.deleted && x.IsFav).Select(e => new
                                                {
                                                    e.EventName,
                                                    e.EventTime,
                                                }).OrderBy(x => x.EventTime).ToList(),
                                                Take = db.TakeRecord.Where(x => x.UserId == id).FirstOrDefault().Records,
                                            }).AsNoTracking().FirstOrDefaultAsync();
                        if (adData != null)
                        {
                            responseDTO.Status = true;
                            responseDTO.Result = adData;
                        }
                        else
                        {
                            responseDTO.Status = false;
                            responseDTO.Result = adData;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                responseDTO.Status = false;
                responseDTO.Result = ex.Message;
            }
            return Ok(responseDTO);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpGet]
        [Route("CheckLoginId")]
        public bool ExistsUserName(string userId)
        {
            return db.SignUp.Count(e => e.UserId == userId) > 0;
        }
        [HttpGet]
        [Route("CheckMobile")]
        public bool ExistsMnumber(string m_number)
        {
            return db.SignUp.Count(e => e.MobileNumber == m_number) > 0;
        }
    }
}