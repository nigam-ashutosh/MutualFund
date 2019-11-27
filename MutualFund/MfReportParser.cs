//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MutualFund
//{
//    internal class MfReportParser
//    {
//        DateTime historicalAnalysiStartDate = new DateTime(1900, 1, 1);
//        public MfReportParser()
//        {

//        }

//        internal void RefreshInputFileFromDateFiles()
//        {
//            var directory = System.Environment.CurrentDirectory;
//            List<string> files = new List<string>();

//            files.AddRange(System.IO.Directory.GetFiles(directory).Where(f => f.Contains("Report_")));
//            foreach (var subDir in System.IO.Directory.GetDirectories(directory))
//            {
//                files.AddRange(System.IO.Directory.GetFiles(subDir).Where(f => f.Contains("Report_")));
//            }

//            Dictionary<DateTime, double> totalValue = new Dictionary<DateTime, double>();
//            Dictionary<DateTime, double> totalOrigValue = new Dictionary<DateTime, double>();
//            Dictionary<DateTime, double> impByDate = new Dictionary<DateTime, double>();

//            Dictionary<string, Tuple<double, double, double>> preNavByName = new Dictionary<string, Tuple<double, double, double>>();
//            Dictionary<string, Tuple<double, double, double>> curNavByName = new Dictionary<string, Tuple<double, double, double>>();
//            bool foundToday = false;
//            bool foundYesterday = false;

//            int minDayDiff = int.MaxValue;
//            string minDayDiffFile = string.Empty;

//            foreach (var file in files)
//            {
//                if (!file.Contains("Report_"))
//                    continue;
//                var arr = file.Split(new char[] { '_', '.' });
//                bool succes = false;
//                DateTime date = DateTime.Now;
//                foreach (var elem in arr)
//                {
//                    try
//                    {
//                        date = DateTime.ParseExact(elem, "yyyyMMdd",
//                                                        System.Globalization.CultureInfo.InvariantCulture,
//                                                        System.Globalization.DateTimeStyles.None);
//                        succes = true;
//                        break;
//                    }
//                    catch
//                    {
//                        succes = false;
//                    }
//                }
//                if (!succes)
//                {
//                    Console.WriteLine("There are no report files!");
//                    return;
//                }
//                if ((date - historicalAnalysiStartDate).Days < 0)
//                    continue;
//                var lines = System.IO.File.ReadAllLines(file);

//                var arr2 = lines[lines.Count() - 3].Split(new char[] { ' ' });
//                double value = 0;
//                int counter = 0;
//                double gain = 0;
//                while (!double.TryParse(arr2[counter++], out value)) ;
//                while (!double.TryParse(arr2[counter++], out gain)) ;

//                arr2 = lines[lines.Count() - 4].Split(new char[] { ' ' });
//                double value2 = 0;
//                counter = 0;
//                while (!double.TryParse(arr2[counter++], out value2)) ;

//                if (!totalValue.ContainsKey(date))
//                {
//                    totalValue.Add(date, 0);
//                    totalOrigValue.Add(date, 0);
//                    impByDate.Add(date, double.MinValue);
//                }
//                totalValue[date] = Math.Max(totalValue[date], value);
//                totalOrigValue[date] = Math.Max(totalOrigValue[date], value2);
//                impByDate[date] = Math.Max(impByDate[date], gain);
//                if (detailedAnalysisForMFs || detailedGraph)
//                {
//                    if ((DateTime.Now - date).Days == 0)
//                    {
//                        curNavByName = dayData;
//                        foundToday = true;
//                    }
//                    if ((DateTime.Now - date).Days == interval)
//                    {
//                        preNavByName = dayData;
//                        foundYesterday = true;
//                    }
//                    if (foundToday && foundYesterday)
//                    {
//                        //break;
//                    }
//                    if ((DateTime.Now - date).Days != 0
//                        && (DateTime.Now - date).Days != interval
//                        && (DateTime.Now - date).Days > interval
//                        && minDayDiff > (DateTime.Now - date).Days)
//                    {
//                        minDayDiff = (DateTime.Now - date).Days;
//                        minDayDiffFile = file;
//                    }
//                }
//            }
//        }

//        private Dictionary<string, Tuple<double, double, double>> GetInformationFromDatedFile(string file)
//        {
//            Dictionary<string, Tuple<double, double, double>> navByName = new Dictionary<string, Tuple<double, double, double>>();
//            var lines = System.IO.File.ReadAllLines(file);
//            for (int i = 0; i <= lines.Count() - 1; i++)
//            {
//                List<string> arr3 =
//                    lines[i].Split(new string[] { "  " }, StringSplitOptions.RemoveEmptyEntries).ToList();
//                if (arr3.Count() < 6)
//                    continue;
//                try
//                {
//                    string name = "";
//                    double nav = -1;
//                    double netvalue = -1;
//                    double origvalue = -1;
//                    bool success = true;
//                    string tempName = Program.ShortenName(arr3[0]);
//                    double tempNav = 0, tempVal = 0, tempOrig = 0;
//                    success &= double.TryParse(arr3[3], out tempNav);
//                    success &= double.TryParse(arr3[5], out tempVal);
//                    success &= double.TryParse(arr3[4], out tempOrig);
//                    if (success)
//                    {
//                        name = tempName;
//                        nav = tempNav;
//                        netvalue = tempVal;
//                        origvalue = tempOrig;
//                        navByName.Add(name, Tuple.Create(nav, netvalue, origvalue));
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex.StackTrace);
//                    Console.WriteLine("Something went wrong!");
//                }
//            }
//            return navByName;
//        }

//        internal void UpdateForLatestReport()
//        {

//        }

//        internal void ParseData()
//        {

//        }

//    }
//}
