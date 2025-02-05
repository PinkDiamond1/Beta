﻿using Auctus.DataAccess.Account;
using Auctus.DataAccessInterfaces.Account;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Auctus.Model;
using Auctus.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.Business.Account
{
    public class ActionBusiness : BaseBusiness<DomainObjects.Account.Action, IActionData<DomainObjects.Account.Action>>
    {
        public ActionBusiness(IConfigurationRoot configuration, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(configuration, serviceProvider, serviceScopeFactory, loggerFactory, cache, email, ip) { }

        public void InsertNewWallet(DateTime dateTime, int userId, string message, decimal? aucAmount)
        {
            InsertActionAsync(new DomainObjects.Account.Action()
            {
                CreationDate = dateTime,
                UserId = userId,
                Type = ActionType.NewWallet.Value,
                Message = message,
                AucAmount = aucAmount,
                Ip = LoggedIp
            });
        }

        public void InsertNewLogin(int userId, decimal? aucAmount, SocialNetworkType socialNetworkType)
        {
            InsertActionAsync(new DomainObjects.Account.Action()
            {
                CreationDate = Data.GetDateTimeNow(),
                UserId = userId,
                Type = ActionType.NewLogin.Value,
                AucAmount = aucAmount,
                Ip = LoggedIp,
                Message = socialNetworkType?.GetDescription()
            });
        }

        public void InsertNewAucVerification(int userId, decimal aucAmount)
        {
            InsertActionAsync(new DomainObjects.Account.Action()
            {
                CreationDate = Data.GetDateTimeNow(),
                UserId = userId,
                Type = ActionType.NewAucVerification.Value,
                AucAmount = aucAmount,
                Ip = LoggedIp
            });
        }

        public void InsertJobAucVerification(int userId, decimal aucAmount)
        {
            InsertActionAsync(new DomainObjects.Account.Action()
            {
                CreationDate = Data.GetDateTimeNow(),
                UserId = userId,
                Type = ActionType.JobVerification.Value,
                AucAmount = aucAmount,
                Ip = LoggedIp
            });
        }

        public void InsertEditAdvisor(int userId, string message)
        {
            InsertActionAsync(new DomainObjects.Account.Action()
            {
                CreationDate = Data.GetDateTimeNow(),
                UserId = userId,
                Type = ActionType.EditAdvisor.Value,
                Message = message,
                Ip = LoggedIp
            });
        }

        private void InsertActionAsync(DomainObjects.Account.Action action)
        {
            RunAsync(() => Insert(action));
        }

        public DashboardResponse GetDashboardData()
        {
            var cutDayForActivity = Data.GetDateTimeNow().AddDays(-7);
            var advisors = AdvisorBusiness.ListAllAdvisors();
            var users = Task.Factory.StartNew(() => UserBusiness.ListAllUsersData());
            var advisorFollowers = Task.Factory.StartNew(() => FollowAdvisorBusiness.ListFollowers(advisors.Select(c => c.Id).Distinct(), false));
            var assetFollowers = Task.Factory.StartNew(() => FollowAssetBusiness.ListFollowers());
            var activities = Task.Factory.StartNew(() => Data.FilterActivity(cutDayForActivity, ActionType.NewAucVerification, ActionType.NewLogin));
            Task.WaitAll(users, advisorFollowers, assetFollowers, activities);

            var adminsId = users.Result.Where(c => Admins?.Any(a => a == c.Email) == true).Select(c => c.Id).ToHashSet();
            var consideredUsers = users.Result.Where(c => !adminsId.Contains(c.Id) && (!c.ReferredId.HasValue || !adminsId.Contains(c.ReferredId.Value))).ToList();
            var consideredAdvisors = advisors.Where(c => !adminsId.Contains(c.Id) && (!c.ReferredId.HasValue || !adminsId.Contains(c.ReferredId.Value))).ToList();
            var consideredAdvisorFollowers = advisorFollowers.Result.Where(c => consideredUsers.Any(u => u.Id == c.UserId)).ToList();
            var consideredAssetFollowers = assetFollowers.Result.Where(c => consideredUsers.Any(u => u.Id == c.UserId)).ToList();
            var consideredActivities = activities.Result.Where(c => consideredUsers.Any(u => u.Id == c.UserId)).ToList();

            var result = new DashboardResponse();
            result.TotalUsersConfirmed = consideredUsers.Count(c => c.Wallets.Any());
            result.TotalUsersConfirmedFromReferral = consideredUsers.Count(c => c.ReferredId.HasValue && c.Wallets.Any());
            result.TotalUsersStartedRegistration = consideredUsers.Count(c => !consideredAdvisors.Any(a => a.Id == c.Id) && !c.Wallets.Any());
            result.TotalUsersStartedRegistrationFromReferral = consideredUsers.Count(c => c.ReferredId.HasValue && !consideredAdvisors.Any(a => a.Id == c.Id) && !c.Wallets.Any());
            result.TotalAdvisors = consideredAdvisors.Count;
            result.TotalActiveUsers = consideredActivities.Any() ? consideredActivities.Select(c => c.UserId).Distinct().Count(c => !consideredAdvisors.Any(a => a.Id == c) && consideredUsers.Any(u => u.Id == c && u.Wallets.Any())) : 0;
            result.TotalWalletsInProgress = consideredUsers.Count(c => c.ReferralStatusType == ReferralStatusType.InProgress);
            result.TotalFollowing = consideredAdvisorFollowers.Count + consideredAssetFollowers.Count;
            result.TotalAdvisorsFollowed = consideredAdvisorFollowers.Any() ? consideredAdvisorFollowers.Select(c => c.AdvisorId).Distinct().Count() : 0;
            result.TotalUsersFollowing = consideredAdvisorFollowers.Any() || consideredAssetFollowers.Any() ? 
                consideredAdvisorFollowers.Select(c => c.UserId).Concat(consideredAssetFollowers.Select(c => c.UserId)).Distinct().Count() : 0;

            var confirmedUsers = new Dictionary<int, DateTime>();
            foreach (var user in consideredUsers.Where(c => c.Wallets.Any()))
            {
                if (!consideredAdvisors.Any(c => c.Id == user.Id))
                        confirmedUsers[user.Id] = user.Wallets.OrderBy(c => c.CreationDate).First().CreationDate;

                var currentWallet = user.Wallets.OrderByDescending(c => c.CreationDate).First();
                result.TotalWalletsWithAuc += currentWallet.AUCBalance > 0 ? 1 : 0;
                result.AucHolded += Convert.ToDouble(currentWallet.AUCBalance ?? 0);
                result.AucHoldedInProgress += user.ReferralStatusType == ReferralStatusType.InProgress ? Convert.ToDouble(currentWallet.AUCBalance ?? 0) : 0;
            }
            result.AucRatioPerConfirmedUser = result.TotalUsersConfirmed > 0 ? result.AucHolded / result.TotalUsersConfirmed : 0;
            result.AucRatioPerUserInProgress = result.TotalWalletsInProgress > 0 ? result.AucHoldedInProgress / result.TotalWalletsInProgress : 0;

            var usersConfirmedData = !confirmedUsers.Any() ? null : confirmedUsers.GroupBy(c => c.Value.ToString("yyyy-MM-dd"))
                .Select(g => new DashboardResponse.RegistrationData() { Date = DateTime.Parse(g.Key), Value = g.Count() }).OrderBy(c => c.Date);

            var advisorsData = !consideredAdvisors.Any() ? null : consideredAdvisors.GroupBy(c => c.BecameAdvisorDate.ToString("yyyy-MM-dd"))
                .Select(g => new DashboardResponse.RegistrationData() { Date = DateTime.Parse(g.Key), Value = g.Count() }).OrderBy(c => c.Date);

            var usersStartedRegistrationData = !consideredUsers.Any(c => !consideredAdvisors.Any(a => a.Id == c.Id) && !c.Wallets.Any()) ? null : 
                consideredUsers.Where(c => !consideredAdvisors.Any(a => a.Id == c.Id) && !c.Wallets.Any()).GroupBy(c => c.CreationDate.ToString("yyyy-MM-dd"))
                .Select(g => new DashboardResponse.RegistrationData() { Date = DateTime.Parse(g.Key), Value = g.Count() }).OrderBy(c => c.Date);

            var minDate = GetMinDate(usersConfirmedData, advisorsData, usersStartedRegistrationData);

            result.UsersConfirmed = GetRegistrationData(usersConfirmedData, minDate);
            result.Advisors = GetRegistrationData(advisorsData, minDate);
            result.UsersStartedRegistration = GetRegistrationData(usersStartedRegistrationData, minDate);
            result.UsersConfirmedLastSitutation = GetFlagData(result.UsersConfirmed);
            result.AdvisorsLastSitutation = GetFlagData(result.Advisors);
            result.UsersStartedRegistrationLastSitutation = GetFlagData(result.UsersStartedRegistration);

            result.UsersConfirmed.Add(new DashboardResponse.RegistrationData() { Date = Data.GetDateTimeNow().Date.AddDays(1), Value = result.UsersConfirmed.LastOrDefault()?.Value ?? 0 });
            result.Advisors.Add(new DashboardResponse.RegistrationData() { Date = Data.GetDateTimeNow().Date.AddDays(1), Value = result.Advisors.LastOrDefault()?.Value ?? 0 });
            result.UsersStartedRegistration.Add(new DashboardResponse.RegistrationData() { Date = Data.GetDateTimeNow().Date.AddDays(1), Value = result.UsersStartedRegistration.LastOrDefault()?.Value ?? 0 });
            
            result.Following.Add(new DashboardResponse.DistributionData() { Name = "Asset", Amount = consideredAssetFollowers.Count() });
            result.Following.Add(new DashboardResponse.DistributionData() { Name = "Expert", Amount = consideredAdvisorFollowers.Count() });

            var usersWithReferral = consideredUsers.Where(c => c.ReferralStatus.HasValue);
            result.ReferralStatus = !usersWithReferral.Any() ? new List<DashboardResponse.DistributionData>() : usersWithReferral.GroupBy(c => c.ReferralStatus.Value)
                .Select(g => new DashboardResponse.DistributionData() { Name = ReferralStatusType.Get(g.Key).GetDescription(), Amount = g.Count() }).ToList();

            var groupedFollowers = !consideredAdvisorFollowers.Any() ? null : consideredAdvisorFollowers.GroupBy(c => c.AdvisorId).Select(g => new { Id = g.Key, Value = g.Count() }).OrderByDescending(c => c.Value);
            if (groupedFollowers?.Any() == true)
            {
                groupedFollowers = groupedFollowers.Take(groupedFollowers.Count() > 10 ? 10 : groupedFollowers.Count()).OrderByDescending(c => c.Value);
                var consideredFollowers = consideredAdvisorFollowers.Where(c => groupedFollowers.Any(a => a.Id == c.AdvisorId));
                result.AdvisorFollowers = groupedFollowers.Select(c => new DashboardResponse.AdvisorData()
                {
                    Id = c.Id,
                    Name = consideredAdvisors.First(a => a.Id == c.Id).Name,
                    UrlGuid = consideredAdvisors.First(a => a.Id == c.Id).UrlGuid.ToString(),
                    Total = c.Value,
                    SubValue1 = consideredFollowers.Count(a => a.CreationDate >= cutDayForActivity && a.AdvisorId == c.Id)
                }).ToList();
            }

            var groupedReferred = !usersWithReferral.Any() ? null : usersWithReferral.GroupBy(c => c.ReferredId.Value).Select(g => new { Id = g.Key, Value = g.Count() }).OrderByDescending(c => c.Value);
            if (groupedReferred?.Any() == true)
            {
                groupedReferred = groupedReferred.Take(groupedReferred.Count() > 10 ? 10 : groupedReferred.Count()).OrderByDescending(c => c.Value);
                var consideredReferred= consideredUsers.Where(c => c.ReferralStatus.HasValue).Where(c => groupedReferred.Any(a => a.Id == c.ReferredId));
                result.AdvisorReferral = groupedReferred.Select(c => new DashboardResponse.AdvisorData()
                {
                    Id = c.Id,
                    Name = consideredAdvisors.Any(a => a.Id == c.Id) ? consideredAdvisors.First(a => a.Id == c.Id).Name : 
                        consideredUsers.Any(u => u.Id == c.Id) ? consideredUsers.First(u => u.Id == c.Id).Email : "(ADMIN) " + users.Result.First(u => u.Id == c.Id).Email,
                    UrlGuid = consideredAdvisors.Any(a => a.Id == c.Id) ? consideredAdvisors.First(a => a.Id == c.Id).UrlGuid.ToString() : null,
                    Total = c.Value,
                    SubValue1 = consideredReferred.Count(a => a.ReferredId == c.Id && a.ReferralStatusType == ReferralStatusType.InProgress),
                    SubValue2 = consideredReferred.Count(a => a.ReferredId == c.Id && a.ReferralStatusType == ReferralStatusType.Interrupted),
                    SubValue3 = consideredReferred.Count(a => a.ReferredId == c.Id && (a.ReferralStatusType == ReferralStatusType.Finished || a.ReferralStatusType == ReferralStatusType.Paid))
                }).ToList();
            }

            return result;
        }

        private List<DashboardResponse.RegistrationData> GetRegistrationData(IEnumerable<DashboardResponse.RegistrationData> baseData, DateTime? minDate)
        {
            var result = new List<DashboardResponse.RegistrationData>();
            if (minDate.HasValue)
            {
                var today = Data.GetDateTimeNow().Date;
                for (var date = minDate.Value; date <= today; date = date.AddDays(1))
                {
                    var data = baseData?.FirstOrDefault(c => c.Date == date);
                    if (data == null)
                        result.Add(new DashboardResponse.RegistrationData() { Date = date, Value = 0 });
                    else
                        result.Add(data);
                }
            }
            return result;
        }

        private DateTime? GetMinDate(params IEnumerable<DashboardResponse.RegistrationData>[] registrationDatas)
        {
            DateTime? result = null;
            if (registrationDatas != null)
            {
                foreach(var data in registrationDatas)
                {
                    if (data != null)
                    {
                        var minDate = data.Min(c => c.Date);
                        if (!result.HasValue || result.Value > minDate)
                            result = minDate;
                    }
                }
            }
            return result;
        }

        private DashboardResponse.FlagData GetFlagData(List<DashboardResponse.RegistrationData> registrationDatas)
        {
            if (!registrationDatas.Any())
                return null;
            else
            {
                var last = registrationDatas.Last();
                return new DashboardResponse.FlagData()
                {
                    Date = last.Date,
                    Description = registrationDatas.Count == 1 ? GetFlagDescription(last.Value) : GetFlagDescription(last.Value - registrationDatas.ElementAt(registrationDatas.Count - 2).Value)
                };
            }
        }

        private string GetFlagDescription(int value)
        {
            return value == 0 ? "zero" : value > 0 ? $"+{value}" : $"{value}";
        }
    }
}
