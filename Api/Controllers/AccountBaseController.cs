﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Model.Account;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Web3;
using Auctus.Model;
using Auctus.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
    public class AccountBaseController : BaseController
    {
        protected AccountBaseController(ILoggerFactory loggerFactory, Cache cache, IServiceProvider serviceProvider) : base(loggerFactory, cache, serviceProvider) { }

        protected virtual IActionResult ValidateSignature(ValidateSignatureRequest signatureRequest)
        {
            if (signatureRequest == null)
                return BadRequest();
            
            try
            {
                return Ok(UserBusiness.ValidateSignature(signatureRequest.Address, signatureRequest.Signature));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        protected virtual IActionResult Login(LoginRequest loginRequest)
        {
            if (loginRequest == null)
                return BadRequest();

            try
            {
                var loginResponse = UserBusiness.Login(loginRequest.Email, loginRequest.Password);
                return Ok(new { logged = true, jwt = GenerateToken(loginRequest.Email.ToLower().Trim()), data = loginResponse });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}