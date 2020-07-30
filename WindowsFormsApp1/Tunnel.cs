using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotAirBalloon
{
    class Tunnel: IDisposable
    {
        TcpClient tcpClient;
        Socket _client;
        Socket _remote;
        RC4 enCodeRc4;
        RC4 deCodeRc4;
        ConfigJson _userConfigJson;
        Dictionary<string, object> _targetInfo;
        Form1 _form;
        //private TcpClient client;
        //private Dictionary<string, object> targetInfo;
        //private object userConfigJson;
        //private Form1 form1;

        public Tunnel (Socket client, Dictionary<string, object> targetInfo, ConfigJson userConfigJson, Form1 form)
        {
            _client = client;
            _userConfigJson = userConfigJson;
            _targetInfo = targetInfo;
            _form = form;
            enCodeRc4 = new RC4(userConfigJson.passport);
            deCodeRc4 = new RC4(userConfigJson.passport);
            _form.SetLogTextBox("Tunnel");
            NetworkStream clientStream = new NetworkStream(_client);
            TunnelStart(clientStream);
            //_remote = new Socket(AddressFamily.InterNetwork,
            //    SocketType.Stream,
            //    ProtocolType.Tcp);
            //Thread sendTargetThread = null;
            //NetworkStream clientStream = new NetworkStream(client);
            //try
            //{
            //    _remote.Connect(_userConfigJson.server, _userConfigJson.port);
            //    NetworkStream remoteStream = new NetworkStream(_remote);
            //    SendTargetInfoToRemote(_targetInfo, remoteStream, enCodeRc4);
            //    _form.SetLogTextBox("send target Info");
            //    sendTargetThread = new Thread(ThreadForTarget);
            //    sendTargetThread.Start(new ArrayList { clientStream, remoteStream, enCodeRc4 });
            //    LoopReceiveAndSend(remoteStream, clientStream, deCodeRc4);
            //}
            //catch (Exception e)
            //{
            //    _form.SetLogTextBox("传输异常结束: " + e.ToString());
            //}
            //Dispose();
        }
        public Tunnel(TcpClient client, NetworkStream clientStream, Dictionary<string, object> targetInfo, ConfigJson userConfigJson, Form1 form1)
        {
            tcpClient = client;
            _userConfigJson = userConfigJson;
            _targetInfo = targetInfo;
            _form = form1;
            enCodeRc4 = new RC4(userConfigJson.passport);
            deCodeRc4 = new RC4(userConfigJson.passport);
            _form.SetLogTextBox("http Tunnel");
            TunnelStart(clientStream);
        }
        public void TunnelStart(NetworkStream clientStream)
        {
            _form.SetLogTextBox("Tunnel");
            _remote = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            Thread sendTargetThread = null;
            try
            {
                _remote.Connect(_userConfigJson.server, _userConfigJson.port);
                NetworkStream remoteStream = new NetworkStream(_remote);
                SendTargetInfoToRemote(_targetInfo, remoteStream, enCodeRc4);
                _form.SetLogTextBox("send target Info");
                sendTargetThread = new Thread(ThreadForTarget);
                sendTargetThread.Start(new ArrayList { clientStream, remoteStream, enCodeRc4 });
                LoopReceiveAndSend(remoteStream, clientStream, deCodeRc4);
            }
            catch (Exception e)
            {
                _form.SetLogTextBox("传输异常结束: " + e.ToString());
            }
            Dispose();
        }
        private void SendTargetInfoToRemote(Dictionary<string, object> targetInfo, NetworkStream remoteStream, RC4 enCodeRc4)
        {
            // lenth addrLength addr port md5
            byte[] sendData = new byte[512];
            string addr = (string)targetInfo["addr"];
            byte[] addrLength = Utils.ShortToHByte((short)addr.Length);
            byte[] port = Utils.ShortToHByte((short)targetInfo["port"]);
            MD5 md5Hash = MD5.Create();
            string md5 = Utils.GetMd5Hash(md5Hash, (string)targetInfo["addr"]);
            byte[] md5b = Utils.GetBytes(md5);
            sbyte length = (sbyte)(2 + 2 + 32 + (sbyte)(addr.Length));
            byte[] bLength = BitConverter.GetBytes((sbyte)length);
            byte[] encode = new byte[4 + 32 + addr.Length];
            // bLength.CopyTo(encode, 0);
            addrLength.CopyTo(encode, 0);
            byte[] tmpAddr = Utils.GetBytes(addr);
            tmpAddr.CopyTo(encode, 2);
            port.CopyTo(encode, 2 + addr.Length);
            md5b.CopyTo(encode, 4 + addr.Length);
            byte[] rcEncode = enCodeRc4.Encrypt(encode);
            rcEncode.CopyTo(sendData, 1);
            sendData[0] = (byte)length;
            remoteStream.Write(sendData, 0, addr.Length + 5 + 32);
        }
        private void ThreadForTarget(object socketList)
        {
            ArrayList arrayList = socketList as ArrayList;
            // Socket clientSock = arrayList[0] as Socket;
            // Socket targetSock = arrayList[1] as Socket;
            NetworkStream clientStream = arrayList[0] as NetworkStream;
            NetworkStream targetStream = arrayList[1] as NetworkStream;
            RC4 enCodeRc4 = arrayList[2] as RC4;
            try
            {
                LoopReceiveAndSend(clientStream, targetStream, enCodeRc4);
            }
            catch (Exception)
            {
               
            }
        }
        private void LoopReceiveAndSend(NetworkStream reviceStream, NetworkStream sendStream, RC4 enCodeRc4)
        {
            while (true)
            {
                if (!reviceStream.CanRead || !sendStream.CanWrite)
                {
                    break;
                }
                byte[] data = new byte[4096 * 10];
                try
                {
                    int tlength = reviceStream.Read(data, 0, data.Length);
                    byte[] sendData = data.Take(tlength).ToArray();
                    sendStream.Write(enCodeRc4.Encrypt(sendData), 0, tlength);
                } catch (Exception)
                {
                    break;
                }
                // mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(_data));
            }
        }
        //private void LoopReceiveAndSend(Socket reviceSock, Socket sendSock, RC4 enCodeRc4)
        //{
        //    while (true)
        //    {
        //        if (reviceSock.Poll(1, SelectMode.SelectRead) && reviceSock.Available == 0)
        //        {
        //            break;
        //        }
        //        byte[] data = new byte[4096 * 10];
        //        int tlength = reviceSock.Receive(data);
        //        byte[] sendData = data.Take(tlength).ToArray();
        //        int sendLength = sendSock.Send(enCodeRc4.Encrypt(sendData), 0, tlength, SocketFlags.None);
        //        // mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(_data));
        //        if (tlength != sendLength)
        //        {
        //            break;
        //        }
        //    }
        //}
        public void Dispose()
        {
            if (_client != null)
            {
                _client.Close();
            }
            _remote.Close();
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
        }
    }
}
