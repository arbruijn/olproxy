using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*
example message exachange:
client -> server {"max":2,"name":"MMRequest","uid":"892d5795-31cd-4bf4-96e3-a3bc407eef91","attr":{"mm_ticketType":{"S":"request"},"mm_name":{"S":"PRIVATE-Overload-PROD"},"mm_ticket":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72"},"mm_version":{"I":11},"mm_players":{"S":"[{\"PlayerId\":\"27a3b506-b368-11e8-a0d7-0d8a3d4259a5\",\"Team\":\"players\",\"PlayerAttributes\":{\"private_match_data\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"BGFybmWQGBAFAADAwAAoAmoAAAAAEJCAAA==\"},\"password\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"loc\"]},\"private_initiator\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"max_num_players\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"devId\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"PROD\"},\"version\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":11},\"playlists\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"private\"]},\"skill\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":500},\"platform_self\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\"]},\"platform_other\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\",\"ps4\",\"xbox\"]},\"uid\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"99ea9514-186e-423f-875b-78a1452b59e6\"}}}]"},"mm_createTime":{"S":"48D630E5F3E43ACA"}}}
server -> client {"max":8,"name":"MMMatch","uid":"91a3526c-ea24-4b02-8e43-448862b5fea1","attr":{"mm_ticketType":{"S":"pending"},"mm_claims":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72"},"mm_createTime":{"S":"48D630E6080C2FF2"}}}
client -> server {"max":2,"name":"MMRequest","uid":"892d5795-31cd-4bf4-96e3-a3bc407eef91","attr":{"mm_ticketType":{"S":"claimed"},"mm_name":{"S":"PRIVATE-Overload-PROD"},"mm_ticket":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72"},"mm_version":{"I":11},"mm_players":{"S":"[{\"PlayerId\":\"27a3b506-b368-11e8-a0d7-0d8a3d4259a5\",\"Team\":\"players\",\"PlayerAttributes\":{\"private_match_data\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"BGFybmWQGBAFAADAwAAoAmoAAAAAEJCAAA==\"},\"password\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"loc\"]},\"private_initiator\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"max_num_players\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"devId\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"PROD\"},\"version\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":11},\"playlists\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"private\"]},\"skill\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":500},\"platform_self\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\"]},\"platform_other\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\",\"ps4\",\"xbox\"]},\"uid\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"99ea9514-186e-423f-875b-78a1452b59e6\"}}}]"},"mm_createTime":{"S":"48D630E5F3E43ACA"},"mm_claimed":{"S":"91a3526c-ea24-4b02-8e43-448862b5fea1"}}}
server -> client {"max":8,"name":"MMMatch","uid":"91a3526c-ea24-4b02-8e43-448862b5fea1","attr":{"mm_ticketType":{"S":"match"},"mm_claims":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72"},"mm_createTime":{"S":"48D630E608AC3A8C"},"internalIP":{"S":"192.168.1.123"},"port":{"I":7621},"mm_mmGSArn":{"S":"arn:aws:local:local-lan::gamesession/fleet-00000000-0000-0000-0000-000000000000/5112e4e8-140c-4612-99ae-a5591370f742"},"mm_mmTickets":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72"},"mm_mmPlayerIds":{"S":"5b83d89d-eab3-4744-9acd-132c71719e72:27a3b506-b368-11e8-a0d7-0d8a3d4259a5=524f19e3-f95d-4f1f-93cf-e4dc54eb9499"},"mm_mmPlayers":{"S":"[{\"PlayerId\":\"27a3b506-b368-11e8-a0d7-0d8a3d4259a5\",\"Team\":\"players\",\"PlayerAttributes\":{\"private_match_data\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"BGFybmWQGBAFAADAwAAoAmoAAAAAEJCAAA==\"},\"password\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"loc\"]},\"private_initiator\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"max_num_players\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":1},\"devId\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"PROD\"},\"version\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":11},\"playlists\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"private\"]},\"skill\":{\"attributeType\":\"DOUBLE\",\"valueAttribute\":500},\"platform_self\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\"]},\"platform_other\":{\"attributeType\":\"STRING_LIST\",\"valueAttribute\":[\"pc\",\"ps4\",\"xbox\"]},\"uid\":{\"attributeType\":\"STRING\",\"valueAttribute\":\"99ea9514-186e-423f-875b-78a1452b59e6\"}}}]"}}}
*/

namespace olproxy
{
    class Program
    {
        private const int broadcastPort = 8000;
        private const int remotePort = 8001;
        private IPEndPoint[] BroadcastEndpoints;
        private HashSet<IPAddress> LocalIPSet;
        private IPAddress FirstLocalIP;
        private Dictionary<BroadcastPeerId, int> fakePids;
        private Dictionary<IPAddress, IPEndPoint> localIPToBroadcastEndPoint;
        private UdpClient localSocket, remoteSocket;
        private BroadcastHandler bcast;
        private int myPid;
        private Dictionary<IPEndPoint, DateTime> remotePeers;
        private IPAddress curLocalIP;
        private DateTime curLocalIPLast;

#if NETCORE
        [DllImport("libc", SetLastError = true)]
        private unsafe static extern int setsockopt(int socket, int level, int option_name, void* option_value, uint option_len);
#endif

        void AddMessage(string s)
        {
            Console.WriteLine(s);
            Debug.WriteLine(s);
        }

        void InitInterfaces()
        {
            LocalIPSet = new HashSet<IPAddress>();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            FirstLocalIP = null;
            fakePids = new Dictionary<BroadcastPeerId, int>();
            localIPToBroadcastEndPoint = new Dictionary<IPAddress, IPEndPoint>();
            foreach (NetworkInterface intf in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipProps = intf.GetIPProperties();
                if (intf.OperationalStatus != OperationalStatus.Up || ipProps == null)
                    continue;
                bool loopback = intf.NetworkInterfaceType == NetworkInterfaceType.Loopback;
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    LocalIPSet.Add(addr.Address);
                    if (FirstLocalIP == null)
                        FirstLocalIP = addr.Address;
                    if (!loopback)
                    {
                        uint ipAddress = BitConverter.ToUInt32(addr.Address.GetAddressBytes(), 0);
                        uint ipMask = BitConverter.ToUInt32(addr.IPv4Mask.GetAddressBytes(), 0);
                        var broadcast = new IPAddress(BitConverter.GetBytes(ipAddress | ~ipMask));
                        eps.Add(new IPEndPoint(broadcast, broadcastPort));
                        localIPToBroadcastEndPoint[addr.Address] = new IPEndPoint(broadcast, broadcastPort);
                    }
                }
            }
            BroadcastEndpoints = eps.ToArray();
            AddMessage("Found local broadcast addresses " + String.Join(", ", BroadcastEndpoints.Select(x => x.ToString())));
        }

        void InitSockets()
        {
            remoteSocket = new UdpClient();
            remoteSocket.Client.Bind(new IPEndPoint(IPAddress.Any, remotePort));

            localSocket = CreateUDPBroadcastSocket();
            localSocket.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
        }

        void Init()
        {
            InitInterfaces();
            InitSockets();

            bcast = new BroadcastHandler() { AddMessage = AddMessage };
            myPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            remotePeers = new Dictionary<IPEndPoint, DateTime>();
            curLocalIP = null;
            curLocalIPLast = DateTime.MinValue;
        }

        UdpClient CreateUDPBroadcastSocket()
        {
            UdpClient socket = new UdpClient();
            //socket.EnableBroadcast = true;
            //socket.DontFragment = true;
            //socket.ExclusiveAddressUse = false;
            socket.MulticastLoopback = false;
            socket.EnableBroadcast = true;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
            //socket.Client.ExclusiveAddressUse = false;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 0);
            socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                unsafe {
                    // set SO_REUSEADDR (https://github.com/dotnet/corefx/issues/32027)
                    int value = 1;
                    setsockopt(socket.Client.Handle.ToInt32(), 1, 2, &value, sizeof(int));
                }
            }
#endif
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            return socket;
        }

        class ParsedMessage
        {
            public bool IsRequest; // client -> server
            public string Password;
        }

        private static readonly Regex msgNameRegex = new Regex("\"name\":\"([^\"]+)\",\"uid\":");
        private static readonly Regex msgPasswordRegex = new Regex(
                    "\\\\\"password\\\\\":\\{\\\\\"attributeType\\\\\":\\\\\"STRING_LIST\\\\\",\\\\\"valueAttribute\\\\\":\\[\\\\\"([^\"]+)\\\\\"\\]}"
                    );
        
        private static IPAddress FindPasswordAddress(string password)
        {
            var i = password.IndexOf('_'); // allow password suffix with '_'
            var name = i == -1 ? password : password.Substring(0, i);
            if (!name.Contains('.'))
                return null;
            if (new Regex(@"\d{1,3}([.]\d{1,3}){3}").IsMatch(name) &&
                IPAddress.TryParse(name, out IPAddress adr))
                return adr;
            var adrs = Dns.GetHostAddresses(name);
            return adrs == null || adrs.Length == 0 ? null : adrs[0];
        }

        private static ParsedMessage ParseMessage(string message)
        {
            string msgName = msgNameRegex.Match(message)?.Groups[1].Value;
            var ret = new ParsedMessage();
            ret.IsRequest = msgName == "MMRequest";
            if (ret.IsRequest)
                ret.Password = msgPasswordRegex.Match(message)?.Groups[1].Value;
            return ret;
        }

        void ProcessLocalPacket(IPEndPoint endPoint, byte[] packet)
        {
            if (!LocalIPSet.Contains(endPoint.Address)) // v0.2 policy: only accept local packets, better for multiple olproxies on LAN
                return;

            var pktPid = BroadcastHandler.GetPacketPID(packet);
            if (bcast.activeFakePids.Contains(pktPid)) // ignore packets sent by ourself
                return;

            // v0.2 policy: only accept packets from single local ip, allow ip change if idle for 20 seconds
            var now = DateTime.UtcNow;
            var timeout = now.AddSeconds(-20);
            if (curLocalIPLast < timeout)
            {
                curLocalIP = endPoint.Address;
                AddMessage("Using local address " + curLocalIP);
            }
            curLocalIPLast = now;

            if (!endPoint.Address.Equals(curLocalIP))
                return;
    
            //if (LocalIPSet.Contains(endPoint.Address)) // treat all local ips as same
            //    endPoint.Address = FirstLocalIP;

            var msgStr = bcast.HandlePacket(endPoint, packet);
            if (msgStr == null) // message not yet complete / invalid
                return;

            var msg = ParseMessage(msgStr);
            if (msg.IsRequest)
            {
                var adr = FindPasswordAddress(msg.Password);
                if (adr == null)
                    return;
                bcast.Send(msgStr, remoteSocket.Client, new IPEndPoint(adr, remotePort), pktPid);
                return;
            }

            if (!remotePeers.Any())
                return;

            var dels = new List<IPEndPoint>();
            foreach (var peerEP in remotePeers.Keys)
                if (remotePeers[peerEP] < timeout)
                    dels.Add(peerEP);
            foreach (var ep in dels)
                remotePeers.Remove(ep);

            if (!remotePeers.Any())
                return;

            if (msgStr != "")
                AddMessage("Sending to remote " + String.Join(", ", remotePeers.Keys.Select(x => x.ToString())));
            bcast.SendMulti(msgStr, remoteSocket.Client, remotePeers.Keys, pktPid);
        }

        void ProcessRemotePacket(IPEndPoint endPoint, byte[] packet)
        {
            if (LocalIPSet.Contains(endPoint.Address))
                return;

            // store peer last seen
            remotePeers[endPoint] = DateTime.UtcNow;

            var message = bcast.HandlePacket(endPoint, packet);
            if (message == null) // message not yet complete / invalid
                return;

            var pktPid = BroadcastHandler.GetPacketPID(packet);
            var bid = new BroadcastPeerId() { source = endPoint, pid = pktPid };
            var pid = bcast.peers[bid].fakePid;

            // change advertized ip to ip we've received this from
            message = new Regex("(\"internalIP\":[{]\"S\":\")([^\"]+)(\"[}])").Replace(message,
                "${1}" + endPoint.Address + "$3");
            //Debug.WriteLine("Sending " + message);
            var broadcastEP = localIPToBroadcastEndPoint[curLocalIP];
            if (message != "")
                AddMessage("Sending to local " + broadcastEP + " pid " + pid);
            bcast.Send(message, localSocket.Client, broadcastEP, pid);
        }

        void MainLoop()
        {
            AddMessage("Ready.");
            var taskLocal = localSocket.ReceiveAsync();
            var taskRemote = remoteSocket.ReceiveAsync();
            for (;;)
            {
                var idx = Task.WaitAny(new Task[] { taskLocal, taskRemote }, 250);
                if (idx == 0) // local
                {
                    var result = taskLocal.Result;
                    ProcessLocalPacket(result.RemoteEndPoint, result.Buffer);
                    taskLocal = localSocket.ReceiveAsync();
                }
                else if (idx == 1) // remote
                {
                    var result = taskRemote.Result;
                    ProcessRemotePacket(result.RemoteEndPoint, result.Buffer);
                    taskRemote = remoteSocket.ReceiveAsync();
                }
                else if (idx == -1) // timeout
                {
                    
                }
            }
        }

        void Run(string[] args)
        {
            Init();
            MainLoop();
        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
