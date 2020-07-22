using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private delegate void SafeCallDelegate(string text);
        private SocketListener sockListener = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConfigJson userConfigJson =  Config.LoadJson();
            Thread listener = new Thread(RunListener);
            listener.Start(this);
            // MessageBox.Show(userConfigJson.Passport, userConfigJson.Servers[0], MessageBoxButtons.YesNo);
        }

        private void RunListener(object mainForm)
        {
            Form1 form = mainForm as Form1;
            sockListener = SocketListener.Instance();
            sockListener.Init(form);
        }

        private void label1_Click(object sender, EventArgs e)
        {
            RC4 rc4 = new RC4("niceDayIn2020@998", Encoding.UTF8);
            byte[] results1 = rc4.Encrypt("1111");
            byte[] results2 = rc4.Encrypt("11");
            // string reuslt = Convert.ToBase64String(RC4.Encrypt("ABCDDDDDDDDDDDDDDDDDDDDDD", "ToolGood", Encoding.UTF8));
            SetLogTextBox(BitConverter.ToString(results1) + BitConverter.ToString(results2));
        }
        public void SetLogTextBox(String logStr)
        {
            if (this.textBox1.InvokeRequired)
            {
                var d = new SafeCallDelegate(SetLogTextBox);
                this.textBox1.Invoke(d, new object[] { logStr });
            }
            else
            {
                this.textBox1.AppendText(logStr + "\r\n");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.notifyIcon1.Visible = true;
                this.Hide();
                return;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Visible)
            {
                this.WindowState = FormWindowState.Minimized;
                this.notifyIcon1.Visible = true;
                this.Hide();
            } else
            {
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }
    }
}
