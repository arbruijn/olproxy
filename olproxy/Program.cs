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
        private IPEndPoint[] BroadcastEndpoints;
        private HashSet<IPAddress> LocalIPSet;
        private IPAddress FirstLocalIP;
        private Dictionary<BroadcastPeerId, int> fakePids;

        #if NETCORE
        [DllImport("libc", SetLastError = true)]
        private unsafe static extern int setsockopt(int socket, int level, int option_name, void* option_value, uint option_len);
        #endif

        void AddMessage(string s)
        {
            Console.WriteLine(s);
            Debug.WriteLine(s);
        }

        void Init()
        {
            LocalIPSet = new HashSet<IPAddress>();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            FirstLocalIP = null;
            fakePids = new Dictionary<BroadcastPeerId, int>();
            foreach (NetworkInterface intf in NetworkInterface.GetAllNetworkInterfaces())
            {
                bool loopback = intf.NetworkInterfaceType == NetworkInterfaceType.Loopback;
                foreach (UnicastIPAddressInformation addr in intf.GetIPProperties().UnicastAddresses)
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
        }


        void MainLoop()
        {
            var bcast = new BroadcastHandler();
            int myPid = System.Diagnostics.Process.GetCurrentProcess().Id;

            int remotePort = 8001;
            UdpClient remoteSocket = new UdpClient();
            remoteSocket.Client.Bind(new IPEndPoint(IPAddress.Any, remotePort));

            UdpClient localSocket = new UdpClient();
            //socket.EnableBroadcast = true;
            //socket.DontFragment = true;
            //socket.ExclusiveAddressUse = false;
            localSocket.MulticastLoopback = false;
            localSocket.EnableBroadcast = true;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
            //socket.Client.ExclusiveAddressUse = false;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 0);
            localSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            #if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                unsafe {
                    // set SO_REUSEADDR (https://github.com/dotnet/corefx/issues/32027)
                    int value = 1;
                    setsockopt(localSocket.Client.Handle.ToInt32(), 1, 2, &value, sizeof(int));
                }
            }
            #endif
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            localSocket.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
            //socket.BeginReceive(new AsyncCallback(OnUdpData), this);
            AddMessage("Ready.");
            var taskLocal = localSocket.ReceiveAsync();
            var taskRemote = remoteSocket.ReceiveAsync();
            var remotePeers = new Dictionary<IPEndPoint, DateTime>();
            while (true)
            {
                var idx = Task.WaitAny(new Task[] { taskLocal, taskRemote }, 250);
                var now = DateTime.UtcNow;
                if (idx == 0) { // local
                    var result = taskLocal.Result;
                    var pktPid = BroadcastHandler.GetPacketPID(result.Buffer);
                    if (LocalIPSet.Contains(result.RemoteEndPoint.Address)) // treat all local ips as same
                        result.RemoteEndPoint.Address = FirstLocalIP;
                    if (!LocalIPSet.Contains(result.RemoteEndPoint.Address) ||
                        !bcast.activeFakePids.Contains(pktPid))
                    {
                        var message = bcast.HandlePacket(result.RemoteEndPoint, result.Buffer);
                        if (message != null) {
                            var rem = bcast.FindMessageEndPoints(message);
                            if (rem == null) { // it's not a client -> server message with destination, try remote peers we've received from
                                var dels = new List<IPEndPoint>();
                                var timeout = now.AddSeconds(-20);
                                int n = 0;
                                foreach (var peerEP in remotePeers.Keys)
                                    if (remotePeers[peerEP].CompareTo(timeout) < 0)
                                        dels.Add(peerEP);
                                    else
                                        n++;
                                foreach (var ep in dels)
                                    remotePeers.Remove(ep);
                                if (n > 0)
                                {
                                    rem = new IPEndPoint[n];
                                    remotePeers.Keys.CopyTo(rem, 0);
                                }
                            }
                            if (rem != null)
                            {
                                if (message != "")
                                    AddMessage("Sending to remote " + String.Join(", ", rem.Select(x => x.ToString())));
                                bcast.SendMulti(message, remoteSocket.Client, rem, pktPid);
                            }
                        }
                    }
                    taskLocal = localSocket.ReceiveAsync();
                } else if (idx == 1) { // remote
                    var result = taskRemote.Result;
                    if (!LocalIPSet.Contains(result.RemoteEndPoint.Address))
                    {
                        remotePeers[result.RemoteEndPoint] = now;
                        var message = bcast.HandlePacket(result.RemoteEndPoint, result.Buffer);
                        if (message != null)
                        {
                            var pktPid = BroadcastHandler.GetPacketPID(result.Buffer);
                            var bid = new BroadcastPeerId() { source = result.RemoteEndPoint, pid = pktPid };
                            var pid = bcast.peers[bid].fakePid;

                            // change advertized ip to ip we've received this from
                            message = new Regex("(\"internalIP\":[{]\"S\":\")([^\"]+)(\"[}])").Replace(message,
                                "${1}" + result.RemoteEndPoint.Address + "$3");
                            //Debug.WriteLine("Sending " + message);
                            if (message != "")
                                AddMessage("Sending to local " + String.Join(", ", BroadcastEndpoints.Select(x => x.ToString())) + " pid " + pid);
                            bcast.SendMulti(message, localSocket.Client, BroadcastEndpoints, pid);
                        }
                    }
                    taskRemote = remoteSocket.ReceiveAsync();
                } else if (idx == -1) {
                    // timeout
                }
            }
        }

        void Run(string[] args)
        {
            Init();
            //BroadcastHandler.test();
            MainLoop();
        }

        static void Main(string[] args)
        {
            new Program().Run(args);
        }
    }
}
