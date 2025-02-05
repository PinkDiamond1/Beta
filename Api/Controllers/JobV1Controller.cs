﻿using Api.Hubs;
using Api.Model.Account;
using Auctus.DomainObjects.Account;
using Auctus.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Produces("application/json")]
    [Route("api/v1/jobs/")]
    [Authorize("Bearer")]
    [EnableCors("Default")]
    [OnlyAdmin]
    public class JobV1Controller : JobBaseController
    {
        public JobV1Controller(ILoggerFactory loggerFactory, Cache cache, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory, IHubContext<AuctusHub> hubContext) :
            base(loggerFactory, cache, serviceProvider, serviceScopeFactory, hubContext) { }

        [Route("advisors")]
        [HttpPost]
        public new IActionResult UpdateAdvisorsRankingAndProfit()
        {
            return base.UpdateAdvisorsRankingAndProfit();
        }

        [Route("advisors/history")]
        [HttpPost]
        public new IActionResult SetAdvisorsRankingAndProfitHistory()
        {
            return base.SetAdvisorsRankingAndProfitHistory();
        }

        [Route("advisors/ranking")]
        [HttpPost]
        public new IActionResult SetAdvisorsMonthlyRanking()
        {
            return base.SetAdvisorsMonthlyRanking();
        }

        [Route("events")]
        [HttpPost]
        public new IActionResult UpdateAssetsEvents()
        {
            return base.UpdateAssetsEvents();
        }

        [Route("news")]
        [HttpPost]
        public new IActionResult UpdateLastNews()
        {
            return base.UpdateLastNews();
        }

        [Route("assets/binance/values")]
        [HttpPost]
        public new IActionResult UpdateAssetsValues([FromRoute]string api)
        {
            return base.UpdateAssetsValues(api);
        }

        [Route("assets/{api}/values/7dAnd30d")]
        [HttpPost]
        [ValidApi]
        public new IActionResult UpdateAssetsValues7dAnd30d([FromRoute]string api)
        {
            return base.UpdateAssetsValues7dAnd30d(api);
        }

        [Route("assets/{api}")]
        [HttpPost]
        [ValidApi]
        public new IActionResult CreateAssets([FromRoute]string api)
        {
            return base.CreateAssets(api);
        }

        [Route("assets/{api}/icons")]
        [HttpPost]
        [ValidApi]
        public new IActionResult UpdateAssetsIcons([FromRoute]string api)
        {
            return base.UpdateAssetsIcons(api);
        }

        [Route("assets/{api}/marketcap")]
        [HttpPost]
        [ValidApi]
        public new IActionResult UpdateAssetsMarketcap([FromRoute]string api)
        {
            return base.UpdateAssetsMarketcap(api);
        }

        [Route("users/auc")]
        [HttpPost]
        public new IActionResult SetUsersAuc()
        {
            return base.SetUsersAuc();
        }
    }
}
