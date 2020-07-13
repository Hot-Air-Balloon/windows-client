using System;
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
            SocketListener.Instance().Init(form);
        }

        private void label1_Click(object sender, EventArgs e)
        {

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
    }
}
