using FixWebApi.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace FixWebApi.Authentication
{
    public class CustomAuthorization: AuthorizeAttribute
    {
        ResponseDTO responseDto = new ResponseDTO();
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("You are unauthorized to access this resource")
            };
        }
    }
}