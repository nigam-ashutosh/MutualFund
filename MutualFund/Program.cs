using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Windows.Forms;

namespace MutualFund
{
    public static class Program
    {
        
        static string webURL = "https://www.amfiindia.com/spages/NAVAll.txt?t=20122017043431";

        static Dictionary<string, double> latesNAVByName = new Dictionary<string, double>();
        static Dictionary<int, Tuple<DateTime, double>> latestNAVByNameDateAndCode = new Dictionary<int, Tuple<DateTime, double>>();
        static Dictionary<int, string> mfCodeNameMapping = new Dictionary<int, string>();
        static Dictionary<string, double> latesValueByName = new Dictionary<string, double>();
        static DateTime latestDate = new DateTime();
        static Dictionary<string, Dictionary<double, double>> existingAmount
            = new Dictionary<string, Dictionary<double, double>>();
        static Dictionary<string, List<Tuple<DateTime, double>>> dividends;
        static Dictionary<string, DateTime> schemeUpdate = new Dictionary<string, DateTime>();
        static  HistoricalDataStore HistoricalDataStore;
        //internal static HistoricalNavStore HistoricalNavStore;
        static void Main(string[] args)
        {
            //Sensex.GetSensexData();
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Blue;

            UpdateNameChangeMapping();

            DoAnalysis();

            Console.WriteLine("\nPress any key for more options!");
            Console.ReadLine();
            int attempt = 0;
            while (true)
            {
                ClearScreen();
                Console.WriteLine("\n\nMenu:\n");

                Console.WriteLine("Analysis:");
                Console.WriteLine("1:\tAnalyze Mutual Fund");
                Console.WriteLine("2:\tCompare past two days");
                Console.WriteLine("3:\tHistorical Analysis");

                var origColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("4:\tPrint Return Graph");
                Console.ForegroundColor = origColor;

                Console.WriteLine("5:\tCalculate Return and correlation analysis");
                Console.WriteLine("6:\tRealized Gain Analysis");

                Console.WriteLine("\nModification:");
                Console.WriteLine("7:\tAdd New Mutual Fund");
                Console.WriteLine("8:\tDelete Mutual Fund");
                Console.WriteLine("9:\tModify Mutual Fund Data");
                Console.WriteLine("10:\tUpdate Dividend Payout(s)");
                

                Console.WriteLine("\nUtility:");
                Console.WriteLine("11:\tGenerate Report in Excel");
                Console.WriteLine("12:\tRefresh Mutual Fund Data");
                Console.WriteLine("13:\tUpdate Risk Free Interest Rate");
                Console.WriteLine("14:\tCopy .exe to Box Folder");
                Console.WriteLine("15:\tOpen Folder in Explorer");
                
                //Console.WriteLine("10:\tReplace Mutual Fund Name");
                Console.WriteLine("\n0:\tExit (Use this option to close not the windows one)\n");
               

                Console.Write("Selection:  ");
                bool exit = false;
                int ret = -1;
                bool success = int.TryParse(Console.ReadLine(), out ret);
                Console.WriteLine();
                if (!success || ret < 0 || ret > 15)
                {
                    Console.WriteLine("Wrong entry, please try again");
                    if (++attempt > 3)
                    {
                        Console.WriteLine("Exceeded maximum trials");
                        break;
                    }
                    else
                        continue;
                }
                attempt = 0;
                if(ret > 1 && ret < 7 && HistoricalDataStore == null)
                    HistoricalDataStore = new HistoricalDataStore();
                switch (ret)
                {
                    //Analysis
                    case 1:
                        DoAnalysis(false);
                        break;
                    case 2:
                        HistoricalAnalysis(true);
                        break;
                    case 3:
                        HistoricalAnalysis();
                        break;
                    case 4:
                        
                        ////ClearScreen();
                        ////HistoricalAnalysis(false, true, true);
                        ////OpenExcel();
                        Console.WriteLine("**Graph analysis not working due to lack of sensex data!\n\n");
                        Console.ReadLine();

                        continue;
                    case 5:
                        CalculateReturns();
                        break;
                    case 6:
                        ProcessRealizedGains();
                        break;

                    //Modification
                    case 7:
                        AddMutualFundInteractive(latesNAVByName);
                        break;
                    case 8:                         
                        DeleteMF();
                        break;
                    case 9:
                        ModifyMFAmount();
                        break;
                    case 10:
                        UpdatePayout();
                        break;


                    //Utility
                    case 11:
                        CreateReportExcel();
                        break;
                    case 12:
                        GetMutualFundLatestData(true);
                        doNotAskForOrder = true;
                        DoAnalysis(false);
                        doNotAskForOrder = false;
                        break;
                    case 13:
                        UpdateRiskFreeInterestRate();
                        break;

                    case 14:
                        PublishExeFile();
                        break;

                    case 15:
                        System.Diagnostics.Process.Start(System.Environment.CurrentDirectory);
                        break;

                    default:
                        exit = true;
                        break;
                }
                if (exit)
                    break;
                
                Console.WriteLine("Press any key to continue");
                Console.ReadLine();
                ClearScreen();
            }
            Console.WriteLine("Will automatically exit in 5 seconds!");
            System.Threading.Thread.Sleep(5000);            
            //Console.ReadLine();
        }
        private static void DoAnalysis(bool firstTime= true)
        {
            if(firstTime)
                GetMutualFundLatestData();
            if ((DateTime.Now - latestDate).Days > 1)
                GetMutualFundLatestData(true);
            Analysis(firstTime);
        }

        private static void ReadInput()
        {
            string[] lines2 = null;
            try
            {
                lines2 = System.IO.File.ReadAllLines("input.csv");
            }
            catch
            {
                Console.WriteLine("You don't have any data please add data with menu");
                bool success = AddMutualFundInteractive(latesNAVByName);
                if (!success)
                {
                    Console.WriteLine("No funds exists. Exiting application");
                    Console.ReadLine();
                    return;
                }
                lines2 = System.IO.File.ReadAllLines("input.csv");
            }
            existingAmount = new Dictionary<string, Dictionary<double, double>>();
            bool riskFreeRateFound = false;
            foreach (var line in lines2)
            {
                var content = line.Split(new char[] { ',' });
                if(line.Contains("Risk Free Interest Rate"))
                {
                    double.TryParse(content[1], out RiskFreeInterestRate);
                    RiskFreeInterestRate /= 100;
                    riskFreeRateFound = true;
                    continue;
                }
                string name = content[0];
                double origValue = double.Parse(content[1]);
                double origNav = double.Parse(content[2]);
                if (!existingAmount.ContainsKey(name))
                {
                    existingAmount.Add(name, new Dictionary<double, double>());
                }
                if (!existingAmount[name].ContainsKey(origNav))
                {
                    existingAmount[name].Add(origNav, origValue);
                }
            }
            if(!riskFreeRateFound)
            {
                Console.WriteLine("Using Default Risk free return value = {0}%", RiskFreeInterestRate * 100);
                Console.WriteLine("Press enter to continue!", RiskFreeInterestRate * 100);
                Console.ReadLine();
            }
        }
        public static string separator = Repeat("=", 150) + "\n";

        /// <summary>
        /// Item1: name
        /// Item2: Units
        /// Item3: Orig NAV
        /// Item4: Cur NAV
        /// Item5: Inv Value
        /// Item6: Cur Value
        /// Item7: Change
        /// </summary>
        public static List<Tuple<string, double, double, double, double, double, double, Tuple<DateTime>>> latestAnalyzedData;

        /// Item1: Total Orig
        /// Item2: Total current
        static Tuple<double, double, double, double> latestAggData;

        static double totalDividents;

        /// <summary>
        /// Item1: name
        /// Item2: Units
        /// Item3: Orig NAV
        /// Item4: Cur NAV
        /// Item5: Inv Value
        /// Item6: Cur Value
        /// Item7: Change
        /// </summary>
        public static List<Tuple<string, double, double, double, double, double, double, Tuple<DateTime>>> Analysis(bool first, bool forEmail = false)
        {
            ReadInput();
            ProcessRealizedGains(displayDetails: false);
            latestAnalyzedData = new List<Tuple<string, double, double, double, double, double, double, Tuple<DateTime>>>();
            double totalValue = 0;
            double totalOrig = 0;
            totalDividents = 0;
            StringBuilder missingDataLog = new StringBuilder();
            foreach (var name in existingAmount.Keys)
            {
                double currentnav = 0;

                if (latesNAVByName.ContainsKey(name))
                {
                    currentnav = latesNAVByName[name];
                }
                else
                {
                    missingDataLog.AppendFormat("{0}\n", name);
                    continue;
                }
                double totalMfValue = 0;
                double totalUnits = 0;
                foreach (var origNav in existingAmount[name].Keys)
                {
                    double origValue = existingAmount[name][origNav];
                    double units = origValue / origNav;

                    totalUnits += units;
                    totalMfValue += origValue;
                }
                double value = currentnav * totalUnits;

                var divValue = GetTotalValueFromDividends(name);

                if (divValue > 0)
                { 
                    value += divValue;
                    totalDividents += divValue;
                }
                double avgOrigNav = totalMfValue / totalUnits;
                double change = totalMfValue != 0 ? (value - totalMfValue) * 100 / totalMfValue : 0;
                string newName = ShortenName(name);
                var modifiedDate = schemeUpdate[name];
                var t = Tuple.Create(newName, totalUnits, avgOrigNav, currentnav, totalMfValue, value, change, modifiedDate);
                latestAnalyzedData.Add(t);

                totalValue += value;
                totalOrig += totalMfValue;

                if (!latesValueByName.ContainsKey(name))
                    latesValueByName.Add(name, value);
            }
            if(missingDataLog.Length > 0)
            {
                Console.WriteLine("Missing data for following mutual fund(s): (the names might have changed, please check the log)\n\n");
                Console.WriteLine(missingDataLog.ToString());
                Console.ReadLine();
            }
            //totalValue += netRealizedGains;
            double netChange = totalOrig != 0 ? (totalValue - totalOrig) * 100 / totalOrig : 0;
            StringBuilder sb = new StringBuilder();
            latestAggData = Tuple.Create(totalOrig, totalValue, netChange, netRealizedGains);

            if(!forEmail)
                DisplayAnalyzedData(first);

            return latestAnalyzedData;
        }
        private static void OrderAnalyzedData(bool firstTime)
        {
            SortReportEnum sort = SortReportEnum.CurrentAmount;
            Dictionary<int, SortReportEnum> enumDic = new Dictionary<int, SortReportEnum>();
            int i = 0;            
            if (!firstTime && !doNotAskForOrder)
            {
                Console.WriteLine("Sort results by:\n");
                foreach (SortReportEnum e in Enum.GetValues(typeof(SortReportEnum)))
                {
                    enumDic.Add(i, e);
                    Console.WriteLine("{0}:\t{1}", ++i, e);
                }
                int ret = -1;
                int.TryParse(Console.ReadLine(), out ret);
                if (ret <= 0 || ret > i)
                    Console.WriteLine("error in input!");
                else
                    sort = enumDic[ret -1];
            }
            else if(doNotAskForOrder)
            {
                sort = SortReportEnum.Name;
            }
            switch (sort)
            {
                case SortReportEnum.InvestmentAmount:
                    latestAnalyzedData = latestAnalyzedData.OrderByDescending(t => t.Item5).ToList();
                    break;
                case SortReportEnum.CurrentAmount:
                    latestAnalyzedData = latestAnalyzedData.OrderByDescending(t => t.Item6).ToList();
                    break;
                case SortReportEnum.Name:
                    latestAnalyzedData = latestAnalyzedData.OrderBy(t => t.Item1).ToList();
                    break;
                case SortReportEnum.RelativeChange:
                    latestAnalyzedData = latestAnalyzedData.OrderByDescending(t => t.Item7).ToList();
                    break;
                case SortReportEnum.AbsChange:
                    latestAnalyzedData = latestAnalyzedData.OrderByDescending(t => t.Item6 - t.Item5).ToList();
                    break;
                case SortReportEnum.Units:
                    latestAnalyzedData = latestAnalyzedData.OrderByDescending(t => t.Item2).ToList();
                    break;
                default:
                    break;
            }

        }
        static bool doNotAskForOrder = false;
        private static void DisplayAnalyzedData(bool first)
        {
            ClearScreen();
            OrderAnalyzedData(first);
            ClearScreen();
            StringBuilder output = new StringBuilder();
            var origColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkCyan;

            var sortedNames = existingAmount.OrderByDescending(x => x.Value.Sum(y => y.Value)).ThenBy(x => x.Key).Select(x => x.Key);

            latestDate = schemeUpdate.Where(x => sortedNames.Contains(x.Key)).Max(x => x.Value);
            string str = string.Format("Report created on {0:yyyy/MM/dd} [NAV latest from {1:yyyy/MM/dd}]\n\n",
                DateTime.Now, latestDate);
            AppendAndPrint(output, str);
            AppendAndPrint(output, separator);
            AppendAndPrint(output, Header() + "\n");
            AppendAndPrint(output, separator);
            foreach (var t in latestAnalyzedData)
            {
                string log = string.Format("{0}{1:0.000}{2:0.000}{3:0.000}{4:0.00}{5:0.00}{6:0.00}{7}\n",
                    DoSpacing(t.Item1, 40),
                    DoSpacing(t.Item2, 15, 3),
                    DoSpacing(t.Item3, 15, 3),
                    DoSpacing(t.Item4, 15, 3),
                    DoSpacing(t.Item5, 20, 2),
                    DoSpacing(t.Item6, 20, 2),
                    DoSpacing(t.Item7, 20, 2),
                    t.Rest.Item1.ToString("dd-MMM"));
                AppendAndPrint(output, log, true, t.Item7 >= 0);
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(separator);
            sb.AppendFormat("{0}{1}", DoSpacing("Net Original value", 40), DoSpacing(latestAggData.Item1, 105, 2));
            AppendAndPrint(output, "\n" + sb.ToString());
            sb.Clear();
            sb.AppendFormat("{0}{1}", DoSpacing("Net Current value", 40), DoSpacing(latestAggData.Item2, 15, 2));
            double diff = latestAggData.Item2 - latestAggData.Item1;
            string sign = diff >= 0 ? "+" : "";
            string diffText = sign.ToString() + diff.ToString("N" + 2);
            sb.AppendFormat("{0}", DoSpacing(diffText, 15));
            double effRealizedGain = netRealizedGains - netRealizedDivGains;
            double netTotalGain = diff + effRealizedGain;
            sb.AppendFormat("{0}{1}{2}{3}\n", 
                                DoSpacing("RealizedGain", 15), 
                                DoSpacing((effRealizedGain > 0 ? "+" : "") + effRealizedGain.ToString("N2"), 20),
                                DoSpacing("TotalGain", 20),
                                DoSpacing((netTotalGain > 0 ? "+" : "") + netTotalGain.ToString("N2"), 20));
            sb.AppendFormat("{0}{1}", DoSpacing("Net Change(%)", 40), DoSpacing(latestAggData.Item3, 15, 2));

            AppendAndPrint(output, "\n" + sb.ToString(), true, latestAggData.Item3 > 0);
            AppendAndPrint(output, "\n" + separator);
            sb.Clear();
            Console.ForegroundColor = origColour;

            string folder = string.Format("Reports\\{0}\\{1}_{0}", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MMM"));
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string fileName = string.Format("Report_{0}.txt", DateTime.Now.ToString("yyyyMMdd"));
            fileName = System.IO.Path.Combine(folder, fileName);

            System.IO.File.WriteAllText(fileName, output.ToString());

            origColour = Console.ForegroundColor;
            if (diff >= 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("The portfolio is in net gain!!\n\n");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The portfolio is in net loss!!\n\n");
            }
            Console.ForegroundColor = origColour;
        }
        private static void AppendAndPrint(StringBuilder sb, string log, bool colored = false, bool profit = true)
        {
            var origColour = Console.ForegroundColor;
            sb.Append(log);
            if (colored)
            {
                
                if(profit)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else
                    Console.ForegroundColor = ConsoleColor.Red;
            }
            //replace the old names with new names only for console printing
            log = ReplaceOldNameWithNewNameOnDisplay(log);
            Console.Write(log);
            Console.ForegroundColor = origColour;
        }

        private static string ReplaceOldNameWithNewNameOnDisplay(string log)
        {
            foreach (var newName in newNameToOldNameMapping.Keys)
            {
                var oldName = newNameToOldNameMapping[newName];
                var oldShortenName = ShortenName(oldName);
                var newShortenName = ShortenName(newName);
                if (log.Contains(oldShortenName))
                {
                    int extraSpace = newShortenName.Length - oldShortenName.Length;
                    int absExtraSpace = Math.Abs(extraSpace);
                    if (extraSpace > 0)
                        for (int i = 0; i < absExtraSpace; i++)
                        {
                            oldShortenName += " ";
                        }
                    else if (extraSpace < 0)
                        for (int i = 0; i < absExtraSpace; i++)
                        {
                            newShortenName += " ";
                        }
                    log = log.Replace(oldShortenName, newShortenName);
                }
            }
            return log;
        }

        private static void GetMutualFundLatestData(bool fromWeb = false)
        {
            bool fileSucces = false;
            bool webSuccess = false;
            string webData = string.Empty;
            if (!fromWeb)
            {
                try
                {
                    webData = System.IO.File.ReadAllText("LatestNavData.txt");
                    if (webData.Count() > 0)
                        fileSucces = true;
                }
                catch
                {
                    Console.WriteLine("Error reading from file. Downloading from web!");
                }
            }            
            if (!fileSucces)
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                Console.WriteLine("Downloading content from web....");
                try
                {
                    System.IO.File.Delete("LatestNavData.txt");
                    wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    webURL += "?";
                    webURL += Guid.NewGuid().ToString();
                    webData = wc.DownloadString(webURL);
                    webSuccess = true;
                    Console.WriteLine("Downloading completed successfully!!");
                    System.IO.File.WriteAllText("LatestNavData.txt", webData);
                }
                catch
                {
                    Console.WriteLine("Error in connecting to web, please check network and retry");
                }
                
            }
            else
            {
                Console.WriteLine("Read data from the file!");
            }
            if (!fileSucces && !webSuccess)
            {
                Console.WriteLine("Data import issue! Press any key to continue");
                Console.ReadLine();
                return;
            }
            #region Process Data()
           
            var lines = webData.Split(new char[] { '\n', '\r' });
            int counter = 0;
            StringBuilder log = new StringBuilder();

            log.AppendFormat("All Mutual Fund NAV as on {0}\n\n", DateTime.Now.ToString());

            int ctr = 0;
            foreach (var line in lines)
            {
                ctr++;

                try
                {
                    var content = line.Split(new char[] { ';' });
                    if (content.Count() < 3)
                        continue;
                    counter++;
                    if (counter < 2)
                        continue;
                    double value = 0;
                    bool success = double.TryParse(content[4], out value);
                    if (!success)
                        continue;
                    var name = content[3];

                    if (newNameToOldNameMapping.ContainsKey(name))
                        name = newNameToOldNameMapping[name];
                    log.AppendFormat("{0}\t{1}\n", name, value);
                    DateTime curDate = new DateTime();
                    success = DateTime.TryParse(content[5], out curDate);
                    int code;
                    int.TryParse(content[0], out code);
                    if (!latesNAVByName.ContainsKey(name))
                    {
                        latesNAVByName.Add(name, value);
                        if (success)
                        {
                            schemeUpdate.Add(name, curDate);
                            latestDate = new DateTime(Math.Max(latestDate.Ticks, curDate.Ticks));
                        }
                        if (!mfCodeNameMapping.ContainsKey(code))
                        {
                            mfCodeNameMapping.Add(code, name);
                        }
                    }
                    
                    if (!latestNAVByNameDateAndCode.ContainsKey(code))
                    {
                        latestNAVByNameDateAndCode.Add(code, Tuple.Create(latestDate, value));
                    }
                }
                catch
                {
                    Console.WriteLine("Error in parsing line: "+ line);

                }
            }

            #endregion //--Process Data()

            //if (HistoricalNavStore == null)
            //{
            //    HistoricalNavStore = new HistoricalNavStore(latestNAVByNameDateAndCode, mfCodeNameMapping);
            //}
        }

        //key new name, value old name
        private static Dictionary<string, string> newNameToOldNameMapping; 

        private static void UpdateNameChangeMapping()
        {
            if (newNameToOldNameMapping != null)
                return;
            newNameToOldNameMapping = new Dictionary<string, string>();
            try
            {
                var file = "MfNameChange.csv";
                bool first = true;
                foreach (var line in System.IO.File.ReadLines(file))
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    var arr = line.Split(new char[] { ',' });
                    var oldName = arr[0];
                    var newName = arr[1];
                    newNameToOldNameMapping.Add(newName, oldName);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("!!Error: " + ex.Message);
                Console.WriteLine("To continue without this input press enter!");
                Console.ReadLine();
            }
        }
        
        
        private static string Repeat(string orig, int maxWidth)
        {
            string rv = string.Empty;
            if (orig.Length > maxWidth)
                return orig;

            int counter = maxWidth / orig.Length;
            int remainder = maxWidth % orig.Length;
            for (int i = 0; i < counter; i++)
            {
                rv += orig;
            }
            rv += orig.Substring(0, remainder);
            return rv;
        }
        public static string DoSpacing(object original1, int maxWidth, int decimalPlace = -1)
        {
            string rv = string.Empty;

            string original = original1.ToString();

            bool success = false;
            double var = double.MinValue;
            success = double.TryParse(original, out var);

            if (var != double.MinValue && var != 0 && decimalPlace != -1)
            {
                int digitsBeforeDecimal = (int) Math.Log10(Math.Abs(var));
                int digitesAfterDecimal = Math.Min(decimalPlace, maxWidth - digitsBeforeDecimal);
                var = Math.Round(var, digitesAfterDecimal);
                original = var.ToString("N" + decimalPlace);
            }

            if (original.Length < maxWidth)
            {
                rv += original;
                for (int i = 0; i < maxWidth - original.Length; i++)
                {
                    rv += " ";
                }
            }
            else
            {
                rv = original.Substring(0, maxWidth - 1);
            }
            return rv;
        }
        private static string Header()
        {
            string rv = string.Empty;
            rv += DoSpacing("Mutual Fund Name", 40);
            rv += DoSpacing("Units", 15);
            rv += DoSpacing("Avg NAV", 15);
            rv += DoSpacing("Cur NAV", 15);
            rv += DoSpacing("Orig Val", 20);
            rv += DoSpacing("Cur Val", 20);
            rv += DoSpacing("%Change", 20);
            rv += "Latest NAV\n";
            return rv;
        } 
        public static void ClearScreen()
        {
            Console.Clear();
            PrintLogo();
        }
        private static void PrintLogo()
        {
            Console.WriteLine("*******************************************************************************");
            Console.WriteLine("*******************************************************************************");
            Console.WriteLine("**************THE SOFTWARE DEVELEOPED BY " + "(c)" + "ASHUTOSH NIGAM************************");
            Console.WriteLine("*******************************************************************************");
            Console.WriteLine("*******************************************************************************\n\n");
        }
        private static bool AddMutualFundInteractive(Dictionary<string, double> funds)
        {
            ClearScreen();
            
            bool success = false;
            Console.WriteLine("Adding a new Mutual Fund\n");
            try
            {
                int val2 = -1;
                bool success2 = false;

                HashSet<string> fundName = new HashSet<string>(funds.Keys);
                HashSet<string> filtered = new HashSet<string>(fundName);
                string foundName = string.Empty;
                while (true)
                {
                    Console.WriteLine("Give search string (0 to exit)(use ; to provide multiple search strings)");
                    string key = Console.ReadLine();

                    int val1 = -1;
                    success2 = int.TryParse(key, out val1);
                    if (success2 && val1 == 0)
                    {
                        if (filtered.Count < 50)
                            break;
                        else
                        {
                            Console.WriteLine("There are too many funds!");
                            Console.WriteLine("Press 0 to continue any other key to exit");
                            val1 = -1;
                            success2 = int.TryParse(Console.ReadLine(), out val1);
                            if (success2 && val1 == 0)
                            {
                                continue;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    string[] searchStrings = key.Split(new char[] { ';' });
                    HashSet<string> filteredThisIteration = new HashSet<string>(filtered);
                    foreach (var searchstring in searchStrings)
                    {
                        var searchstringtrim = searchstring.Trim();
                        if (searchstringtrim.Length < 2)
                            continue;
                        filteredThisIteration = new HashSet<string>(filteredThisIteration.Where(x => x.ToLower().Contains(searchstringtrim.ToLower())));
                    }
                    if (filteredThisIteration.Count() == 0)
                    {
                        Console.WriteLine("Wrong string please try again(press 0 to exit)");
                        Console.WriteLine("There are {0} matching funds", filteredThisIteration.Count());
                        continue;                        
                    }
                    Console.WriteLine("There are {0} matching funds", filteredThisIteration.Count());
                    filtered = new HashSet<string>(filteredThisIteration);                    
                }
                if (filtered.Count() == 1)
                {
                    foundName = filtered.FirstOrDefault();
                }
                else if (filtered.Count() > 1)
                {
                    Dictionary<int, string> dict = new Dictionary<int, string>();
                    for (int i = 0; i < filtered.Count(); i++)
                    {
                        dict.Add(i + 1, filtered.ElementAt(i));
                        string mf = filtered.ElementAt(i);
                        Console.WriteLine("{0}:\t{1}({2})", i + 1, mf, funds[mf]);
                    }
                    Console.WriteLine("Choose your selection");
                    int sel = int.Parse(Console.ReadLine());
                    foundName = dict[sel];
                }
                Console.WriteLine("{0} has latest NAV = {1} ", foundName, funds[foundName]);
                
                if (existingAmount.Count > 0 && existingAmount.ContainsKey(foundName))
                {
                    Console.WriteLine("Mutual fund already exist!\n");
                }
                Console.WriteLine("To add the selected MF press 0, else any other key");
                val2 = -1;
                success2 = int.TryParse(Console.ReadLine(), out val2);
                if (!success2 || val2 != 0)
                {
                    return false;
                }
                Console.Write("Invested Amount:  INR ");
                double amount = double.Parse(Console.ReadLine());
                //Console.WriteLine();
                Console.Write("Initial NAV value: ");
                SendKeys.SendWait(funds[foundName].ToString("N" + 5));
                double nav = double.Parse(Console.ReadLine());
                Console.WriteLine();
                string rv = string.Format("{0},{1},{2:0.000},{3:0.000}\n", foundName, amount, nav, amount * nav);
                System.IO.File.AppendAllText("input.csv", rv);
                success = true;
            }
            catch 
            {
                Console.WriteLine("The inputs were not in proper format, skippping the addition routine!");
            }

            return success;
        }
        private static void DeleteMF()
        {
            ClearScreen();
            ReadInput();
            try
            {
                Console.WriteLine("To delete a MF press 0, else any other key");
                int val2 = -1;
                bool success2 = int.TryParse(Console.ReadLine(), out val2);
                if (!success2 || val2 != 0)
                {
                    return;
                }
                var lines = System.IO.File.ReadAllLines("input.csv");
                StringBuilder sb = new StringBuilder();

                string mf = GetMutualFundSelection();

                Console.WriteLine("Selected {0} to delete, to continue press 0 else any other key!", mf);
                val2 = -1;
                success2 = int.TryParse(Console.ReadLine(), out val2);
                if (!success2 || val2 != 0)
                {
                    return;
                }

                foreach (var line in lines)
                {
                    if(line.Contains(mf))
                        continue;
                    sb.AppendLine(line);
                }
                Console.WriteLine();

                UpdatePayout(mf, true);
                System.IO.File.WriteAllText("input.csv", sb.ToString());
            }
            catch
            {
                Console.WriteLine("There is no file");
            }
        }

        private static string GetMutualFundSelection(bool forDivident = false)
        {
            int counter = 0;
            Dictionary<int, string> dict = new Dictionary<int, string>();
            foreach (var key in existingAmount.Keys)
            {
                if (forDivident && !key.ToLower().Contains("div"))
                    continue;
                dict.Add(++counter, key);
                Console.WriteLine("{0}\t{1}", counter, key);
            }
            Console.Write("Choose the MF by selecting corresponding number:");
            int trials = 0;
            string mf = string.Empty;

            while (true)
            {
                trials++;
                int ret = -1;
                bool parse = int.TryParse(Console.ReadLine(), out ret);
                if (!parse || !dict.ContainsKey(ret))
                {
                    Console.WriteLine("Wrong entry. Please choose again!");
                    if (trials > 3)
                    {
                        return null;
                    }
                    continue;
                }
                mf = dict[ret];
                return mf;
            }
        }
        private static void UpdatePayout(string mf = null, bool redemption = false)
        {
            ClearScreen();
            if (mf == null && !redemption)
                mf = GetMutualFundSelection(true);
            if (mf == null)
                return;
            DateTime redemptionDate = DateTime.Now.AddDays(-1);
            Console.WriteLine("Enter Y to provide the redemption date, else it will consider yesterday as redemption date!");
            if (Console.ReadLine().Trim().ToLower() == "Y")
            {
                int year = -1;
                int month = -1;
                int date = -1;
                Console.Write("Year of redemption/payout:");
                SendKeys.SendWait(redemptionDate.Year.ToString());
                int.TryParse(Console.ReadLine(), out year);
                SendKeys.SendWait(redemptionDate.Month.ToString());
                int.TryParse(Console.ReadLine(), out month);
                SendKeys.SendWait(redemptionDate.Date.ToString());
                int.TryParse(Console.ReadLine(), out date);
                if (year > 0 && month > 0 && date > 0)
                    redemptionDate = new DateTime(year, month, date);
               else
                    Console.WriteLine("Redemption date is {0} please change in the file", redemptionDate.ToString("yyyy-MMM-dd"));
            }
            var payoutStr = redemption ? "Redemption" : "Dividend";

            Console.WriteLine("Please provide net gain for the fund: ");
            double gain = double.MinValue;
            var success = double.TryParse(Console.ReadLine(), out gain);
            if (!success || gain <= double.MinValue)
            {
                Console.WriteLine("Incorrect data please retry later");
                Console.ReadLine();
                return;
            }

            StringBuilder newLine = new StringBuilder();
            newLine.AppendFormat("{0},{1},{2},{3}\n", ShortenName(mf), redemptionDate.ToString("dd-MMM-yy"), gain, payoutStr);
            System.IO.File.AppendAllText("RealizedGains.csv", newLine.ToString());
        }
        private static void ModifyMFAmount()
        {
            ClearScreen();
            ReadInput();
            try
            {
                Console.WriteLine("Modifying the MF information!");
                bool success2 = false;

                var lines = System.IO.File.ReadAllLines("input.csv");
                StringBuilder sb = new StringBuilder();
                Dictionary<int, string> dict = new Dictionary<int, string>();
                Dictionary<string, double> mfAmount = new Dictionary<string, double>();
                Dictionary<string, double> navDict = new Dictionary<string, double>();
                int counter = 0;
                foreach (var key in existingAmount.Keys)
                {
                    dict.Add(++counter, key);
                    double initialAmount = existingAmount[key].Sum(x => x.Value);
                    double nav = existingAmount[key].Average(x => x.Key);
                    double amount = initialAmount / nav;
                    mfAmount.Add(key, amount);
                    navDict.Add(key, nav);
                    Console.WriteLine("{0}\t{1}", counter, key);
                }
                Console.Write("Choose the MF number to modify:");
                int trials = 0;
                string mf = string.Empty;

                while (true)
                {
                    trials++;
                    int ret = -1;
                    bool parse = int.TryParse(Console.ReadLine(), out ret);
                    if (!parse || !dict.ContainsKey(ret))
                    {
                        Console.WriteLine("Wrong entry. Please choose again!");
                        if (trials > 3)
                        {
                            return;
                        }
                        continue;
                    }
                    mf = dict[ret];
                    break;
                }
                
                Console.WriteLine("Selected {0} to modify, to confirm press 0 else any other key!", mf);
                
                double currentUnits = mfAmount[mf];
                double currentNav = navDict[mf];
                double newNAV = currentNav;
                double newAmount = currentUnits * currentNav;
                double newUnits = currentUnits;
                double newInvestment = 0;
                trials = 0;
                while (true)
                {
                    ClearScreen();
                    trials++;
                    Console.WriteLine("Chosen {0} to modify", mf);
                    Console.WriteLine("Current amount = {0}", currentUnits * currentNav);
                    Console.WriteLine("Current NAV = {0}", currentNav);
                    Console.WriteLine("Current number of units = {0}", currentUnits);
                    Console.WriteLine();
                    Console.WriteLine("NAV of new investment");
                    SendKeys.SendWait((latesNAVByName[mf]).ToString("N" + 5));
                    double invNAV = 0;
                    success2 = double.TryParse(Console.ReadLine(), out invNAV);
                    Console.Write("New investment (give minus for disinvestment)= ");
                    double tempAmount = newAmount;
                    double tempNAV = newNAV;
                    double tempUnits = newUnits;
                    success2 = double.TryParse(Console.ReadLine(), out newInvestment);
                    if (!success2 && newInvestment <= 0)
                    {
                        Console.WriteLine("There is something wrong with entry, please try again!");
                        if (trials >= 3)
                            Console.WriteLine("Exceeded max trials, please start over again");
                        continue;
                    }
                    tempAmount += newInvestment;
                    tempUnits += (newInvestment / invNAV);
                    tempNAV = tempAmount / tempUnits;

                    tempAmount = Math.Round(tempAmount, 3);
                    tempUnits = Math.Round(tempUnits, 3);
                    tempNAV = Math.Round(tempNAV, 3);

                    Console.WriteLine("Old amount: {0}\tUnits : {1}\tNAV: {2}", newAmount, newUnits, newNAV);
                    Console.WriteLine("New amount: {0}\tUnits : {1}\tNAV: {2}", tempAmount, tempUnits, tempNAV);
                    Console.WriteLine("To confirm press 1 else any other key");
                    int ret = 0;
                    bool success3 = int.TryParse(Console.ReadLine(), out ret);
                    if (success3 && ret == 1)
                    {
                        newAmount = tempAmount;
                        newNAV = tempNAV;
                        newUnits = tempUnits;
                        Console.WriteLine("Update the information!");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Failed to update! to exit press 0");
                        Console.WriteLine("To confirm press 1 else any other key");
                        ret = -1;
                        success3 = int.TryParse(Console.ReadLine(), out ret);
                        if (success3 && ret == 0)
                        {
                            return;
                        }
                        continue;
                    }
                }                
                bool alreadyModified = false;
                foreach (var line in lines)
                {
                    if (line.Contains(mf) && !alreadyModified)
                    {
                        sb.Append(string.Format("{0},{1},{2:0.000},{3:0.000}", mf, newAmount, newNAV, newUnits));
                        sb.AppendLine();
                        alreadyModified = true;
                        continue;
                    }
                    sb.AppendLine(line);
                }                
                System.IO.File.Delete("input.csv");
                System.IO.File.AppendAllText("input.csv", sb.ToString());
            }
            catch
            {
                Console.WriteLine("There is no file");
            }
        }
        private static bool ContainsString(string string1, string string2)
        {
            bool rv = false;

            string lookintosmall = string1.ToLower();
            string lookupsmall = string2.ToLower();
            rv = lookintosmall.Contains(lookupsmall) || lookupsmall.Contains(lookintosmall);
            return rv;
        }

        public static Dictionary<string, List<string>> shortNameToFullNameMapping = new Dictionary<string, List<string>>();
        static Dictionary<string, string> fullNameToShortNameMapping = new Dictionary<string, string>();
        public static string ShortenName(string name)
        {
            name = name.Replace("(Previously Magnum NRI - LTP Upto 22/11/09)", "");
            if (fullNameToShortNameMapping.ContainsKey(name))
                return fullNameToShortNameMapping[name];
            if (shortNameToFullNameMapping.ContainsKey(name))
                return name;
            var arr = name.Split(new char[] { ' ' });
            StringBuilder newName = new StringBuilder();
            int subcounter = 0;
            foreach (var elem in arr)
            {
                if (elem.Length < 2)
                    continue;
                subcounter++;
                if (subcounter < 3)
                {
                    newName.Append(elem.Substring(0, Math.Min(3, elem.Length)) + " ");
                    continue;
                }
                

                if (ContainsString(elem, "fund"))
                    continue;
                if (ContainsString(elem, "plan"))
                    continue;
                if (elem.Contains("-"))
                    continue;
                if (ContainsString(elem, "direct"))
                {
                    newName.Append("D. ");
                    continue;
                }
                if (ContainsString(elem, "GROWTH"))
                {
                    newName.Append("(G) ");
                    continue;
                }
                if (ContainsString(elem, "REGULAR"))
                {
                    newName.Append("R. ");
                    continue;
                }
                if (ContainsString(elem, "DIVIDEND"))
                {
                    newName.Append("(Div.) ");
                    continue;
                }
                if (ContainsString(elem, "EQUIT"))
                {
                    newName.Append("Eq ");
                    continue;
                }
                if (ContainsString(elem, "compan"))
                {
                    newName.Append("co. ");
                    continue;
                }
                if (ContainsString(elem, "dynamic"))
                {
                    newName.Append("Dy. ");
                    continue;
                }
                if (ContainsString(elem, "option"))
                {
                    newName.Append("Op. ");
                    continue;
                }
                if (ContainsString(elem, "value"))
                {
                    newName.Append("Val. ");
                    continue;
                }
                if (ContainsString(elem, "natural"))
                {
                    newName.Append("Nat. ");
                    continue;
                }
                if (ContainsString(elem, "and"))
                {
                    newName.Append("& ");
                    continue;
                }
                if (ContainsString(elem, "new"))
                {
                    newName.Append("");
                    continue;
                }
                if (ContainsString(elem, "energy"))
                {
                    newName.Append("Egy.");
                    continue;
                }
                if (ContainsString(elem, "Resources"))
                {
                    newName.Append("Res.");
                    continue;
                }
                if (elem.Length > 4)
                {
                    newName.Append(elem.Substring(0, 3)+ ". ");
                    continue;
                }
                newName.Append(elem + " ");
            }
            var rv = newName.ToString().Trim();
            fullNameToShortNameMapping.Add(name, rv);
            if (!shortNameToFullNameMapping.ContainsKey(rv))
                shortNameToFullNameMapping.Add(rv, new List<string>());
            shortNameToFullNameMapping[rv].Add(name);
            return rv;
        }

        public static Dictionary<DateTime, Dictionary<string, double>> MfNavByDate = new Dictionary<DateTime, Dictionary<string, double>>();
        public static Dictionary<DateTime, Dictionary<string, double>> MfOriginalAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>();
        public static Dictionary<DateTime, Dictionary<string, double>> MfNetAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>();

        public static Dictionary<DateTime, double> OrigValueForReturn = new Dictionary<DateTime, double>();
        public static Dictionary<DateTime, double> NetValueForReturn = new Dictionary<DateTime, double>();

        static bool calculateReturn = false;
        static DateTime historicalAnalysiStartDate = new DateTime(1900, 1, 1);

        public static void SetHistoricalAnalysisStartDate(DateTime dateTime)
        {
            historicalAnalysiStartDate = dateTime;
        }
        public static void ResetHistoricalAnalysisStartDate()
        {
            historicalAnalysiStartDate = new DateTime(1900, 1, 1);
        }
        public static void HistoricalAnalysis(bool detailedAnalysisForMFs= false, bool graph = false, bool detailedGraph = false, bool printGraph= true)
        {
            //first read all inputs
            ClearScreen();
            
            int interval = -1;
            if (detailedAnalysisForMFs)
            {
                Console.WriteLine("Please select the interval:");
                Console.WriteLine("-N: Exact days");
                Console.WriteLine(" 1: Daily");
                Console.WriteLine(" 2: Weekly");
                Console.WriteLine(" 3: 15-days");
                Console.WriteLine(" 4: Monthly");
                Console.WriteLine(" 5: Quartely");
                Console.WriteLine(" 6: Half-Yearly");
                Console.WriteLine(" 7: Yearly");
                try
                {
                    int ret = int.Parse(Console.ReadLine());
                    if (ret == 1)
                        interval = 1;
                    if (ret == 2)
                        interval = 7;
                    if (ret == 3)
                        interval = 15;
                    if (ret == 4)
                        interval = 30;
                    if (ret == 5)
                        interval = 91;
                    if (ret == 6)
                        interval = 183;
                    if (ret == 7)
                        interval = 365;
                    if (ret < 0)
                    {
                        interval = -ret;
                    }
                }
                catch
                {
                    Console.WriteLine("Incorrect input");
                    return;
                }
                if(interval < 1)
                {
                    Console.WriteLine("Incorrect input");
                    return;
                }
                
            }
            ClearScreen();
            
            
            if (detailedAnalysisForMFs)
            {
                DateTime preDate, curDate;
                var preNavByName = HistoricalDataStore.GetValueForDateDiff(interval, out preDate);
                var curNavByName = HistoricalDataStore.GetValueForDateDiff(0, out curDate);
                if (preNavByName == null || curNavByName == null)
                {
                    Console.WriteLine("Today's and previous day's reports are not there to compare!");
                }
                else
                {
                    var names = curNavByName.Keys;//preNavByName.Keys.Union(curNavByName.Keys);
                    string val1 = string.Format("{0}", curDate.ToString("dd-MMM-yy"));
                    string val2 = string.Format("{0}", preDate.ToString("dd-MMM-yy"));
                    Console.Write("{0}{1}{2}{3}{4}{5}\n",
                        DoSpacing("Fund", 40),
                        DoSpacing(val2, 15), 
                        DoSpacing(val1, 15),
                        DoSpacing("Diff", 15),
                        DoSpacing("Gain (NAV) %", 20),
                        DoSpacing("Gain Annual (NAV) %", 20));
                    double totalValPre = 0;
                    double totalValPost = 0;
                    double totalValPreApprox = 0;
                    double totalValPostApprox = 0;
                    var origColor = Console.ForegroundColor;

                    List<Tuple<string, double, double, double, double, double, double>> changeValues = new List<Tuple<string, double, double, double, double, double, double>>();

                    double cumulativeDiff = 0;
                    foreach (var name in names)
                    {
                        double preNav = 0;
                        double preVal = 0;
                        double curNav = 0;
                        double curVal = 0;
                        double curUnits = 0;
                        if (curNavByName.ContainsKey(name))
                        {
                            curNav = curNavByName[name].Item1;
                            curVal = curNavByName[name].Item2;
                            curUnits = curVal / curNav;
                        }
                        bool exists = false;
                        if (preNavByName.ContainsKey(name))
                        {
                            preNav = preNavByName[name].Item1;
                            preVal = preNavByName[name].Item2;
                            exists = true;
                        }
                        else
                        {
                            var name2 = GetOldName(name, preNavByName.Keys.ToList(), out exists);
                            if (!exists)
                                continue;
                            preNav = preNavByName[name2].Item1;
                            preVal = preNavByName[name2].Item2;
                        }

                        double change = (curNav - preNav) / preNav;
                        double absDiff = curUnits * (curNav - preNav);

                        cumulativeDiff += absDiff;

                        changeValues.Add(Tuple.Create(name, curNav, preNav, change, curVal, preVal, absDiff));
                        
                        totalValPre += preVal;
                        totalValPost += curVal;
                        if (curNav > 0 && preNav > 0)
                        {
                            totalValPostApprox += curVal;
                            totalValPreApprox += (curVal * preNav / curNav);
                        }
                    }
                    changeValues = changeValues.OrderByDescending(t => t.Item4).ToList();
                    foreach (var tuple in changeValues)
                    {
                        var name = tuple.Item1;
                        var change = tuple.Item4;
                        var curNav = tuple.Item2;
                        var preNav = tuple.Item2;
                        var preVal = tuple.Item6;
                        var curVal = tuple.Item5;
                        var absDiff = tuple.Item7;
                        if (change > 0)
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                        else if (change < 0)
                            Console.ForegroundColor = ConsoleColor.Red;
                        else
                            Console.ForegroundColor = origColor;
                        double chgAnn = AnnualizeReturn(change, interval);

                        string changeStr
                            = curNav > 0 && preNav > 0 ?
                            (change > 0 ? "+" : "") + (change * 100).ToString("N" + 2) + "%" :
                                "    NA";
                        string changeAnnStr
                           = curNav > 0 && preNav > 0 ?
                           (chgAnn > 0 ? "+" : "") + chgAnn.ToString("N" + 2) + "%" :
                               "    NA";

                        Console.Write("{0}{1}{2}{3}{4}{5}\n",
                                ReplaceOldNameWithNewNameOnDisplay(DoSpacing(name, 40, 2)),
                                DoSpacing(preVal, 15, 0),
                                DoSpacing(curVal, 15, 0),
                                (absDiff > 0 ? "+" : "") + DoSpacing(absDiff, 15, 0),
                                DoSpacing(changeStr, 20),
                                DoSpacing(changeAnnStr, 20));
                    }
                    double netChange = (totalValPostApprox - totalValPreApprox) / totalValPreApprox;
                    Console.ForegroundColor = origColor;
                    Console.Write("\n" + separator);                    
                    if (netChange > 0)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    else if(netChange < 0)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else
                        Console.ForegroundColor = origColor;

                    double impAnn = AnnualizeReturn(netChange, interval);
                    Console.Write("{0}{1}{2}{3}{4}{5}\n",
                        DoSpacing("Net", 40),
                        DoSpacing(totalValPre, 15,  0),
                        DoSpacing(totalValPost, 15, 0),
                        (cumulativeDiff > 0 ? "+" : "") + DoSpacing(cumulativeDiff, 15, 0),
                        DoSpacing((netChange > 0 ? "+" : "") + (netChange * 100).ToString("N" + 2) + "%", 20),
                        DoSpacing((impAnn > 0 ? "+" : "") + impAnn.ToString("N" + 2) + "%", 20));
                    Console.ForegroundColor = origColor;
                    Console.Write(separator);
                }
                return;
            }
            var keys = NetValueForReturn.Keys.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Date);
            

            int count = keys.Count();
            int start = Math.Max(0, keys.Count() - count);
            Dictionary<DateTime, double> returnByDate = new Dictionary<DateTime, double>();
            if (!detailedAnalysisForMFs)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}{1}{2}{3}\n", 
                    DoSpacing("Date", 20), 
                    DoSpacing("Value", 20), 
                    DoSpacing("Gain", 15), 
                    DoSpacing("%Imp", 15));
                if (!graph)
                {
                    Console.WriteLine(sb.ToString());
                }
                for (int i = start; i < keys.Count(); i++)
                {
                    sb.Clear();
                    var date = keys.ElementAt(i);
                    double imp = (NetValueForReturn[date] - OrigValueForReturn[date]) * 100 / OrigValueForReturn[date];
                    string sign = imp <= 0 ? "" : "+";
                    sb.AppendFormat("{0}{1}{2}{3}",
                       DoSpacing(date.ToString("yyyy-MM-dd") + "(" + date.DayOfWeek.ToString().Substring(0, 3) + ")", 20),
                       DoSpacing(NetValueForReturn[date], 20, 2),
                       DoSpacing(NetValueForReturn[date] - OrigValueForReturn[date], 15, 2),
                       DoSpacing(sign + imp.ToString("N" + 2) + "%", 15));

                    ConsoleColor origColor = Console.ForegroundColor;
                    if (imp > 0)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    else if (imp < 0)
                        Console.ForegroundColor = ConsoleColor.Red;
                    if (!graph)
                    {
                        Console.WriteLine(sb.ToString());
                    }
                    Console.ForegroundColor = origColor;
                    if (graph && !returnByDate.ContainsKey(date))
                    {
                        returnByDate.Add(date, imp);
                    }
                }

            }
            if (graph)
            {
                try
                {
                    if (printGraph)
                    {
                        try
                        {
                            System.IO.File.Delete("Return.xlsx");
                        }
                        catch
                        {
                            Console.WriteLine("Error in close Return.xlsx");
                        }
                    }
                    returnByDate.OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month).ThenBy(x => x.Key.Day);
                    //ExcelGraph g = new ExcelGraph();
                    var detailedReturn = GetMFReturnByDate(returnByDate.Keys.ToList(), !detailedGraph);
                    //var detailedReturn2 = ProcessMFreturns(detailedReturn, returnByDate.Keys.ToList());
                    if (printGraph)
                    {
                        bool rv = false;
                        try
                        {
                            //rv = g.CreateGraph(returnByDate.Keys.ToList(), returnByDate.Values.ToList(), detailedReturn);
                            if (rv)
                            {
                                Console.WriteLine("Succesfully created graph!");
                            }
                            else
                            {
                                Console.WriteLine("Error while creating graph!");
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Error while creating graph!");
                        }
                    }                    
                }
                catch(Exception ex)
                {
                    Console.WriteLine("{0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
            
        }
        static double AnnualizeReturn(double r, int interval)
        {
            return (Math.Pow(1 + r, 365.0/interval) - 1)*100;
        }
        
        public static HashSet<string> mfNames = new HashSet<string>();
        static Dictionary<DateTime, Dictionary<string, double>> detailedReturns = null;
        public static Dictionary<string, Dictionary<DateTime, double>> DetailedReturnsByName = null;
        public static Dictionary<DateTime, Dictionary<string, double>> GetMFReturnByDate(List<DateTime> dates, bool skip)
        {
            if (skip)
                return null;
            if (detailedReturns != null)
                return detailedReturns;
            detailedReturns = new Dictionary<DateTime, Dictionary<string, double>>();
            DetailedReturnsByName = new Dictionary<string, Dictionary<DateTime, double>>();
            MfNavByDate = ProcessMFreturns(MfNavByDate, dates);
            var sortedDates = MfNavByDate.Keys.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            Dictionary<string, double> baseNav = new Dictionary<string, double>();
            foreach (var mf in mfNames)
            {
                baseNav.Add(mf, double.MinValue);
            }
            Dictionary<DateTime, Dictionary<string, double>> returnData = new Dictionary<DateTime, Dictionary<string, double>>();
            Dictionary<string, double> preNavMF = new Dictionary<string, double>();
            foreach (var date in sortedDates)
            {
                foreach (var mf in mfNames)
                {
                    if (!preNavMF.ContainsKey(mf))
                        preNavMF.Add(mf, double.MinValue);
                    double ret = double.MinValue;
                    double ret2 = double.MinValue;
                    if (MfNavByDate[date].ContainsKey(mf))
                    {
                        double curNav = MfNavByDate[date][mf];
                        if (baseNav[mf] > double.MinValue)
                        {                            
                            ret = (curNav - baseNav[mf]) * 100 / baseNav[mf];
                            ret2 = (curNav - preNavMF[mf])/ preNavMF[mf];
                            //ret2 = AnnualizeReturn(ret2, 1);
                            preNavMF[mf] = curNav;
                            //baseNav[mf] = curNav;
                        }
                        else
                        {
                            baseNav[mf] = curNav;
                            preNavMF[mf] = curNav;
                        }
                        if (ret != double.MinValue)
                        {
                            if (!returnData.ContainsKey(date))
                            {
                                returnData.Add(date, new Dictionary<string, double>());
                            }
                            if (!returnData[date].ContainsKey(mf))
                            {
                                returnData[date].Add(mf, ret);
                            }
                            //if (ret2 != 0)
                            {
                                var mf1 = ShortenName(mf);
                                if (!DetailedReturnsByName.ContainsKey(mf1))
                                {
                                    DetailedReturnsByName.Add(mf1, new Dictionary<DateTime, double>());
                                }
                                if (!DetailedReturnsByName[mf1].ContainsKey(date))
                                {
                                    //int days2 = (date - sortedDates.First()).Days;
                                    //ret = Math.Pow(ret, (365 / days2)) - 1;
                                    DetailedReturnsByName[mf1].Add(date, ret2);
                                }
                            }
                        }
                    }
                }
            }
            //DetailedReturnsByName = new Dictionary<string, Dictionary<DateTime, double>>(returnData);
            return returnData;
        }

        static Dictionary<DateTime, Dictionary<string, double>> ProcessMFreturns(Dictionary<DateTime, Dictionary<string, double>> input, List<DateTime> dates, bool groupSimilar = false)
        {
            HashSet<string> currentMfNames = new HashSet<string>(existingAmount.Keys.Select(x => ShortenName(x)));

            dates = dates.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
            dates.Reverse();

            //transform data
            Dictionary<string, Dictionary<DateTime, double>> unprocessed = new Dictionary<string, Dictionary<DateTime, double>>();
            foreach (var date in input.Keys)
            {
                if (date.Year == 2019 && date.Month == 11 && date.Day == 25)
                { }
                foreach (var name in input[date].Keys)
                {
                    var name2 = GetChangedName(name);
                    double nav = input[date][name];
                    if (!unprocessed.ContainsKey(name2))
                        unprocessed.Add(name2, new Dictionary<DateTime, double>());
                    if (!unprocessed[name2].ContainsKey(date))
                        unprocessed[name2].Add(date, nav);
                }
            }
            Dictionary<string, Dictionary<DateTime, double>> rv = new Dictionary<string, Dictionary<DateTime, double>>();
            HashSet<string> notFoundNamesAll = new HashSet<string>();
            HashSet<string> notFoundNamesCurrent = new HashSet<string>();
            HashSet<string> foundNames = new HashSet<string>();
            
            foreach (var name in unprocessed.Keys)
            {
                if (name == "Rel Mon Mar. Op.")
                { }
                if (!currentMfNames.Contains(name))
                {
                    notFoundNamesAll.Add(name);
                    continue;
                }
                if (AllDateExists(dates, unprocessed[name].Keys.ToList()))
                {
                    rv.Add(name, new Dictionary<DateTime, double>());
                    rv[name] = new Dictionary<DateTime, double>(unprocessed[name]);
                    foundNames.Add(name);
                    continue;
                }
                notFoundNamesCurrent.Add(name);
            }
            for (int i = 0; i < notFoundNamesCurrent.Count; i++)
            {
                var name1 = notFoundNamesCurrent.ElementAt(i);
                if (foundNames.Contains(name1))
                    continue;
                Dictionary<int, List<string>> distanceDict = new Dictionary<int, List<string>>();
                List<string> matchingNames = new List<string>(){name1};
                List<DateTime> foundDates = new List<DateTime>(unprocessed[name1].Keys);
                for (int j = 0; j < notFoundNamesAll.Count; j++)
                {
                    var name2 = notFoundNamesAll.ElementAt(j);
                    if (name1 == name2)
                        continue;
                    if (foundNames.Contains(name2))
                        continue;
                    int dist = LevenshteinDistance(name1, name2);
                    if (!distanceDict.ContainsKey(dist))
                        distanceDict.Add(dist, new List<string>());
                    distanceDict[dist].Add(name2);
                }
                var sortedDist = distanceDict.Keys.OrderBy(x => x).ToList();
                bool foundAll = false;
                foreach (var dist in sortedDist)
                {
                    foreach (var name2 in distanceDict[dist])
                    {
                        //Console.WriteLine("Are following two mutual funds same: (Y/N) ");
                        //Console.WriteLine(name1);
                        //Console.WriteLine(name2);
                        var ret = "";// Console.ReadLine();
                        if (ret == "Y" || ret == "y")
                        {
                            foundNames.Add(name1);
                            foundNames.Add(name2);
                            matchingNames.Add(name2);
                            foundDates.AddRange(unprocessed[name2].Keys);
                            if (AllDateExists(dates, foundDates))
                            {
                                rv.Add(name1, new Dictionary<DateTime, double>());
                                foreach (var name in matchingNames)
                                {
                                    foreach (var date in unprocessed[name].Keys)
                                    {
                                        rv[name1].Add(date, unprocessed[name][date]);
                                    }
                                }
                                foundAll = true;
                                break;
                            }
                        }
                    }
                    if (foundAll)
                        break;
                }
            }
            //if (currentMfNames.Except(rv.Keys).Any())
            //{
            //    Console.WriteLine("Could not find following:");
            //    foreach (var name in currentMfNames.Except(foundNames))
            //    {
            //        Console.WriteLine(name);
            //    }
            //    Console.ReadLine();
            //}
            Dictionary<DateTime, Dictionary<string, double>> rv1 = new Dictionary<DateTime, Dictionary<string, double>>();
            foreach (var name in rv.Keys)
            {
                foreach (var date in rv[name].Keys)
                {
                    if (!rv1.ContainsKey(date))
                        rv1.Add(date, new Dictionary<string, double>());
                    rv1[date].Add(name, rv[name][date]);
                }
            }

            return rv1;
        }
        static Dictionary<string, Tuple<double, double, double>> GetInformationFromDatedFile(string file)
        {
            Dictionary<string, Tuple<double, double, double>> navByName = new Dictionary<string, Tuple<double, double, double>>();
            var lines = System.IO.File.ReadAllLines(file);
            for (int i = 0; i <= lines.Count()- 1; i++)
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
                    string tempName = ShortenName(arr3[0]);
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
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Something went wrong!");
                }
            }
            return navByName;
        }
        static void OpenExcel(string fileName = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (fileName.Length < 1)
                startInfo.FileName = "EXCEL.exe";
            else
                startInfo.FileName = fileName;
            startInfo.Arguments = "Return.xlsx";
            Process.Start(startInfo);
        }

        static bool AllDateExists(List<DateTime> refDatesSortedReverse, List<DateTime> dates)
        {
            dates = dates.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
            var firstDate = dates.FirstOrDefault();
            foreach (var date in refDatesSortedReverse)
            {
                if (!dates.Contains(date))
                    return false;
                if (date == firstDate)
                    break;
            }
            return true;
        }
        private static Dictionary<string, List<string>> changedNameMapping =
            new Dictionary<string, List<string>>()
            {
                {"ICI Pru Val. Discovery (G) ", new List<string>{"ICI Pru Val. Discovery R. (G) "}},
                {"Tat Ind Tax Savings R. ", new List<string>{"Tat Lon Term Eq R. "}},
            };
        private static string GetChangedName(string oldName)
        {
            foreach (var name in changedNameMapping.Keys)
            {
                if (changedNameMapping[name].Contains(oldName))
                    return name;
            }
            return oldName;
        }
        private static string GetOldName(string newName, List<string> oldList, out bool exists)
        {
            exists = true;
            foreach (var name in oldList)
            {
                if (changedNameMapping.ContainsKey(newName) && changedNameMapping[newName].Contains(name))
                    return name;
            }
            exists = false;
            return "";
        }
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static double RiskFreeInterestRate = 0.06;
        
        private static void CalculateReturns()
        {
            ClearScreen();
            System.IO.File.WriteAllText("ReturnCalulation.txt", "");
            calculateReturn = true;
            HistoricalAnalysis(false, true, true, false);
            ProcessRealizedGains(displayDetails: false);
            ClearScreen();
            Console.WriteLine("Return analysis for mutual funds\n\n\n");
            Console.WriteLine("{0}\n\n\n", Repeat("=", 150));

            var dates = NetValueForReturn.Keys.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
            var lastDate = dates.Last();
            System.IO.File.Delete("StdDev.txt");
            MfReturn r = new MfReturn(OrigValueForReturn, NetValueForReturn[lastDate] + netRealizedGains);
            var mfRet = r.GetReturns();
            Console.WriteLine("{0}\n\n\n", Repeat("=", 150));

            string sharpRatioDetail = string.Format("Sharp R.(rF:{0:0.00}%)", RiskFreeInterestRate * 100);
            Console.WriteLine("{0}{1}{2}{3}{4}{5}\n",
           // Console.WriteLine("{0}{1}{2}{3}{4}{5}{6}\n",
                Program.DoSpacing("MF Name", 40),
                Program.DoSpacing("Return (%)", 20),
                Program.DoSpacing("Std.Dev", 20),
                Program.DoSpacing(sharpRatioDetail, 20),
                //Program.DoSpacing("Weighted Days", 20),
                Program.DoSpacing("Start Date", 20),
                Program.DoSpacing("Unlocked Investment", 50)
                );
            if(latesValueByName.Count == 0)
            {
                Console.WriteLine("Do analysis first for mutual fund returns!");
                return;
            }
            MfOriginalAmountByDate = ProcessMFreturns(MfOriginalAmountByDate, dates);
            Dictionary<string, double> retByMf = new Dictionary<string, double>();
            Dictionary<string, double> stddevByMf = new Dictionary<string, double>();
            Dictionary<string, double> stddevAvgRatioByMf = new Dictionary<string, double>();
            Dictionary<string, double> correlationByMf = new Dictionary<string, double>();
            Dictionary<string, double> avgAgeByMf = new Dictionary<string, double>();
            Dictionary<string, double> invWithoutExitLoadByMf = new Dictionary<string, double>();
            Dictionary<string, double> totalInvByMf = new Dictionary<string, double>();
            Dictionary<string, DateTime> startDateByMf = new Dictionary<string, DateTime>();
            Dictionary<string, Dictionary<DateTime, double>> navByDate = new Dictionary<string, Dictionary<DateTime, double>>();
            Dictionary<string, Dictionary<DateTime, double>> investmentByDateAndMutualFund = new Dictionary<string, Dictionary<DateTime, double>>();
            foreach (var date in MfNavByDate.Keys)
            {
                foreach (var name in MfNavByDate[date].Keys)
                {
                    if (!navByDate.ContainsKey(name))
                        navByDate.Add(name, new Dictionary<DateTime, double>());
                    if (!navByDate[name].ContainsKey(date))
                        navByDate[name].Add(date, MfNavByDate[date][name]);
                }
            }
            investmentByDateAndMutualFund.Add("All", new Dictionary<DateTime, double>(r.GetInvestmentByDateInformation()));
            
            foreach (var mf in existingAmount.Keys)
            {
                var name = ShortenName(mf);
                Dictionary<DateTime, double> valDict = new Dictionary<DateTime, double>();
                int counter = 0;
                foreach (var date in MfOriginalAmountByDate.Keys)
                {
                    if (MfOriginalAmountByDate[date].ContainsKey(name))
                    {
                        if (!valDict.ContainsKey(date))
                        {
                            valDict.Add(date, MfOriginalAmountByDate[date][name]);
                            counter++;
                        }
                    }
                }
                
                if (counter < 3)
                    continue;
                if(!latesValueByName.ContainsKey(mf))
                    continue;
                var dividents = dividendWithShortNameKeys.ContainsKey(name)
                                    ? dividendWithShortNameKeys[name] : null;
                r = new MfReturn(valDict, latesValueByName[mf], name, false, navByDate[name], dividents);
                var x = r.GetReturns();

                

                foreach (var newName in newNameToOldNameMapping.Keys)
                {
                    var oldName = newNameToOldNameMapping[newName];
                    var oldShortenName = ShortenName(oldName);
                    var newShortenName = ShortenName(newName);
                    if (oldShortenName == name)
                        name = newShortenName;
                }
                retByMf.Add(name, x);
                stddevByMf.Add(name, r.stdDev);
                stddevAvgRatioByMf.Add(name, (x- RiskFreeInterestRate) / r.stdDev);
                correlationByMf.Add(name, r.Correlation);
                investmentByDateAndMutualFund.Add(name, new Dictionary<DateTime, double>(r.GetInvestmentByDateInformation()));
                avgAgeByMf.Add(name, r.GetAverageInvestementAgeInDays());
                startDateByMf.Add(name, r.FirstInvestmentDate);
                invWithoutExitLoadByMf.Add(name, r.OrigInvBeyondCriticalInterval);
                totalInvByMf.Add(name, r.TotalInvestment);
            }
            double avgStddev = stddevByMf.Values.Average();
            retByMf = retByMf.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            stddevAvgRatioByMf = stddevAvgRatioByMf.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            double avgRatio = stddevAvgRatioByMf.Values.Average();
            var colour = Console.ForegroundColor;
            foreach (var mf in retByMf.Keys)
            {
                var ret = retByMf[mf] * 100;
                double stdDev = stddevByMf[mf];
                if (retByMf[mf] > mfRet)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else if (retByMf[mf] > 0)
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                else
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.Write("{0}{1}", Program.DoSpacing(mf, 40),
                    Program.DoSpacing(ret.ToString("N" + 2) + "%", 20));
                if (stdDev != double.MinValue)
                {
                    if (stdDev < avgStddev)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("{0}", Program.DoSpacing(stdDev.ToString("N" + 2) + "%", 20));
                }
                if (stddevAvgRatioByMf[mf] > avgRatio)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("{0}", Program.DoSpacing((stddevAvgRatioByMf[mf]).ToString("N" + 2), 20));
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                //Console.Write("{0}", Program.DoSpacing((avgAgeByMf[mf]).ToString("N" + 2), 20));
                Console.Write("{0}", Program.DoSpacing((startDateByMf[mf]).ToString("yyyy-MMM-dd"), 20));
                string lockInDetails = string.Format("{2:0}% : [{0}/{1}]", invWithoutExitLoadByMf[mf], totalInvByMf[mf], invWithoutExitLoadByMf[mf] * 100 / totalInvByMf[mf]);

                Console.Write("{0}", Program.DoSpacing(lockInDetails, 50, 2));

                Console.Write("\n");
            }
            Console.ForegroundColor = colour;
            Console.WriteLine("{0}\n\n\n", Repeat("=", 150));
            LogInvestmentByDateDetails(investmentByDateAndMutualFund);
            CorrelationCreator.Create(DetailedReturnsByName);
        }

        private static void LogInvestmentByDateDetails(Dictionary<string, Dictionary<DateTime, double>> investmentByDateAndMutualFund)
        {
            var allNames = investmentByDateAndMutualFund.Keys.ToList();
            List<DateTime> allDates = new List<DateTime>();
            HashSet<int> uniqueDayDiff = new HashSet<int>();
            StringBuilder sb = new StringBuilder();
            sb.Append("Date,");
            foreach (var name in allNames)
            {
                sb.Append(name + ",");
                foreach (var date in investmentByDateAndMutualFund[name].Keys)
                {
                    int diff = (DateTime.Now - date).Days;
                    if (uniqueDayDiff.Add(diff))
                        allDates.Add(date);
                }
            }
            allDates = allDates.OrderByDescending(d => (DateTime.Now - d).Days).ToList();
            sb.AppendLine();
            Dictionary<DateTime, Dictionary<string, double>> cumulativeValue = new Dictionary<DateTime, Dictionary<string, double>>();

            foreach (var date in allDates)
            {
                sb.AppendFormat("\n{0},", date.ToString("yyyy-MM-dd"));
                foreach (var name in allNames)
                {
                    var mfDates = investmentByDateAndMutualFund[name].Keys.OrderByDescending(d => (DateTime.Now - d).Days).ToList();
                    double value = 0;

                    if (investmentByDateAndMutualFund[name].ContainsKey(date))
                    {
                        value = investmentByDateAndMutualFund[name][date];
                        sb.Append(value + ",");
                        continue;
                    }
                    sb.Append(",");
                }
                
            }

            System.IO.File.WriteAllText("InvestmentByDate.csv", sb.ToString());
        }

        private static void ModifyDividendInformation()
        {
            Dictionary<string, List<Tuple<DateTime, double>>> tempData = new Dictionary<string, List<Tuple<DateTime, double>>>();
            foreach (var shortName in dividends.Keys)
            {
                if (!shortNameToFullNameMapping.ContainsKey(shortName))
                    continue;
                string fullName = shortNameToFullNameMapping[shortName].FirstOrDefault();
                foreach (var name in shortNameToFullNameMapping[shortName])
                {
                    if(latesNAVByName.ContainsKey(name))
                    {
                        fullName = name;
                        break;
                    }
                }
                tempData.Add(fullName, new List<Tuple<DateTime, double>>());
                foreach (var tuple in dividends[shortName])
                {
                    tempData[fullName].Add(Tuple.Create(tuple.Item1, tuple.Item2));
                }
            }
            dividendWithFullNameKeys = new Dictionary<string, List<Tuple<DateTime, double>>>(tempData);
        }

        static Dictionary<string, List<Tuple<DateTime, double>>> dividendWithFullNameKeys;
        static Dictionary<string, Dictionary<DateTime, double>> dividendWithShortNameKeys = new Dictionary<string, Dictionary<DateTime, double>>();

        private static double GetTotalValueFromDividends(string mfName)
        {
            if (RealizedGain.totalDividends == 0)
                return 0;
            var shortName = ShortenName(mfName);
            if (!shortNameToFullNameMapping.ContainsKey(shortName))
                shortNameToFullNameMapping.Add(shortName, new List<string>() { mfName });
            if (dividendWithFullNameKeys == null || dividendWithFullNameKeys.Count < 1)
                ModifyDividendInformation();
            if (dividendWithFullNameKeys.ContainsKey(mfName))
            {
                if (!dividendWithShortNameKeys.ContainsKey(shortName))
                    dividendWithShortNameKeys.Add(shortName, new Dictionary<DateTime, double>());
                double sum = 0;
                foreach (var tuple in dividendWithFullNameKeys[mfName])
                {
                    if(!dividendWithShortNameKeys[shortName].ContainsKey(tuple.Item1))
                        dividendWithShortNameKeys[shortName].Add(tuple.Item1, tuple.Item2);
                    sum += tuple.Item2;
                }
                return sum;
            }
            return 0;
        }

        static double netRealizedGains;
        static double netRealizedDivGains;
        private static void ProcessRealizedGains(bool displayDetails = true)
        {
            ClearScreen();
            netRealizedGains = 0;
            RealizedGain.Process(out netRealizedGains, out netRealizedDivGains, displayDetails);
            dividends = new Dictionary<string, List<Tuple<DateTime, double>>>(RealizedGain.dividends);
        }

        private static void CreateReportExcel()
        {
            try
            {
                doNotAskForOrder = true;
                var data = Analysis(false);
                StringBuilder sb = new StringBuilder();
                double totalAmount = 0;
                double totalCurrent = 0;
                sb.AppendLine("Name,Units,Orig NAV,Cur NAV,Value,Cur Value,Change,%Change");
                foreach (var item in data)
                {
                    sb.AppendFormat("{0},{1:0.00},{2:0.00},{3:0.00},{4:0},{5:0},{6:0},{7:0.00}\n",
                                       item.Item1,
                                       item.Item2,
                                       item.Item3,
                                       item.Item4,
                                       item.Item5,
                                       item.Item6,
                                       item.Item6 - item.Item5,
                                       item.Item7);
                    totalAmount += item.Item5;
                    totalCurrent += item.Item6;
                }
                double netChange = totalCurrent - totalAmount;
                sb.AppendFormat("\n{0},{1},{2},{3},{4:0},{5:0},{6:0},{7:0.00}\n",
                                       "Net",
                                       "",
                                       "",
                                       "",
                                       totalAmount,
                                       totalCurrent,
                                       netChange,
                                       netChange * 100 / totalAmount);
                string fileName = "Report.csv";
                System.IO.File.WriteAllText(fileName, sb.ToString());
                OpenExcel(fileName);
                doNotAskForOrder = false;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void UpdateRiskFreeInterestRate()
        {
            Console.WriteLine("Current Risk Free Rate: {0}", RiskFreeInterestRate);
            Console.WriteLine("Press Y to update else any other key to exit");
            string rv = Console.ReadLine().Trim();
            if (rv.ToLower() != "y")
                return;
            Console.WriteLine("Please provide a new risk free rate!");
            double newRate = double.MinValue;
            var success = double.TryParse(Console.ReadLine(), out newRate);
            newRate /= 100;

            if (!success || newRate == double.MinValue || newRate == RiskFreeInterestRate)
            {
                Console.WriteLine("Invalid entry please try again later!");
                Console.ReadLine();    
                return;
            }
            if (newRate == RiskFreeInterestRate)
            {
                Console.WriteLine("Rate is same as before so exiting!");
                Console.ReadLine();
                return;
            }
            var lines = System.IO.File.ReadLines("input.csv");
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.Contains("Risk Free Interest Rate"))
                {
                    var oldRateStr = RiskFreeInterestRate.ToString();
                    sb.AppendLine(line.Replace(oldRateStr, newRate.ToString()));
                    continue;
                }
                sb.AppendLine(line);
            }
            System.IO.File.WriteAllText("input.csv", sb.ToString());
            RiskFreeInterestRate = newRate;
        }

        private static void PublishExeFile()
        {
            if(Environment.CurrentDirectory.Contains("Box Sync"))
            {
                Console.WriteLine("Can't publish from Box folder itself!");
                return;
            }
            bool success = false;
            try
            {
                System.IO.File.Copy("MutualFund.exe", $"C:\\Users\\ashutosh.nigam\\Box Sync\\Mutual Fund Analysis\\MutualFund.exe", true);
                success = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Couldn't overwrite the file!");
                Console.ReadLine();
                return;
            }
            if(success)
                Console.WriteLine("Updated the exe file @ {0}", DateTime.Now);
        }

    }
    internal enum SortReportEnum
    {
        InvestmentAmount,
        Name,
        RelativeChange,
        AbsChange,
        Units,
        CurrentAmount,
    }
}
