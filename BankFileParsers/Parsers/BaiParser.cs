using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BankFileParsers.Classes;
using BankFileParsers.Enums;

namespace BankFileParsers.Parsers
{
    public class BaiParser
    {
        //private string[] _data;

        public static async Task<BaiFile> Parse(string fileName)
        {
            if (!File.Exists(fileName)) throw new Exception("File not found, nothing to parse");
            var lines = File.OpenRead(fileName);
            return await Parse(lines);
        }

        public static async Task<BaiFile> Parse(Stream stream)
        {
            var bai = new BaiFile();
            var group = new BaiGroup("--default--");
            var account = new BaiAccount("--default--");
            var detail = new BaiDetail("--default--");
            var continuation = ContinuationType.Account;

            var s = new StreamReader(stream);
            while (!s.EndOfStream)
            {
                var line = await s.ReadLineAsync();
                var type = line[..2];

                switch (type)
                {
                    case "01":
                        bai.FileHeader = line;
                        break;
                    case "99":
                        bai.FileTrailer = line;
                        break;
                    case "02":
                        continuation = ContinuationType.Group;
                        group = new BaiGroup(line);
                        break;
                    case "98":
                        group.GroupTrailer = line;
                        bai.Groups.Add(group);
                        break;
                    case "03":
                        continuation = ContinuationType.Account;
                        account = new BaiAccount(line);
                        detail = new BaiDetail("--default--");
                        break;
                    case "49":
                    {
                        if (detail.TransactionDetail != "--default--")
                            account.Details.Add(detail);
                        account.AccountTrailer = line;
                        group.Accounts.Add(account);
                        break;
                    }
                    case "16":
                    {
                        if (detail.TransactionDetail != "--default--")
                        {
                            account.Details.Add(detail);
                        }
                        continuation = ContinuationType.Detail;
                        detail = new BaiDetail(line);
                        break;
                    }
                    case "88":
                        switch (continuation)
                        {
                            case ContinuationType.Group:
                                group.GroupContinuation.Add(line);
                                break;
                            case ContinuationType.Account:
                                account.AccountContinuation.Add(line);
                                break;
                            case ContinuationType.Detail:
                                detail.DetailContinuation.Add(line);
                                break;
                        }

                        break;
                    default:
                        throw new NotImplementedException("I don't know what to do with this line: " + line);
                }
            }

            return bai;
        }

        public static Stream Write(BaiFile data)
        {
            var lines = new List<string>
            {
                data.FileHeader
            };
            foreach (var group in data.Groups)
            {
                lines.Add(group.GroupHeader);
                lines.AddRange(group.GroupContinuation);
                foreach (var account in group.Accounts)
                {
                    lines.Add(account.AccountIdentifier);
                    lines.AddRange(account.AccountContinuation);

                    foreach (var detail in account.Details)
                    {
                        lines.Add(detail.TransactionDetail);
                        lines.AddRange(detail.DetailContinuation);
                    }

                    lines.Add(account.AccountTrailer);
                }
                lines.Add(group.GroupTrailer);
            }
            lines.Add(data.FileTrailer);
            var s = lines.SelectMany(l => System.Text.Encoding.UTF8.GetBytes(l)).ToArray();
            var m = new MemoryStream(s);
            return m;
        }
    }
}
