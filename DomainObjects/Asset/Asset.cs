﻿using Auctus.Util.DapperAttributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auctus.DomainObjects.Asset
{
    public class Asset
    {
        [DapperKey(true)]
        [DapperType(System.Data.DbType.Int32)]
        public int Id { get; set; }
        [DapperType(System.Data.DbType.AnsiString)]
        public string Name { get; set; }
        [DapperType(System.Data.DbType.AnsiString)]
        public string Code { get; set; }
        [DapperType(System.Data.DbType.Int32)]
        public int Type { get; set; }
        [DapperType(System.Data.DbType.AnsiString)]
        public string CoinGeckoId { get; set; }
        [DapperType(System.Data.DbType.Boolean)]
        public bool ShortSellingEnabled { get; set; }
        [DapperType(System.Data.DbType.Double)]
        public double? MarketCap { get; set; }
        [DapperType(System.Data.DbType.AnsiString)]
        public string CoinMarketCalId { get; set; }
        [DapperType(System.Data.DbType.Boolean)]
        public bool Enabled { get; set; }
        [DapperType(System.Data.DbType.Double)]
        public double? CirculatingSupply { get; set; }

        public AssetType AssetType { get { return AssetType.Get(Type); } }
    }
}
