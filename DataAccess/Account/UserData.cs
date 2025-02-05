﻿using Auctus.DataAccess.Core;
using Auctus.DataAccessInterfaces.Account;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Auctus.DataAccess.Account
{
    public class UserData : BaseSql<User>, IUserData<User>
    {
        public override string TableName => "User";
        public UserData(IConfigurationRoot configuration) : base(configuration) { }

        private const string SQL_FOR_LOGIN = @"SELECT u.*, a.*, w.* 
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                LEFT JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                LEFT JOIN [Wallet] w WITH(NOLOCK) ON w.UserId = u.Id AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                WHERE 
                                                u.Email = @Email";

        private const string SQL_FOR_LOGIN_BY_ID = @"SELECT u.*, a.*, w.* 
                                                    FROM 
                                                    [User] u WITH(NOLOCK)
                                                    LEFT JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                    LEFT JOIN [Wallet] w WITH(NOLOCK) ON w.UserId = u.Id AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                    WHERE 
                                                    u.Id = @Id";

        private const string SQL_FOR_CONFIRMATION = @"SELECT u.*, a.*, w.* 
                                                    FROM 
                                                    [User] u WITH(NOLOCK)
                                                    INNER JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                    LEFT JOIN [Wallet] w WITH(NOLOCK) ON w.UserId = u.Id AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                    WHERE 
                                                    u.ConfirmationCode = @Code";

        private const string SQL_FOR_NEW_WALLET = @"SELECT u.*, a.*
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                INNER JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                WHERE 
                                                u.Email = @Email";

        private const string SQL_BY_EMAIL = @"SELECT u.*, a.*
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                LEFT JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                WHERE 
                                                u.Email = @Email";

        private const string SQL_BY_ID = @"SELECT u.*, a.*
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                INNER JOIN [Advisor] a WITH(NOLOCK) ON a.Id = u.Id
                                                WHERE 
                                                u.Id = @Id";

        private const string SQL_FOR_AUC_SITUATION = @"SELECT u.*, w.*
                                                    FROM 
                                                    [User] u WITH(NOLOCK)
                                                    INNER JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId
                                                    WHERE 
                                                    u.ReferralStatus = @ReferralStatus {0}";

        private const string SQL_FOR_ALL_USER_DATA = @"SELECT u.*, w.*
                                                       FROM 
                                                       [User] u WITH(NOLOCK)
                                                       LEFT JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId";

        private const string SQL_FOR_WALLET_LOGIN = @"SELECT u.*, w.*, u2.*
                                                       FROM 
                                                       [User] u WITH(NOLOCK)
                                                       LEFT JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                       LEFT JOIN [User] u2 WITH(NOLOCK) ON u2.Id = u.ReferredId
                                                       WHERE u.Email = @Email";

        private const string SQL_SIMPLE_WITH_WALLET = @"SELECT u.*, w.*
                                                        FROM 
                                                        [User] u WITH(NOLOCK)
                                                        LEFT JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                        WHERE u.Email = @Email";

        private const string SQL_FOLLOWING = @"SELECT u.*, w.*
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                INNER JOIN [Follow] f WITH(NOLOCK) ON f.UserId = u.Id 
                                                INNER JOIN [FollowAdvisor] fa WITH(NOLOCK) ON fa.Id = f.Id 
                                                LEFT JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                WHERE 
                                                u.ConfirmationDate IS NOT NULL AND f.ActionType = @ActionType AND fa.AdvisorId = @AdvisorId AND u.AllowNotifications = @AllowNotifications AND
                                                f.CreationDate = (SELECT MAX(f2.CreationDate) FROM 
                                                                    [Follow] f2 WITH(NOLOCK) 
                                                                    INNER JOIN [FollowAdvisor] fa2 WITH(NOLOCK) ON fa2.Id = f2.Id 
                                                                    WHERE f2.UserId = u.Id AND fa2.AdvisorId = @AdvisorId)
                                                UNION 
                                                SELECT u.*, w.*
                                                FROM 
                                                [User] u WITH(NOLOCK)
                                                INNER JOIN [Follow] f WITH(NOLOCK) ON f.UserId = u.Id 
                                                INNER JOIN [FollowAsset] fa WITH(NOLOCK) ON fa.Id = f.Id
                                                LEFT JOIN [Wallet] w WITH(NOLOCK) ON u.Id = w.UserId AND w.CreationDate = (SELECT MAX(w2.CreationDate) FROM [Wallet] w2 WITH(NOLOCK) WHERE w2.UserId = u.Id)
                                                WHERE 
                                                u.ConfirmationDate IS NOT NULL AND f.ActionType = @ActionType AND fa.AssetId = @AssetId AND u.AllowNotifications = @AllowNotifications AND
                                                f.CreationDate = (SELECT MAX(f2.CreationDate) FROM 
                                                                    [Follow] f2 WITH(NOLOCK) 
                                                                    INNER JOIN [FollowAsset] fa2 WITH(NOLOCK) ON fa2.Id = f2.Id 
                                                                    WHERE f2.UserId = u.Id AND fa2.AssetId = @AssetId)";

        public User GetForLogin(string email)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Email", email.ToLower().Trim(), DbType.AnsiString);
            return Query<User, DomainObjects.Advisor.Advisor, Wallet, User>(SQL_FOR_LOGIN,
                        (user, advisor, wallet) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                advisor.Wallet = wallet;
                                return advisor;
                            }
                            else
                            {
                                user.Wallet = wallet;
                                return user;
                            }
                        }, "Id,Id,Id", parameters).SingleOrDefault();
        }

        public User GetByConfirmationCode(string code)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Code", code, DbType.AnsiString);
            return Query<User, DomainObjects.Advisor.Advisor, Wallet, User>(SQL_FOR_CONFIRMATION,
                        (user, advisor, wallet) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                advisor.Wallet = wallet;
                                return advisor;
                            }
                            else
                            {
                                user.Wallet = wallet;
                                return user;
                            }
                        }, "Id,Id,Id", parameters).SingleOrDefault();
        }

        public User GetForNewWallet(string email)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Email", email.ToLower().Trim(), DbType.AnsiString);
            return Query<User, DomainObjects.Advisor.Advisor, User>(SQL_FOR_NEW_WALLET,
                        (user, advisor) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                return advisor;
                            }
                            else
                            {
                                return user;
                            }
                        }, "Id,Id", parameters).SingleOrDefault();
        }

        public User GetForWalletLogin(string email)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Email", email, DbType.AnsiString);
            return Query<User, Wallet, User, User>(SQL_FOR_WALLET_LOGIN,
                        (user, wallet, referred) =>
                        {
                            user.Wallet = wallet;
                            user.ReferredUser = referred;
                            return user;
                        }, "Id,Id", parameters).SingleOrDefault();
        }

        public User GetSimpleWithWallet(string email)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Email", email, DbType.AnsiString);
            return Query<User, Wallet, User>(SQL_SIMPLE_WITH_WALLET,
                        (user, wallet) =>
                        {
                            user.Wallet = wallet;
                            return user;
                        }, "Id", parameters).SingleOrDefault();
        }

        public User GetByEmail(string email)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Email", email.ToLower().Trim(), DbType.AnsiString);
            return Query<User, DomainObjects.Advisor.Advisor, User>(SQL_BY_EMAIL,
                        (user, advisor) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                return advisor;
                            }
                            else
                                return user;
                        }, "Id", parameters).SingleOrDefault();
        }

        public User GetById(int id)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);
            return Query<User, DomainObjects.Advisor.Advisor, User>(SQL_BY_ID,
                        (user, advisor) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                return advisor;
                            }
                            else
                                return user;
                        }, "Id", parameters).SingleOrDefault();
        }

        public User GetForLoginById(int id)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("Id", id, DbType.Int32);
            return Query<User, DomainObjects.Advisor.Advisor, Wallet, User>(SQL_FOR_LOGIN_BY_ID,
                        (user, advisor, wallet) =>
                        {
                            if (advisor != null)
                            {
                                FillAdvisorWithUserData(ref advisor, user);
                                advisor.Wallet = wallet;
                                return advisor;
                            }
                            else
                            {
                                user.Wallet = wallet;
                                return user;
                            }
                        }, "Id,Id,Id", parameters).SingleOrDefault();
        }

        public List<User> ListForAucSituation(IEnumerable<string> ignoredEmails)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("ReferralStatus", ReferralStatusType.InProgress.Value, DbType.Int32);
            var complement = "";
            if (ignoredEmails?.Any() == true)
            {
                complement = $" AND {string.Join(" AND ", ignoredEmails.Select((c, i) => $"u.Email <> @Email{i}"))}";
                for (int i = 0; i < ignoredEmails.Count(); ++i)
                    parameters.Add($"Email{i}", ignoredEmails.ElementAt(i), DbType.AnsiString);
            }
            return QueryParentChild<User, Wallet, int>(string.Format(SQL_FOR_AUC_SITUATION, complement), c => c.Id, c => c.Wallets, "Id", parameters).ToList();
        }

        public User GetByReferralCode(string referralCode)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("ReferralCode", referralCode, DbType.AnsiString);
            return SelectByParameters<User>(parameters).SingleOrDefault();
        }

        public List<User> ListReferredUsers(int referredId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("ReferredId", referredId, DbType.Int32);
            return SelectByParameters<User>(parameters).ToList();
        }

        public List<User> ListAllUsersData()
        {
            return QueryParentChild<User, Wallet, int>(SQL_FOR_ALL_USER_DATA, c => c.Id, c => c.Wallets, "Id").ToList();
        }

        public List<User> ListUsersFollowingAdvisorOrAsset(int advisorId, int assetId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("AdvisorId", advisorId, DbType.Int32);
            parameters.Add("AssetId", assetId, DbType.Int32);
            parameters.Add("ActionType", FollowActionType.Follow.Value, DbType.Int32);
            parameters.Add("AllowNotifications", true, DbType.Boolean);
            return Query<User, Wallet, User>(SQL_FOLLOWING,
                       (user, wallet) =>
                       {
                           user.Wallet = wallet;
                           return user;
                       }, "Id", parameters).ToList();
        }

        private void FillAdvisorWithUserData(ref DomainObjects.Advisor.Advisor advisor, User user)
        {
            advisor.Id = user.Id;
            advisor.Email = user.Email;
            advisor.Password = user.Password;
            advisor.CreationDate = user.CreationDate;
            advisor.ConfirmationCode = user.ConfirmationCode;
            advisor.ConfirmationDate = user.ConfirmationDate;
            advisor.ReferralCode = user.ReferralCode;
            advisor.ReferralStatus = user.ReferralStatus;
            advisor.BonusToReferred = user.BonusToReferred;
            advisor.ReferredId = user.ReferredId;
            advisor.AllowNotifications = user.AllowNotifications;
            advisor.DiscountProvided = user.DiscountProvided;
            advisor.IsAdvisor = true;
        }
    }
}
