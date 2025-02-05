﻿using Auctus.Util;
using Auctus.Util.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Auctus.DomainObjects.Asset
{
    public class AssetModeType : IntType
    {
        public static readonly AssetModeType Neutral = new AssetModeType(0);
        public static readonly AssetModeType ModerateBuy = new AssetModeType(1);
        public static readonly AssetModeType StrongBuy = new AssetModeType(2);
        public static readonly AssetModeType ModerateSell = new AssetModeType(3);
        public static readonly AssetModeType StrongSell = new AssetModeType(4);

        private AssetModeType(int type) : base(type)
        { }

        public static AssetModeType Get(int type)
        {
            switch (type)
            {
                case 0:
                    return Neutral;
                case 1:
                    return ModerateBuy;
                case 2:
                    return StrongBuy;
                case 3:
                    return ModerateSell;
                case 4:
                    return StrongSell;
                default:
                    throw new BusinessException("Invalid type.");
            }
        }
    }
}
