using FixWebApi.Models;
using FixWebApi.Models.DTO;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace FixWebApi.Authentication
{
    public class CheckTokenExpire
    {
        //private IJsonSerializer _serializer = new JsonNetSerializer();
        //private IDateTimeProvider _provider = new UtcDateTimeProvider();
        //private IBase64UrlEncoder _urlREncoder = new JwtBase64UrlEncoder();
        //private IJwtAlgorithm _algorithm = new HMACSHA256Algorithm();
        //IJwtValidator _validator = new JwtValidator(_serializer, _provider);
        //IJwtDecoder decoder = new JwtDecoder(_serializer, _validator, _urlREncoder, _algorithm);
        //var token = decoder.DecodeToObject<JwtToken>(access_token);
        //DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(token.exp);
        //return dateTimeOffset.LocalDateTime.ToString();

        public string refreshToken(string access_token,SignUpModel signUpModel)
        {
            try
            {
                if (!string.IsNullOrEmpty(access_token))
                {
                    var jwthandler = new JwtSecurityTokenHandler();
                    var jwtToken = jwthandler.ReadToken(access_token as string);
                    var expDate = jwtToken.ValidTo;
                    if (expDate < DateTime.Now)
                    {
                        access_token= TokenManager.generateToken(signUpModel);
                    }
                    
                }
                else
                {
                   access_token= TokenManager.generateToken(signUpModel);
                }
                return access_token;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}