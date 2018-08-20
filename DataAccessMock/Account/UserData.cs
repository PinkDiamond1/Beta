﻿using Auctus.DataAccessInterfaces;
using Auctus.DataAccessInterfaces.Account;
using Auctus.DomainObjects.Account;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Auctus.DataAccessMock.Account
{
    public class UserData : BaseData<User>, IUserData<User>
    {
        public User GetByConfirmationCode(string code)
        {
            throw new NotImplementedException();
        }

        public User GetByEmail(string email)
        {
            return new DomainObjects.Advisor.Advisor()
            {
                Id = 1,
                Email = "test@auctus.org",
                CreationDate = new DateTime(2018, 5, 1, 0, 0, 0),
                ConfirmationDate = new DateTime(2018, 5, 2, 0, 0, 0),
                ConfirmationCode = "",
                Password = "",
                IsAdvisor = true,
                Name = "Tester Advisor",
                Description = "Test Advisor description",
                Enabled = true,
                BecameAdvisorDate = new DateTime(2018, 5, 8, 0, 0, 0),
                Wallet = new Wallet()
                {
                    Id = 1,
                    UserId = 1,
                    Address = "0x0000000000000000000000000000000000000000",
                    CreationDate = new DateTime(2018, 5, 3, 0, 0, 0)
                },
                RequestToBeAdvisor = new DomainObjects.Advisor.RequestToBeAdvisor()
                {
                    Id = 1,
                    UserId = 1,
                    Approved = true,
                    CreationDate = new DateTime(2018, 5, 4, 0, 0, 0),
                    Description = "Test Advisor description",
                    Name = "Tester Advisor",
                    PreviousExperience = ""
                }
            };
        }

        public User GetById(int id)
        {
            throw new NotImplementedException();
        }

        public User GetForLogin(string email)
        {
            throw new NotImplementedException();
        }

        public User GetForNewWallet(string email)
        {
            throw new NotImplementedException();
        }

        public User GetByReferralCode(string referralCode)
        {
            throw new NotImplementedException();
        }
    }
}
