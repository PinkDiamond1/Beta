﻿using Auctus.DataAccessInterfaces.Account;
using Auctus.DataAccessInterfaces.Exchange;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Exchange;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auctus.DataAccessMock.Exchange
{
    public class GoogleApi : IGoogleApi
    {
        public SocialUser GetSocialUser(String accessToken)
        {
            throw new NotImplementedException();
        }
    }
}
