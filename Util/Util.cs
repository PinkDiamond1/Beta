﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Linq;

namespace Auctus.Util
{
    public class Util
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTime UnixMillisecondsTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

        public static Int64 DatetimeToUnixSeconds(DateTime datetime)
        {
            return (Int64)(datetime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static Int64 DatetimeToUnixMilliseconds(DateTime datetime)
        {
            return (Int64)(datetime.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }

        public static double? ConvertMonthlyToDailyRate(double? monthlyRate)
        {
            if (!monthlyRate.HasValue)
                return null;
            return Math.Pow((monthlyRate.Value / 100.0) + 1.0, 1.0 / 30.0) - 1.0;
        }

        public static decimal ConvertBigNumber(string bigNumber, int decimals)
        {
            char separator = Convert.ToChar(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            if (bigNumber.Length <= decimals)
                return decimal.Parse($"0{separator}{bigNumber.PadLeft(decimals, '0')}");
            else
            {
                var integerPart = bigNumber.Substring(0, bigNumber.Length - decimals);
                var decimalPart = bigNumber.Substring(bigNumber.Length - decimals, decimals);
                return decimal.Parse($"{integerPart}{separator}{decimalPart}");
            }
        }

        public static decimal ConvertHexaBigNumber(string hexaNumber, int decimals)
        {
            return ConvertBigNumber(BigInteger.Parse("0" + hexaNumber.TrimStart('0', 'x'), NumberStyles.AllowHexSpecifier).ToString(), decimals);
        }

        public static bool IsEqualWithTolerance(double a, double b, double percentageTolerance)
        {
            if (a == b)
                return true;
            else if (percentageTolerance <= 0)
                return false;
            else if (a > b && (a - (a * percentageTolerance)) <= b)
                return true;
            else if (a < b && (b - (b * percentageTolerance)) <= a)
                return true;
            else
                return false;
        }

        private const string CHARACTER_SET = "123456789ABCDEFGHIJKLMNPQRSTUVWXYZ";
        private static Random rnd = new Random();

        public static String GetRandomString(int length)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                builder.Append(CHARACTER_SET[rnd.Next(CHARACTER_SET.Length)]);
            }
            return builder.ToString();
        }

        public static string GetFormattedValue(double value)
        {
            if (value >= 100)
                return value.ToString("#########0.0#", System.Globalization.CultureInfo.InvariantCulture);
            else if (value >= 10)
                return value.ToString("#########0.0##", System.Globalization.CultureInfo.InvariantCulture);
            else if (value > 0)
                return value.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);
            else if (value >= 0.01)
                return value.ToString("0.0####", System.Globalization.CultureInfo.InvariantCulture);
            else if (value >= 0.001)
                return value.ToString("0.0#####", System.Globalization.CultureInfo.InvariantCulture);
            else if (value >= 0.0001)
                return value.ToString("0.0######", System.Globalization.CultureInfo.InvariantCulture);
            else if (value >= 0.00001)
                return value.ToString("0.0#######", System.Globalization.CultureInfo.InvariantCulture);
            else 
                return value.ToString("0.0########", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
