using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BankFileParsers.Classes;

namespace BankFileParsers.Helpers
{
    internal class AccountFundTypeFactory
    {
        public string RecordCode { get; set; }
        public string CustomerAccountNumber { get; set; }
        public string CurrencyCode { get; set; }

        private readonly Stack<string> _stack;

        public AccountFundTypeFactory(IEnumerable<string> data)
        {
            var lineData = "";
            foreach (var section in data)
            {
                var line = section.Trim();
                if (!line.EndsWith("/")) throw new Exception("I got a line without a trailing /");
                line = line.Replace("/", "");

                var m = line[..2];
                switch (m)
                {
                    case "03":
                    {
                        // The first three fields are record, account and currency
                        var fields = line.Split(',');
                        RecordCode = fields[0];
                        CustomerAccountNumber = fields[1];
                        CurrencyCode = fields[2];
                        var trailing = fields.Length > 2? ",":"";
                        var replaced = $"{fields[0]},{fields[1]},{fields[2]}{trailing}";
                        line = line[replaced.Length..];
                        break;
                    }
                    case "88":
                        line = line[2..];
                        break;
                    default:
                        throw new Exception("I got a bad line: " + line);
                }
                lineData += line;
            }
            _stack = new Stack<string>(lineData.Split(',').Reverse().ToArray());
        }

        public FundType GetNext()
        {
            return FundTypeHelper.GetNext(_stack, CurrencyCode);
        }
    }
}