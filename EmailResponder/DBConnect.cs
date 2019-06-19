using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace EmailResponder
{
    public class DBConnect
    {
        public MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        public bool status;
        public string error;

        //Constructor
        public DBConnect()
        {
            Initialize();
        }

        public DBConnect(string licence)
        {
            server = "fnfhost.com";
            database = "fnfhost_responder";
            uid = "fnfhost_respond";
            password = "fnf@1234";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
            openconnection();
        }

        //Initialize values
        private void Initialize()
        {
            server = "localhost";
            database = "responder";
            uid = "user";
            password = "fnf@1234";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
            openconnection();
        }



        private void openconnection()
        {
            try
            {
                connection.Open();
                this.status= true;
            }

            catch(TimeoutException ex)
            {
                this.status = false;
                this.error = ex.Message;
            }

            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        //MessageBox.Show("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        //MessageBox.Show("Invalid username/password, please try again");
                        break;
                }
                this.status= false;
            }

          

        }

        public void closeconnection()
        {
            connection.Close();
        }
    }
}
