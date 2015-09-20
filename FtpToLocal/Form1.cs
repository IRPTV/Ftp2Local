using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FtpToLocal
{
    public partial class Form1 : Form
    {
        int _CurIndex = -1;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label5.Text = System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim();
            label6.Text = System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim();
            label7.Text = System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim();
            label8.Text = System.Configuration.ConfigurationSettings.AppSettings["DestRoot"].Trim().Replace("\\\\", "\\");
            label10.Text = System.Configuration.ConfigurationSettings.AppSettings["Extention"].Trim();
            label12.Text = System.Configuration.ConfigurationSettings.AppSettings["Interval"].Trim();
            timer1.Interval = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["Interval"].Trim()) * 1000;
            button1_Click(null, null);

        }
        protected void LoadList()
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim());
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                    System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                request.UsePassive = bool.Parse(System.Configuration.ConfigurationSettings.AppSettings["Passive"].Trim());
                request.UseBinary = true;
                request.KeepAlive = false;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                DataTable DTable = new DataTable();
                DataColumn col1 = new DataColumn("#");
                DataColumn col2 = new DataColumn("FileName");
                DataColumn col3 = new DataColumn("Status");
                DTable.Columns.Add(col1);
                DTable.Columns.Add(col2);
                DTable.Columns.Add(col3);
                int RowIndex = 0;
                while (!reader.EndOfStream)
                {
                    Application.DoEvents();
                    string Ln = reader.ReadLine();
                    if (IsFile(Ln))
                    {
                        //  richTextBox1.AppendText(Ln + "\n");


                        RowIndex++;
                        DataRow row = DTable.NewRow();
                        row[col1] = RowIndex.ToString();
                        row[col2] = System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim() + "/" + Ln;
                        row[col3] = "Waiting";
                        DTable.Rows.Add(row);
                    }
                }
                //Clean-up
                reader.Close();
                responseStream.Close(); //redundant
                response.Close();

                if (DTable.Rows.Count > 0)
                {
                    dataGridView1.Rows.Clear();
                }

                dataGridView1.DataSource = DTable;
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
                }
                dataGridView1.Columns[2].Width = 85;
                dataGridView1.Columns[0].Width = 50;
                dataGridView1.Columns[1].Width = 400;
            }
            catch (Exception Exp)
            {
                richTextBox1.Text += "Ftp Connection Error : \n " + Exp + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                timer1.Enabled = true;
                //Application.DoEvents();
            }


        }
        public bool IsFile(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentOutOfRangeException(); // or however you want to handle null values
            }

            else if (System.IO.Path.GetExtension(directory) == string.Empty)  // returns string.Empty when no extension found
            {
                return false;
            }
            else
            {
                string Str = System.IO.Path.GetExtension(directory);
                if (Str.ToLower() == System.Configuration.ConfigurationSettings.AppSettings["Extention"].Trim())
                {
                    return true; // extension found, therefore it's a file
                }
                else
                {
                    return false;
                }
                // return true;
            }
        }
        protected int QeueProcess()
        {
            int Index = -1;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "Waiting")
                {
                    Index = i;
                    return Index;
                }
            }
            return Index;
        }
        protected void DownLoadFileInBackground2(int Indx)
        {
            try
            {

                string[] Paths = System.Configuration.ConfigurationSettings.AppSettings["DestRoot"].Trim().Split('#');
                string FilePath = dataGridView1.Rows[Indx].Cells[1].Value.ToString();
                foreach (var item in Paths)
                {
                    FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FilePath);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    request.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                        System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                    request.UsePassive = bool.Parse(System.Configuration.ConfigurationSettings.AppSettings["Passive"].Trim());
                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);
                    if (!Directory.Exists(item))
                    {
                        Directory.CreateDirectory(item);
                    }
                    FileStream file = File.Create(item + Path.GetFileName(FilePath));
                    byte[] buffer = new byte[32 * 1024];
                    int read;
                    FtpWebRequest request2 = (FtpWebRequest)FtpWebRequest.Create(new Uri(FilePath));
                    request2.Method = WebRequestMethods.Ftp.GetFileSize;
                    //string ss= ConfigurationManager.AppSettings["ConnectionString"];
                    request2.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                          System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                    request2.UsePassive = bool.Parse(System.Configuration.ConfigurationSettings.AppSettings["Passive"].Trim());
                    FtpWebResponse result2 = (FtpWebResponse)request2.GetResponse();
                    long length = result2.ContentLength;
                    progressBar1.Maximum = 100;
                    dataGridView1.Rows[_CurIndex].Cells[2].Value = "In Progress";
                    if (dataGridView1.Rows.Count > 0)
                    {
                        dataGridView1.Rows[_CurIndex].Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = _CurIndex;
                    }
                    long bfr = 0;
                    while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bfr += read;
                        int Pr = (int)Math.Ceiling(double.Parse(((bfr * 100) / length).ToString()));
                        progressBar1.Value = Pr;
                        file.Write(buffer, 0, read);
                        label4.Text = Pr.ToString() + "%";
                        Application.DoEvents();
                    }

                    file.Close();
                    responseStream.Close();
                    response.Close();
                }


                // string fileName = "arahimkhan.txt";

                //if (System.Configuration.ConfigurationSettings.AppSettings["Delete"].Trim().ToLower() == "true")
                //{
                FtpWebRequest request3 = (FtpWebRequest)FtpWebRequest.Create(new Uri(FilePath));
                request3.Method = WebRequestMethods.Ftp.DeleteFile;
                request3.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                      System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                request3.UsePassive = bool.Parse(System.Configuration.ConfigurationSettings.AppSettings["Passive"].Trim());
                FtpWebResponse result3 = (FtpWebResponse)request3.GetResponse();
                result3.Close();
                // }

                dataGridView1.Rows[_CurIndex].Cells[2].Value = "Done";
                label14.Text = DateTime.Now.ToString();
                timer1.Enabled = true;

            }
            catch (Exception Exp)
            {
                richTextBox1.Text += "Ftp Download Error : \n " + Exp + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                dataGridView1.Rows[_CurIndex].Cells[2].Value = "Error";
                //Application.DoEvents();

                //dataGridView1.Rows[_CurIndex].Cells[2].Value = "Error" + Exp.Message;
                //_CurIndex = QeueProcess();
                //if (_CurIndex >= 0)
                //{
                //    DownLoadFileInBackground2(_CurIndex);
                //}               
                timer1.Enabled = true;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                button1.Text = "Stop";
                button1.BackColor = Color.Red;
                timer1.Enabled = true;

            }
            else
            {
                timer1.Enabled = false;
                button1.Text = "Start";
                button1.BackColor = Color.MidnightBlue;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            timer1.Enabled = false;
            dataGridView1.DataSource = null;
            label14.Text = DateTime.Now.ToString();
            LoadList();
            _CurIndex = QeueProcess();
            if (_CurIndex >= 0)
            {
                DownLoadFileInBackground2(_CurIndex);
            }
            else
            {
                timer1.Enabled = true;
            }
        }
    }
}
