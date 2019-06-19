using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace EmailResponder
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btnlogin_Click(object sender, EventArgs e)
        {
            if(txtuser.Text.ToString()=="" || txtpassword.Text.ToString()=="")
            {
                MessageBox.Show("Please enter username and password...", "Missing",MessageBoxButtons.OK,MessageBoxIcon.Hand);
                return;
            }
            bool status;
            Licence li = new Licence();
            pictureBox1.Visible = true;
            status= li.CheckLicence(txtuser.Text.ToString(), txtpassword.Text.ToString());
            
            if (status)
            {

                DBConnect dbconnect = new DBConnect("licence");

                try
                {
                    string query = "UPDATE licence SET log=1,device_id='" + myCPUInfo() + "' WHERE user='" + txtuser.Text.ToString() + "'";

                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd = new MySqlCommand(query, dbconnect.connection);

                    //Execute command
                    cmd.ExecuteNonQuery();
                    dbconnect.closeconnection();
                   
                }
                catch(Exception ex)
                {
                    dbconnect.closeconnection();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    pictureBox1.Visible = false;
                    return;
                }

                this.Hide();
                EmailResponder frm = new EmailResponder(li.tbname,li.remaindays,txtuser.Text.ToString());
               
                //frm.ShowDialog();
                frm.Closed += (s, args) => this.Close();
                frm.Show();

            }
            else
            {
                pictureBox1.Visible = false;
            }

        }

     

        private void txtpassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnlogin_Click(this, new EventArgs());
            }
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

            if (cpuInfo == null || cpuInfo == "")
                cpuInfo = "BACD234334";
            return cpuInfo;
        }



    }
}
