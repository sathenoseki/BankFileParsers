using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BankFileParsers.Helpers;
using BankFileParsers.Parsers;

namespace BankFileParsers.Example
{
    public static class Program
    {
        public static async Task Main()
        {
         // await  Process(@"Files", @"Files\test.txt", @"Bai-sample.txt");
         var s = File.OpenRead(Path.Combine("Files", "BAI-Test.txt"));
         var bai = await BaiParser.Parse(s);
         var trans = BaiTranslator.Translate(bai);
         var g = trans.Groups.First();
         var acc = g.Accounts.SelectMany(a => a.Details).GroupBy(d => d.TypeCode);

        }

        static async Task Process(string basePath, string transPath, string logName)
        {
          //  var parser = new BaiParser();
            var total = new Stopwatch();
            total.Start();

            var sw = new Stopwatch();
            foreach (var fileName in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
            {
                sw.Restart();
                Console.Write(fileName + ": ");
                try
                {
                    var bai = await BaiParser.Parse(File.OpenRead(fileName));
                    var t = BaiTranslator.Translate(bai);
                    var newFileName = fileName.Replace(basePath, transPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileName));
                    await using var fs = new FileStream(fileName, FileMode.OpenOrCreate);
                    await using var s = BaiParser.Write(bai);
                    await s.CopyToAsync(fs);
                }
                catch { }
                sw.Stop();
                var ts = sw.Elapsed;

                // Format and display the TimeSpan value.
                var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
                Console.WriteLine(elapsedTime);
                await File.AppendAllTextAsync(logName, fileName + ": " + elapsedTime + Environment.NewLine);
                //break;
            }
            total.Stop();
            var fin = total.Elapsed;
            var totalTime = $"{fin.Hours:00}:{fin.Minutes:00}:{fin.Seconds:00}.{fin.Milliseconds / 10:00}";
            await File.AppendAllTextAsync(logName, "Total: " + totalTime + Environment.NewLine);
        }

        public static async Task Main_old()
        {

            const string fileName = @"BAI-sample.txt";
            var bai = await BaiParser.Parse(fileName);
            var trans = BaiTranslator.Translate(bai);

            var summary = BaiTranslator.GetSummaryInformation(trans);
            Console.WriteLine("Summary Count: " + summary.Count());

            var dictionaryKeys = new List<string> { "PREAUTHORIZED ACH FROM", "ORIGINATOR ID", "ENTRY DESCRIPTION",
                "PAYMENT ID", "RECEIVER INFORMATION", "ADDENDA INFORMATION" };

            var detail = BaiTranslator.GetDetailInformation(trans, dictionaryKeys);
            var detailDictionary = detail.Where(p => p.TextDictionary.Count > 0).ToList();
            Console.WriteLine("Detail Count: " + detail.Count());
            Console.WriteLine("Detail with Dictionary: " + detailDictionary.Count);

            // Verify that the parser works - do a diff with the input file
            // var stream = BaiParser.Write( bai);
            //
            // File.Open(fileName + ".new", FileMode.OpenOrCreate).c;

            // Dump to CSV?
            //detail.CsvFieldSeparator('|');
            // Set the prefix
            //detail.CsvFieldPrefix('[');
            // different than the postfix
            //detail.CsvFieldPostfix(']');
            // or set them to the same
            //detail.CsvFieldPrefixPostfix('"');
            // or turn them all off
            //detail.CsvDisablePrefixPostFix();
            var csv = detail.ExportToCsv(dictionaryKeys);

            // you can even just export a single column if you want
            //var csv = detail.ExportToCsv(null, new List<string> { "FileIdentificationNumber" });
            // It can be just a dictionary key
            //var csv = detail.ExportToCsv(new List<string>{"PAYMENT ID"}, new List<string>());
            await File.WriteAllTextAsync(@"BAI-sample.csv", csv);
        }
    }
}
