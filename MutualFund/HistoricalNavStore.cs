using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace MutualFund
{
    internal class HistoricalNavStore
    {
        internal Dictionary<int, Dictionary<int, double>> HistoricalNav = new Dictionary<int, Dictionary<int, double>>();
        DateTime baseDate = new DateTime(2000, 1, 1);
        string fileName = "HistoricalNav.csv";
        Dictionary<int, Tuple<DateTime, double>> latestNAVByNameDateAndCode;
        internal Dictionary<int, string> MfCodeNameMapping;
        internal Dictionary<int, List<double>> MfReturns = new Dictionary<int, List<double>>();
        SortedSet<int> dateIds = new SortedSet<int>();
        public HistoricalNavStore(Dictionary<int, Tuple<DateTime, double>> _latestNAVByNameDateAndCode, Dictionary<int, string> _mfCodeNameMapping)
        {
            latestNAVByNameDateAndCode = new Dictionary<int, Tuple<DateTime, double>>(_latestNAVByNameDateAndCode);
            MfCodeNameMapping = new Dictionary<int, string>(_mfCodeNameMapping);
            ReadExistingFile();
            UpdateFile();
        }
        private void ReadExistingFile()
        {
            if (!File.Exists("HistoricalNav.csv"))
                return;
            var lines = File.ReadAllLines(fileName);
            //first read dates
            var arr = SplitStringBySeparator(lines[0]);
            foreach (var elem in arr)
            {
                DateTime d;
                DateTime.TryParse(elem, out d);
                int diff = (d - baseDate).Days;
                dateIds.Add(diff);
            }
            for (int i = 1; i < lines.Count(); i++)
            {
                var line = lines[i];
                var arr2 = SplitStringBySeparator(line);
                int code = int.Parse(arr2[0]);
                for (int j = 1; j < arr2.Count(); j++)
                {
                    double nav;
                    double.TryParse(arr2[j], out nav);
                    var key = dateIds.ElementAt(j - 1);
                    if (!HistoricalNav.ContainsKey(code))
                        HistoricalNav.Add(code, new Dictionary<int, double>());
                    if (!HistoricalNav[code].ContainsKey(key))
                        HistoricalNav[code].Add(key, nav);
                }
            }
        }

        private void UpdateFile()
        {
            var commonCode = latestNAVByNameDateAndCode.Keys.ToList();

            if (HistoricalNav.Keys.Count > 0)
            {
                commonCode = commonCode.Intersect(HistoricalNav.Keys).ToList();
            }
            foreach (var code in commonCode)
            {
                var date = latestNAVByNameDateAndCode[code].Item1;
                var nav = latestNAVByNameDateAndCode[code].Item2;
                int diff = (date - baseDate).Days;
                if (HistoricalNav.Keys.Count > 0)
                {
                    if (!HistoricalNav[code].ContainsKey(diff))
                        HistoricalNav[code].Add(diff, nav);
                    if (!dateIds.Contains(diff))
                        dateIds.Add(diff);
                }
            }

            StringBuilder sb = new StringBuilder();
            
            foreach (var dateId in dateIds)
            {
                sb.Append($",{dateId}");
            }
            sb.AppendLine();
            foreach (var code in HistoricalNav.Keys)
            {
                double preValue = 0;
                int preDateId = -1;
                
                foreach (var dateId in dateIds)
                {
                    double ret = double.MaxValue;
                    if (HistoricalNav[code].ContainsKey(dateId))
                    {
                        sb.Append($",{HistoricalNav[code][dateId]}");
                        if (preValue != 0 && preDateId != -1)
                        {
                            ret = (HistoricalNav[code][dateId] - preValue) / (dateId - preDateId);
                        }
                        preValue = HistoricalNav[code][dateId];
                        preDateId = dateId;
                        if(ret != double.MaxValue)
                        {
                            if (!MfReturns.ContainsKey(code))
                                MfReturns.Add(code, new List<double>());
                            MfReturns[code].Add(ret);
                        }
                    }
                    else
                    {
                        sb.Append($",{preValue}");
                    }
                }
            }
            File.WriteAllText(fileName, sb.ToString());
        }

        internal static string[] SplitStringBySeparator(string input, char separator = ',')
        {
            return input.Split(new char[] { separator });
        }
    }
}
