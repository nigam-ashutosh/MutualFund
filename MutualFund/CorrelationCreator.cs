using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutualFund
{
    internal static class CorrelationCreator
    {
        static Dictionary<string, Dictionary<string, double>> correlationMatrix = new Dictionary<string, Dictionary<string, double>>();

        static bool created = false;

        static Dictionary<double, List<Tuple<string, string>>> correlationToMfs = new Dictionary<double, List<Tuple<string, string>>>();
        internal static void Create(Dictionary<string, Dictionary<DateTime, double>> returnsByMf)
        {
            Console.WriteLine("Press 0 for correlation study any other key for main menu");
            int ret = -1;
            bool success=  int.TryParse(Console.ReadLine(), out ret);
            if (!success || ret != 0)
                return;
            Program.ClearScreen();
            if (created)
                return;
            int mfCount = returnsByMf.Count;

            for (int i = 0; i < mfCount; i++)
            {
                for (int j = i + 1; j < mfCount; j++)
                {
                    var mf1 = returnsByMf.Keys.ElementAt(i);
                    var mf2 = returnsByMf.Keys.ElementAt(j);

                    var corr = GetCorrelation(returnsByMf[mf1], returnsByMf[mf2]) * 100;
                    if (!correlationMatrix.ContainsKey(mf1))
                        correlationMatrix.Add(mf1, new Dictionary<string, double>());
                    correlationMatrix[mf1].Add(mf2, corr);

                    if (!correlationToMfs.ContainsKey(corr))
                        correlationToMfs.Add(corr, new List<Tuple<string, string>>());
                    correlationToMfs[corr].Add(Tuple.Create(mf1, mf2));
                }
            }
            Print();
            created = true;
        }

        private static double GetCorrelation(Dictionary<DateTime, double> r1, Dictionary<DateTime, double> r2)
        {
            List<double> retList1 = new List<double>();
            List<double> retList2 = new List<double>();
            bool updated = false;
            foreach (var date in r1.Keys)
            {
                if (!r2.ContainsKey(date))
                    continue;
                retList1.Add(r1[date]);
                retList2.Add(r2[date]);
                if(retList1.Count != retList2.Count)
                    { }
                updated = true;
            }
            if(!updated)
                return 0;

            return Sensex.Correlation(retList1.ToArray(), retList2.ToArray());
        }

        private static void Print()
        {
            var names = correlationMatrix.Keys;
            StringBuilder sb = new StringBuilder();
            sb.Append(",");
            //first print the header
            foreach (var name in names)
            {
                sb.Append(name + ",");
            }
            sb.AppendLine();

            //now print values
            foreach (var name1 in names)
            {
                sb.Append(name1 + ",");
                foreach (var name2 in names)
                {
                    string val = "";
                    if (correlationMatrix[name1].ContainsKey(name2))
                        val = correlationMatrix[name1][name2].ToString("N4") + "%";
                    else if (correlationMatrix[name2].ContainsKey(name1))
                        val = correlationMatrix[name2][name1].ToString("N4") + "%";
                    sb.Append(val + ",");
                }
                sb.AppendLine();
            }
            System.IO.File.WriteAllText("CorrelationMatrix.csv", sb.ToString());

            var sortedCorr = correlationToMfs.Keys.Where(x => x > 90).OrderByDescending(x => x);
            int counter = 0;
            int maxCounter = int.MaxValue;
            Console.WriteLine("Recommentation for replacement (based on correlation)");
            Console.WriteLine("{0}{1}{2}",
                       Program.DoSpacing("Mutual Fund 1", 40),
                       Program.DoSpacing("Mutual Fund 2", 40),
                       Program.DoSpacing("Correlation", 20));
            foreach (var corr in sortedCorr)
            {
                foreach (var tuple in correlationToMfs[corr])
                {
                    if (tuple.Item1 == "DSP Bla Tax Sav. R. (G)"
                        || tuple.Item1 == "DSP Bla Tax Sav. R. (G)")
                        continue;
                    counter++;
                    Console.WriteLine("{0}{1}{2}", 
                        Program.DoSpacing(tuple.Item1, 40),
                        Program.DoSpacing(tuple.Item2, 40), 
                        Program.DoSpacing(corr.ToString("N2") + "%", 20));
                    if (counter > maxCounter)
                        break;
                }
                if (counter > maxCounter)
                    break;
            }
        }

        
    }
}
