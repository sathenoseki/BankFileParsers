using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BankFileParsers.Classes;
using BankFileParsers.Helpers;

namespace BankFileParsers.Parsers
{
    public static class BaiTranslator
    {
        public static TranslatedBaiFile Translate(BaiFile data)
        {
            return new TranslatedBaiFile(data);
        }

        private static string Format(string name, object value, int indent)
        {
            var space = "";
            for (var i = 1; i < indent; i++) space += "  ";
            return $"{space}{name}: {value}{Environment.NewLine}";
        }

        /// <summary>
        /// Pretty prints the translated object
        /// </summary>
        /// <param name="data">The object to pretty print</param>
        /// <param name="maxWidth">The max width of the output string (just for the data column)</param>
        /// <returns>A string of the pretty printed data</returns>
        public static string PrettyPrint(TranslatedBaiFile data, int maxWidth = 60)
        {
            // I wanted to make this use reflection, but I also wanted to remove ReShaper warnings/hints
            // This is ugly - almost on purpose - ugh
            var ret = "";
            var indent = 1;

            ret += Format("HeaderRecordCode", data.HeaderRecordCode, indent);
            ret += Format("SenderIdentification", data.SenderIdentification, indent);
            ret += Format("ReceiverIdentification", data.ReceiverIdentification, indent);
            ret += Format("FileCreationDateTime", data.FileCreationDateTime, indent);
            ret += Format("FileIdentificationNumber", data.FileIdentificationNumber, indent);
            ret += Format("PhysicalRecordLength", data.PhysicalRecordLength, indent);
            ret += Format("BlockSize", data.BlockSize, indent);
            ret += Format("VersionNumber", data.VersionNumber, indent);

            foreach (var group in data.Groups)
            {
                indent++;
                ret += Format("HeaderRecordCode", group.HeaderRecordCode, indent);
                ret += Format("UltimateReceiverIdentification", group.UltimateReceiverIdentification, indent);
                ret += Format("OriginatorIdentification", group.OriginatorIdentification, indent);
                ret += Format("GroupStatus", group.GroupStatus, indent);
                ret += Format("AsOfDateTime", group.AsOfDateTime, indent);
                ret += Format("CurrencyCode", group.CurrencyCode, indent);
                ret += Format("AsOfDateModifier", group.AsOfDateModifier, indent);

                foreach (var account in group.Accounts)
                {
                    indent++;
                    ret += Format("IdentifierRecordCode", account.IdentifierRecordCode, indent);
                    ret += Format("CustomerAccountNumber", account.CustomerAccountNumber, indent);
                    ret += Format("CurrencyCode", account.CurrencyCode, indent);
                    ret += Format("FundTypes", "", indent);
                    // funds
                    indent++;
                    foreach (var fundType in account.FundsTypes)
                    {
                        ret += Format("TypeCode", fundType.TypeCode, indent);
                        ret += Format("TransactionDetail", "", indent);
                        indent++;
                        ret += Format("CategoryType", fundType.Detail.CategoryType, indent);
                        ret += Format("TypeCode", fundType.Detail.TypeCode, indent);
                        ret += Format("Transaction", fundType.Detail.Transaction, indent);
                        ret += Format("Level", fundType.Detail.Level, indent);
                        ret += Format("Description", fundType.Detail.Description, indent);
                        indent--;

                    }
                    indent--;

                    // details
                    ret += Format("Details", "", indent);
                    indent++;
                    foreach (var detail in account.Details)
                    {
                        ret += Format("Detail", detail.RecordCode, indent);
                        indent++;
                        ret += Format("RecordCode", detail.RecordCode, indent);
                        ret += Format("TypeCode", detail.TypeCode, indent);
                        ret += Format("Amount", detail.Amount, indent);
                        ret += Format("FundsType", detail.FundsType, indent);
                        ret += Format("Immediate", detail.Immediate, indent);
                        ret += Format("OneDay", detail.OneDay, indent);
                        ret += Format("TwoOrMoreDays", detail.TwoOrMoreDays, indent);
                        ret += Format("AvailableDate", detail.FundsAvailableDate, indent);
                        ret += Format("BankReferenceNumber", detail.BankReferenceNumber, indent);
                        ret += Format("CustomerReferenceNumber", detail.CustomerReferenceNumber, indent);
                        ret += Format("Text", detail.Text[..Math.Min(detail.Text.Length, maxWidth)], indent);
                        indent--;
                    }
                    indent--;
                    ret += Format("TrailerRecordCode", account.TrailerRecordCode, indent);
                    ret += Format("AccountControlTotal", account.AccountControlTotal, indent);
                    ret += Format("NumberOfRecords", account.NumberOfRecords, indent);
                }

                ret += Format("TrailerRecordCode", group.TrailerRecordCode, indent);
                ret += Format("GroupControlTotal", group.GroupControlTotal, indent);
                ret += Format("NumberOfAccounts", group.NumberOfAccounts, indent);
                ret += Format("NumberOfRecords", group.NumberOfRecords, indent);
            }

            indent--;

            ret += Format("TrailerRecordCode", data.TrailerRecordCode, indent);
            ret += Format("FileControlTotal", data.FileControlTotal, indent);
            ret += Format("NumberOfGroups", data.NumberOfGroups, indent);
            ret += Format("NumberOfRecords", data.NumberOfRecords, indent);
            return ret;
        }

        /// <summary>
        /// Returns a IEnumerable of SummaryHeader information in the BAI file
        /// </summary>
        /// <param name="data">The translated BAI object</param>
        /// <returns>A IEnumerable of SummaryHeader</returns>
        public static IEnumerable<SummaryHeader> GetSummaryInformation(TranslatedBaiFile data)
        {
            return from @group in data.Groups
                from account in @group.Accounts
                from fund in account.FundsTypes
                let amount = BaiFileHelpers.GetAmount(fund.Amount, @group.CurrencyCode)
                select new SummaryHeader()
                {
                    Date = @group.AsOfDateTime,
                    CreationDate = data.FileCreationDateTime,
                    SenderIdentification = data.SenderIdentification,
                    ReceiverIdentification = data.ReceiverIdentification,
                    FileIdentificationNumber = data.FileIdentificationNumber,
                    CurrencyCode = account.CurrencyCode,
                    CustomerAccountNumber = account.CustomerAccountNumber,
                    Amount = amount,
                    Count = fund.ItemCount,
                    FundType = fund.FundsType,
                    TypeCode = fund.Detail.TypeCode,
                    TypeDescription = fund.Detail.Description
                };
        }

        /// <summary>
        /// Returns a IEnumerable of DetailSummary
        /// </summary>
        /// <param name="data">The translated BAI object</param>
        /// <param name="dictionaryKeys">Any Keys in the Detail.TextDictionary (if any) you would like to export</param>
        /// <returns>A List of DetailSummary</returns>
        public static IEnumerable<DetailSummary> GetDetailInformation(TranslatedBaiFile data, List<string> dictionaryKeys)
        {
            var ret = new List<DetailSummary>();
            foreach (var group in data.Groups)
            {
                foreach (var account in group.Accounts)
                {
                    foreach (var detail in account.Details)
                    {
                        var detailType = BaiFileHelpers.GetTransactionDetail(detail.TypeCode);
                        var textDictionary = dictionaryKeys is null
                            ? new Dictionary<string, string>()
                            : dictionaryKeys.Where(key => detail.TextDictionary.ContainsKey(key))
                                .ToDictionary(key => key, key => detail.TextDictionary[key]);

                        var ds = new DetailSummary()
                        {
                            Date = group.AsOfDateTime,
                            CreationDate = data.FileCreationDateTime,
                            FileIdentificationNumber = data.FileIdentificationNumber,
                            SenderIdentification = data.SenderIdentification,
                            Amount = BaiFileHelpers.GetAmount(detail.Amount, group.CurrencyCode),
                            BankReferenceNumber = detail.BankReferenceNumber,
                            CustomerReferenceNumber = detail.CustomerReferenceNumber,
                            CustomerAccountNumber = account.CustomerAccountNumber,
                            Text = detail.Text,
                            TypeCode = detailType.TypeCode,
                            TypeDescription = detailType.Description,
                            FundType = detail.FundsType,
                            TextDictionary = textDictionary
                        };

                        // I don't want to return an optional, I want a blank string
                        if (!string.IsNullOrEmpty(detail.Immediate))
                            ds.Immediate = BaiFileHelpers.GetAmount(detail.Immediate, group.CurrencyCode).ToString(CultureInfo.CurrentCulture);
                        if (!string.IsNullOrEmpty(detail.OneDay))
                            ds.OneDay = BaiFileHelpers.GetAmount(detail.OneDay, group.CurrencyCode).ToString(CultureInfo.CurrentCulture);
                        if (!string.IsNullOrEmpty(detail.TwoOrMoreDays))
                            ds.TwoOrMoreDays = BaiFileHelpers.GetAmount(detail.TwoOrMoreDays, group.CurrencyCode).ToString(CultureInfo.CurrentCulture);

                        ret.Add(ds);
                    }
                }
            }
            return ret;
        }
    }
}
