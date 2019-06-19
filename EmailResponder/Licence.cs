using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace EmailResponder
{
    class Licence
    {
        public double remaindays=0;
        public string tbname;

        public Licence()
        {

        }

        public bool CheckLicence(string user, string pass)
        {
            DBConnect dbconnect = new DBConnect("licence");
            bool status = dbconnect.status;            
            
            DateTime enddate;
            DateTime presentdate;
            TimeSpan remaindate;
            
            int found = 0,log=0;
            string cpuInfo = string.Empty;


            if (status)
            {

                try
                {
                    string query = "SELECT * FROM licence WHERE user='" + user + "' and pass='" + pass + "'";
                    MySqlCommand cmd1 = new MySqlCommand(query, dbconnect.connection);
                    MySqlDataReader reader = cmd1.ExecuteReader();
                    while (reader.Read())
                    {
                        found = 1;
                        enddate = DateTime.Parse(reader.GetString("enddate")).Date;
                        this.tbname = reader.GetString("tbname");
                        log =Convert.ToInt32(reader.GetString("log"));
                        cpuInfo = reader.GetString("device_id");
                        string pdate = date();
                        presentdate = DateTime.Parse(pdate).Date;
                        remaindate = enddate - presentdate;
                        this.remaindays = remaindate.TotalDays;

                    }
                    
                    dbconnect.closeconnection();
                }
                catch(Exception e)
                {
                    
                    dbconnect.closeconnection();
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }

                if (found == 0)
                {
                    MessageBox.Show("Username and Password doesn't match, try again ... ", "Failed Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }

                if (log == 1)
                {
                    if(cpuInfo!= myCPUInfo())
                    {
                        MessageBox.Show("You are already logged in another computer.", "Failed Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return false;
                    }
                }         


                if (this.remaindays > 0)
                {
                    MessageBox.Show("Successfully Logged. You have " + remaindays + " days remaining.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }
                else
                {
                    MessageBox.Show("Your Licence has been Expired. Contact with +8801719870570.", "Licence Expired", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }

            else
            {
                MessageBox.Show("Database Connection Failed. Contact with Administrator.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
            }

            return false;
        }


        public string date()
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
            var response = myHttpWebRequest.GetResponse();
            string todaysDates = response.Headers["date"];
            DateTime dateTime = DateTime.ParseExact(todaysDates, "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
            string[] words = dateTime.ToString().Split(' ');
           // MessageBox.Show(words[0].ToString());

            return words[0].ToString();
        }


        private string myCPUInfo()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (cpuInfo == "")
                {
                    //Get only the first CPU's ID
                    cpuInfo = mo.Properties["processorID"].Value.ToString();
                    break;
                }
            }
            return cpuInfo;
        }

   }
}
