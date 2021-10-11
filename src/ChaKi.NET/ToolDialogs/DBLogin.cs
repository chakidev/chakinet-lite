using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.Common;
using MySql.Data.MySqlClient;
using ChaKi.Entity.Settings;
using ChaKi.Service.Database;
using ChaKi.Entity;

namespace ChaKi
{
    public partial class DBLogin : Form
    {
        public List<string>  Databases;

        public DBLogin()
        {
            Databases = new List<string>();

            InitializeComponent();

            // デフォルト値を表示
            UserSettings us = UserSettings.GetInstance();
            if (us.DefaultDBServer != null)
            {
                textBox1.Text = us.DefaultDBServer;
            }
            if (us.DefaultDBUser != null)
            {
                textBox2.Text = us.DefaultDBUser;
            }
            if (us.DefaultDBPassword != null)
            {
                textBox3.Text = us.DefaultDBPassword;
            }
            if (us.DefaultDBMS != null)
            {
                comboBox1.Text = us.DefaultDBMS;
            }
        }

        public string DBMS
        {
            get { return comboBox1.Text; }
        }
        public string Server
        {
            get { return textBox1.Text; }
        }

        public string User
        {
            get { return textBox2.Text; }
        }

        public string Password
        {
            get { return textBox3.Text; }
        }


        /// <summary>
        /// Login実行
        /// DB一覧を取得する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string dbms = comboBox1.Text;
            string server = textBox1.Text;
            string user = textBox2.Text;
            string password = textBox3.Text;

            DBParameter param = new DBParameter(dbms, server, user, password);
            try
            {
                DBService svc = DBService.Create(param);
                svc.GetDatabaseList(ref this.Databases);

                UserSettings us = UserSettings.GetInstance();
                us.DefaultDBMS = dbms;
                us.DefaultDBServer = server;
                us.DefaultDBUser = user;
                us.DefaultDBPassword = password;

                this.Close();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Login Error: {0}", ex.Message);
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
