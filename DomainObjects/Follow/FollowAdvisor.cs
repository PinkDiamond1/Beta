﻿using Auctus.Util.DapperAttributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auctus.DomainObjects.Follow
{
    public class FollowAdvisor : Follow
    {
        [DapperType(System.Data.DbType.Int32)]
        public int AdvisorId { get; set; }
    }
}
