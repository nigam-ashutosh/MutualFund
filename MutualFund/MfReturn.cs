using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutualFund
{
    class MfReturn
    {
        Dictionary<DateTime, double> investmnetByDate;
        Dictionary<DateTime, double> investmentDiffByDate;
        double curNetValue = 0;
        double curActValue = 0;
        string name;
        internal DateTime FirstInvestmentDate;
        DateTime lastDate;
        private double ret = 0;
        public double stdDev = double.MinValue;
        private Dictionary<DateTime, double> navByDate;
        Dictionary<DateTime, double> dividents;
        public MfReturn(Dictionary<DateTime, double> orig, double curValue, string entityName = null, bool displayreturn = true, Dictionary<DateTime, double> navByDate = null, Dictionary<DateTime, double> dividends = null)
        {
            investmnetByDate = new Dictionary<DateTime, double>(orig);
            curNetValue = curValue;
            name = entityName;
            this.navByDate = navByDate;
            if (dividends != null)
                this.dividents = new Dictionary<DateTime, double>(dividends);
            else
                this.dividents = new Dictionary<DateTime, double>();
            this.Extract();
            if (this.printLog)
            {
                this.PrintLog();                
            }
            var r = this.GetReturn();
            this.GetStandardDeviation();
            this.ret = r;
            if (displayreturn)
            {
                this.DisplayReturn(r * 100);
            }
            //this.GetCorrelation();
            System.IO.File.AppendAllText("ReturnCalulation.txt", sb.ToString());
        }

        public double GetReturns()
        {
            return this.ret;
        }
        StringBuilder sb = new StringBuilder(); 
        private void PrintLog()
        {
            sb.AppendFormat("\n\n\n{0}\n", this.name == null ? "MutualFund" : this.name);

            sb.AppendFormat("Investment Date\t\t\t\tIncremental Amount(INR.)\n");
            sb.AppendFormat("================================================\n");
            foreach (var date in investmentDiffByDate.Keys)
            {
                sb.AppendFormat("{0:yyyy/MM/dd}\t\t\t\t{1}\n", date, investmentDiffByDate[date]);
            }
            sb.AppendFormat("================================================\n");
        }
        SortedSet<int> daysDiffSorted = new SortedSet<int>();
        private void Extract()
        {
            var dates = investmnetByDate.Keys.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
            investmentDiffByDate = new Dictionary<DateTime,double>();
            double prevRecord = 0;
            foreach (var date in dates)
            {
                double record = RoundBy100(investmnetByDate[date]);
                if (record != prevRecord)
                {
                    UpdateInvestmentDiffByDate(date, record - prevRecord, true);
                    prevRecord = record;
                }
                if (this.name != null)
                {
                    UpdateInvestmentDiffByDate(date, dividents);
                }
                else
                {
                    UpdateInvestmentDiffByDate(date, RealizedGain.RealizedGainByDate);
                }
            }
            FirstInvestmentDate = dates.FirstOrDefault();
            lastDate = dates.LastOrDefault();
            curActValue = investmnetByDate[lastDate];
            allDates = dates;
        }

        private void UpdateInvestmentDiffByDate(DateTime date, Dictionary<DateTime, double> gainDictionary)
        {
            if (gainDictionary != null && gainDictionary.ContainsKey(date))
            {
                UpdateInvestmentDiffByDate(date, -gainDictionary[date]);
            }
        }

        private void UpdateInvestmentDiffByDate(DateTime date, double value, bool doNotUpdateIfExisting = false)
        {
            if (investmentDiffByDate.ContainsKey(date) && doNotUpdateIfExisting)
                return;
            if (!investmentDiffByDate.ContainsKey(date))
            {
                investmentDiffByDate[date] = 0;
            }
            investmentDiffByDate[date] += value;
            int daysDiff = (DateTime.Now - date).Days;
            daysDiffSorted.Add(daysDiff);
        }
        public Dictionary<DateTime, double> GetInvestmentByDateInformation()
        {
            return new Dictionary<DateTime, double>(investmentDiffByDate);
        }

        private static List<DateTime> allDates;
        private double RoundBy100(double val)
        {
            int mult = (int) Math.Round(val/100);
            return mult * 100;
        }
        private double eps = 0.0001;
        private bool printLog = true;
        private double GetReturn()
        {
            //double ub = curNetValue > curActValue ?  1000 : 0;
            //double lb = curNetValue > curActValue ? 0 : -1000;
            double ub = GetReturnBound(true);
            double lb = GetReturnBound(false);
            int iteration = 1;

            //adjust current net value for today's investment
            foreach (var date in investmentDiffByDate.Keys)
            {
                var days = (lastDate - date).Days - 1;
                if (days < 0)
                {
                    curNetValue -= investmentDiffByDate[date];
                }
            }
            while (true)
            {
                double r = (ub + lb) / 2;
                double netValue = 0;
                foreach (var date in investmentDiffByDate.Keys)
                {
                    //if((DateTime.Now - date).Days == 0)
                    //    Console.WriteLine();
                    var days = (lastDate - date).Days;
                    double value = investmentDiffByDate[date];
                    if (days <= 0)
                        continue;
                    double v = value * Math.Pow(1 + r,  (double) days/365);
                    netValue += v;
                }
                if(printLog)
                    sb.AppendFormat("r: {0:0.000}%\tCalc: {1:E}\t Actual: {2:E}\tLB: {3:E}\tUB: {4:E}\tIt: {5}\n", 
                                    r * 100,
                                    netValue, 
                                    curNetValue, 
                                    lb,
                                    ub,
                                    iteration);
                if(Math.Abs(netValue - curNetValue) < eps)
                {                    
                    return r;
                }
                if (netValue > curNetValue)
                {
                    ub = r;
                }
                else
                {
                    lb = r;
                }
                iteration++;
            }
        }

        private double GetReturnBound(bool upper)
        {
            //if positive return then for lower bound use max interval
            int interval = -1;
            if(upper)
            {
                interval = curNetValue > curActValue ? daysDiffSorted.Min : daysDiffSorted.Max;
            }
            else
            {
                interval = curNetValue > curActValue ? daysDiffSorted.Max : daysDiffSorted.Min;
            }
            double ret = Math.Pow(curNetValue / curActValue, 365.0 / interval) - 1;
            return ret;
        }

        internal double GetAverageInvestementAgeInDays()
        {
            double weightedSum = 0;
            double investment = 0;
            foreach (var date in investmentDiffByDate.Keys)
            {
                int days = (DateTime.Now - date).Days;
                investment += investmentDiffByDate[date];
                weightedSum += days * investmentDiffByDate[date];
            }
            var rv = weightedSum / investment;
            return rv;
        }
        private void DisplayReturn(double r)
        {
            var origColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (name == null)
            {
                Console.Write("{0}{1}\n", Program.DoSpacing("Mutual Fund Overall", 40),
                    Program.DoSpacing(r.ToString("N" + 2) + "%", 20));
            }
            else
            {
                Console.Write("{0}{1}", Program.DoSpacing(this.name, 40),
                    Program.DoSpacing(r.ToString("N" + 2) + "%", 20));
                if (this.stdDev != double.MinValue)
                {
                    Console.Write("{0}", Program.DoSpacing(this.stdDev, 20));
                }
                Console.Write("\n");
            }
            Console.ForegroundColor = origColour;
        }
        private double SensexReturn(out double standardDeviation)
        {
            try
            {
                List<DateTime> dates = new List<DateTime>(investmnetByDate.Keys);
                var returns = Sensex.GetReturnByDate(dates, true);
                int days = (Sensex.lastRecord.Item1 - Sensex.firstRecord.Item1).Days;
                double ret = Sensex.lastRecord.Item2 / Sensex.firstRecord.Item2;
                //ret = (Math.Pow(ret, (double) (1.0 / days)) - 1)*(365.0/days);
                ret = Math.Pow(ret, 365.0 / days) - 1;
                standardDeviation = this.GetStandardDeviation(returns.Values.Select(r => r).ToList());
                //standardDeviation /= 100;
                return ret * 100;
            }
            catch
            {
                Console.WriteLine("Error in fetching/reading sensex data!");
            }
            finally
            {
                standardDeviation = 0;
            }
            return 0;
        }
        private void GetStandardDeviation()
        {
            if (this.name == null)
                return;
            if (Program.DetailedReturnsByName == null)
                return;
            if (!Program.DetailedReturnsByName.ContainsKey(this.name))
                return;
            var returns = Program.DetailedReturnsByName[this.name].Values;
            StringBuilder sb = new StringBuilder(this.name + "\n");
            foreach (var date in Program.DetailedReturnsByName[this.name].Keys)
            {
                double val = Program.DetailedReturnsByName[this.name][date];
                sb.AppendFormat("{0}\t{1}\n", date.ToString("yyyyMMdd"), val);
            }
            try
            {
                this.stdDev = this.GetStandardDeviation(returns.ToList());
            }
            catch
            {

            }
        }

        private double GetStandardDeviation(List<double> returns)
        {
            returns = returns.Where(r => r != 0).ToList();
            double avg = returns.Average();
            double sumOfSquareDiff = returns.Sum(x => (x - avg) * (x - avg));
            var stdDev = Math.Sqrt(sumOfSquareDiff / returns.Count) * 100;

            return stdDev;
        }

        internal double Correlation = 0;

        private void GetCorrelation()
        {
            try
            {
                if (navByDate == null)
                    return;
                navByDate = navByDate.OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month).ThenBy(x => x.Key.Day).
                    ToDictionary(x => x.Key, x => x.Value);
                var sensexReturn = Sensex.GetCloseByDate();
                List<Tuple<double, double>> pairForCorrelation = new List<Tuple<double, double>>();
                StringBuilder sb = new StringBuilder();
                foreach (var date in navByDate.Keys)
                {
                    bool found = false;
                    var preDate = this.FindPreviousDate(date, sensexReturn.Keys.ToList(), out found);
                    if (!found)
                        continue;
                    if (!sensexReturn.ContainsKey(preDate))
                        continue;
                    pairForCorrelation.Add(Tuple.Create(navByDate[date], sensexReturn[preDate]));
                    sb.AppendFormat("{0}\t{1}\t{2}\n", date, navByDate[date], sensexReturn[preDate]);
                }
                //this.Correlation = Sensex.Correlation(pairForCorrelation.Select(p => p.Item1).ToArray(), pairForCorrelation.Select(p => p.Item2).ToArray());
            }
            catch
            {

            }
        }

        private DateTime FindPreviousDate(DateTime date, List<DateTime> dates, out bool found)
        {
            date = date.AddDays(-1);
            found = false;
            while (true)
            {
                if (dates.Contains(date))
                {
                    found = true;
                    return date;
                }
                date = date.AddDays(-1);
                if ((date - Sensex.firstRecord.Item1).Days < 0)
                    break;
            }
            return date;
        }
    }
}
