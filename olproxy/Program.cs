using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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
    class ProxyPeerInfo
    {
// not yet used
/*
        public bool IsRemote;
        public bool IsServer;
        public bool IsTeam;
        public MatchInfo MatchInfo; // server only
*/
    }

    class Program
    {
        private const int broadcastPort = 8000;
        private const int remotePort = 8001;
        private IPEndPoint[] BroadcastEndpoints;
        private HashSet<IPAddress> LocalIPSet;
        private IPAddress FirstLocalIP;
        private UdpClient localSocket, remoteSocket;
        private BroadcastHandler<ProxyPeerInfo> bcast;
        private Dictionary<IPEndPoint, DateTime> remotePeers;
        private IPAddress curLocalIP;
        private DateTime curLocalIPLast;
        private bool debug;
        private ConsoleSpinner spinner = new ConsoleSpinner();

#if NETCORE
        [DllImport("libc", SetLastError = true)]
        private unsafe static extern int setsockopt(int socket, int level, int option_name, void* option_value, uint option_len);
#endif

        void AddMessage(string s)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + s);
            Debug.WriteLine(s);
        }

        void AddMessageDebug(string s)
        {
            Debug.WriteLine(s);
        }

        void InitInterfaces()
        {
            LocalIPSet = new HashSet<IPAddress>();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            FirstLocalIP = null;
            foreach (NetworkInterface intf in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipProps = intf.GetIPProperties();
                if (intf.OperationalStatus != OperationalStatus.Up || ipProps == null)
                    continue;
                #if !NETCORE
                if (ipProps.DnsAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Count() > 2)
                {
                    AddMessage("Warning! Interface " + String.Join(", ", ipProps.UnicastAddresses
                        .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(x => x.Address)) + " has more than 2 DNS servers.");
                    AddMessage("This will crash Overload LAN Mode!");
                    AddMessage("Remove one or more DNS servers from your DNS settings.");
                    AddMessage("See for example http://lifewire.com/dns-2626242");
                    AddMessage("");
                }
                #endif
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
                    }
                }
            }
            BroadcastEndpoints = eps.ToArray();
            if (debug)
                AddMessage("Found local broadcast addresses " + String.Join(", ", BroadcastEndpoints.Select(x => x.Address.ToString())));
        }

        void InitSockets()
        {
            try {
                remoteSocket = new UdpClient();
                remoteSocket.Client.Bind(new IPEndPoint(IPAddress.Any, remotePort));
            } catch (SocketException ex) {
                throw new Exception("Cannot open UDP port " + remotePort + ". Is olprxoy already running? (error " + ex.ErrorCode + ")");
            }

            localSocket = CreateUDPBroadcastSocket();
            localSocket.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
        }

        void Init()
        {
            InitInterfaces();
            InitSockets();

            bcast = new BroadcastHandler<ProxyPeerInfo>() { AddMessage = AddMessageDebug};
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
            public bool IsMatch; // server -> client
            public string Password;
            public string ticketType;
            public string ticket;
            public bool HasPrivateMatchData;
        }

        private static readonly Regex msgNameRegex = new Regex("\"name\":\"([^\"]+)\",\"uid\":");
        private static readonly Regex msgTicketTypeRegex = new Regex("[{,]\"mm_ticketType\":[{]\"S\":\"([^\"]+)\"");
        private static readonly Regex msgTicket = new Regex("[{,]\"mm_ticket\":[{]\"S\":\"([^\"]+)\"");
        private static readonly Regex msgClaims = new Regex("[{,]\"mm_claims\":[{]\"S\":\"([^\"]+)\"");
        private static readonly Regex msgPrivateMatchData = new Regex("[{,]\\\\\"private_match_data\\\\\":");
        private static readonly Regex msgPasswordRegex = new Regex(
                    "\\\\\"password\\\\\":\\{\\\\\"attributeType\\\\\":\\\\\"STRING_LIST\\\\\",\\\\\"valueAttribute\\\\\":\\[\\\\\"([^\"]+)\\\\\"\\]}"
                    );
        
        private IPAddress FindPasswordAddress(string password, out string name)
        {
            var i = password.IndexOf('_'); // allow password suffix with '_'
            name = i == -1 ? password : password.Substring(0, i);
            if (!name.Contains('.'))
                return null;
            if (new Regex(@"\d{1,3}([.]\d{1,3}){3}").IsMatch(name) &&
                IPAddress.TryParse(name, out IPAddress adr))
                return adr;
            try {
                var adrs = Dns.GetHostAddresses(name);
                return adrs == null || adrs.Length == 0 ? null : adrs[0];
            } catch (SocketException ex) {
                AddMessage("Cannot find " + name + ": " + ex.Message);
            } catch (Exception) {
            }
            return null;
        }

        private static ParsedMessage ParseMessage(string message)
        {
            string msgName = msgNameRegex.Match(message)?.Groups[1].Value;
            var ret = new ParsedMessage();
            ret.IsRequest = msgName == "MMRequest";
            ret.IsMatch = msgName == "MMMatch";
            ret.ticketType = msgTicketTypeRegex.Match(message)?.Groups[1].Value;
            if (ret.IsRequest)
                ret.Password = msgPasswordRegex.Match(message)?.Groups[1].Value;
            ret.ticket = (ret.IsRequest ? msgTicket : msgClaims).Match(message)?.Groups[1].Value;
            ret.HasPrivateMatchData = msgPrivateMatchData.IsMatch(message);
            return ret;
        }

        void ProcessLocalPacket(IPEndPoint endPoint, byte[] packet)
        {
            if (!LocalIPSet.Contains(endPoint.Address)) // v0.2 policy: only accept local packets, better for multiple olproxies on LAN
                return;

            var pktPid = BroadcastHandler<ProxyPeerInfo>.GetPacketPID(packet);
            if (bcast.activeFakePids.Contains(pktPid)) // ignore packets sent by ourself
                return;

            // v0.2 policy: only accept packets from single local ip, allow ip change if idle for 10 seconds
            var now = DateTime.UtcNow;
            var timeout = now.AddSeconds(-10);
            if (curLocalIPLast < timeout)
            {
                if (debug && !endPoint.Address.Equals(curLocalIP))
                    AddMessage("Using local address " + endPoint.Address);
                curLocalIP = endPoint.Address;
            }
            curLocalIPLast = now;

            if (!endPoint.Address.Equals(curLocalIP))
                return;
    
            //if (LocalIPSet.Contains(endPoint.Address)) // treat all local ips as same
            //    endPoint.Address = FirstLocalIP;

            var msgStr = bcast.HandlePacket(endPoint, packet, out bool isNew, out BroadcastPeer<ProxyPeerInfo> peer);
            if (msgStr == null) // message not yet complete / invalid
                return;

            var msg = ParseMessage(msgStr);
            if (msg.IsRequest) {
                var adr = FindPasswordAddress(msg.Password, out string hostname);
                if (adr == null)
                    return;
                var destEndPoint = new IPEndPoint(adr, remotePort);
                if (isNew)
                    AddMessage(debug ? pktPid + " " + isNew + " Sending match " + msg.ticketType +
                        " " + msg.ticket +
                        " to server " + hostname :
                        "Sending " + (msg.HasPrivateMatchData ? "create" : "join") + " match " + msg.ticketType +
                            (msg.HasPrivateMatchData ? " (" + new MatchInfo(msgStr) + ")" : "") +
                            " to " + hostname);
                else
                    spinner.Spin();
                bcast.Send(msgStr, remoteSocket.Client, destEndPoint, pktPid, isNew);
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

            //if (msgStr == "")
            //    bcast.peers.Remove(new BroadcastPeerId() { source = endPoint, pid = BroadcastHandler.GetPacketPID(packet) });

            if (isNew)
                AddMessage(debug ? pktPid + " Sending match " +
                        (msgStr == "" ? "done" : msg.ticketType + " " + msg.ticket) +
                        " to clients " + String.Join(", ", remotePeers.Keys.Select(x => x.Address.ToString())) :
                    "Sending " + (msgStr != "" ? (msg.HasPrivateMatchData ? "create " : "join ") : "") +
                        "match " + (msgStr == "" ? "done" : msg.ticketType) +
                        (msg.HasPrivateMatchData || msg.ticketType == "match" ? " (" + new MatchInfo(msgStr) + ")": ""));
            else
                spinner.Spin();
            Debug.WriteLine((msgStr == "" ? "done" : msg.ticketType) + " to " + String.Join(", ", remotePeers.Keys.Select(x => x.ToString())));
            bcast.SendMulti(msgStr, remoteSocket.Client, remotePeers.Keys, pktPid, isNew);
        }

        void ProcessRemotePacket(IPEndPoint endPoint, byte[] packet)
        {
            if (LocalIPSet.Contains(endPoint.Address))
                return;

            // store peer last seen
            remotePeers[endPoint] = DateTime.UtcNow;

            var message = bcast.HandlePacket(endPoint, packet, out bool isNew, out BroadcastPeer<ProxyPeerInfo> peer);
            if (message == null) // message not yet complete / invalid
                return;

            var pid = peer.fakePid;

            if (message == "" && peer != null) {
                Debug.WriteLine("Removing peer " + peer.peerId);
                bcast.peers.Remove(peer.peerId);
            }

            // change advertized ip to ip we've received this from
            message = new Regex("(\"internalIP\":[{]\"S\":\")([^\"]+)(\"[}])").Replace(message,
                "${1}" + endPoint.Address + "$3");

            if (isNew)
            {
                MatchInfo matchInfo = null;

                var msg = ParseMessage(message);
                if (msg.HasPrivateMatchData || msg.ticketType == "match")
                {
                    matchInfo = new MatchInfo(message);
                }

                string ticketType = message == "" ? "done" : msg.ticketType;
                AddMessage(debug ? peer.lastNewSeq + " Received match " + ticketType + " " + msg.ticket +
                    ", forward to " + String.Join(", ", BroadcastEndpoints.Select(x => x.ToString())) + " pid " + pid :
                    "Received " + (msg.IsRequest ? msg.HasPrivateMatchData ? "create " : "join " : "") + "match " + ticketType +
                    (matchInfo != null ? " (" + matchInfo + ")" : ""));
            }
            else
                spinner.Spin();
            Debug.WriteLine("Received " + ParseMessage(message).ticketType + " forward to " + String.Join(", ", BroadcastEndpoints.Select(x => x.ToString())) + " pid " + pid);
            bcast.SendMulti(message, localSocket.Client, BroadcastEndpoints, pid, isNew);
        }

        void MainLoop()
        {
            AddMessage("olproxy " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3) + " Ready.");
            AddMessage("Create/Join LAN Match in Overload and use server IP address as password");
            AddMessage("(or start Overload server)");

            var taskLocal = localSocket.ReceiveAsync();
            var taskRemote = remoteSocket.ReceiveAsync();
            var tasks = new Task[2];
            for (;;)
            {
                tasks[0] = taskLocal;
                tasks[1] = taskRemote;
                var idx = Task.WaitAny(tasks, spinner.Active ? 1000 : Timeout.Infinite);
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
                if (spinner.Active && spinner.LastUpdateTime < DateTime.UtcNow.AddSeconds(-2))
                    spinner.Clear();
            }
        }

        void Run(string[] args)
        {
            foreach (var arg in args)
                if (arg == "-v")
                    debug = true;
            Init();
            MainLoop();
        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
