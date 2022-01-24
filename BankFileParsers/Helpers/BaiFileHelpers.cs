using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BankFileParsers.Classes;
using BankFileParsers.Enums;

namespace BankFileParsers.Helpers
{
    public static class BaiFileHelpers
    {
        private static List<TransactionDetail> _transactionDetail;

        public static DateTime DateTimeFromFields(string date, string time)
        {
            if (string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(time)) return DateTime.MinValue;

            // An end of day can be 9999, if it is they really meant 2400
            var dateString = date;
            if (time == "9999") time = "2400";
            if (time == string.Empty) dateString += "0000";
            else dateString += time;

            const string format = "yyMMddHHmm";
            var hourPos = format.IndexOf("HH", StringComparison.Ordinal);
            var hour = dateString.Substring(hourPos, 2);
            var addDay = hour == "24";
            if (addDay) dateString = string.Concat(dateString.AsSpan(0, hourPos), "00", dateString.AsSpan(hourPos + 2));

            var dateTime = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);
            if (addDay) dateTime += TimeSpan.FromHours(24);

            return dateTime;
        }

        public static GroupStatus GetGroupStatus(string statusCode) =>
             statusCode switch
            {
                "1" => GroupStatus.Update,
                "2" => GroupStatus.Deletion,
                "3" => GroupStatus.Correction,
                "4" => GroupStatus.TestOnly,
                _ => GroupStatus.Update
            };


        public static string GetCurrencyCode(string code)
        {
            return code == string.Empty ? "USD" : code;
        }

        private static int GetDecimalPlaces(string currencyCode)
        {
            currencyCode = GetCurrencyCode(currencyCode);
            var zeroDecimal = new List<string> { "BRL", "EUR", "JPY", "KMF", "XAF", "XOF", "XPF" };
            var oneDecimal = new List<string> { "MRO" };
            var threeDecimal = new List<string> { "BHD", "EGP", "IQD", "JOD", "KWD", "LYD", "MTL", "OMR", "SDP", "TND", "YDD" };

            if (zeroDecimal.Contains(currencyCode)) return 0;
            if (oneDecimal.Contains(currencyCode)) return 1;
            if (threeDecimal.Contains(currencyCode)) return 3;
            // Everything else should be two decimal places
            return 2;
        }

        public static decimal GetAmount(string amount, string currencyCode)
        {
            amount = TrimStart(amount, "+");
            var neededLength = GetDecimalPlaces(currencyCode);
            if (string.IsNullOrEmpty(amount)) return 0;
            if (amount.Length < neededLength) amount = amount.PadLeft(neededLength + 1, '0');
            amount = amount.Insert(amount.Length - neededLength, ".");
            return decimal.Parse(amount);
        }

        private static string TrimStart(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString)) return target;

            var result = target;
            while (result.StartsWith(trimString))
            {
                result = result[trimString.Length..];
            }

            return result;
        }

        public static AsOfDateModifier GetAsOfDateModifier(string modifier) =>
            modifier switch
            {
                "1" => AsOfDateModifier.InterimPreviousDay,
                "2" => AsOfDateModifier.FinalPreviousDay,
                "3" => AsOfDateModifier.InterimSameDay,
                "4" => AsOfDateModifier.FinalSameDay,
                _ => AsOfDateModifier.Missing
            };

        public static TransactionDetail GetTransactionDetail(string typeCode)
        {
            _transactionDetail ??= TransactionDetailBuilder.Build();
            // It looks like our bank uses type codes that are not in the spec - return a "dummy record"
            var item = _transactionDetail.FirstOrDefault(i => i.TypeCode == typeCode) ?? new TransactionDetail()
            {
                CategoryType = CategoryTypeCodes.NonMonetaryInformation,
                Transaction = TransactionType.NotApplicable,
                Level = LevelType.Status,
                TypeCode = typeCode,
                Description = "Unknown Type Code"
            };
            return item;
        }
    }
}
