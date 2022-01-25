using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BankFileParsers.Helpers;

namespace BankFileParsers.Classes
{
    public class Detail
    {
        public string RecordCode { get; set; }
        public string TypeCode { get; set; }
        public string Amount { get; set; }
        public string FundsType { get; set; }
        public string Immediate { get; set; }
        public string OneDay { get; set; }
        public string TwoOrMoreDays { get; set; }
        public DateTime? FundsAvailableDate { get; set; }
        public string BankReferenceNumber { get; set; }
        public string CustomerReferenceNumber { get; set; }
        public string Text { get; set; }
        public List<string> TextList { get; set; }
        public Dictionary<string, string> TextDictionary { get; set; }

        public Detail(BaiDetail data, string currencyCode)
        {
            TextList = new List<string>();
            TextDictionary = new Dictionary<string, string>();

            var lineData =  data.TransactionDetail.Split(',')
                .Concat(data.DetailContinuation.Select(d => d[2..]))
                .Select(detail => detail.TrimStart(',').Trim().TrimEnd('/'));

            var queue = new Queue<string>(lineData);

            RecordCode = queue.Dequeue();
            TypeCode = queue.Dequeue();
            Amount = queue.Dequeue();
            FundsType = queue.Dequeue();

            switch (FundsType.ToUpper())
            {
                case "S":
                    Immediate = queue.Dequeue();
                    OneDay = queue.Dequeue();
                    TwoOrMoreDays = queue.Dequeue();
                    break;
                case "D":
                    // next field is the number of distribution pairs
                    // number of days, available amount
                    // currencyCode would be used here
                    throw new Exception("I don't want to deal with this one yet - " + currencyCode);
                case "V":
                    var date = queue.Dequeue();
                    var time = queue.Dequeue();
                    FundsAvailableDate = BaiFileHelpers.DateTimeFromFields(date, time);
                    break;
            }

            BankReferenceNumber = queue.Dequeue();
            CustomerReferenceNumber = queue.Dequeue();


            CreateTextList(queue);
            CreateTextDictionary();

            Text = ConcatenateTextLines();
        }



        private void CreateTextList(IEnumerable<string> strings)
        {

            foreach (var field in strings)
            {
                TextList.Add(field);
            }
        }

        private void CreateTextDictionary()
        {
            // var dictionaryList = new List<string>();
            // foreach (var field in TextList)
            // {
            //     if (dictionaryList.Count > 0 && !field.Contains(':'))
            //     {
            //         var text = dictionaryList[^1];
            //         if (text.EndsWith(":")) text += field;
            //         else text += " " + field;
            //         dictionaryList[^1] = text;
            //     }
            //     else
            //         dictionaryList.Add(field);
            // }

            foreach (var parts in TextList.Select(item => item.Split('=')).Where(parts => parts.Length == 2))
            {
                if (!TextDictionary.ContainsKey(parts[0]))
                {
                    TextDictionary.Add(parts[0], parts[1]);
                }
                else
                {
                    try
                    {
                        // TODO - actually create a counter object in case there's a third one
                        TextDictionary.Add(parts[0] + "2", parts[1]);
                    }
                    catch
                    {
                        // I'm doing this as a helper, makes no sense if it crashes
                    }
                }
            }
        }

        private string ConcatenateTextLines()
        {

            var concatText = new StringBuilder();

            foreach (var field in TextList)
            {
                if (concatText.ToString().EndsWith(":"))
                {
                    concatText.Append(field);
                }
                else
                {
                    concatText.Append(" " + field);
                }
            }

            return concatText.ToString();
        }
    }
}
