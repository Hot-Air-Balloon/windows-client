using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HotAirBalloon.sysyemProxy;
using System.Net.Sockets;
using System.Threading.Tasks;
using WatsonWebserver;
using System.Collections;

namespace HotAirBalloon
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
            this.radioButton1.Checked = true;
            SystemProxy.UpdateSystemProxy(1);
            Thread listener = new Thread(RunListener);
            listener.Start(this);
            // MessageBox.Show(userConfigJson.Passport, userConfigJson.Servers[0], MessageBoxButtons.YesNo);
        }

        private void RunListener(object mainForm)
        {
            Form1 form = mainForm as Form1;
            Thread httpListener = new Thread(SimpleListenerExample);
            httpListener.Start();
            Thread httpProxyListener = new Thread(httpProxy);
            httpProxyListener.Start(new ArrayList { form });
            sockListener = SocketListener.Instance();
            sockListener.Init(form);
        }
        public void httpProxy (object arrayList)
        {
            ArrayList _arrayList = arrayList as ArrayList;
            Form1 form = (Form1)_arrayList[0];
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8009);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Task.Run(() => ProcessConnection(client, form));
            }
        }
        public void ProcessConnection(TcpClient client, Form1 form)
        {
            try
            {
                Stream httpStream = new System.IO.MemoryStream();
                // HttpRequest req = HttpRequest.FromTcpClient(client);
                byte[] myReadBuffer = new byte[1024 * 1024];
                NetworkStream clientStream = client.GetStream();
                int numberOfBytesRead = clientStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                httpStream.Write(myReadBuffer, 0, numberOfBytesRead);
                form.SetLogTextBox(Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                httpStream.Position = 0;
                HttpRequest req = HttpRequest.FromStream(httpStream);

                Dictionary<string, object> targetinfo = new Dictionary<string, object>();
                targetinfo.Add("addr", req.DestHostname);
                targetinfo.Add("port", (short)req.DestHostPort);
                if (req.Method == WatsonWebserver.HttpMethod.CONNECT)
                {
                    form.SetLogTextBox(" proxying request via connect to " + req.DestHostname + ":" + req.DestHostPort);
                    string resp = "HTTP/1.1 200 Connection Established\r\n\r\n";
                    clientStream.Write(Encoding.UTF8.GetBytes(resp), 0, resp.Length);
                    // 兼容 tcpclient的情况，第一次
                    Tunnel mytunnel = new Tunnel(client, clientStream, targetinfo, Config.LoadJson(), form, new byte[1], 0);
                    return;
                } else
                {
                    if (req.DestHostname == "" || req.DestHostPort == 0)
                    {
                        string resp = "HTTP/1.1 200 Connection Established\r\n\r\n";
                        clientStream.Write(Encoding.UTF8.GetBytes(resp), 0, resp.Length);
                        client.Close();
                        return;
                    }
                    // 如果时直接连接，就转发请求到对应
                    form.SetLogTextBox(" proxying http " + req.DestHostname + ":" + req.DestHostPort);
                    Tunnel mytunnel = new Tunnel(client, clientStream, targetinfo, Config.LoadJson(), form, myReadBuffer, numberOfBytesRead);
                    return;
                }
            } catch (Exception)
            {
                // string resp = "HTTP/1.1 200 Connection Established\r\n\r\n";
                // clientStream.Write(Encoding.UTF8.GetBytes(resp), 0, resp.Length);
                client.Close();
            }
        }
        public static void SimpleListenerExample()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            //if (prefixes == null || prefixes.Length == 0)
            //    throw new ArgumentException("prefixes");

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            // foreach (string s in prefixes)
            //{
            // Random paccounter = new Random();
            listener.Prefixes.Add($"http://127.0.0.1:8008/pac/");
            //}
            listener.Start();
            while(true)
            {
                try
                {
                    // Note: The GetContext method blocks while waiting for a request.
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    // Construct a response.
                    using (StreamReader r = new StreamReader("MyPac.js"))
                    {
                        string responseString = r.ReadToEnd();
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        // Get a response stream and write the response to it.
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/x-ns-proxy-autoconfig";
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        // You must close the output stream.
                        output.Close();
                    }
                } catch (Exception)
                {
                    listener.Stop();
                }
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {
            SimpleListenerExample();
        }
        public void SetLogTextBox(String logStr)
        {
            try
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
            } catch (Exception)
            {

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

        private void 最小化ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SystemProxy.UpdateSystemProxy(2);
            (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SystemProxy.UpdateSystemProxy(1);
            this.notifyIcon1.Visible = false;
            this.Close();
            this.Dispose();
            Application.Exit();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Checked)
            {
                SystemProxy.UpdateSystemProxy(1);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Checked)
            {
                SystemProxy.UpdateSystemProxy(2);
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Checked)
            {
                SystemProxy.UpdateSystemProxy(3);
            }
        }
    }
}
