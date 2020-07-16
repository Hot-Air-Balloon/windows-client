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
            port = 1080;
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
            mainForm.SetLogTextBox(string.Format("正在监听{0}：{1} ", ipAddress, port));
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientSocket"></param>
        private void HandleSocket(object clientSocket)
        {
            ArrayList arrayList = clientSocket as ArrayList;
            int index = (int)arrayList[0];
            Socket clientSock = arrayList[1] as Socket;
            String targetAddr = "";
            short targetPort = 0;
            try
            {
                // if (clientSock.Poll(-1, SelectMode.SelectRead) && clientSock.Available == 0)
                // {
                //    clientSock.Close(); ;
                // }
                byte[] strbyte = new byte[256];
                int count = clientSock.Receive(strbyte);
                mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端第一次发送的数据量{1}", index, count));
                if (count == 0)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端初始化数据错误", index));
                    clientSock.Close(); ;
                }
                clientSock.Send(StringToByteArray("0500"));
                byte[] addrType = new byte[4];
                clientSock.Receive(addrType);
                mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端初始化数据{1} {2}", index, addrType[0], addrType[1]));
                if (Convert.ToInt32(addrType[1]) != 1)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端connect错误", index));
                    clientSock.Close(); ;
                }
                IPAddress ipa = null;
                switch (addrType[3])
                {
                    case 1:
                        byte[] addrIP = new byte[4];
                        clientSock.Receive(addrIP);
                        ipa = new IPAddress(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(addrIP, 0)));
                        break;
                    case 3:
                        byte[] addrLen = new byte[1];
                        clientSock.Receive(addrLen);
                        byte[] addrDomain = new byte[1024];
                        clientSock.Receive(addrDomain, addrLen[0], 0);
                        targetAddr = System.Text.Encoding.UTF8.GetString(addrDomain, 0, addrLen[0]);
                        mainForm.SetLogTextBox(targetAddr);
                        break;
                    case 4:
                        byte[] ipv6AddrLen = new byte[16];
                        clientSock.Receive(ipv6AddrLen);
                        ipa = new IPAddress(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ipv6AddrLen, 0)));
                        break;
                }
                byte[] targetPortByte = new byte[2];
                clientSock.Receive(targetPortByte);
                Array.Reverse(targetPortByte);
                targetPort = BitConverter.ToInt16(targetPortByte, 0);
                if (ipa != null)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端connect ip {1}", index, ipa.ToString()));
                }
                if (count > 0)
                {
                    string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                    mainForm.SetLogTextBox(string.Format("{0}给你发送了消息：{1}", clientSock.RemoteEndPoint, ret));
                    // clientSock.Send(Encoding.UTF8.GetBytes("test"));
                }
            } catch (Exception)
            {
                mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端异常离去！", index));
                recvThreadList[index][0].Abort();
            }
            byte[] sendData = new byte[11];
            SocketListener.StringToByteArray("05000001").CopyTo(sendData, 0);
            IPAddress.Parse("127.0.0.1").GetAddressBytes().CopyTo(sendData, 4);
            short sendPort = 1080;
            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sendPort)).CopyTo(sendData, 8);
            mainForm.SetLogTextBox(BitConverter.ToString(sendData));
            clientSock.Send(sendData);
            Socket targetSock = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
            try
            {
                targetSock.Connect(targetAddr, targetPort);
            } catch(Exception)
            {
                mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端 目标连接异常", index));
                clientSock.Close();
                recvThreadList[index][0].Abort();
            }
            while (true)
            {
                try
                {
                    if (clientSock.Poll(1, SelectMode.SelectRead) && clientSock.Available == 0)
                    {
                        break;
                    }
                    if (targetSock.Poll(1, SelectMode.SelectRead) && targetSock.Available == 0)
                    {
                        break;
                    }
                    if (clientSock.Poll(1, SelectMode.SelectRead))
                    {
                        byte[] data = new byte[4096 * 10];
                        int length = clientSock.Receive(data);
                        if (length == 0)
                        {
                            mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端 收到数据是0", index));
                            break;
                        }
                        int sendLength = targetSock.Send(data, length, SocketFlags.None);
                        mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(data));
                        if (length != sendLength)
                        {
                            break;
                        }
                    }
                    if (targetSock.Poll(1, SelectMode.SelectRead))
                    {
                        byte[] _data = new byte[4096 * 10];
                        int tlength = targetSock.Receive(_data);
                        int sendLength = clientSock.Send(_data, tlength, SocketFlags.None);
                        mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(_data));
                        if (tlength != sendLength)
                        {
                            mainForm.SetLogTextBox("数据未发送完");
                            break;
                        }
                    }
                } catch (Exception)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端异常离去！", index));
                    recvThreadList[index][0].Abort();
                }
            }
            mainForm.SetLogTextBox(string.Format("代号为:{0}的客户端正常离去！", index));
            clientSock.Close();
            targetSock.Close();
        }
    }
}
