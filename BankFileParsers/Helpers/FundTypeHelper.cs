using System.Collections;
using System.Collections.Generic;
using BankFileParsers.Classes;

namespace BankFileParsers.Helpers
{
    public static class FundTypeHelper
    {
        public static FundType GetNext(Stack<string> stack, string currencyCode)
        {
            if (stack.Count < 4) return null;
            var typeCode = stack.Pop();
            var amount = stack.Pop();
            var itemCount = stack.Pop();
            var fundsType = stack.Pop();

            switch (fundsType.ToUpper())
            {
                case "S":
                    var immediate = stack.Pop();
                    var oneDay = stack.Pop();
                    var moreDays = stack.Pop();

                    return new FundType(typeCode, amount, itemCount, fundsType, immediate, oneDay, moreDays);
                case "D":
                    // next field is the number of distribution pairs
                    // number of days, available amount
                    var info = new Dictionary<int, decimal>();
                    var count = int.Parse(stack.Pop());
                    for (var i = 0; i < count; i++)
                    {
                        var key = int.Parse(stack.Pop());
                        var v = BaiFileHelpers.GetAmount(stack.Pop(), currencyCode);
                        info.Add(key, v);
                    }
                    return new FundType(typeCode, amount, itemCount, fundsType, count.ToString(), info);
                case "V":
                    var date = stack.Pop();
                    var time = stack.Pop();
                    var value = BaiFileHelpers.DateTimeFromFields(date, time);
                    return new FundType(typeCode, amount, itemCount, fundsType, value);
            }
            return new FundType(typeCode, amount, itemCount, fundsType);
        }
    }
}
