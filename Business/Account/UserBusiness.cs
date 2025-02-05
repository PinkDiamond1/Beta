﻿using Auctus.DataAccessInterfaces.Account;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Auctus.DomainObjects.Asset;
using Auctus.DomainObjects.Event;
using Auctus.Model;
using Auctus.Util;
using Auctus.Util.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Auctus.Business.Advisor.AdvisorBusiness;

namespace Auctus.Business.Account
{
    public class UserBusiness : BaseBusiness<User, IUserData<User>>
    {
        public UserBusiness(IConfigurationRoot configuration, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(configuration, serviceProvider, serviceScopeFactory, loggerFactory, cache, email, ip) { }

        public User GetByEmail(string email)
        {
            return Data.GetByEmail(email);
        }

        public User GetById(int id)
        {
            return Data.GetById(id);
        }

        public User GetForLoginById(int id)
        {
            return Data.GetForLoginById(id);
        }

        public LoginResponse SocialLogin(SocialNetworkType socialNetworkType, string email, string accessToken, bool requestedToBeAdvisor)
        {
            BaseEmailValidation(email);
            EmailValidation(email);
            SocialUser socialUser = GetSocialUser(socialNetworkType, accessToken);
            ValidateSocialUser(socialUser, email);

            var user = GetForLoginByEmail(email);
            if (user != null)
            {
                return SocialLogin(user, socialNetworkType);
            }
            else
            {
                return SocialRegister(email, requestedToBeAdvisor, socialNetworkType);
            }
        }

        private LoginResponse SocialRegister(string email, bool requestedToBeAdvisor, SocialNetworkType socialNetworkType)
        {
            var user = SetNewUser(email, null, null, true);
            Data.Insert(user);
            
            ActionBusiness.InsertNewLogin(user.Id, null, socialNetworkType);
            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                HasInvestment = false,
                PendingConfirmation = false,
                IsAdvisor = false,
                RequestedToBeAdvisor = requestedToBeAdvisor
            };            
        }

        private LoginResponse SocialLogin(User user, SocialNetworkType socialNetworkType)
        {
            if (!user.ConfirmationDate.HasValue)
            {
                user.ConfirmationDate = Data.GetDateTimeNow();
                Data.Update(user);
            }
            bool hasInvestment = GetUserHasInvestment(user);
            ActionBusiness.InsertNewLogin(user.Id, null, socialNetworkType);

            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                PendingConfirmation = !user.ConfirmationDate.HasValue,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                HasInvestment = hasInvestment
            };
        }

        private static void ValidateSocialUser(SocialUser socialUser, string email)
        {
            if (socialUser == null || socialUser.Email == null || email == null || socialUser.Email.ToLower() != email.ToLower())
            {
                throw new BusinessException("Invalid parameters");
            }
        }

        private SocialUser GetSocialUser(SocialNetworkType socialNetworkType, string accessToken)
        {
            SocialUser socialUser = null;
            if (socialNetworkType == SocialNetworkType.Facebook)
            {
                socialUser = FacebookBusiness.GetSocialUser(accessToken);
            }
            else if (socialNetworkType == SocialNetworkType.Google)
            {
                socialUser = GoogleBusiness.GetSocialUser(accessToken);
            }
            return socialUser;
        }

        public User GetForLoginByEmail(string email)
        {
            return Data.GetForLogin(email);
        }

        public LoginResponse Login(string email, string password)
        {
            BaseEmailValidation(email);
            EmailValidation(email);
            BasePasswordValidation(password);

            var user = GetForLoginByEmail(email);
            if (user == null || user.Password != GetHashedPassword(password, user.Email, user.CreationDate))
                throw new BusinessException("Invalid credentials.");

            bool hasInvestment = GetUserHasInvestment(user);
            ActionBusiness.InsertNewLogin(user.Id, null, null);
            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                PendingConfirmation = !user.ConfirmationDate.HasValue,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                HasInvestment = hasInvestment
            };
        }

        public LoginResponse GetLoginResponse()
        {
            var user = GetForLoginByEmail(LoggedEmail);
            if (user == null)
                throw new NotFoundException("User not found.");
            
            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                PendingConfirmation = !user.ConfirmationDate.HasValue,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                HasInvestment = GetUserHasInvestment(user)
            };
        }

        public bool GetUserHasInvestment(User user)
        {   
            return OrderBusiness.GetUserHasAnyOrder(user.Id);
        }

        public bool IsValidAdvisor(User user)
        {
            return user.IsAdvisor && ((DomainObjects.Advisor.Advisor)user).Enabled;
        }

        public string GetAdvisorName(User user)
        {
            return IsValidAdvisor(user) ? ((DomainObjects.Advisor.Advisor)user).Name : null;
        }

        public string GetProfileUrlGuid(User user)
        {
            return IsValidAdvisor(user) ? ((DomainObjects.Advisor.Advisor)user).UrlGuid.ToString() : null;
        }

        public decimal GetMinimumAucAmountForUser(User user)
        {
            return Convert.ToDecimal(user.ReferredId.HasValue && user.BonusToReferred.HasValue ? user.BonusToReferred.Value: 0);
        }

        public async Task<LoginResponse> RegisterAsync(string email, string password, string referralCode)
        {
            var user = GetValidUserToRegister(email, password, referralCode);
            Data.Insert(user);

            await SendEmailConfirmationAsync(user.Email, user.ConfirmationCode);

            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                HasInvestment = false,
                PendingConfirmation = true,
                IsAdvisor = false,
                RequestedToBeAdvisor = false
            };
        }

        public User GetValidUserToRegister(string email, string password, string referralCode)
        {
            BaseEmailValidation(email);
            EmailValidation(email);
            BasePasswordValidation(password);
            PasswordValidation(password);

            var user = GetByEmail(email);
            if (user != null)
                throw new BusinessException("Email already registered.");

            var referredUser = GetReferredUser(referralCode);

            return SetNewUser(email, password, referredUser, false);
        }

        private User SetNewUser(string email, string password, User referredUser, bool emailConfirmed)
        {
            User user = new User();
            user.Email = email.ToLower().Trim();
            user.CreationDate = Data.GetDateTimeNow();
            user.ConfirmationCode = Guid.NewGuid().ToString();
            user.Password = String.IsNullOrWhiteSpace(password) ? null : GetHashedPassword(password, user.Email, user.CreationDate);
            user.ReferralCode = GenerateReferralCode();
            user.ReferredId = referredUser?.Id;
            user.BonusToReferred = GetBonusToReferredUser(referredUser);
            user.ReferralStatus = referredUser != null ? ReferralStatusType.InProgress.Value : (int?)null;
            user.AllowNotifications = true;
            user.ConfirmationDate = emailConfirmed ? user.CreationDate : (DateTime?)null;
            user.DiscountProvided = DiscountPercentageOnAuc;
            return user;
        }

        public SetReferralCodeResponse SetReferralCode(string referralCode)
        {
            var user = Data.GetSimpleWithWallet(LoggedEmail);
            if (user == null)
                throw new NotFoundException("User cannot be found.");
            if (user.Wallet != null)
                throw new BusinessException("User with wallet already registered.");

            var response = new SetReferralCodeResponse();
            response.StandardAUCAmount = MinimumAucLogin;
            var referredUser = GetReferredUser(referralCode, false);
            if (referredUser == null)
            {
                user.ReferredId = null;
                user.BonusToReferred = null;
                Data.Update(user);
                response.Valid = false;
                response.AUCRequired = MinimumAucLogin;
                response.Discount = 0;
            }
            else if (referredUser.Id == user.Id)
                throw new BusinessException("User cannot referral yourself.");
            else
            {
                user.ReferredId = referredUser.Id;
                user.BonusToReferred = GetBonusToReferredUser(referredUser);
                Data.Update(user);
                response.Valid = true;
                response.Discount = referredUser.DiscountProvided;
                response.AUCRequired = GetMinimumAucAmountForUser(user);
            }
            return response;
        }

        public double? GetBonusToReferredUser(User referredUser)
        {
            return referredUser != null ? (MinimumAucLogin * referredUser.DiscountProvided / 100.0) : (double?)null;
        }

        private string GenerateReferralCode()
        {
            User user;
            String referralCode;
            do
            {
                referralCode = Util.Util.GetRandomString(7);
                user = Data.GetByReferralCode(referralCode.ToUpper());
            } while (user != null);

            return referralCode;
        }

        public User GetReferredUser(string referralCode, bool throwException = true)
        {
            if (!string.IsNullOrWhiteSpace(referralCode) && referralCode.Length == 7)
            {
                var user = Data.GetByReferralCode(referralCode.ToUpper());
                if (user == null && throwException)
                    throw new BusinessException("Invalid invitation code");

                return user;
            }
            return null;
        }

        private string GetHashedPassword(string password, string email, DateTime creationDate)
        {
            return Security.Hash(password, $"{email}{(new DateTime(creationDate.Ticks - (creationDate.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc)).Ticks}{HashSecret}");
        }

        public async Task<LoginResponse> ResendEmailConfirmationAsync()
        {
            var email = LoggedEmail;
            BaseEmailValidation(email);
            EmailValidation(email);

            var user = GetForLoginByEmail(email);
            if (user == null)
                throw new NotFoundException("User cannot be found.");

            if (!user.ConfirmationDate.HasValue)
                await SendEmailConfirmationAsync(email, user.ConfirmationCode);
            
            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                PendingConfirmation = !user.ConfirmationDate.HasValue,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                HasInvestment = GetUserHasInvestment(user)
            };
        }

        public LoginResponse ConfirmEmail(string code)
        {
            var user = Data.GetByConfirmationCode(code);
            if (user == null)
                throw new BusinessException("Invalid confirmation code.");
            else if (user.ConfirmationDate.HasValue)
                throw new BusinessException("Email already confirmed.");

            user.ConfirmationDate = Data.GetDateTimeNow();
            Data.Update(user);
        
            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                PendingConfirmation = false,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                HasInvestment = GetUserHasInvestment(user)
            };
        }

        public LoginResponse ValidateSignature(string address, string signature)
        {
            BaseEmailValidation(LoggedEmail);
            var user = Data.GetForNewWallet(LoggedEmail);
            if (user == null)
                throw new NotFoundException("User cannot be found.");
            if (string.IsNullOrWhiteSpace(signature))
                throw new BusinessException("Signature cannot be empty.");

            address = WalletBusiness.GetAddressFormatted(address);

            var wallet = WalletBusiness.GetByAddress(address);
            if (wallet != null)
            {
                if (wallet.UserId == user.Id)
                    throw new BusinessException("The wallet is already linked to your account.");
                else
                    throw new BusinessException("The wallet is already on used.");
            }

            var message = $"I accept the Privacy Policy and Terms of Use.";
            var recoveryAddress = Signature.HashAndEcRecover(message, signature)?.ToLower();
            if (address != recoveryAddress)
                throw new BusinessException("Invalid signature.");

            decimal? aucAmount = null;
            if (!IsValidAdvisor(user))
            {
                aucAmount = WalletBusiness.GetAucAmount(address);
                WalletBusiness.ValidateAucAmount(aucAmount.Value, GetMinimumAucAmountForUser(user));
            }

            var creationDate = Data.GetDateTimeNow();
            using (var transaction = TransactionalDapperCommand)
            {
                transaction.Insert(WalletBusiness.CreateNew(creationDate, user.Id, address, aucAmount));
                if (user.ReferredId.HasValue)
                {
                    user.ReferralStatus = ReferralStatusType.InProgress.Value;
                    transaction.Update(user);
                }
                transaction.Commit();
            }
            ActionBusiness.InsertNewWallet(creationDate, user.Id, $"Message: {message} --- Signature: {signature}", aucAmount ?? null);

            return new LoginResponse()
            {
                Id = user.Id,
                Email = user.Email,
                HasInvestment = false,
                IsAdvisor = IsValidAdvisor(user),
                AdvisorName = UserBusiness.GetAdvisorName(user),
                ProfileUrlGuid = UserBusiness.GetProfileUrlGuid(user),
                PendingConfirmation = !user.ConfirmationDate.HasValue
            };
        }

        public void ChangePassword(string currentPassword, string newPassword)
        {
            var user = GetValidUser();
            if (user.Password != GetHashedPassword(currentPassword, user.Email, user.CreationDate))
                throw new BusinessException("Current password is incorrect.");

            UpdatePassword(user, newPassword);
        }

        public void SetUsersAucSituation()
        {
            var users = Data.ListForAucSituation(Admins.Where(c => !string.IsNullOrEmpty(c)));
            foreach (var user in users)
            {
                var start = user.Wallets.OrderBy(c => c.CreationDate).First().CreationDate;
                var currentWallet = user.Wallets.OrderByDescending(c => c.CreationDate).First();
                currentWallet.AUCBalance = WalletBusiness.GetAucAmount(currentWallet.Address);
                ActionBusiness.InsertJobAucVerification(user.Id, currentWallet.AUCBalance.Value);
                using (var transaction = TransactionalDapperCommand)
                {
                    transaction.Update(currentWallet);
                    if (currentWallet.AUCBalance < GetMinimumAucAmountForUser(user))
                    {
                        user.ReferralStatus = ReferralStatusType.Interrupted.Value;
                        transaction.Update(user);
                    }
                    else if (Data.GetDateTimeNow().Subtract(start).TotalDays >= MinimumDaysToKeepAuc)
                    {
                        user.ReferralStatus = ReferralStatusType.Finished.Value;
                        transaction.Update(user);
                    }
                    transaction.Commit();
                }
            }
        }

        public void UpdatePassword(User user, string password)
        {
            BasePasswordValidation(password);
            PasswordValidation(password);

            user.Password = GetHashedPassword(password, user.Email, user.CreationDate);
            if (!user.ConfirmationDate.HasValue)
                user.ConfirmationDate = Data.GetDateTimeNow();
            Update(user);
        }

        public new void Update(User user)
        {
            Data.Update(user);

            Parallel.Invoke(() => user.FollowedAdvisors = FollowAdvisorBusiness.ListAdvisorsFollowed(user.Id),
                            () => user.FollowingUsers = FollowAdvisorBusiness.ListFollowers(new int[] { user.Id }, true).Select(c => c.User.Email).ToList(),
                            () => user.FollowedAssets = FollowAssetBusiness.ListAssetsFollowed(user.Id));

            SetUserToCache(user);
        }

        public List<User> ListAllUsersData()
        {
            return Data.ListAllUsersData();
        }

        public async Task SendEmailConfirmationAsync(string email, string code)
        {
            await EmailBusiness.SendUsingTemplateAsync(new string[] { email }, "Verify your email address - Auctus Trading",
                string.Format(@"<p>To confirm your Auctus Trading account please <a href='{0}?confirmemail=true&c={1}' target='_blank'>click here</a>.</p>
                        <p style=""font-size: 12px; font-style: italic;"">If you didn’t ask to verify this address, you can ignore this email.</p>", WebUrl, code),
                EmailTemplate.NotificationType.ConfirmEmail);
        }

        public static void BaseEmailValidation(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new BusinessException("Email must be filled.");
        }

        public void EmailValidation(string email)
        {
            if (!EmailBusiness.IsValidEmail(email))
                throw new BusinessException("Email informed is invalid.");
        }

        private void BasePasswordValidation(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new BusinessException("Password must be filled.");
        }

        private void PasswordValidation(string password)
        {
            if (password.Length < 8)
                throw new BusinessException("Password must be at least 8 characters.");
            if (password.Length > 100)
                throw new BusinessException("Password cannot have more than 100 characters.");
            if (password.Contains(" "))
                throw new BusinessException("Password cannot have spaces.");
        }

        public FollowAdvisor FollowUnfollowAdvisor(int advisorId, FollowActionType followActionType)
        {
            var user = GetValidUser();
            var followAdvisor = FollowAdvisorBusiness.Create(user.Id, advisorId, followActionType);
            if (user.FollowedAdvisors != null)
            {
                if (followActionType == FollowActionType.Follow)
                    user.FollowedAdvisors.Add(advisorId);
                else if(user.FollowedAdvisors.Contains(advisorId))
                    user.FollowedAdvisors.Remove(advisorId);

                SetUserToCache(user);
            }
            if (user.FollowingUsers != null)
            {
                var advisorCache = GetUserFromCache(GetById(advisorId));
                if (advisorCache != null)
                {
                    if (followActionType == FollowActionType.Follow)
                        advisorCache.FollowingUsers.Add(user.Email);
                    else if (advisorCache.FollowingUsers.Contains(user.Email))
                        advisorCache.FollowingUsers.Remove(user.Email);

                    SetUserToCache(advisorCache);
                }
            }
            return followAdvisor;
        }

        public FollowAsset FollowUnfollowAsset(int assetId, FollowActionType followActionType)
        {
            var user = GetValidUser();

            var asset = AssetBusiness.GetById(assetId);
            if (!asset.Enabled)
                throw new BusinessException("Invalid asset.");

            var followAsset = FollowAssetBusiness.Create(user.Id, assetId, followActionType);
            if (user.FollowedAssets != null)
            {
                if (followActionType == FollowActionType.Follow)
                    user.FollowedAssets.Add(assetId);
                else
                    user.FollowedAssets.Remove(assetId);

                SetUserToCache(user);
            }
            return followAsset;
        }

        public ReferralProgramInfoResponse GetReferralProgramInfo()
        {
            var user = GetValidUser();
            var referredUsers = Data.ListReferredUsers(user.Id);
            ReferralProgramInfoResponse response = ConvertReferredUsersToReferralProgramInfo(referredUsers);
            response.ReferralCode = user.ReferralCode;
            response.BonusToReferred = MinimumAucLogin * user.DiscountProvided / 100.0;
            return response;
        }

        public WalletLoginInfoResponse GetWalletLoginInfo()
        {
            var user = Data.GetForWalletLogin(LoggedEmail);
            if (user == null)
                throw new NotFoundException("User cannot be found.");

            return new WalletLoginInfoResponse()
            {
                ReferralCode = user.ReferredUser?.ReferralCode,
                StandardAUCAmount = MinimumAucLogin,
                Discount = (user.BonusToReferred ?? 0) / MinimumAucLogin * 100.0,
                RegisteredWallet = user.Wallet?.Address,
                AUCRequired = GetMinimumAucAmountForUser(user)
            };
        }

        public ValidateReferralCodeResponse IsValidReferralCode(string referralCode)
        {
            var loggedUser = GetLoggedUser(); 
            var user = Data.GetByReferralCode(referralCode);
            var valid = user != null && (loggedUser == null || loggedUser.Id != user.Id);
            return new ValidateReferralCodeResponse()
            {
                Valid = valid,
                Discount = valid ? user.DiscountProvided : 0,
                BonusAmount = valid ? MinimumAucLogin * user.DiscountProvided / 100.0 : 0
            };
        }

        public void SetConfiguration(bool allowNotifications)
        {
            var user = GetValidUser();
            user.AllowNotifications = allowNotifications;
            Update(user);
        }

        public UserConfigurationResponse GetConfiguration()
        {
            var user = GetValidUser();
            var wallet = WalletBusiness.GetByUser(user.Id);
            return new UserConfigurationResponse()
            {
                AllowNotifications = user.AllowNotifications,
                Wallet = wallet?.Address
            };
        }

        private ReferralProgramInfoResponse ConvertReferredUsersToReferralProgramInfo(List<User> referredUsers)
        {
            var response = new ReferralProgramInfoResponse();
            foreach (var group in referredUsers.GroupBy(c => c.ReferralStatus))
            {
                if (group.Key == ReferralStatusType.InProgress.Value)
                    response.Pending = GetReferralStatusValue(group);
                else if (group.Key == ReferralStatusType.Interrupted.Value)
                    response.Canceled = GetReferralStatusValue(group);
                else if (group.Key == ReferralStatusType.Finished.Value)
                    response.Available = GetReferralStatusValue(group);
                else if (group.Key == ReferralStatusType.Paid.Value)
                    response.CashedOut = GetReferralStatusValue(group);
            }
            return response;
        }

        private double GetReferralStatusValue(IEnumerable<User> groupedData)
        {
            return Math.Floor(groupedData.Where(c => c.BonusToReferred.HasValue).Sum(c => c.BonusToReferred.Value));
        }

        public IEnumerable<User> ListValidUsersFollowingAdvisorOrAsset(int advisorId, int assetId)
        {
            return Data.ListUsersFollowingAdvisorOrAsset(advisorId, assetId).Where(c => c.Id != advisorId);
        }

        public SearchResponse Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
                return new SearchResponse();

            IEnumerable<DomainObjects.Advisor.Advisor> advisors = null;
            IEnumerable<DomainObjects.Asset.Asset> assets = null;
            Parallel.Invoke(() => advisors = AdvisorBusiness.ListByName(searchTerm), () => assets = AssetBusiness.ListByNameOrCode(searchTerm));

            if (!assets.Any() && !advisors.Any())
                return new SearchResponse();

            var response = new SearchResponse();
            foreach(DomainObjects.Advisor.Advisor advisor in advisors)
            {
                response.Advisors.Add(new SearchResponse.AdvisorResult()
                {
                    Id = advisor.Id,
                    Description = advisor.Description,
                    Name = advisor.Name,
                    UrlGuid = advisor.UrlGuid.ToString()
                });
            }
            foreach (DomainObjects.Asset.Asset asset in assets)
            {
                response.Assets.Add(new SearchResponse.AssetResult()
                {
                    Id = asset.Id,
                    Code = asset.Code,
                    Name = asset.Name,
                    MarketCap = asset.MarketCap,
                    CirculatingSupply = asset.CirculatingSupply
                });
            }
            response.Advisors = response.Advisors.OrderBy(c => c.Name).ToList();
            response.Assets = response.Assets.OrderByDescending(c => c.MarketCap).ThenBy(c => c.Name).ToList();
            return response;
        }

        public void ClearUserCache(string email)
        {
            MemoryCache.Set<User>(GetUserCacheKey(email), null);
        }
    }
}
