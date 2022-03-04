using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IdentityModel.Tokens.Jwt;
using FixWebApi.Authentication.Interface;

namespace FixWebApi.Authentication
{
    public class Runtime : IRuntime
    {
        private const string AuthTokenHeaderName = "Authorization";
       

        public string RunTimeRole()
        {
            string userloginRole = "";
            var headers = HttpContext.Current.Request.Headers;
            string _authToken = headers.Get(AuthTokenHeaderName);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(_authToken.Replace("Bearer ", "")) as JwtSecurityToken;
            if (token != null)
            {
                var payload = (Dictionary<string, object>)token.Payload;
                userloginRole = payload["Role"].ToString();
            }
            return userloginRole;
        }

        public string RunTimeUserId()
        {
            string userloginRole = "";
            var headers = HttpContext.Current.Request.Headers;
            string _authToken = headers.Get(AuthTokenHeaderName);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(_authToken.Replace("Bearer ", "")) as JwtSecurityToken;
            if (token != null)
            {
                var payload = (Dictionary<string, object>)token.Payload;
                userloginRole = payload["UserId"].ToString();
            }
            return userloginRole;
        }

        public string RunTimeToken()
        {
            var headers = HttpContext.Current.Request.Headers;
            string _authToken = headers.Get(AuthTokenHeaderName);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadToken(_authToken.Replace("Bearer ", "")) as JwtSecurityToken;
            return token.ToString();
        }
    }
}