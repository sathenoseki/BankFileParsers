﻿using System;
using System.Collections;
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
        public DateTime? AvailableDate { get; set; }
        public string BankReferenceNumber { get; set; }
        public string CustomerReferenceNumber { get; set; }
        public string Text { get; set; }
        public List<string> TextList { get; set; }
        public Dictionary<string, string> TextDictionary { get; set; }

        public Detail(BaiDetail data, string currencyCode)
        {
            TextList = new List<string>();
            TextDictionary = new Dictionary<string, string>();

            var list = new List<string> { data.TransactionDetail };
            list.AddRange(data.DetailContinuation);

            var lineData = "";
            foreach (var section in list)
            {
                var line = section.Trim();
                // Some / are optional?
                //if (!line.EndsWith("/")) throw new Exception("I got a line without a trailing /");

                if (line.StartsWith("16"))
                {
                    if (!line.EndsWith("/"))
                    {
                        line += "/";
                    }
                }
                else if (line.StartsWith("88"))
                {
                    line = line[2..];//.Replace("/", " ");

                    if (!line.EndsWith("/"))
                    {
                        line += "/";
                    }
                }
                else throw new Exception("I got a bad line: " + line);
                lineData += line;
            }

            // Now try to figure out what's left ;-)
            var stack = new Stack(lineData.Split(',').Reverse().ToArray());

            RecordCode = stack.Pop().ToString();
            TypeCode = stack.Pop().ToString();
            Amount = stack.Pop().ToString();
            FundsType = stack.Pop().ToString();

            switch (FundsType.ToUpper())
            {
                case "S":
                    Immediate = stack.Pop().ToString();
                    OneDay = stack.Pop().ToString();
                    TwoOrMoreDays = stack.Pop().ToString();
                    break;
                case "D":
                    // next field is the number of distribution pairs
                    // number of days, available amount
                    // currencyCode would be used here
                    throw new Exception("I don't want to deal with this one yet - " + currencyCode);
                case "V":
                    var date = stack.Pop().ToString();
                    var time = stack.Pop().ToString();
                    AvailableDate = BaiFileHelpers.DateTimeFromFields(date, time);
                    break;
            }

            BankReferenceNumber = stack.Pop().ToString();
            CustomerReferenceNumber = stack.Pop().ToString();
            // What's left on the stack?
            Text = LeftoverStackToString(stack);

            CreateTextList();
            CreateTextDictionary();

            Text = ConcatenateTextLines();
        }

        private static string LeftoverStackToString(Stack stack)
        {
            var ret = "";
            while (stack.Count > 0)
                ret += stack.Pop().ToString();
            return ret;
        }

        private void CreateTextList()
        {
            // Now fill up the List
            var fields = Text.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in fields)
            {
                TextList.Add(field);
            }
        }

        private void CreateTextDictionary()
        {
            var dictionaryList = new List<string>();
            var fields = Text.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var field in fields)
            {
                if (dictionaryList.Count > 0 && !field.Contains(':'))
                {
                    var text = dictionaryList[^1];
                    if (text.EndsWith(":")) text += field;
                    else text += " " + field;
                    dictionaryList[^1] = text;
                }
                else
                    dictionaryList.Add(field);
            }

            foreach (var item in dictionaryList)
            {
                var parts = item.Split(':');
                if (parts.Length != 2) continue;
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
            var fields = Text.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var concatText = new StringBuilder();

            foreach (var field in fields)
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
