﻿using Auctus.DataAccess.Advisor;
using Auctus.DataAccess.Core;
using Auctus.DataAccessInterfaces.Advisor;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Auctus.DomainObjects.Asset;
using Auctus.Model;
using Auctus.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.Business.Advisor
{
    public class AdvisorBusiness : BaseBusiness<DomainObjects.Advisor.Advisor, IAdvisorData<DomainObjects.Advisor.Advisor>>
    {
        public enum CalculationMode { AdvisorBase = 0, AdvisorDetailed = 1, AssetBase = 2, AssetDetailed = 3 }

        public AdvisorBusiness(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(serviceProvider, loggerFactory, cache, email, ip) { }

        public IEnumerable<AdvisorResponse> ListAdvisorsData()
        {
            List<AdvisorResponse> advisorsResult;
            List<AssetResponse> assetsResult;
            CalculateForAdvisorsData(CalculationMode.AdvisorBase, out advisorsResult, out assetsResult);
            return advisorsResult;
        }

        public AdvisorResponse GetAdvisorData(int advisorId)
        {
            List<AdvisorResponse> advisorsResult;
            List<AssetResponse> assetsResult;
            CalculateForAdvisorsData(CalculationMode.AdvisorDetailed, out advisorsResult, out assetsResult);
            var result = advisorsResult.Single(c => c.UserId == advisorId);
            result.Assets = assetsResult.Where(c => c.AssetAdvisor.Any(a => a.UserId == advisorId)).ToList();
            result.Assets.ForEach(a => a.AssetAdvisor = a.AssetAdvisor.Where(c => c.UserId == advisorId).ToList());
            return result;
        }

        private void CalculateForAdvisorsData(CalculationMode mode, out List<AdvisorResponse> advisorsResult, out List<AssetResponse> assetsResult)
        {
            var user = GetValidUser();
            var advisors = GetAdvisors();
            var advices = Task.Factory.StartNew(() => AdviceBusiness.List(advisors.Select(c => c.Id).Distinct()));
            var advisorFollowers = Task.Factory.StartNew(() => FollowAdvisorBusiness.ListFollowers(advisors.Select(c => c.Id).Distinct()));
            Task.WaitAll(advices, advisorFollowers);

            Calculation(mode, out advisorsResult, out assetsResult, user, advices.Result, advisors, advisorFollowers.Result, null);
        }

        public List<DomainObjects.Advisor.Advisor> GetAdvisors()
        {
            string cacheKey = "Advisors";
            var advisors = MemoryCache.Get<List<DomainObjects.Advisor.Advisor>>(cacheKey);
            if (advisors == null)
            {
                advisors = Data.ListEnabled();
                if (advisors != null && advisors.Any())
                    MemoryCache.Set(cacheKey, advisors, 240);
            }
            return advisors;
        }

        public void Calculation(CalculationMode mode, out List<AdvisorResponse> advisorsResult, out List<AssetResponse> assetsResult, User loggedUser, 
            IEnumerable<Advice> allAdvices, IEnumerable<DomainObjects.Advisor.Advisor> allAdvisors, IEnumerable<FollowAdvisor> advisorFollowers,
            IEnumerable<FollowAsset> assetFollowers)
        {
            advisorsResult = new List<AdvisorResponse>();
            assetsResult = new List<AssetResponse>();

            var assetsIds = allAdvices.Select(a => a.AssetId);
            if (assetsIds.Any())
            {
                assetsIds = assetsIds.Distinct();
                var assets = AssetBusiness.ListAssets(assetsIds);
                var minimumDate = allAdvices.Min(c => c.CreationDate).AddDays(-30);
                var assetValues = AssetValueBusiness.List(assetsIds, minimumDate);

                var adviceDetails = new List<AdviceDetail>();
                foreach (var asset in assets)
                {
                    var assetAdviceDetails = new List<AdviceDetail>();
                    var previousAdvice = new Dictionary<int, AdviceDetail>();
                    var startAdviceType = new Dictionary<int, AdviceDetail>();
                    var values = assetValues.Where(c => c.AssetId == asset.Id).OrderByDescending(c => c.Date);
                    if (values.Any())
                    {
                        var assetAdvices = allAdvices.Where(a => a.AssetId == asset.Id).OrderBy(c => c.CreationDate);
                        foreach (var advice in assetAdvices)
                        {
                            var detail = SetAdviceDetail(values, assetAdviceDetails, advice, previousAdvice.ContainsKey(advice.AdvisorId) ? previousAdvice[advice.AdvisorId] : null,
                                startAdviceType.ContainsKey(advice.AdvisorId) ? startAdviceType[advice.AdvisorId] : null);
                            if (detail != null)
                            {
                                previousAdvice[advice.AdvisorId] = detail;
                                if (detail.Advice.AdviceType == AdviceType.ClosePosition)
                                    startAdviceType[advice.AdvisorId] = null;
                                else if (!startAdviceType.ContainsKey(advice.AdvisorId) || startAdviceType[advice.AdvisorId] == null || 
                                    startAdviceType[advice.AdvisorId].Advice.Type != detail.Advice.Type)
                                    startAdviceType[advice.AdvisorId] = detail;
                            }   
                        }

                        var assetAdvisorsId = assetAdvices.Select(c => c.AdvisorId).Distinct();

                        AssetResponse assetResultData = null;
                        if (mode != CalculationMode.AdvisorBase)
                        {
                            var assFollowers = assetFollowers?.Where(c => c.AssetId == asset.Id);
                            assetResultData = new AssetResponse()
                            {
                                AssetId = asset.Id,
                                Code = asset.Code,
                                Name = asset.Name,
                                Following = assFollowers?.Any(c => c.UserId == loggedUser.Id),
                                NumberOfFollowers = assFollowers?.Count(),
                                TotalAdvisors = assetAdvisorsId.Count(),
                                TotalRatings = assetAdvices.Count(),
                                LastValue = values.First().Value,
                                Variation24h = values.First().Value / values.First(c => c.Date <= values.First().Date.AddDays(-1)).Value,
                                Variation7d = values.First().Value / values.First(c => c.Date <= values.First().Date.AddDays(-7)).Value,
                                Variation30d = values.First().Value / values.First(c => c.Date <= values.First().Date.AddDays(-30)).Value,
                                Values = mode == CalculationMode.AssetBase ? null : SwingingDoorCompression.Compress(values.ToDictionary(c => c.Date, c => c.Value))
                                    .Select(c => new AssetResponse.ValuesResponse() { Date = c.Key, Value = c.Value }).ToList()
                            };
                        }
                        foreach (var advisorId in assetAdvisorsId)
                        {
                            var currentStatus = new Advice()
                            {
                                AssetId = asset.Id,
                                CreationDate = Data.GetDateTimeNow(),
                                Type = AdviceType.ClosePosition.Value,
                                AdvisorId = advisorId
                            };
                            SetAdviceDetail(values, assetAdviceDetails, currentStatus, previousAdvice.ContainsKey(advisorId) ? previousAdvice[advisorId] : null,
                                startAdviceType.ContainsKey(advisorId) ? startAdviceType[advisorId] : null);

                            if (mode != CalculationMode.AdvisorBase)
                            {
                                var advisorDetailsValues = assetAdviceDetails.Where(c => c.Advice.AdvisorId == advisorId).OrderBy(c => c.Advice.CreationDate);
                                assetResultData.AssetAdvisor.Add(new AssetResponse.AssetAdvisorResponse()
                                {
                                    UserId = advisorId,
                                    AverageReturn = advisorDetailsValues.Where(c => c.Return.HasValue).Sum(c => c.Return.Value) / advisorDetailsValues.Count(c => c.Return.HasValue),
                                    SuccessRate = (double)advisorDetailsValues.Count(c => c.Success.HasValue && c.Success.Value) / advisorDetailsValues.Count(c => c.Success.HasValue),
                                    TotalRatings = advisorDetailsValues.Count(),
                                    LastAdviceDate = advisorDetailsValues.Last().Advice.CreationDate,
                                    LastAdviceMode = advisorDetailsValues.Last().ModeType.Value,
                                    LastAdviceType = advisorDetailsValues.Last().Advice.Type,
                                    Advices = mode == CalculationMode.AdvisorDetailed ? advisorDetailsValues.Select(c => new AssetResponse.AdviceResponse() { AdviceType = c.Advice.Type, Date = c.Advice.CreationDate }).ToList() : null
                                });
                            }
                        }

                        if (mode != CalculationMode.AdvisorBase)
                        {
                            if (mode != CalculationMode.AdvisorDetailed)
                            {
                                assetResultData.RecommendationDistribution = assetResultData.AssetAdvisor.GroupBy(c => c.LastAdviceType)
                                    .Select(g => new RecommendationDistributionResponse() { Type = g.Key, Total = g.Count() }).ToList();
                                assetResultData.Mode = GetAssetModeType(assetResultData);
                                assetResultData.Advices = mode == CalculationMode.AssetBase ? null : assetAdviceDetails
                                    .Select(c => new AssetResponse.AdviceResponse() { AdviceType = c.Advice.Type, Date = c.Advice.CreationDate }).OrderBy(c => c.Date).ToList();
                            }
                            assetsResult.Add(assetResultData);
                        }

                        adviceDetails.AddRange(assetAdviceDetails);
                    }
                }
                var advisorsData = new Dictionary<int, IEnumerable<AdviceDetail>>();
                if (mode != CalculationMode.AssetBase)
                {
                    foreach (var advisor in allAdvisors)
                    {
                        var details = adviceDetails.Where(c => c.Advice.AdvisorId == advisor.Id);
                        var assetsAdvised = details.Select(c => c.Advice.AssetId);
                        var advFollowers = advisorFollowers.Where(c => c.AdvisorId == advisor.Id);
                        advisorsResult.Add(new AdvisorResponse()
                        {
                            UserId = advisor.Id,
                            Name = advisor.Name,
                            CreationDate = advisor.BecameAdvisorDate,
                            Description = advisor.Description,
                            Owner = advisor.Id == loggedUser.Id,
                            NumberOfFollowers = advFollowers.Count(),
                            TotalAssetsAdvised = assetsAdvised.Any() ? assetsAdvised.Distinct().Count() : 0,
                            Following = advFollowers.Any(c => c.UserId == loggedUser.Id),
                            AverageReturn = details.Any(c => c.Return.HasValue) ? details.Where(c => c.Return.HasValue).Sum(c => c.Return.Value) / details.Count(c => c.Return.HasValue) : 0,
                            SuccessRate = details.Any(c => c.Success.HasValue) ? (double)details.Count(c => c.Success.HasValue && c.Success.Value) / details.Count(c => c.Success.HasValue) : 0,
                            RecommendationDistribution = !details.Any() ? new List<RecommendationDistributionResponse>() :
                                details.GroupBy(c => c.Advice.Type).Select(g => new RecommendationDistributionResponse() { Type = g.Key, Total = g.Count() }).ToList()
                        });
                        advisorsData[advisor.Id] = details;
                    }
                    SetAdvisorsRanking(advisorsResult, advisorsData);
                }
            }
        }

        private void SetAdvisorsRanking(List<AdvisorResponse> advisorsResult, Dictionary<int, IEnumerable<AdviceDetail>> advisorsData)
        {
            var advisorsConsidered = advisorsResult.Where(c => c.CreationDate < Data.GetDateTimeNow().AddDays(-3));
            if (!advisorsConsidered.Any())
                advisorsConsidered = advisorsResult;

            var newAdvisors = advisorsResult.Where(c => !advisorsConsidered.Any(a => a.UserId == c.UserId));
            var details = advisorsData.Where(c => advisorsConsidered.Any(a => a.UserId == c.Key));

            var maxAvg = advisorsConsidered.Max(c => c.AverageReturn);
            var maxSucRate = advisorsConsidered.Max(c => c.SuccessRate);
            var maxAssets = advisorsConsidered.Max(c => c.TotalAssetsAdvised);
            var lastActivity = details.Max(c => c.Value.Max(a => a.Advice.CreationDate));

            var advDays = advisorsResult.Select(c => new { Id = c.UserId, Days = Data.GetDateTimeNow().Subtract(c.CreationDate).TotalDays }).ToDictionary(c => c.Id, c => c.Days);
            var maxAdvices = details.Max(c => (double)c.Value.Count() / advDays[c.Key]);
            var maxFollowers = advisorsConsidered.Max(c => (double)c.NumberOfFollowers / advDays[c.UserId]);

            var generalNormalization = 1.2;
            advisorsResult.ForEach(c =>
            {
                var maximumValue = newAdvisors.Any(a => a.UserId == c.UserId) ? 0.7 : 1.0;
                c.Rating = Math.Min(5.0, generalNormalization * (
                      (0.35 * 5.0 * Math.Min(maximumValue, maxAvg == 0 ? 0 : c.AverageReturn / maxAvg))
                    + (0.30 * 5.0 * Math.Min(maximumValue, maxSucRate == 0 ? 0 : c.SuccessRate / maxSucRate))
                    + (0.01 * 5.0 * Math.Min(maximumValue, maxAssets == 0 ? 0 : (double)c.TotalAssetsAdvised / maxAssets))
                    + (0.15 * 5.0 * Math.Min(maximumValue, maxAdvices == 0 ? 0 : ((double)advisorsData[c.UserId].Count() / advDays[c.UserId]) / maxAdvices))
                    + (0.15 * 5.0 * Math.Min(maximumValue, maxFollowers == 0 ? 0 : ((double)c.NumberOfFollowers / advDays[c.UserId]) / maxFollowers))
                    + (0.04 * 5.0 * Math.Min(maximumValue, (double)c.CreationDate.Ticks / lastActivity.Ticks))));
            });
            advisorsResult = advisorsResult.OrderByDescending(c => c.Rating).ToList();
            for (int i = 0; i < advisorsResult.Count; ++i)
                advisorsResult[i].Ranking = i + 1;
        }

        private AdviceDetail SetAdviceDetail(IEnumerable<DomainObjects.Asset.AssetValue> values, List<AdviceDetail> assetAdviceDetails, 
            Advice advice, AdviceDetail previousAdvice, AdviceDetail startAdviceType)
        {
            var value = values.FirstOrDefault(c => c.Date <= advice.CreationDate);
            if (value != null)
            {
                if (previousAdvice != null)
                {
                    previousAdvice.Return = previousAdvice.Advice.AdviceType == AdviceType.ClosePosition ? (double?)null :
                                            (previousAdvice.Advice.AdviceType == AdviceType.Buy ? 1.0 : -1.0) * (value.Value / previousAdvice.Value - 1);

                    assetAdviceDetails.Add(previousAdvice);
                }
                if (startAdviceType != null && startAdviceType.Advice.Type != advice.Type)
                {
                    var advisorAdvices = assetAdviceDetails.Where(c => c.Advice.AdvisorId == startAdviceType.Advice.AdvisorId && startAdviceType.Advice.Id <= c.Advice.Id
                                            && startAdviceType.Advice.Type == c.Advice.Type).ToList();
                    advisorAdvices.ForEach(c =>
                    {
                        if (c.Advice.AdviceType != AdviceType.ClosePosition)
                            c.Success = startAdviceType.Advice.AdviceType == AdviceType.Buy ? value.Value >= c.Value : value.Value <= c.Value;
                    });
                }
                return new AdviceDetail()
                {
                    Advice = advice,
                    Value = value.Value,
                    ModeType = previousAdvice == null ? AdviceModeType.Initiate : 
                                previousAdvice.Advice.Type == advice.Type ? AdviceModeType.Reiterate :
                                previousAdvice.Advice.AdviceType == AdviceType.Buy ? AdviceModeType.Downgrade : AdviceModeType.Upgrade
                };
            }
            return null;
        }

        private int GetAssetModeType(AssetResponse asset)
        {
            if (asset.RecommendationDistribution.Any())
            {
                var total = asset.RecommendationDistribution.Sum(c => c.Total);
                var percentages = asset.RecommendationDistribution.Select(c => new { Type = c.Type, Percentage = 100.0 * c.Total / total });
                var majorityType = percentages.SingleOrDefault(c => c.Percentage > 50);
                if (majorityType != null)
                {
                    if (majorityType.Type == AdviceType.ClosePosition.Value)
                        return AssetModeType.Close.Value;

                    if (total < 5 || majorityType.Percentage < 70)
                        return majorityType.Type == AdviceType.Buy.Value ? AssetModeType.ModerateBuy.Value : AssetModeType.ModerateSell.Value;
                    else
                        return majorityType.Type == AdviceType.Buy.Value ? AssetModeType.StrongBuy.Value : AssetModeType.StrongSell.Value;
                }
            }
            return AssetModeType.Neutral.Value;
        }

        private class AdviceDetail
        {
            public Advice Advice{ get; set; }
            public double Value { get; set; }
            public double? Return { get; set; }
            public bool? Success { get; set; }
            public AdviceModeType ModeType { get; set; }
        }
    }
}
