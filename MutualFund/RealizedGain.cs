using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutualFund
{
    public static class RealizedGain
    {
        static Dictionary<DateTime, Dictionary<string, double>> mfOriginalAmountByDate;
        static Dictionary<DateTime, Dictionary<string, double>> mfNetAmountByDate;

        static Dictionary<string, Dictionary<DateTime, Tuple<double, double>>> infoByMfByDate;// = new Dictionary<string, Dictionary<DateTime, Tuple<double, double>>>();
        static Dictionary<string, DateTime> maxDateForMutualFund;// = new Dictionary<string, DateTime>();
        static DateTime maxDateFoAllrMutualFunds;// = new DateTime();

        static bool firstTime = true;
        public static void Process(out double totalRealizedGains, out double totalRealizedDivgains, bool display)
        {
            if(!firstTime)
            {
                totalRealizedGains = netReturnAdjustedgains;
                totalRealizedDivgains = netReturnAdjustedDividends;
                if (display)
                    UpdateAndDisplay();
                return;
            }
            firstTime = false;
            latestDateInFile = DateTime.MinValue;
            GetDataFromFile(out latestDateInFile);
            if (display)
            {
                UpdateAndDisplay();
            }   
            totalRealizedGains = netReturnAdjustedgains;
            totalRealizedDivgains = netReturnAdjustedDividends;
        }

        private static bool updated = false;
        private static void UpdateAndDisplay()
        {
            if (!updated)
            {
                Program.HistoricalAnalysis(false, true, true, false);
                mfNetAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>(Program.mfNetAmountByDate);
                mfOriginalAmountByDate = new Dictionary<DateTime, Dictionary<string, double>>(Program.mfOriginalAmountByDate);
                infoByMfByDate = new Dictionary<string, Dictionary<DateTime, Tuple<double, double>>>();
                maxDateForMutualFund = new Dictionary<string, DateTime>();
                maxDateFoAllrMutualFunds = new DateTime();
                ModifyData();
                UpdateRealizedGains(realizedGainsData.Count > 0);
                GetDataFromFile(out latestDateInFile);
                updated = true;
            }
            DisplayRealizedGain();
        }

        static DateTime latestDateInFile;

        private static  void ModifyData()
        {
            foreach (var date in mfOriginalAmountByDate.Keys)
            {
                if ((date - latestDateInFile).Days <= 0)
                    continue;
                if((date - maxDateFoAllrMutualFunds).Days > 0)
                {
                    maxDateFoAllrMutualFunds = date;
                }
                foreach (var mf in mfNetAmountByDate[date].Keys)
                {
                    if(!infoByMfByDate.ContainsKey(mf))
                    {
                        infoByMfByDate.Add(mf, new Dictionary<DateTime, Tuple<double, double>>());
                        maxDateForMutualFund.Add(mf, date);
                    }
                    infoByMfByDate[mf].Add(date, Tuple.Create(mfOriginalAmountByDate[date][mf], mfNetAmountByDate[date][mf]));
                    if ((date - maxDateForMutualFund[mf]).Days > 0)
                    {
                        maxDateForMutualFund[mf] = date;
                    }
                }
            }
        }

        private static void UpdateRealizedGains(bool previousExistingData)
        {
            StringBuilder sb = new StringBuilder();
            if(!previousExistingData)
                sb.AppendLine("MutualFund,LastDate,RealizedGain");
            foreach (var mf in infoByMfByDate.Keys)
            {
                if (mf.Contains("DSP Bla Nat. Res.& Egy.R. (G)			163.784"))
                    continue;
                if (mf.Contains("ICI Pru Val. Discovery R. (G)"))
                    continue;
                if (mf.Contains("Tat Lon Term Eq R."))
                    continue;
                var lastDate = maxDateForMutualFund[mf];
                if ((lastDate - maxDateFoAllrMutualFunds).Days >= 0)
                    continue;
                
                var lastDateStr = lastDate.ToString("dd-MMM-yyyy");
                var realizedGain = infoByMfByDate[mf][lastDate].Item2 - infoByMfByDate[mf][lastDate].Item1;
                if (!mf.Contains("Liq"))
                {
                    double commission = infoByMfByDate[mf][lastDate].Item2 * 0.01;
                    realizedGain -= commission;
                }
                if (Math.Abs(realizedGain) < 0.001)
                    continue;
                sb.AppendFormat("{0},{1},{2:0.00},Redemption\n", mf, lastDateStr, realizedGain.ToString());
            }
            if (sb.Length < 1)
                return;
            System.IO.File.AppendAllText("RealizedGains.csv", sb.ToString());
        }

        public static Dictionary<string, List<Tuple<DateTime, double>>> dividends;
        internal static double totalDividends = 0;
        static double netGains = 0;
        static double netReturnAdjustedgains = 0;
        static double netReturnAdjustedDividends = 0;

        static List<Tuple<string, DateTime, double, double>> realizedGainsData;
        internal static Dictionary<DateTime, double> RealizedGainByDate;
        private static void GetDataFromFile(out DateTime maxDate)
        {
            netGains = 0;
            netReturnAdjustedgains = 0;
            maxDate = DateTime.MinValue;
            dividends = new Dictionary<string, List<Tuple<DateTime, double>>>();
            realizedGainsData = new List<Tuple<string, DateTime, double, double>>();
            RealizedGainByDate = new Dictionary<DateTime, double>();
            if (!System.IO.File.Exists("RealizedGains.csv"))
                return;
            var allLines = System.IO.File.ReadLines("RealizedGains.csv");

            if (allLines.Count() < 1)
            {
                return;
            }
            int counter = 0;
            foreach (var line in allLines)
            {
                if (counter == 0)
                {
                    counter++;
                    continue;
                }
                var arr = line.Split(new char[] { ',' });
                if (arr[0].Contains("DSP Bla Nat. Res.& Egy.R. (G)			163.784"))
                    continue;
                if (arr[0].Contains("ICI Pru Val. Discovery R. (G)"))
                    continue;
                if (arr[0].Contains("Tat Lon Term Eq R."))
                    continue;
                double mfGain = double.Parse(arr[2]);
                mfGain = Math.Round(mfGain, 2);
                DateTime date = DateTime.Parse(arr[1]);

                netGains += mfGain;
                int daysToCurrentDate = (DateTime.Now - date).Days;
                double adjGain = mfGain * Math.Pow(1 + Program.RiskFreeInterestRate, daysToCurrentDate / 365.0);
                netReturnAdjustedgains += adjGain;
                realizedGainsData.Add(Tuple.Create(arr[0], date, mfGain, adjGain));

                if (!RealizedGainByDate.ContainsKey(date))
                    RealizedGainByDate.Add(date, 0);
                RealizedGainByDate[date] += mfGain;

                if ((date - maxDate).Days > 0)
                    maxDate = date;
                if(arr[3].ToLower().Contains("div"))
                {
                    var name = arr[0].Replace("*", "");
                    name = name.Replace(" (Div)", "");
                    //var longName = Program.shortNameToFullNameMapping[arr[0]].FirstOrDefault();
                    if (!dividends.ContainsKey(arr[0]))
                        dividends.Add(arr[0], new List<Tuple<DateTime, double>>());
                    dividends[arr[0]].Add(Tuple.Create(date, mfGain));
                    totalDividends += mfGain;
                    netReturnAdjustedDividends += adjGain;
                }
            }            
        }

        private static void DisplayRealizedGain()
        {
            var allLines = System.IO.File.ReadLines("RealizedGains.csv");
            
            if(allLines.Count() < 1)
            {
                Console.WriteLine("Realized gains file is not populated!");
                return;
            }
            Console.WriteLine("====================================================================================================\n\n\n");
            Console.WriteLine("{0,-40}{2,20}{3,20}{1,20}","MutualFund","Eff Date", "Gain", "Ret. Adj. Gain");
            var origColor = Console.ForegroundColor;
            DateTime defaultDate = new DateTime(2000, 1, 1);
            foreach (var t in realizedGainsData.OrderBy(x => (x.Item2 - defaultDate).Days))
            {
                
                double mfGain = t.Item3;
                int daysToCurrentDate = (DateTime.Now - t.Item2).Days;
                double adjGain = t.Item4;
                if(mfGain >= 0)
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else
                    Console.ForegroundColor = ConsoleColor.Red;
                var gainStr = (mfGain > 0 ? "+" : "") + mfGain;
                var gainStr2 = (adjGain > 0 ? "+" : "") + Math.Round(adjGain, 0);
                Console.WriteLine("{0,-40}{2,20}{3,20}{1,20}", t.Item1, t.Item2.ToString("dd-MMM-yyyy"), gainStr, gainStr2);
            }
            
            Console.WriteLine();
            if (netGains >= 0)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else
                Console.ForegroundColor = ConsoleColor.Red;
            var netGainStr = (netGains > 0 ? "+" : "") + netGains;
            var netGainStr2 = (netReturnAdjustedgains > 0 ? "+" : "") + Math.Round(netReturnAdjustedgains, 0); ;
            Console.WriteLine("{0,-40}{2,20}{3,20}{1,20}", "Net", "", netGainStr, netGainStr2);
            Console.ForegroundColor = origColor;
            Console.WriteLine("====================================================================================================\n\n\n");
        }
    }
}
