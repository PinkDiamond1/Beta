﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Auctus.Model
{
    public class SearchResponse
    {
        public List<AssetResult> Assets { get; set; }
        public List<AdvisorResult> Advisors { get; set; }

        public class AssetResult
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public double? MarketCap { get; set; }
            public double? CirculatingSupply { get; set; }
        }

        public class AdvisorResult
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string UrlGuid { get; set; }
        }

        public SearchResponse()
        {
            Assets = new List<AssetResult>();
            Advisors = new List<AdvisorResult>();
        }
    }
}
