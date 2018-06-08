using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Cache;

namespace MutualFund
{
    
    static class Sensex
    {
        public static Tuple<DateTime, double> firstRecord;
        public static Tuple<DateTime, double> lastRecord;
        static List<Tuple<DateTime, DateTime>> UStravelDates
            = new List<Tuple<DateTime, DateTime>>()
            {
                Tuple.Create(new DateTime(2016, 3, 29), new DateTime(2016, 5, 24)),
                Tuple.Create(new DateTime(2015, 9, 4), new DateTime(2015, 10, 19)),
            };
        static bool IsUStravelDate(DateTime date)
        {
            foreach (var interval in UStravelDates)
            {
                if (IntervalContainsDate(interval, date))
                    return true;
            }
            return false;
        }
        static bool IntervalContainsDate(Tuple<DateTime, DateTime> interval, DateTime date)
        {
            bool rv = false;
            if (interval.Item1.Year > date.Year || interval.Item2.Year < date.Year)
                return rv;
            if (interval.Item1.Month > date.Month || interval.Item2.Month < date.Month)
                return rv;
            if (interval.Item1.Date > date.Date || interval.Item2.Date < date.Date)
                return rv;
            rv = true;
            return rv;
        }
        private static string fileName = "Sensex.csv";
        public static void GetSensexData()
        {
            string url = "http://real-chart.finance.yahoo.com/table.csv?s=%5EBSESN&d=2&e=1&f=2016&g=d&a=5&b=1&c=2015&ignore=.csv";
            url += "?";
            url += System.IO.Path.GetRandomFileName();
            int date = DateTime.Now.Day;
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;
            if (IsUStravelDate(DateTime.Now))
                date++;
            url = url.Replace("d=2", string.Format("d={0}", month - 1));
            url = url.Replace("e=1", string.Format("e={0}", date));
            url = url.Replace("f=2016", string.Format("f={0}", year));
            System.Net.WebClient wc = new System.Net.WebClient();
            wc.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            //Console.WriteLine("Downloading sensex data from web....");
            string webData = "";
            try
            {
                System.IO.File.Delete(fileName);
                webData = wc.DownloadString(url);
                //Console.WriteLine("Downloading completed successfully!!");
                System.IO.File.WriteAllText(fileName, webData);
            }
            catch
            {
                Console.WriteLine("Error in connecting to web, please check network and retry");
            }
        }
        public static bool IsFileUpdated()
        {
            bool rv= false;
            if (!System.IO.File.Exists(fileName))
                return rv;
            var lines = System.IO.File.ReadLines(fileName);
            var arr = lines.ElementAt(1).Split(new char[] { ',' });
            DateTime lastDate = DateTime.MinValue;
            bool success = DateTime.TryParse(arr[0], out lastDate);
            if (!success)
                return rv;
            return (DateTime.Now - lastDate).Days < 2;
        }
        private static int DayOffSetForSensex = 20;

        static Dictionary<DateTime, double> returnByDate = null;
        static Dictionary<DateTime, double> actualCloseByDate = null;

        public static Dictionary<DateTime, double> GetCloseByDate()
        {
            if (actualCloseByDate == null)
            {
                Console.WriteLine("Recheck the flow! Sensex data not available yet");
                Console.ReadLine();
                return new Dictionary<DateTime, double>();
            }
            return actualCloseByDate;
        }
        public static Dictionary<DateTime, double> GetReturnByDate(List<DateTime> refDates, bool forReturn = false)
        {
            returnByDate = new Dictionary<DateTime, double>();
            if (!IsFileUpdated())
            {
                //commenting out as there is no live source available
                //GetSensexData();
            }
            actualCloseByDate = new Dictionary<DateTime, double>();
            var lines = System.IO.File.ReadAllLines("SENSEX.csv");
            bool first = true;
            foreach (var line in lines)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                var arr = line.Split(new char[] { ',' });
                DateTime date = DateTime.Parse(arr[0]);
                double close = double.Parse(arr[4]);
                actualCloseByDate.Add(date, close);
            }
            actualCloseByDate = actualCloseByDate.OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month).ThenBy(x => x.Key.Day).
                ToDictionary(x => x.Key, x => x.Value);
            Console.WriteLine("Latest sensex data date: {0:yyyy-MM-dd}", actualCloseByDate.Keys.Last());
            Dictionary<DateTime, double> closeByDate = new Dictionary<DateTime, double>(actualCloseByDate);
            refDates = refDates.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
            var firstDate = refDates.First();
            var lastDate = refDates.Last();
            double firstValue = double.MinValue;
            int counter = DayOffSetForSensex; //first report is on 01-Oct 2015, while first investment was on 14-Sep 2015 (diff = 17 days)
            while (true)
	        {
	            DateTime date = refDates.FirstOrDefault().AddDays(-counter++);
                if(closeByDate.ContainsKey(date))
                {
                    firstValue = closeByDate[date];
                    break;
                }
            }
            //now modify closeby dates-- fill in the blanks
            for (DateTime day = firstDate; (lastDate - day).Days >= 0; day = day.AddDays(1))
            {
                if (closeByDate.ContainsKey(day))
                    continue;
                var findDay = day.AddDays(-1);
                while (true)
                {
                    if (closeByDate.ContainsKey(findDay))
                    {
                        closeByDate.Add(day, closeByDate[findDay]);
                        break;
                    }
                    findDay = findDay.AddDays(-1);
                }
            }
            int days = (refDates.Last() - refDates.First()).Days;
            if (forReturn)
            {
                var baseDate = refDates.First();
                for (int i = 1; i <= DayOffSetForSensex; i++)
                {
                    refDates.Add(baseDate.AddDays(-i));
                }
                refDates = refDates.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ToList();
                firstRecord = Tuple.Create(refDates.First(), closeByDate[refDates.First()]);
                lastRecord = Tuple.Create(refDates.Last(), closeByDate[refDates.Last()]);
                //return closeByDate;
            }
           
            foreach (var date in refDates)
            {
                DateTime preDay = DateTime.Now;
                bool found = GetDateWithDaysDiff(closeByDate.Keys.ToList(), date, -1, out preDay);
                if (!found)
                    continue;
                if (!closeByDate.ContainsKey(preDay))
                {
                    if(!forReturn)
                        Console.WriteLine("Check there is some error while updating sensex close values on {0:yyyy/MM/dd}", preDay);
                    continue;
                }
                double preClose = closeByDate[preDay];
                DateTime preDay2 = DateTime.Now;
                found = GetDateWithDaysDiff(closeByDate.Keys.ToList(), preDay, -1, out preDay2);
                if (!found)
                    continue;
                double preClose2 = closeByDate[preDay2];
                double ret = 0;
                ret = ((preClose - preClose2)) / preClose2;
                //int days2 = (preDay - refDates.First()).Days;
                //if (days2 <= 0)
                //    continue;
                //ret = Math.Pow(ret, (365 / days2)) - 1;
                returnByDate.Add(date, ret);
            }            
            return returnByDate;
        }

        private static bool GetDateWithDaysDiff(List<DateTime> refDates, DateTime date, int days, out DateTime prevDate)
        {
            prevDate = date.AddDays(days);
            int mod = days >= 0 ? 1 : -1;
            int counter = 0;
            while (true)
            {
                if (refDates.Contains(prevDate))
                    return true;
                prevDate = prevDate.AddDays(mod);
                if (counter++ > refDates.Count)
                    break;
            }
            return false;
        }

        public static double Correlation(double[] arr1, double[] arr2, int length = -1)
        {
            if (arr1.Count() != arr2.Count())
            {
                Console.WriteLine("Array mismatch");
                return double.MinValue;
            }
            if (length == -1)
                length = arr1.Count();
            else
            {
                double[] tempArr1 = new double[length];
                double[] tempArr2 = new double[length];
                Array.Copy(arr1, tempArr1, length);
                Array.Copy(arr2, tempArr2, length);

                arr1 = new double[length];
                arr2 = new double[length];
                Array.Copy(tempArr1, arr1, length);
                Array.Copy(tempArr2, arr2, length);
            }
            double ave1 = arr1.Average();
            double ave2 = arr2.Average();

            double sum = arr1.Zip(arr2, (x1, x2) => (x1 - ave1) * (x2 - ave2)).Sum();
            double sumSqr1 = /*Math.Sqrt*/(arr1.Sum(x => Math.Pow((x - ave1), 2.0)));
            double sumSqr2 = /*Math.Sqrt*/(arr2.Sum(x => Math.Pow((x - ave2), 2.0)));

            double result = sum / Math.Sqrt(sumSqr1 * sumSqr2);

            return result;
        }
    }
    
    
    
}
