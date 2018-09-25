﻿using Auctus.DataAccessInterfaces.Asset;
using Auctus.DomainObjects.Advisor;
using Auctus.DomainObjects.Asset;
using Auctus.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Auctus.Business.Advisor.AdvisorBusiness;

namespace Auctus.Business.Asset
{
    public class AssetCurrentValueBusiness : BaseBusiness<AssetCurrentValue, IAssetCurrentValueData<AssetCurrentValue>>
    {
        public AssetCurrentValueBusiness(IConfigurationRoot configuration, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(configuration, serviceProvider, serviceScopeFactory, loggerFactory, cache, email, ip) { }

        public List<AssetCurrentValue> ListAllAssets(IEnumerable<int> ids = null)
        {
            return Data.ListAllAssets(ids);
        }

        public void UpdateAssetCurrentValues(List<AssetCurrentValue> assetCurrentValues)
        {
            Data.UpdateAssetValue(assetCurrentValues);
        }

        public double? GetCurrentValue(int assetId)
        {
            var assetCurrentValue = ListAllAssets(new int[] { assetId });
            if (assetCurrentValue == null || !assetCurrentValue.Any() || assetCurrentValue[0].UpdateDate < Data.GetDateTimeNow().AddHours(-4))
                return AssetValueBusiness.LastAssetValue(assetId)?.Value;
            else
                return assetCurrentValue[0].CurrentValue;
        }

        public List<AssetCurrentValue> ListAssetsValuesForCalculation(IEnumerable<int> assetIds, CalculationMode mode, IEnumerable<Advice> allAdvices, int? selectAssetId = null, int? selectAdvisorId = null)
        {
            var assetCurrentValues = ListAllAssets(assetIds);

            var assetDateMapping = new Dictionary<int, DateTime>();
            foreach (var asset in assetCurrentValues)
            {
                if (asset.UpdateDate < Data.GetDateTimeNow().AddHours(-4))
                {
                    if (mode == CalculationMode.AdvisorBase || mode == CalculationMode.Feed || mode == CalculationMode.AdvisorDetailed)
                        assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddHours(-4));
                    else if (mode == CalculationMode.AssetDetailed)
                    {
                        if (selectAssetId.Value == asset.Id)
                            assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddDays(-30).AddHours(-4));
                        else
                            assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddHours(-4));
                    }
                    else if (mode == CalculationMode.AssetBase)
                        assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddDays(-1).AddHours(-4));
                }
                else if (mode == CalculationMode.AssetDetailed && selectAssetId.Value == asset.Id && !asset.Variation24Hours.HasValue)
                    assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddDays(-30).AddHours(-4));
                else if (mode == CalculationMode.AssetBase && !asset.Variation24Hours.HasValue)
                    assetDateMapping.Add(asset.Id, Data.GetDateTimeNow().AddDays(-1).AddHours(-4));
            }

            var assetValues = AssetValueBusiness.FilterAssetValues(assetDateMapping);

            foreach (var asset in assetCurrentValues)
            {
                if (assetDateMapping.ContainsKey(asset.Id))
                {
                    asset.AssetValues = assetValues.Where(c => c.AssetId == asset.Id).OrderByDescending(c => c.Date);
                    if (asset.AssetValues.Any())
                    {
                        var lastValue = asset.AssetValues.First();
                        asset.CurrentValue = lastValue.Value;
                        asset.UpdateDate = lastValue.Date;
                        AssetValueBusiness.VariantionCalculation(lastValue.Value, lastValue.Date, asset.AssetValues, out double? variation24h, out double? variation7d, out double? variation30d);
                        asset.Variation24Hours = variation24h;
                        asset.Variation7Days = variation7d;
                        asset.Variation30Days = variation30d;
                    }
                }
            }
            return assetCurrentValues;
        }
    }
}
