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

        /**
         * 把16进制字符串传为字节数组
         */
        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
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
        /**
         * 返回客户端需要连接的目标地址和IP
         */
        private Dictionary<string, Object> GetRemoteInfo(Socket clientSock)
        {
            Dictionary<string, Object> targetInfo = new Dictionary<string, Object>();
            targetInfo.Add("addr", "");
            targetInfo.Add("port", 0);
            string targetAddr = "";
            short targetPort = 0;
            // 验证连接是否是socket格式
            try
            {
                byte[] strbyte = new byte[256];
                int count = clientSock.Receive(strbyte);
                mainForm.SetLogTextBox(string.Format("代号为:的客户端第一次发送的数据量{0}", count));
                if (count == 0)
                {
                    mainForm.SetLogTextBox(string.Format("的客户端初始化数据错误"));
                    return targetInfo;
                }
                clientSock.Send(StringToByteArray("0500"));
                byte[] addrType = new byte[4];
                clientSock.Receive(addrType);
                mainForm.SetLogTextBox(string.Format("代号为:的客户端初始化数据{0} {1}", addrType[0], addrType[1]));
                if (Convert.ToInt32(addrType[1]) != 1)
                {
                    mainForm.SetLogTextBox(string.Format("代号为:的客户端connect错误"));
                    return targetInfo;
                }
                IPAddress ipa = null;
                // 判断socks5的3种类型
                switch (addrType[3])
                {
                    case 1:
                        byte[] addrIP = new byte[4];
                        clientSock.Receive(addrIP);
                        ipa = new IPAddress(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(addrIP, 0)));
                        targetAddr = ipa.ToString();
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
                        targetAddr = ipa.ToString();
                        break;
                }
                byte[] targetPortByte = new byte[2];
                clientSock.Receive(targetPortByte);
                Array.Reverse(targetPortByte);
                targetPort = BitConverter.ToInt16(targetPortByte, 0);
                // 告诉客户端可以开始传输数据
                byte[] sendData = new byte[10];
                SocketListener.StringToByteArray("05000001").CopyTo(sendData, 0);
                IPAddress.Parse("127.0.0.1").GetAddressBytes().CopyTo(sendData, 4);
                short sendPort = 1080;
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(sendPort)).CopyTo(sendData, 8);
                clientSock.Send(sendData);
            } catch (Exception)
            {
                return targetInfo;
            }
            targetInfo["addr"] = targetAddr;
            targetInfo["port"] = targetPort;
            return targetInfo;
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
            Dictionary<string, Object> remoteInfo = GetRemoteInfo(clientSock);
            if ((string)(remoteInfo["addr"]) == String.Empty)
            {
                clientSock.Close();
                recvThreadList[index][0].Abort();
                return;
            }
            // 给目标建立连接

            Socket targetSock = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
            Thread sendTargetThread = null;
            try
            {
                targetSock.Connect((string)remoteInfo["addr"], (short)remoteInfo["port"]);
                sendTargetThread = new Thread(ThreadForTarget);
                sendTargetThread.Start(new ArrayList { clientSock, targetSock });
                LoopReceiveAndSend(targetSock, clientSock);
            } catch (Exception) {
                mainForm.SetLogTextBox(string.Format("客户端发送接受异常"));
            }
            mainForm.SetLogTextBox(string.Format("终止客户端连接"));
            clientSock.Close();
            targetSock.Close();
            recvThreadList[index][0].Abort();
            sendTargetThread.Abort();
            return;
        }
        private void ThreadForTarget(object socketList)
        {
            ArrayList arrayList = socketList as ArrayList;
            Socket clientSock = arrayList[0] as Socket;
            Socket targetSock = arrayList[1] as Socket;
            try
            {
                LoopReceiveAndSend(clientSock, targetSock);
            } catch(Exception)
            {
                mainForm.SetLogTextBox("发送给目标数据异常");
            }
        }
        private void LoopReceiveAndSend(Socket reviceSock, Socket sendSock)
        {
            while(true)
            {
                if (reviceSock.Poll(1, SelectMode.SelectRead) && reviceSock.Available == 0)
                {
                    break;
                }
                byte[] data = new byte[4096 * 10];
                int tlength = reviceSock.Receive(data);
                int sendLength = sendSock.Send(data, 0, tlength, SocketFlags.None);
                // mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(_data));
                if (tlength != sendLength)
                {
                    break;
                }
            }
        }
    }
}
