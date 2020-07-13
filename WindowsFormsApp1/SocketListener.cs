using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class SocketListener
    {
        private static SocketListener listenerIns;
        private Socket server;
        private IPAddress ipAddress;
        private int port;
        private List<Socket> connectSocketList = new List<Socket>();
        private Dictionary<int, List<Thread>> recvThreadList = new Dictionary<int, List<Thread>>();
        private Form1 mainForm;

        public static SocketListener Instance()
        {
            if (listenerIns == null)
            {
                listenerIns = new SocketListener();
            }
            return listenerIns;
        }

        public void Init(Form1 form)
        {
            ipAddress = IPAddress.Parse("127.0.0.1");
            port = 1088;
            mainForm = form;
            mainForm.SetLogTextBox("SocketListener init");
            CreateSocket();
            BindAndListen();
            WaitClientConnection();
        }

        private void CreateSocket()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void BindAndListen()
        {
            server.Bind(new IPEndPoint(ipAddress, port));
            server.Listen(1024);
        }

        private void WaitClientConnection()
        {
            int index = 1;
            while (true)
            {
                mainForm.SetLogTextBox("当前链接数量: " + recvThreadList.Count);
                mainForm.SetLogTextBox("等待客户端链接: ");
                Socket ClientSocket = server.Accept();
                if (ClientSocket != null)
                {
                    mainForm.SetLogTextBox(string.Format("{0}链接成功! ", ClientSocket.RemoteEndPoint));
                    connectSocketList.Add(ClientSocket);
                    Thread recv = new Thread(HandleSocket);
                    recv.Start(new ArrayList { index, ClientSocket });
                    recvThreadList.Add(index, new List<Thread> { recv });
                    index++;
                }
            }
        }
        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private void HandleSocket(object clientSocket)
        {
            ArrayList arrayList = clientSocket as ArrayList;
            int index = (int)arrayList[0];
            Socket clientSock = arrayList[1] as Socket;
            while (true)
            {
                try
                {
                    if (clientSock.Poll(-1, SelectMode.SelectRead) && clientSock.Available == 0)
                    {
                        break;
                    }
                    byte[] strbyte = new byte[256];
                    int count = clientSock.Receive(strbyte);
                    if (count != 256)
                    {
                        mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端初始化数据错误", index));
                        break;
                    }
                    clientSock.Send(StringToByteArray("0500"));
                    byte[] addrType = new byte[4];
                    clientSock.Receive(addrType);
                    if (addrType[0] != 1)
                    {
                        mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端connect错误", index));
                        break;
                    }
                    switch (addrType[3])
                    {
                        case 1:

                            break;
                        case 2:
                            break;
                        case 3:
                            break;
                    }
                    if (count > 0)
                    {
                        string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                        mainForm.SetLogTextBox(string.Format("{0}给你发送了消息：{1}", clientSock.RemoteEndPoint, ret));
                        clientSock.Send(Encoding.UTF8.GetBytes("test"));
                    }
                } catch (Exception)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端异常离去！", index));
                    recvThreadList[index][0].Abort();
                }
            }
            mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端正常离去！", index));
            clientSock.Close();
        }
    }
}
