using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using MutualFund;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Net.Mail;

namespace EmailService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.ScheduleService();
        }

        protected override void OnStop()
        {
            this.Scheduler.Dispose();
        }
        private Timer Scheduler;
        public void ScheduleService()
        {
            Scheduler = new Timer(new TimerCallback(SchedulerCallBack));
            string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();

            DateTime scheduledTime = DateTime.MinValue;
            if (mode == "Daily")
            {
                scheduledTime = DateTime.Parse(ConfigurationManager.AppSettings["ScheduledTime"]);
                if (DateTime.Now > scheduledTime)
                {
                    scheduledTime.AddDays(1);
                }
            }
            TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
            int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

            Scheduler.Change(dueTime, Timeout.Infinite);
        }

        public void SchedulerCallBack(object e)
        {
            try
            {
                var mfData = Program.Analysis(true, true);

                string userName = ConfigurationManager.AppSettings["UserName"];
                string password = ConfigurationManager.AppSettings["Password"];

                double netProfit = mfData.Sum(t => t.Item6) - mfData.Sum(t => t.Item5);
                double ratio = netProfit / (mfData.Sum(t => t.Item5));

                MailMessage mm = new MailMessage();
                mm.Subject = "[Portfolio Report]: Current Value INR" + ((int)mfData.Sum(t => t.Item6));
                if (netProfit >= 0)
                {
                    mm.Subject += " Up by " + ((int)(100 * ratio)) + "%";
                }
                else
                {
                    mm.Subject += " Down by " + ((int)(-100 * ratio)) + "%";
                }
                mm.Body = "Dear Ashutosh,\n\nPlease find below the detailed report\n";
                int maxLen1 = mfData.Max(t => t.Item1.Length) + 3;
                int maxLen2 = mfData.Max(t => t.Item5.ToString("N" + 0).Length) + 2;
                int maxLen3 = mfData.Max(t => t.Item6.ToString("N" + 0).Length) + 2;
                foreach (var mf in mfData)
                {
                    string line = string.Format("{0}{1}{2}{3}\n",
                                            Program.DoSpacing(mf.Item1, maxLen1),
                                            Program.DoSpacing(mf.Item5, maxLen2, 0),
                                            Program.DoSpacing(mf.Item6, maxLen2, 0),
                                            Program.DoSpacing(mf.Item7 * 100, 6, 0));
                    mm.Body += line;
                }
                mm.IsBodyHtml = true;
                mm.From = new MailAddress(userName);

                mm.To.Add("atn.iitd@gmail.com");
                mm.To.Add("ashutosh.nigam@yahoo.co.in");
                mm.To.Add("ashutosh.nigam@optym.com");

                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.EnableSsl = true;
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential();
                credentials.UserName = userName;
                credentials.Password = password;
                smtp.UseDefaultCredentials = true;

                smtp.Credentials = credentials;
                smtp.Port = 587;
                smtp.Send(mm);

                this.ScheduleService();
            }
            catch (Exception ex)
            {
                string details = "Exception details: \n\n";
                details += DateTime.Now.ToString() + "\n";
                details += ex.Message + "\n";
                details += ex.StackTrace + "\n";

                System.IO.File.WriteAllText("Exception.txt", details);
            }
        }
    }
}
