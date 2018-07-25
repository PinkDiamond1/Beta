﻿using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Asset;
using Auctus.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.Service
{
    public class AssetServices : BaseServices
    {
        public AssetServices(ILoggerFactory loggerFactory, Cache cache) : base(loggerFactory, cache) { }

        public void UpdateAllAssetsValues()
        {
            AssetBusiness.UpdateAllAssetsValues();
        }

        public void CreateAssets()
        {
            AssetBusiness.CreateCoinMarketCapNotIncludedAssets();
        }
        
        public List<Asset> ListAssets()
        {
            return AssetBusiness.ListAssets();
        }
    }
}
