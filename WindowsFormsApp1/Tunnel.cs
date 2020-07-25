using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class Tunnel: IDisposable
    {
        Socket _client;
        Socket _remote;
        RC4 enCodeRc4;
        RC4 deCodeRc4;
        ConfigJson _userConfigJson;
        Dictionary<string, object> _targetInfo;
        Form1 _form;
        public Tunnel (Socket client, Dictionary<string, object> targetInfo, ConfigJson userConfigJson, Form1 form)
        {
            _client = client;
            _userConfigJson = userConfigJson;
            _targetInfo = targetInfo;
            _form = form;
            enCodeRc4 = new RC4(userConfigJson.passport);
            deCodeRc4 = new RC4(userConfigJson.passport);
            _form.SetLogTextBox("Tunnel");
            _remote = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            Thread sendTargetThread = null;
            try
            {
                _remote.Connect(_userConfigJson.server, _userConfigJson.port);
                SendTargetInfoToRemote(_targetInfo, _remote, enCodeRc4);
                _form.SetLogTextBox("send target Info");
                sendTargetThread = new Thread(ThreadForTarget);
                sendTargetThread.Start(new ArrayList { _client, _remote, enCodeRc4 });
                LoopReceiveAndSend(_remote, _client, deCodeRc4);
            }
            catch (Exception)
            {
                Dispose();
            }
        }
        private void SendTargetInfoToRemote(Dictionary<string, object> targetInfo, Socket remoteSock, RC4 enCodeRc4)
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
            remoteSock.Send(sendData, 0, addr.Length + 5 + 32, SocketFlags.None);
        }
        private void ThreadForTarget(object socketList)
        {
            ArrayList arrayList = socketList as ArrayList;
            Socket clientSock = arrayList[0] as Socket;
            Socket targetSock = arrayList[1] as Socket;
            RC4 enCodeRc4 = arrayList[2] as RC4;
            try
            {
                LoopReceiveAndSend(clientSock, targetSock, enCodeRc4);
            }
            catch (Exception)
            {
               
            }
        }
        private void LoopReceiveAndSend(Socket reviceSock, Socket sendSock, RC4 enCodeRc4)
        {
            while (true)
            {
                if (reviceSock.Poll(1, SelectMode.SelectRead) && reviceSock.Available == 0)
                {
                    break;
                }
                byte[] data = new byte[4096 * 10];
                int tlength = reviceSock.Receive(data);
                byte[] sendData = data.Take(tlength).ToArray();
                int sendLength = sendSock.Send(enCodeRc4.Encrypt(sendData), 0, tlength, SocketFlags.None);
                // mainForm.SetLogTextBox(System.Text.Encoding.ASCII.GetString(_data));
                if (tlength != sendLength)
                {
                    break;
                }
            }
        }
        public void Dispose()
        {
            _client.Close();
            _remote.Close();
        }
    }
}
