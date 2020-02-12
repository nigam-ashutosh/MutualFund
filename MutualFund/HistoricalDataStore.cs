using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MutualFund
{
    public class HistoricalDataStore
    {
        public HistoricalDataStore()
        {
            Console.Clear();
            ReadDataFromSummaryFile();
        }
        internal Dictionary<DateTime, Dictionary<string, MfData>> mfHistoricalData = new Dictionary<DateTime, Dictionary<string, MfData>>();
        DateTime historicalAnalysiStartDate = new DateTime(1900, 1, 1);

        HashSet<string> distinctMutualFunds = new HashSet<string>();

        string historicalFileName = "HistoricalData.csv";

        private void PopulateDataToSummaryFile()
        {
            mfHistoricalData = new Dictionary<DateTime, Dictionary<string, MfData>>();
            ReadFromReportFiles();
            PopulateToSummaryFile();
        }

        private void ReadFromReportFiles()
        {
            double fileCounter = 0;
            var directory = System.Environment.CurrentDirectory;
            List<string> files = new List<string>();

            files.AddRange(System.IO.Directory.GetFiles(directory).Where(f => f.Contains("Report_")));
            foreach (var subDir in System.IO.Directory.GetDirectories(directory))
            {
                if (!subDir.Contains("Reports"))
                    continue;
                files.AddRange(System.IO.Directory.GetFiles(subDir).Where(f => f.Contains("Report_")));
                foreach (var subSubDir in System.IO.Directory.GetDirectories(subDir))
                {
                    files.AddRange(System.IO.Directory.GetFiles(subSubDir).Where(f => f.Contains("Report_")));
                    foreach (var subSubSubDir in System.IO.Directory.GetDirectories(subSubDir))
                    {
                        files.AddRange(System.IO.Directory.GetFiles(subSubSubDir).Where(f => f.Contains("Report_")));
                    }
                }
            }
            var filesToRead = files.Where(f => f.Contains("Report_"));
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Stopwatch displayWatch = new Stopwatch();
            displayWatch.Start();

            int timeStepInSec = 5;
            foreach (var file in filesToRead)
            {
                double percentageDone = fileCounter / filesToRead.Count();
                //Console.WriteLine("Reading reports. {0:0.00}% done.", percentageDone * 100.0);
                double elapsedTime = watch.Elapsed.TotalSeconds;
                double expectedTimeOfCompletion = elapsedTime / percentageDone;
                if (displayWatch.Elapsed.TotalSeconds > timeStepInSec)
                {
                    Console.Clear();
                    Console.WriteLine("Elapsed Time: {2:0.0} sec | ETA: {0:0.0}sec | Done:{1:0}%)", expectedTimeOfCompletion - elapsedTime, percentageDone * 100.0, elapsedTime);
                    displayWatch.Restart();
                }
                fileCounter++;
                var arr = file.Split(new char[] { '_', '.' });
                bool succes = false;
                DateTime date = DateTime.Now;
                foreach (var elem in arr)
                {
                    try
                    {
                        date = DateTime.ParseExact(elem, "yyyyMMdd",
                                                        System.Globalization.CultureInfo.InvariantCulture,
                                                        System.Globalization.DateTimeStyles.None);
                        succes = true;
                        break;
                    }
                    catch
                    {
                        succes = false;
                    }
                }
                if (!succes)
                {
                    Console.WriteLine("There are no report files!");
                    return;
                }
                if ((date - historicalAnalysiStartDate).Days < 0)
                    continue;

                Dictionary<string, Tuple<double, double, double>> dayData
                        = GetInformationFromDatedFile(file);
                ParseDayDataToDatedNav(dayData, date);
            }
        }

        private void PopulateToSummaryFile()
        {
            Dictionary<int, string> indexToMfMapping = new Dictionary<int, string>();
            int counter = 0;
            StringBuilder log = new StringBuilder();
            log.Append("Date,");
            foreach (var mf in distinctMutualFunds)
            {
                indexToMfMapping.Add(counter++, mf);
                log.Append(mf + ",");
            }
            log.AppendLine();
            var mfToIndexMapping = indexToMfMapping.ToDictionary(x => x.Value, x => x.Key);
            foreach (var date in mfHistoricalData.Keys.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day))
            {
                log.Append(date.ToString("yyyy-MM-dd") + ",");
                for (int i = 0; i < distinctMutualFunds.Count; i++)
                {
                    var mf = indexToMfMapping[i];
                    if (!mfHistoricalData[date].ContainsKey(mf))
                        log.Append("NA,");
                    else
                    {
                        var record = mfHistoricalData[date][mf];
                        log.AppendFormat("{0:0.00}|{1:0.00}|{2:0.000},", record.OriginalAmount, record.NetAmount, record.Nav);
                    }
                }
                log.AppendLine();
            }
            System.IO.File.WriteAllText(historicalFileName, log.ToString());
        }

        private Dictionary<string, Tuple<double, double, double>> GetInformationFromDatedFile(string file)
        {
            Dictionary<string, Tuple<double, double, double>> navByName = new Dictionary<string, Tuple<double, double, double>>();
            var lines = System.IO.File.ReadAllLines(file);
            for (int i = 0; i <= lines.Count() - 1; i++)
            {
                List<string> arr3 =
                    lines[i].Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (arr3.Count() < 6)
                    continue;
                try
                {
                    string name = "";
                    double nav = -1;
                    double netvalue = -1;
                    double origvalue = -1;
                    bool success = true;
                    string tempName = Program.ShortenName(arr3[0]);
                    double tempNav = 0, tempVal = 0, tempOrig = 0;
                    success &= double.TryParse(arr3[3], out tempNav);
                    success &= double.TryParse(arr3[5], out tempVal);
                    success &= double.TryParse(arr3[4], out tempOrig);
                    if (success)
                    {
                        name = tempName;
                        nav = tempNav;
                        netvalue = tempVal;
                        origvalue = tempOrig;
                        navByName.Add(name, Tuple.Create(nav, netvalue, origvalue));
                    }
                    distinctMutualFunds.Add(name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Something went wrong!");
                }
            }
            return navByName;
        }

        private void ParseDayDataToDatedNav(Dictionary<string, Tuple<double, double, double>> input, DateTime date)
        {
            if (input.Keys.Count < 1)
                return;
            if (mfHistoricalData.ContainsKey(date))
                return;
            if (date.Year <= 2015 && date.Month <= 10 && date.Day <= 10)
                return;
            mfHistoricalData.Add(date, new Dictionary<string, MfData>());
            foreach (var mf in input.Keys)
            {
                MfData data = new MfData();
                data.Nav = input[mf].Item1;
                data.NetAmount = input[mf].Item2;
                data.OriginalAmount = input[mf].Item3;
                mfHistoricalData[date].Add(mf, data);
            }
        }

        private void ReadDataFromSummaryFile()
        {
            if(!System.IO.File.Exists(historicalFileName))
            {
                PopulateDataToSummaryFile();
            }
            ClearPreviousData();
            var lines = System.IO.File.ReadAllLines(historicalFileName);
            var header = lines[0];
            var arr = header.Split(new char[] { ',' });
            Dictionary<int, string> indexToMfMapping = new Dictionary<int, string>();
            for (int i = 1; i < arr.Count(); i++)
            {
                indexToMfMapping.Add(i, arr[i]);
            }
            for (int i = 1; i < lines.Count(); i++)
            {
                var line = lines[i];
                arr = line.Split(new char[] { ',' });
                var date = DateTime.Parse(arr[0]);
                double totalOrigAmount = 0;
                double totalNetAmount = 0;
                for (int j = 1; j < arr.Count(); j++)
                {
                    var mf = indexToMfMapping[j];
                    var record = arr[j];
                    if (record == "NA")
                        continue;
                    var info = record.Split(new char[] { '|' });
                    if (info.Count() < 3)
                        continue;
                    double origAmount = double.Parse(info[0]);
                    double netAmount = double.Parse(info[1]);
                    double nav = double.Parse(info[2]);

                    totalOrigAmount += origAmount;
                    totalNetAmount += netAmount;
                    UpdateSingleEntry(date, mf, nav, origAmount, netAmount, false);
                }
                if(!Program.OrigValueForReturn.ContainsKey(date))
                    Program.OrigValueForReturn.Add(date, totalOrigAmount);
                if(!Program.NetValueForReturn.ContainsKey(date))
                    Program.NetValueForReturn.Add(date, totalNetAmount);
            }
            UpdateFromLatestRecord();
            Program.mfNames = new HashSet<string>(distinctMutualFunds);
        }

        private void ClearPreviousData()
        {
            Program.MfNavByDate = new Dictionary<DateTime, Dictionary<string, double>>();
            Program.MfOriginalAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>();
            Program.MfNetAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>();
            Program.OrigValueForReturn = new Dictionary<DateTime, double>();
            Program.NetValueForReturn = new Dictionary<DateTime, double>();
            mfHistoricalData = new Dictionary<DateTime, Dictionary<string, MfData>>();
        }

        private bool UpdateSingleEntry(DateTime date, string mf, double nav, double origAmount, double netAmount, bool enforceOverride)
        {
            if (!Program.MfNavByDate.ContainsKey(date))
            {
                Program.MfNavByDate.Add(date, new Dictionary<string, double>());
                Program.MfOriginalAmountByDate.Add(date, new Dictionary<string, double>());
                Program.MfNetAmountByDate.Add(date, new Dictionary<string, double>());
                mfHistoricalData.Add(date, new Dictionary<string, MfData>());
            }
            if (Program.MfNavByDate[date].ContainsKey(mf))
                return false;
            if(enforceOverride)
            {
                Program.MfNavByDate[date][mf] = nav;
                Program.MfOriginalAmountByDate[date][mf] = origAmount;
                Program.MfNetAmountByDate[date][mf]= netAmount;
                distinctMutualFunds.Add(mf);
                MfData mfData_temp = new MfData()
                {
                    Nav = nav,
                    NetAmount = netAmount,
                    OriginalAmount = origAmount,
                };
                mfHistoricalData[date][mf] = mfData_temp;
                return true;
            }
            Program.MfNavByDate[date].Add(mf, nav);
            Program.MfOriginalAmountByDate[date].Add(mf, origAmount);
            Program.MfNetAmountByDate[date].Add(mf, netAmount);
            distinctMutualFunds.Add(mf);
            MfData mfData = new MfData()
            {
                Nav = nav,
                NetAmount = netAmount,
                OriginalAmount = origAmount,
            };
            mfHistoricalData[date].Add(mf, mfData);
            return true;
        }

        internal void UpdateFromLatestRecord()
        {
            var curDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            bool enforceOverride = false;
            if (mfHistoricalData.ContainsKey(curDate))
            {
                enforceOverride = true;
            }
            bool updated = false;
            HashSet<string> mfInLatestAnalyzedData = new HashSet<string>();
            foreach (var t in Program.latestAnalyzedData)
            {
                if (UpdateSingleEntry(curDate, t.Item1, t.Item4, t.Item5, t.Item6, enforceOverride))
                    updated = true;
                mfInLatestAnalyzedData.Add(t.Item1);
            }
            UpdateForDeletion(mfInLatestAnalyzedData, curDate);
            if (updated)
                PopulateToSummaryFile();
        }

        private void UpdateForDeletion(HashSet<string> mfInLatestAnalyzedData, DateTime curDate)
        {
            foreach (var mf in distinctMutualFunds)
            {
                if (!mfInLatestAnalyzedData.Contains(mf) && mfHistoricalData[curDate].ContainsKey(mf))
                {
                    Program.MfNavByDate[curDate].Remove(mf);
                    Program.MfOriginalAmountByDate[curDate].Remove(mf);
                    Program.MfNetAmountByDate[curDate].Remove(mf);
                    distinctMutualFunds.Add(mf);
                    mfHistoricalData[curDate].Remove(mf);
                }
            }
        }

        internal Dictionary<string, Tuple<double, double, double>> GetValueForDateDiff(int dateDiff, out DateTime correspondingDate)
        {
            correspondingDate = historicalAnalysiStartDate;
            var lastDate = Program.MfNavByDate.Keys.Last();
            var filteredDates = Program.MfNavByDate.Keys.Where(d => (lastDate - d).Days >= dateDiff).OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            if(!filteredDates.Any())
                return null;    
            correspondingDate = filteredDates.Last();
            Dictionary<string, Tuple<double, double, double>> ret = new Dictionary<string, Tuple<double, double, double>>();
            foreach (var mf in Program.MfNavByDate[correspondingDate].Keys)
            {
                ret.Add(mf, Tuple.Create(Program.MfNavByDate[correspondingDate][mf], Program.MfNetAmountByDate[correspondingDate][mf], Program.MfOriginalAmountByDate[correspondingDate][mf]));
            }
            return ret;
        }
    }

    internal class MfData
    {
        internal double OriginalAmount;

        internal double NetAmount;

        internal double Nav;
    }
}
