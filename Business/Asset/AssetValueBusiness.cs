﻿using Auctus.DataAccess.Asset;
using Auctus.DataAccess.Core;
using Auctus.DataAccess.Exchanges;
using Auctus.DataAccessInterfaces.Asset;
using Auctus.DomainObjects.Asset;
using Auctus.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Auctus.Business.Asset
{
    public class AssetValueBusiness : BaseBusiness<AssetValue, IAssetValueData<AssetValue>>
    {
        public AssetValueBusiness(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(serviceProvider, loggerFactory, cache, email, ip) { }

        internal AssetValue LastAssetValue(int assetId)
        {
            return Data.GetLastValue(assetId);
        }

        internal void UpdateAssetValue(DomainObjects.Asset.Asset asset)
        {
            var lastUpdatedValue = LastAssetValue(asset.Id)?.Date ?? Data.GetDateTimeNow().AddDays(-30).Date;
            if (lastUpdatedValue.AddMinutes(ExchangeApi.GAP_IN_MINUTES_BETWEEN_VALUES) > Data.GetDateTimeNow())
            {
                return;
            }
            Dictionary<DateTime, double> assetDateAndValues = ExchangeApi.GetCloseCryptoValue(asset.Code, lastUpdatedValue, MemoryCache);
            CreateAssetValueForPendingDates(asset, lastUpdatedValue, assetDateAndValues);
        }

        internal List<AssetValue> List(IEnumerable<int> assetsIds, DateTime startDate)
        {
            return Data.List(assetsIds, startDate);
        }

        internal Dictionary<DateTime, List<AssetValue>> GetAssetValuesGroupedByDate(IEnumerable<int> assetsIds, DateTime startDate)
        {
            var assetValues = Data.List(assetsIds, startDate);
            var now = Data.GetDateTimeNow();
            var rangeValueInMinutes = GetRangeValueToGroupAssetsInMinutes((int)Math.Ceiling(now.Subtract(startDate).TotalMinutes));
            var iterateDate = startDate.AddSeconds(-startDate.Second).AddMilliseconds(-startDate.Millisecond);
            var passedMinutes = (iterateDate.Hour * 60 + iterateDate.Minute);
            iterateDate = iterateDate.AddMinutes(rangeValueInMinutes).AddMinutes(-(passedMinutes % rangeValueInMinutes));
            var result = new Dictionary<DateTime, List<AssetValue>>();
            while (iterateDate < now)
            {
                var minimumDate = iterateDate.AddMinutes(-Math.Min((rangeValueInMinutes * 6), 1440));
                var assets = assetValues.Where(c => c.Date > minimumDate && c.Date <= iterateDate).GroupBy(c => c.AssetId).Select(s => s.OrderByDescending(x => x.Date).FirstOrDefault()).Where(c => c != null);
                result[iterateDate] = new List<AssetValue>();
                result[iterateDate].AddRange(assets);
                iterateDate = iterateDate.AddMinutes(rangeValueInMinutes);
            }
            return result;
        }

        private int GetRangeValueToGroupAssetsInMinutes(int totalMinutes)
        {
            var expected = totalMinutes / 300;
            if (expected <= 5)
                return 5;
            var possibilities = new int[] { 5, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 360, 480, 720, 1440 };
            return possibilities.Where(c => c <= expected).OrderByDescending(c => c).First();
        }

        private void CreateAssetValueForPendingDates(DomainObjects.Asset.Asset asset, DateTime lastUpdatedValue, Dictionary<DateTime, double> assetDateAndValues)
        {
            var pendingUpdate = assetDateAndValues?.Where(d => d.Key > lastUpdatedValue).OrderBy(v => v.Key);
           
            if (pendingUpdate != null)
            {
                List<AssetValue> assetValues = new List<AssetValue>();
                foreach (var pending in pendingUpdate)
                {
                    assetValues.Add(new DomainObjects.Asset.AssetValue() { AssetId = asset.Id, Date = pending.Key, Value = pending.Value });
                }
                Data.InsertManyAsync(assetValues);
            }
        }
    }
}
