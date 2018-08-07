﻿using Auctus.DomainObjects.Follow;
using System.Collections.Generic;

namespace Auctus.DataAccessInterfaces.Follow
{
    public interface IFollowAdvisorData<T> : IBaseData<T>
    {
        List<FollowAdvisor> List(IEnumerable<int> advisorIds);
    }
}