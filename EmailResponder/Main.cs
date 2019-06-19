using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenPop.Pop3;
using OpenPop.Mime;
using OpenPop.Mime.Header;
using System.Net;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Net.Mail;
using System.Net.Mime;
using System.IO;
using System.Web;
using System.Text.RegularExpressions;
using EASendMail;

namespace EmailResponder
{
    public partial class EmailResponder : Form
    {
        int count = 0, count2 = 0, process, t1busy = 0, t2busy = 0,selectsmtp=0;
        string date1,reply_email="",user="";
        Thread t1, t2;

        int popactive = 0, smtpactive = 0;
        public int found = 0;

        Licence li = new Licence();
        public string table = "saiful", list="\\smtp.txt",poplist="\\pop.txt", error_file = "\\error.txt";



        System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("127.0.0.1");



        public EmailResponder()
        {
            InitializeComponent();

        }

        public EmailResponder(string tbname, double days, string user)
        {
            InitializeComponent();
            this.table = tbname;
            string frmtext = "Email Responder - Powered by FnF Soft ( User : " +user+ " | " + days + " days remaining.)";
            this.Text = frmtext;
            this.user = user;

            loadingPOPList();


        }

        private void btnStop_Click(object sender, EventArgs e)
        {

        }

        public void showStatus1(string s)
        {
            try
            {
                lblpopstatus.Invoke(new Action(() =>
                {
                    lblpopstatus.Text = s;
                }));
            }
            catch
            {
                return;
            }
        }
        public void showStatus2(string s)
        {

            lblsmtpstatus.Invoke(new Action(() =>
            {
                lblsmtpstatus.Text = s;
            }));
        }

        public void FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {

            string subject, from, body, tempsender, tempsubject;
            this.found = 0;
            this.process = 0;
            int postlink;


            string[] words;
            // The client disconnects from the server when being disposed
            Thread.Sleep(500);
            lblpopstatus.ForeColor = Color.Green;
            showStatus1("Checking POP Server.");

            try
            {

                using (Pop3Client client = new Pop3Client())
                {

                    // Connect to the server
                    client.Connect(hostname, port, useSsl);

                    // Authenticate ourselves towards the server
                    client.Authenticate(username, password, AuthenticationMethod.UsernameAndPassword);

                    // Get the number of messages in the inbox

                    int messageCount = client.GetMessageCount();

                    lblfound.Invoke(new Action(() =>
                    {
                        lblfound.Text = messageCount.ToString();
                    }));

                    if (messageCount > 0)
                    {
                        Thread.Sleep(500);
                        showStatus1("Message Found.");

                    }
                    else
                    {
                        Thread.Sleep(500);
                        showStatus1("No Message Found.");

                        Thread.Sleep(3000);
                        this.t1busy = 0;
                        return;
                    }

                   


                    // We want to download all messages
                    //List<Message> allMessages = new List<Message>(messageCount);

                    OpenPop.Mime.Message message;
                    MessageHeader headers;

                    // Messages are numbered in the interval: [1, messageCount]
                    // Ergo: message numbers are 1-based.
                    // Most servers give the latest message the highest number


                    for (int i = messageCount; i > 0; i--)
                    {

                        Thread.Sleep(500);
                        showStatus1("Message Processing ..... : "+i.ToString());
                        message = client.GetMessage(i);
                        headers = client.GetMessageHeaders(i);

                        date1 = headers.Date;
                        postlink = 0;
                        tempsender = headers.From.ToString();
                        words = tempsender.Split('<');
                        int len = words.Length;
                        if (len > 1)
                        {
                            tempsender = words[1];
                            words = tempsender.Split('>');
                            tempsender = words[0];
                        }
                        else
                        {
                            tempsender = "no_reply@example.com";
                        }
                        //showStatus1("subject : " + headers.Subject.ToString());
                        if (headers.Subject != null)
                        {
                            subject = WebUtility.HtmlEncode(headers.Subject.ToString());
                        }
                        else
                        {
                            subject = "";
                        }
                        from = WebUtility.HtmlEncode(headers.From.ToString());

                        OpenPop.Mime.MessagePart plainText = message.FindFirstPlainTextVersion();

                        if (plainText != null)
                        {
                            body = WebUtility.HtmlEncode(plainText.GetBodyAsText().ToString());
                        }
                        else
                        {
                            OpenPop.Mime.MessagePart htmlText = message.FindFirstHtmlVersion();
                            if (htmlText != null)
                            {
                                body = WebUtility.HtmlEncode(htmlText.GetBodyAsText().ToString());
                            }
                            else
                            {
                                body = WebUtility.HtmlEncode(plainText.GetBodyAsText().ToString());
                            }

                        }

                        //Insert the emails into Database
                        lblname.Invoke(new Action(() =>
                        {
                            lblname.Text = headers.From.ToString();
                        }));

                        lblsubject.Invoke(new Action(() =>
                        {
                            lblsubject.Text = subject;
                        }));

                        tempsubject = subject;

                        if (tempsender == "robot@craigslist.org" || tempsubject.StartsWith("Re:") || tempsubject.StartsWith("RE:"))
                        {
                            postlink = 1;
                        }

                        if (!tempsender.EndsWith("@reply.craigslist.org"))
                        {
                            postlink = 1;
                        }


                        if (InsertMail(subject, from, body, postlink))
                        {
                            Thread.Sleep(500);
                            showStatus1("Successfully Stored into Database");
                            client.DeleteMessage(i);

                        }

                    }

                    // Now return the fetched messages

                }
                this.t1busy = 0;

            }
            catch (Exception ex)
            {
                var lineNumber = new System.Diagnostics.StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                lblpopstatus.ForeColor = Color.Red;
                showStatus1(ex.Message+". line : "+lineNumber.ToString());

                //lblpopstatus.Text = ex.Message + "Fetch Array function";

                Thread.Sleep(30000);
                this.t1busy = 0;
            }
        }



        public bool InsertMail(string subject, string from, string body, int postlink)
        {
            DBConnect dbconnect = new DBConnect();
            bool status = dbconnect.status;
                       
                string query = "INSERT INTO " + table + " (from1,subject1,body,date1,link) VALUES('" + from + "', '" + subject + "', '" + body + "','" + date1 + "','" + postlink + "')";
                if (status == true)
                {
                    try
                    {
                        //create command and assign the query and connection from the constructor
                        MySqlCommand cmd = new MySqlCommand(query, dbconnect.connection);

                        //Execute command
                        cmd.ExecuteNonQuery();

                        this.process++;
                        this.count++;

                        lblprocess.Invoke(new Action(() =>
                        {
                            lblprocess.Text = process.ToString();
                        }));

                        lbltotal.Invoke(new Action(() =>
                        {
                            lbltotal.Text = this.count.ToString();
                        }));



                        dbconnect.closeconnection();
                        return true;
                    }
                    catch (Exception e)
                    {
                    //lblpopstatus.Text = e.Message + "Code insert function";
                    lblpopstatus.ForeColor = Color.Red;
                        showStatus1(e.Message);
                        return false;
                    }
            }
            else
            {
                lblpopstatus.ForeColor = Color.Red;
                Thread.Sleep(500);
                showStatus1("Can not store into database. Database Connection problem");
                return false;
            }



}

       

        private void btnSMTPstart_Click_1(object sender, EventArgs e)
        {
            string line;
            int count = 0;
            if (txtreplymsg.Text == "")
            {
                MessageBox.Show("Please Enter your reply message first.", "Set message");
                return;
            }

            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamReader r = new StreamReader(path + list))
            {

                while ((line = r.ReadLine()) != null)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                MessageBox.Show("NO SMTP Account Added to server.", "SMTP Missing");
            }
            else
            {

                timerOut.Start();
                btnSMTPstart.Enabled = false;
                btnSMTPstop.Enabled = true;
            }
        }

        private void btnSMTPstop_Click_1(object sender, EventArgs e)
        {
            timerOut.Stop();
            // this.t2.Abort();
            btnSMTPstart.Enabled = true;
            btnSMTPstop.Enabled = false;
        }

        private void btnStart_Click_2(object sender, EventArgs e)
        {

            if (popactive == 0)
            {
                MessageBox.Show("Please Configure the POP Server First.", "Configure Missing");
            }
            else
            {
                timerIn.Start();
                btnStart.Enabled = false;                
                btnStop.Enabled = true;
            }

        }

        private void btnconnectpop_Click(object sender, EventArgs e)
        {
            string host, user, pass;
            int port;
            bool ssl;

            host = txtpophost.Text.ToString();
            user = txtpopuser.Text.ToString();
            pass = txtpoppass.Text.ToString();
            ssl = popssl.Checked;
            try
            {
                port = Convert.ToInt32(cmbopopport.Text);
            }
            catch
            {
                return;
            }
            try
            {
                using (Pop3Client client = new Pop3Client())
                {

                    // Connect to the server
                    client.Connect(host, port, ssl);

                    // Authenticate ourselves towards the server
                    client.Authenticate(user, pass);
                    MessageBox.Show("Connected Successfull.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    popactive = 1;


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection unsuccessfull."+ex.Message, "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //lblpopstatus.Text = ex.Message;
            }

        }

        private void btnsmtpconnect_Click(object sender, EventArgs e)
        {
            smtpactive = 0;
            if (SMTP_Test("Hello Brother.", "mikejohan007@gmail.com", "How are you brother, Long days I do not see you.</br>Can you please give me a phone."))
            {
                MessageBox.Show("Connection Successfull.", "Success");
                smtpactive = 1;
            }
            else
                MessageBox.Show("Connection unsuccessfull.", "Failure");
        }



        private void btnshowrefresh_Click(object sender, EventArgs e)
        {
            DBConnect dbconnect4 = new DBConnect();


            dataGridView1.Rows.Clear();            
            dataGridView1.Columns.Clear();
            dataGridView1.Refresh();


            //dataGridView1.ColumnCount = 4;
            dataGridView1.Columns.Add("col1", "Sender Name");
            dataGridView1.Columns[0].Width = 105;
            dataGridView1.Columns.Add("col2", "Subject");
            dataGridView1.Columns[1].Width = 200;
            dataGridView1.Columns.Add("col3", "Location");
            dataGridView1.Columns[2].Width = 130;
            dataGridView1.Columns.Add("col4", "Date & Time");
            dataGridView1.Columns[3].Width = 150;





            bool status = dbconnect4.status;
            string subject, from, body, date2, temp;
            string id;
            string[] row;
            string[] words;
            int count = 0;
            try
            {
                string query2 = "SELECT * FROM " + table + " WHERE link=0";
                if (status == true)
                {

                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd1 = new MySqlCommand(query2, dbconnect4.connection);

                    //Execute command
                    MySqlDataReader reader = cmd1.ExecuteReader();

                    while (reader.Read())
                    {
                        subject = reader.GetString("subject1");
                        from = reader.GetString("from1");

                        body = reader.GetString("body");
                        id = reader.GetString("id");
                        date2 = reader.GetString("date1");

                        subject = WebUtility.HtmlDecode(subject);
                        from = WebUtility.HtmlDecode(from);
                        body = WebUtility.HtmlDecode(body);
                        //MessageBox.Show(from);
                        temp = from;
                        words = temp.Split('"');
                        if (words.Length > 1)
                        {
                            from = words[1];
                        }
                        else
                        {
                            from = temp;
                        }


                        temp = body;
                        words = temp.Split(new string[] { "http://" }, StringSplitOptions.None);
                        if (words.Length > 1)
                        {
                            temp = words[1];
                            words = temp.Split(new string[] { ".craigslist.org/" }, StringSplitOptions.None);

                            if (words.Length > 1)
                            {
                                temp = words[0];
                            }
                            else
                            {
                                temp = "no city";
                            }
                        }
                        else
                        {
                            temp = "no city";
                        }

                        row = new string[] { from, subject, temp, date2 };
                        dataGridView1.Rows.Add(row);

                        count++;


                    }
                    reader.Close();
                    lbltotalcode.Text = count.ToString();
                    dbconnect4.closeconnection();
                }
                if (count == 0)
                    MessageBox.Show("Nothing found.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                //lblpopstatus.Text = ex.Message + "Show Refresh function";
                MessageBox.Show(ex.Message);
            }



        }

        private void button1_Click(object sender, EventArgs e)
        {
            DBConnect dbconnect4 = new DBConnect();
            string query2 = "DELETE FROM " + table;
            try
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd1 = new MySqlCommand(query2, dbconnect4.connection);
                cmd1.ExecuteNonQuery();

                MessageBox.Show("Database Cleared. Please Refresh to get update.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            dbconnect4.closeconnection();

        }

        private void btnStop_Click_1(object sender, EventArgs e)
        {
            timerIn.Stop();
            // this.t1.Abort();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string host, user, pass;
            int port;
            bool ssl;




            if (this.smtpactive == 1)
            {

                host = txtsmtphost.Text.ToString();
                user = txtsmtpuser.Text.ToString();
                pass = txtsmtppass.Text.ToString();
                ssl = smtpssl.Checked;
                port = Convert.ToInt32(cmbosmtpport.Text);
                string line = host + "," + user + "," + pass + "," + port + "," + ssl;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@path + list, true))
                {
                    file.WriteLine(line);
                    MessageBox.Show("SMTP Account is successfully added.", "Success");
                }
                this.smtpactive = 0;
                listSMTP.Items.Clear();
                loadingSMTPList();


            }
            else
            {
                MessageBox.Show("Enter your SMTP Information and Connect.", "SMTP NOT ACTIVE");
            }
        }

        public string[] RandomSMTP()
        {
            string line;
            int count = 0;
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


            if (!File.Exists(@path + list))
            {
                MessageBox.Show("List Not found. Select list and Save first", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return null;
            }
            


            using (StreamReader r = new StreamReader(path + list))
            {

                while ((line = r.ReadLine()) != null)
                {
                    count++;
                }
                line = r.ReadToEnd();

            }
            Random r1 = new Random();
            line = null;

            if (count > 0)
            {
                int i;
                if (selectsmtp<count)
                {                   
                    selectsmtp++;
                    //tempsmtp = selectsmtp;
                }
                else
                {
                    selectsmtp = 1;                    
                }
                i = selectsmtp;



                count = 0;

                using (StreamReader r = new StreamReader(path + list))
                {

                    while ((line = r.ReadLine()) != null)
                    {
                        count++;
                        if (i == count)
                        {
                            break;
                        }
                    }
                    return line.Split(',');

                }
            }
            else
            {
                return null;
            }



        }

        public void loadingSMTPList()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] lines;
            if (File.Exists(@path + list))
                lines = System.IO.File.ReadAllLines(@path + list);
            else
            {
                MessageBox.Show("List Not found. Select list and Save first", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            string[] words;
            listSMTP.Refresh();
            // Display the file contents by using a foreach loop.

            foreach (string line1 in lines)
            {
                // Use a tab to indent each line of the file.
                words = line1.Split(',');
                listSMTP.Items.Add(words[1]);


            }
        }

        public void loadingPOPList()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] lines;
            if (File.Exists(@path + poplist))
                lines = System.IO.File.ReadAllLines(@path + poplist);
            else
            {
                //MessageBox.Show("List Not found. Select list and Save first", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            string[] words;
            cmbopoplist.Refresh();
            // Display the file contents by using a foreach loop.

            foreach (string line1 in lines)
            {
                // Use a tab to indent each line of the file.
                words = line1.Split(',');
                cmbopoplist.Items.Add(words[1]);


            }
        }



        private void timerOut_Tick(object sender, EventArgs e)
        {


            if (this.t2busy == 0)
            {
                this.t2busy = 1;
                this.t2 = new Thread(() => replycode());
                this.t2.Start();

            }
        }
      

        private void btnsmtprefresh_Click_1(object sender, EventArgs e)
        {           
            listSMTP.Items.Clear();
            loadingSMTPList();
        
    }

        private void button2_Click_1(object sender, EventArgs e)
        {
           
            string tempFile = Path.GetTempFileName();
            string email;
            try
            {
                email = listSMTP.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Please Select a Email from list.", "Select First", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }

            String loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (var sr = new StreamReader(loc + list))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.Contains(email))
                        sw.WriteLine(line);
                }
            }

            File.Delete(loc + list);
            File.Move(tempFile, loc + list);

            MessageBox.Show("Successfully Deleted from System. Refresh now.", "Success");

            listSMTP.Items.Clear();
            loadingSMTPList();

        }

        private void EmailResponder_Load(object sender, EventArgs e)
        {
            listSMTP.Items.Clear();
        }

        private void cmbopoplist_SelectedIndexChanged(object sender, EventArgs e)
        {

            string tempFile = Path.GetTempFileName();
            string email;
            string[] words;
            //string ssl="";
                        
            email = cmbopoplist.SelectedItem.ToString();           

            String loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (var sr = new StreamReader(loc + poplist))
            
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains(email))
                    {
                        words = line.Split(',');
                        //listSMTP.Items.Add(words[1]);
                        txtpophost.Text = words[0];
                        txtpopuser.Text = words[1];
                        txtpoppass.Text = words[2];
                        cmbopopport.Text = words[3];
                        if (words[4] == "True")
                        {
                            popssl.Checked = true;
                        }
                        else
                        {
                            popssl.Checked = false;
                        }
                        break;
                    }
                }
            }            

        }

        private void button3_Click(object sender, EventArgs e)
        {

            string tempFile = Path.GetTempFileName();
            string email;
            try
            {
                email = cmbopoplist.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Please Select a Email from list.", "Select First", MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }

            String loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (var sr = new StreamReader(loc + poplist))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (!line.Contains(email))
                        sw.WriteLine(line);
                }
            }

            File.Delete(loc + poplist);
            File.Move(tempFile, loc + poplist);

            MessageBox.Show("Successfully Deleted from System. Refresh now.", "Success");
            cmbopoplist.Items.Clear();
            loadingPOPList();
        }

        private void btndelayset_Click(object sender, EventArgs e)
        {
            int delay = Convert.ToInt32(txtdelay.Text);
            timerOut.Interval = delay;
            MessageBox.Show("SMTP Delay Time is Successfully Set", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnpopdelayset_Click(object sender, EventArgs e)
        {
            int delay = Convert.ToInt32(txtpopdelay.Text);
            timerIn.Interval = delay;
            MessageBox.Show("POP Delay Time is Successfully Set", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnsenthistoryclear_Click(object sender, EventArgs e)
        {

            DBConnect dbconnect4 = new DBConnect();
            string query2 = "DELETE FROM " + table + " WHERE status = 1";
            try
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd1 = new MySqlCommand(query2, dbconnect4.connection);
                cmd1.ExecuteNonQuery();

                MessageBox.Show("Database Cleared. Please Refresh to get update.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            dbconnect4.closeconnection();

        }

        private void btnpopsave_Click(object sender, EventArgs e)
        {
            string host, user, pass;
            int port;
            bool ssl;


            
            if (this.popactive == 1)
            {

                host = txtpophost.Text.ToString();
                user = txtpopuser.Text.ToString();
                pass = txtpoppass.Text.ToString();
                ssl = popssl.Checked;
                port = Convert.ToInt32(cmbopopport.Text);
                string line = host + "," + user + "," + pass + "," + port + "," + ssl;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@path + poplist, true))
                {
                    file.WriteLine(line);
                    MessageBox.Show("POP Account is successfully added.", "Success");
                    
                }
                cmbopoplist.Items.Clear();
                loadingPOPList();
                this.popactive = 0;
                //listSMTP.Items.Clear();
                //loadingSMTPList();

            }
            else
            {
                MessageBox.Show("Enter your POP Information and Connect Again.", "POP NOT ACTIVE");
            }
        }




        private void EmailResponder_FormClosing(object sender, FormClosingEventArgs e)
        {
            DBConnect dbconnect = new DBConnect("licence");

            try
            {
                string query = "UPDATE licence SET log=0 WHERE user='" + user + "'";

                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, dbconnect.connection);

                //Execute command
                cmd.ExecuteNonQuery();
                dbconnect.closeconnection();
            }

            catch (Exception ex)
            {
                dbconnect.closeconnection();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);                
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text == "List-1")
                this.list = "\\smtp.txt";
            else if(comboBox1.Text== "List-2")
                this.list = "\\smtp2.txt";
            else if (comboBox1.Text == "List-3")
                this.list = "\\smtp3.txt";
            else if (comboBox1.Text == "List-4")
                this.list = "\\smtp4.txt";
            else if (comboBox1.Text == "List-5")
                this.list = "\\smtp5.txt";
            else if (comboBox1.Text == "List-6")
                this.list = "\\smtp6.txt";
            else if (comboBox1.Text == "List-7")
                this.list = "\\smtp7.txt";
            else if (comboBox1.Text == "List-8")
                this.list = "\\smtp8.txt";
            else if (comboBox1.Text == "List-9")
                this.list = "\\smtp9.txt";

            //MessageBox.Show(comboBox1.Text.ToString());
        }

        private void btntestemail_Click_1(object sender, EventArgs e)
        {
           
            string em = txttestemail.Text.ToString();
            string msg = txtreplymsg.Text.ToString();
            string line;
            int count = 0;


            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamReader r = new StreamReader(path + list))
            {

                while ((line = r.ReadLine()) != null)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                MessageBox.Show("NO SMTP Account Added to server.", "SMTP Missing");
                return;
            }

            if (msg == "" || em == "")
            {
                MessageBox.Show("Reply Message or Email is empty. ", "Enter Message", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (SMTP("Welcome Message - FnF", em, msg))
            {
                MessageBox.Show("Successfully Message Sent. Check your inbox.", "Success");
                smtpactive = 1;
            }
            else
            {
                MessageBox.Show("Message Sending Failed. Try Again.", "Failed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        
    }

        private void btnSMTPReport_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Refresh();


            //dataGridView1.ColumnCount = 4;
            dataGridView1.Columns.Add("col1", "Sender Name");
            dataGridView1.Columns[0].Width = 105;
            dataGridView1.Columns.Add("col2", "Email");
            dataGridView1.Columns[1].Width = 150;
            dataGridView1.Columns.Add("col3", "Count");
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns.Add("col4", "Reply Message");
            dataGridView1.Columns[3].Width = 400;


            DBConnect dbconnect4 = new DBConnect();
            bool status = dbconnect4.status;
            string from, reply_email, reply_msg, temp;
           // string id;
            string[] row;
            string[] words;
            int total = 0, count = 0; ;
            try
            {
                string query2 = "SELECT from1,reply_email, reply_msg, COUNT(*) as total FROM " + table + " WHERE link=0 GROUP BY reply_email";
                if (status == true)
                {

                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd1 = new MySqlCommand(query2, dbconnect4.connection);

                    //Execute command
                    MySqlDataReader reader = cmd1.ExecuteReader();

                    while (reader.Read())
                    {
                        //subject = reader.GetString("subject1");
                        from = reader.GetString("from1");

                        reply_email = reader.GetString("reply_email");
                        reply_msg = reader.GetString("reply_msg");
                        total = Convert.ToInt32(reader.GetString("total"));

                        //subject = WebUtility.HtmlDecode(subject);
                        from = WebUtility.HtmlDecode(from);
                        //body = WebUtility.HtmlDecode(body);
                        //MessageBox.Show(from);
                        temp = from;
                        words = temp.Split('"');
                        if (words.Length > 1)
                        {
                            from = words[1];
                        }
                        else
                        {
                            from = temp;
                        }



                        row = new string[] { from, reply_email, total.ToString(), reply_msg };
                        dataGridView1.Rows.Add(row);

                        count++;


                    }
                    reader.Close();
                    lbltotalcode.Text = count.ToString();
                    dbconnect4.closeconnection();
                }
                if (count == 0)
                    MessageBox.Show("Nothing found.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                //lblpopstatus.Text = ex.Message + "Show Refresh function";
                MessageBox.Show(ex.Message);
            }


        }

        private void btnreset_Click(object sender, EventArgs e)
        {
            this.process = 0;
            this.count = 0;
            this.count2 = 0;
            lblfound.Text = "0";
            lblprocess.Text = this.process.ToString();
            lbltotal.Text = this.count.ToString();
            lblreply.Text = this.count2.ToString();

        }     


        private void timerIn_Tick(object sender, EventArgs e)
        {
            string host, user, pass;
            int port;
            bool ssl;

            host = txtpophost.Text.ToString();
            user = txtpopuser.Text.ToString();
            pass = txtpoppass.Text.ToString();
            ssl = popssl.Checked;
            port = Convert.ToInt32(cmbopopport.Text);

            if (this.t1busy == 0)
            {
                this.t1busy = 1;
                this.t1 = new Thread(() => FetchAllMessages(host, port, ssl, user, pass));
                this.t1.Start();

            }
        }



        public void replycode()
        {
            DBConnect dbconnect2 = new DBConnect();
            bool status = dbconnect2.status;
            string subject, from, date2, temp, replymsg;
            string id;
            int found = 0;

            lblsmtpstatus.ForeColor = Color.Green;
            showStatus2("Checking Code in Database");


            try
            {
                string query2 = "SELECT * FROM " + table + " WHERE status=0 and link=0";
                if (status == true)
                {
                    //create command and assign the query and connection from the constructor
                    MySqlCommand cmd1 = new MySqlCommand(query2, dbconnect2.connection);

                    //Execute command
                    MySqlDataReader reader = cmd1.ExecuteReader();

                    while (reader.Read())
                    {
                        found = 1;
                        Thread.Sleep(500);
                        showStatus2("Code Message Found.");

                        subject = reader.GetString("subject1");
                        from = reader.GetString("from1");

                        var body = reader.GetString("body");
                        id = reader.GetString("id");
                        date2 = reader.GetString("date1");
                        temp = from;
                        subject = WebUtility.HtmlDecode(subject);
                        from = WebUtility.HtmlDecode(from);
                        body = WebUtility.HtmlDecode(body);
                        //MessageBox.Show(from);
                        body = ToHtml(body, true);
                        string bo="  ";
                        string[] aa = body.Split(new string[] { "<br>" }, StringSplitOptions.None);
                        foreach(string rr in aa)
                        {
                            bo = bo + "  " + rr + Environment.NewLine;
                        }
                        bo = WebUtility.HtmlDecode(bo);
                        temp = WebUtility.HtmlDecode(temp);
                        temp = temp.Replace('"', ' ');

                        lblname2.Invoke(new Action(() =>
                        {
                            lblname2.Text = from;
                        }));

                        lblsubject2.Invoke(new Action(() =>
                        {
                            lblsubject2.Text = subject;
                        }));

                        replymsg = txtreplymsg.Text.ToString();

                        string abc = replymsg + Environment.NewLine + Environment.NewLine + "On " + date2 + ", " + temp + " wrote:"+ Environment.NewLine + Environment.NewLine + bo;



                        body = abc; //"<div><p>" + replymsg + "</p><p>On " + date2 + ", " + temp + " wrote:</p><div style='border-left: thin double grey;padding-left:10px;margin-left:5px;padding-top:10px;'>" + body + "</div></div>";

                        subject = "Re: " + subject;
                        DBConnect dbconnect3 = new DBConnect();
                        if (dbconnect3.status)
                        {
                            Thread.Sleep(500);
                            showStatus2("Sending a reply of code message.");
                            if (SMTP(subject, from, body))
                            {
                                string query1 = "UPDATE " + table + " SET status =1,reply_msg='"+replymsg+"',reply_email='"+reply_email+"' WHERE id='" + id + "'";
                                //create command and assign the query and connection from the constructor
                                MySqlCommand cmd2 = new MySqlCommand(query1, dbconnect3.connection);

                                //Execute command
                                cmd2.ExecuteNonQuery();
                                this.count2++;

                                lblreply.Invoke(new Action(() =>
                                {
                                    lblreply.Text = this.count2.ToString();
                                }));
                                Thread.Sleep(500);
                                showStatus2("Message Sent Successfully.");

                            }
                            dbconnect3.closeconnection();
                        }
                    }
                    if (found == 0)
                    {
                        Thread.Sleep(500);
                        showStatus2("No Code Message Found.");
                    }
                    reader.Close();
                    dbconnect2.closeconnection();

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(e.Message);
                //lblpopstatus.Text = ex.Message + "Reply function";
                lblsmtpstatus.ForeColor = Color.Red;
                showStatus2(ex.Message);

            }



            this.t2busy = 0;

        }

        public string ToHtml(string text, bool allow)
        {
            //Create a StringBuilder object from the string intput
            //parameter
            StringBuilder sb = new StringBuilder(text);
            //Replace all double white spaces with a single white space
            //and &nbsp;
            sb.Replace(" ", " &nbsp;");
            //Check if HTML tags are not allowed
            if (!allow)
            {
                //Convert the brackets into HTML equivalents
                sb.Replace("<", "&lt;");
                sb.Replace(">", "&gt;");
                //Convert the double quote
                sb.Replace("\"", "&quot;");
            }
            //Create a StringReader from the processed string of 
            //the StringBuilder
            StringReader sr = new StringReader(sb.ToString());
            StringWriter sw = new StringWriter();
            //Loop while next character exists
            while (sr.Peek() > -1)
            {
                //Read a line from the string and store it to a temp
                //variable
                string temp = sr.ReadLine();
                //write the string with the HTML break tag
                //Note here write method writes to a Internal StringBuilder
                //object created automatically
                sw.Write(temp + "<br>");
            }
            //Return the final processed text
            return sw.GetStringBuilder().ToString();
        }


        public bool SMTP(string subject, string from, string body)
        {
            string[] smtpinfo = RandomSMTP();
            if (smtpinfo == null)
                return false;
            string host, user, pass;
            int port;
            //bool ssl=false;

            if (smtpinfo != null && smtpinfo.Length == 5)
            {
                host = smtpinfo[0].ToString();
                user = smtpinfo[1].ToString();
                pass = smtpinfo[2].ToString();

                /*if (smtpinfo[4] == "False")
                {
                    ssl = false;
                }
                else
                {
                    ssl = true;
                }
                */
                port = Convert.ToInt32(smtpinfo[3]);

                Thread.Sleep(500);
                showStatus2("Selected No - "+selectsmtp+" and Email - " + user);

                /*

                MailAddress from1 = new MailAddress(user);
                // "vic kie", System.Text.Encoding.UTF8);
                MailAddress to = new MailAddress(from);
                // Specify the message content.
                MailMessage mail = new MailMessage(from1, to);
                var text = body;
                var html = body;
                mail.Body = body;

                mail.BodyEncoding = UTF8Encoding.UTF8;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                mail.Subject = subject;
                mail.Headers.Add("X-Company", "My Company");
                mail.Headers.Add("X-Location", "Hong Kong");
                mail.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
                mail.IsBodyHtml = true;
                smtp.Host = host;//"ext.websitewelcome.com";
                smtp.Port = port;//25;
                smtp.Credentials = new NetworkCredential(
                user, pass);
                smtp.EnableSsl = ssl;
                */

                //String Result = "";
                try
                {
                    SmtpMail oMail = new SmtpMail("TryIt");
                    EASendMail.SmtpClient oSmtp = new EASendMail.SmtpClient();

                    // Set sender email address, please change it to yours 
                    oMail.From = new EASendMail.MailAddress(user);

                    // Add recipient email address, please change it to yours
                    oMail.To.Add(new EASendMail.MailAddress(from));

                    // Set email subject
                    oMail.Subject = subject;

                    // Set email body
                    oMail.TextBody = body;
                    //oMail.HtmlBody = body;
                    // inserts a auto-submitted header to indicate that
                    // the message was originated by an automatic process, or an automatic
                    // responder, rather than by a human
                    oMail.Headers.Add(new HeaderItem("auto-submitted", "auto-generated"));

                    // Your SMTP server address
                    SmtpServer oServer = new SmtpServer(host);

                    // User and password for ESMTP authentication            
                    oServer.User = user;
                    oServer.Password = pass;
                    oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;
                    // If your SMTP server requires TLS connection on 25 port, please add this line
                    // oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;

                    // If your SMTP server requires SSL connection on 465 port, please add this line
                    //oServer.Port = 587;
                    //oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;
                    oServer.Port = 587;                  
                    this.reply_email = user;
                    oSmtp.SendMail(oServer, oMail);
                    oSmtp.Timeout = 10000;
                    return true;
                }
                catch(Exception ex)
                {
                    string line = ex.Message;
                    String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@path + error_file, true))
                    {
                        file.WriteLine(line);                        
                    }
                    return false;
                }


                /*
                try
                {
                    this.reply_email = user;
                    smtp.Send(mail);
                    smtp.Timeout = 10000;
                    return true;
                }
                catch
                {
                    return false;
                }

                */


            }
            else
            {
               
                return false;
            }
        }



        public bool SMTP_Test(string subject, string from, string body)
        {
            string a;
            string host, user, pass;
            int port;
            bool ssl;

            try
            {
                host = txtsmtphost.Text.ToString();
                user = txtsmtpuser.Text.ToString();
                pass = txtsmtppass.Text.ToString();
                ssl = smtpssl.Checked;

                port = Convert.ToInt32(cmbosmtpport.Text.ToString());
            }
            catch(Exception e)
            {
                //MessageBox.Show(e.Message, "Error");
                a = e.Message;
                return false;
            }

            host = txtsmtphost.Text.ToString();
            user = txtsmtpuser.Text.ToString();
            pass = txtsmtppass.Text.ToString();
            ssl = smtpssl.Checked;

            port = Convert.ToInt32(cmbosmtpport.Text.ToString());



            System.Net.Mail.MailAddress from1 = new System.Net.Mail.MailAddress(user);
            // "vic kie", System.Text.Encoding.UTF8);
            System.Net.Mail.MailAddress to = new System.Net.Mail.MailAddress(from);
            // Specify the message content.
            MailMessage mail = new MailMessage(from1, to);
            var text = body;
            var html = body;

            mail.Body = body;
            mail.Headers.Add("X-Company", "My Company");
            mail.Headers.Add("X-Location", "Hong Kong");


            //send the message
            //SmtpClient smtp = new SmtpClient("127.0.0.1");
            mail.BodyEncoding = UTF8Encoding.UTF8;
            mail.DeliveryNotificationOptions = System.Net.Mail.DeliveryNotificationOptions.OnFailure;
            mail.Subject = subject;
            mail.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, "text/html"));
            //mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
            mail.IsBodyHtml = true;
            smtp.Host = host;//"ext.websitewelcome.com";
            smtp.Port = port;//25;
            smtp.Credentials = new NetworkCredential(
            user, pass);
            smtp.EnableSsl = ssl;

            try
            {
                smtp.Send(mail);
                smtp.Timeout = 10000;
                return true;
            }
            catch
            {
                return false;
            }

        }

    }
}
