//using System;
//using System.Windows.Forms;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
////using excel = Microsoft.Office.Interop.Excel;
//using System.Collections.Generic;

//namespace MutualFund
//{
//    class ExcelGraph
//    {
//        public bool CreateGraph(List<DateTime> x, List<double> y = null, Dictionary<DateTime, Dictionary<string, double>> returns = null)
//        {
//            bool rv = false;
//            try
//            {
//                excel.Application xlApp;
//                excel.Workbook wb;
//                excel.Worksheet ws;
//                object misValue = System.Reflection.Missing.Value;

//                DateTime baseDate = new DateTime(1900, 1, 1);

//                xlApp = new excel.Application();
//                wb = xlApp.Workbooks.Add(misValue);
//                ws = (excel.Worksheet)wb.Worksheets.get_Item(1);

//                HashSet<string> mfNames = new HashSet<string>();
//                foreach (var item in returns.Keys)
//                {
//                    foreach (var name in returns[item].Keys)
//                    {
//                        if (!mfNames.Contains(name))
//                            mfNames.Add(name);
//                    }
//                }
//                if (y == null)
//                {
//                    var t = GetDummyData();
//                    y = t.Item2;
//                }
//                DateTime minDate = x[0];
//                ws.Cells[1, 1] = "Date";
//                ws.Cells[1, 2] = "Mutual Fund Return";
//                ws.Cells[1, 3] = "Sensex Return";
//                ws.Cells[1, 5] = "Return";
//                if (returns != null)
//                {
//                    int ctr = 6;
//                    foreach (var item in mfNames)
//                    {
//                        ws.Cells[1, ctr++] = item;
//                    }
//                }
//                double maxReturn = double.MinValue;
//                double minReturn = double.MaxValue;

//                int minDayInt = int.MaxValue;
//                int maxDayInt = int.MinValue;

//                Dictionary<string, double> ubBoundsByMf = new Dictionary<string, double>();
//                Dictionary<string, double> lbBoundsByMf = new Dictionary<string, double>();
//                Dictionary<string, int> ubDateBoundsByMf = new Dictionary<string, int>();
//                Dictionary<string, int> lbDateBoundsByMf = new Dictionary<string, int>();

//                foreach (var name in mfNames)
//                {
//                    ubBoundsByMf[name] = double.MinValue;
//                    lbBoundsByMf[name] = double.MaxValue;
//                    ubDateBoundsByMf[name] = int.MinValue;
//                    lbDateBoundsByMf[name] = int.MaxValue;
//                }
//                List<double> sensexReturns = new List<double>();
//                try
//                {
//                    sensexReturns = Sensex.GetReturnByDate(x).Values.ToList();
//                }
//                catch
//                {
//                    Console.WriteLine("Sensex website is not available!");
//                }
//                for (int i = 1; i < y.Count; i++)
//                {
//                    int dayDiff = (x[i] - baseDate).Days + 1;
//                    ws.Cells[i + 1, 1] = x[i];
//                    ws.Cells[i + 1, 2] = y[i];
//                    minReturn = Math.Min(y[i], minReturn);
//                    maxReturn = Math.Max(y[i], maxReturn);
//                    if (sensexReturns.Count > i)
//                    {
//                        ws.Cells[i + 1, 3] = sensexReturns[i];
//                        minReturn = Math.Min(sensexReturns[i], minReturn);
//                        maxReturn = Math.Max(sensexReturns[i], maxReturn);
//                    }

//                    minDayInt = Math.Min(minDayInt, dayDiff - 1);
//                    maxDayInt = Math.Max(maxDayInt, dayDiff + 1);

//                    ws.Cells[i + 1, 5] = x[i];
//                    if (returns != null)
//                    {
//                        int ctr = 6;
//                        double preValue = double.MinValue;
//                        foreach (var item in mfNames)
//                        {
//                            if (returns.ContainsKey(x[i]) && returns[x[i]].ContainsKey(item))
//                            {
//                                if (returns[x[i]][item] > double.MinValue)
//                                {
//                                    int dayDiff2 = (x[i] - baseDate).Days + 1;
//                                    ws.Cells[i + 1, ctr] = returns[x[i]][item];
//                                    preValue = returns[x[i]][item];
//                                    lbBoundsByMf[item] = Math.Min(returns[x[i]][item], lbBoundsByMf[item]);
//                                    ubBoundsByMf[item] = Math.Max(returns[x[i]][item], ubBoundsByMf[item]);
//                                    ubDateBoundsByMf[item] = Math.Max(ubDateBoundsByMf[item], dayDiff2 + 1);
//                                    lbDateBoundsByMf[item] = Math.Min(lbDateBoundsByMf[item], dayDiff2 - 1);
//                                }
//                            }
//                            //else if (preValue != double.MinValue)
//                            //{
//                            //    ws.Cells[i + 1, ctr] = preValue;
//                            //}
//                            ctr++;
//                        }
//                    }
//                }
//                Program.ClearScreen();
//                if (sensexReturns.Count > 0)
//                {
//                    Console.Write(Program.separator);
//                    double correl1 = Sensex.Correlation(y.ToArray(), sensexReturns.ToArray(), y.Count - 1);
//                    double correl2 = Sensex.Correlation(y.ToArray(), sensexReturns.ToArray());
//                    Console.WriteLine("Yesterday correlation \t= {0:0.00}%", correl1 * 100);
//                    Console.WriteLine("Today correlation \t= {0:0.00}%\t({1:0.00}%)", correl2 * 100, (correl2 - correl1) * 100 / correl1);
//                }
//                Console.Write(Program.separator);   

//                #region Detaiiled Graph

//                if (mfNames.Count > 0)
//                {
//                    int counter = 1;
//                    foreach (var mfName in mfNames)
//                    {
//                        try
//                        {
//                            excel.ChartObjects xlCharts1 = (excel.ChartObjects)ws.ChartObjects(Type.Missing);
//                            excel.ChartObject myChart1 = (excel.ChartObject)xlCharts1.Add(10, 80, 300, 250);
//                            excel.Chart chartPage1 = myChart1.Chart;

//                            excel.Range chartRange1 = ws.get_Range("A1:" + "A" + y.Count + "," +
//                                         "B1:" + "B" + y.Count + "," +
//                                         "C1:" + "C" + y.Count + "," +
//                                         IndexToColumn(counter + 5) + "1:" + IndexToColumn(counter + 5) + y.Count);

//                            double ub = Math.Max(maxReturn, ubBoundsByMf[mfName]);
//                            double lb = Math.Min(minReturn, lbBoundsByMf[mfName]);

//                            //excel.Range chartRange1 = ws.get_Range("E1", IndexToColumn(mfNames.Count + 5) + y.Count);
//                            chartPage1.SetSourceData(chartRange1);
//                            chartPage1.ChartType = excel.XlChartType.xlXYScatterSmoothNoMarkers;
//                            //chartPage1.HasTitle = true;
//                            //chartPage1.ChartTitle.Text = "Mutual Fund Return";

//                            excel.Axis xAxis1 = chartPage1.Axes(excel.XlAxisType.xlCategory, excel.XlAxisGroup.xlPrimary);
//                            excel.Axis yAxis1 = chartPage1.Axes(excel.XlAxisType.xlValue, excel.XlAxisGroup.xlPrimary);

//                            //xAxis.MinimumScale = x[0];
//                            //xAxis.MaximumScale = x[x.Count - 1];
//                            yAxis1.MaximumScale = Math.Ceiling(ub);
//                            yAxis1.MinimumScale = Math.Floor(lb);

//                            xAxis1.MaximumScale = Math.Ceiling((double)ubDateBoundsByMf[mfName]);
//                            xAxis1.MinimumScale = Math.Floor((double)lbDateBoundsByMf[mfName]);

//                            xAxis1.HasDisplayUnitLabel = false;
//                            yAxis1.HasDisplayUnitLabel = true;
//                            yAxis1.MinorUnit = 0.05;
//                            yAxis1.MajorUnit = 0.5;
//                            yAxis1.HasTitle = true;
//                            yAxis1.AxisTitle.Text = "Returns (%)";
//                            yAxis1.HasMajorGridlines = false;
//                            yAxis1.HasMinorGridlines = false;
//                            chartPage1.HasLegend = true;
                            
//                            chartPage1.Location(excel.XlChartLocation.xlLocationAsNewSheet, Abbreviate(mfName));
//                            counter++;
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine(ex.Message + "\n\n\n" + ex.StackTrace);
//                        }
//                    }
//                }

                

//                #endregion

//                #region Return Graph

//                excel.ChartObjects xlCharts = (excel.ChartObjects)ws.ChartObjects(Type.Missing);
//                excel.ChartObject myChart = (excel.ChartObject)xlCharts.Add(10, 80, 300, 250);
//                excel.Chart chartPage = myChart.Chart;

//                excel.Range chartRange = ws.get_Range("A1", "C" + y.Count);
//                chartPage.SetSourceData(chartRange);
//                chartPage.ChartType = excel.XlChartType.xlXYScatterSmoothNoMarkers;                
//                chartPage.HasTitle = true;
//                chartPage.ChartTitle.Text = "Mutual Fund Return";

//                excel.Axis xAxis = chartPage.Axes(excel.XlAxisType.xlCategory, excel.XlAxisGroup.xlPrimary);
//                excel.Axis yAxis = chartPage.Axes(excel.XlAxisType.xlValue, excel.XlAxisGroup.xlPrimary);

//                //xAxis.MinimumScale = x[0];
//                //xAxis.MaximumScale = x[x.Count - 1];
//                yAxis.MaximumScale = Math.Ceiling(Math.Max((sensexReturns.Count < 1 ? double.MinValue : sensexReturns.Max()), y.Max()));
//                yAxis.MinimumScale = Math.Floor(Math.Min((sensexReturns.Count < 1 ? double.MaxValue : sensexReturns.Min()), y.Min()));
//                yAxis.HasDisplayUnitLabel = true;
//                yAxis.MinorUnit = 0.05;
//                yAxis.MajorUnit = 0.5;
//                yAxis.HasTitle = true;
//                yAxis.AxisTitle.Text = "Returns (%)";
//                yAxis.HasMajorGridlines = false;
//                yAxis.HasMinorGridlines = false;

//                xAxis.MaximumScale = Math.Ceiling((double)maxDayInt + 5);
//                xAxis.MinimumScale = Math.Floor((double)minDayInt);
//                xAxis.HasDisplayUnitLabel = false;
//                xAxis.MinorUnit = 10;
//                xAxis.MajorUnit = 50;
                
//                chartPage.HasLegend = false;

//                chartPage.Location(excel.XlChartLocation.xlLocationAsNewSheet, "Returns");

//                #endregion

//                //xlApp.Windows.Application.ActiveWindow.DisplayGridlines = false;


//                string file = string.Format("{0}/Return.xlsx",
//                    System.IO.Directory.GetCurrentDirectory(),
//                    DateTime.Now.ToString("yyyyMMdd_hhmmss"));
//                wb.SaveAs(file, excel.XlFileFormat.xlWorkbookDefault);
//                wb.Close();

//                //chartPage.Export(imgfile, "PNG", false);

//                xlApp.Quit();

//                releaseObject(ws);
//                releaseObject(wb);
//                releaseObject(xlApp);

//                //Console.WriteLine("Chart saved in {0}", file);

//                rv = true;
//            }
//            catch (Exception ex)
//            {
//                rv = false;
//                Console.WriteLine("{0}\n{1}", ex.Message, ex.StackTrace);
//            }
//            return rv;
//        }
//        private void releaseObject(object obj)
//        {
//            try
//            {
//                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
//                obj = null;
//            }
//            catch (Exception ex)
//            {
//                obj = null;
//                MessageBox.Show("Exception Occured while releasing object " + ex.ToString());
//            }
//            finally
//            {
//                GC.Collect();
//            }
//        }
//        public Tuple<List<int>, List<double>> GetDummyData()
//        {
//            List<int> x = new List<int>();
//            List<double> y = new List<double>();
//            for (int i = 0; i <= 100; i++)
//            {
//                x.Add(i);
//                double angle = i * 10 * Math.PI / 180.0;
//                y.Add(Math.Sin(angle));
//            }
//            return Tuple.Create(x, y);
//        }
//        static readonly string[] Columns = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ", "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH" };
//        public static string IndexToColumn(int index)
//        {
//            if (index <= 0)
//                throw new IndexOutOfRangeException("index must be a positive number");

//            return Columns[index - 1];
//        }
//        public static string Abbreviate(string s)
//        {
//            var arr = s.Split(new char[] { ' ', '(', ')' });
//            string rv = "";
//            foreach (var elem in arr)
//            {
//                if (elem.Length < 3)
//                    continue;
//                rv += elem.Substring(0, 1);
//            }
//            return rv;
//        }
        
//    }
    
//}
