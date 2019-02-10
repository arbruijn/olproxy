using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace olproxy
{
    class BroadcastPeer
    {
        public int lastSeq;
        public byte[][] curParts;
        public DateTime lastTime;
        public int fakePid;
    }

    class BroadcastPeerId
    {
        public IPEndPoint source;
        public int pid;
        public override bool Equals(object o)
        {
            var c = o as BroadcastPeerId;
            return source.Equals(c.source) && pid == c.pid;
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + source.GetHashCode();
                hash = hash * 23 + pid.GetHashCode();
                return hash;
            }
        }
    }


    class BroadcastHandler
    {
        public const int defaultRemotePort = 8001;

        UdpClient socket;
        public Dictionary<BroadcastPeerId, BroadcastPeer> peers = new Dictionary<BroadcastPeerId, BroadcastPeer>();
        public HashSet<int> activeFakePids = new HashSet<int>();
        Guid uid = Guid.NewGuid();
        int myPid = System.Diagnostics.Process.GetCurrentProcess().Id;

        public static int GetPacketPID(byte[] packet)
        {
            return BitConverter.ToInt32(packet, 4);
        }

        public string HandlePacket(IPEndPoint source, byte[] packet)
        {
            int seq = BitConverter.ToInt32(packet, 0);
            int ppid = BitConverter.ToInt32(packet, 4);
            uint totalSize = BitConverter.ToUInt16(packet, 12);
            int partCount = BitConverter.ToInt16(packet, 14);
            int partIdx = BitConverter.ToInt16(packet, 16);
            byte chksum = 0;
            for (int i = 0; i < 18; i++)
                chksum += packet[i];
            if (chksum != packet[18])
            {
                AddMessage("Checksum fail from " + source + ":" + ppid);
                return null;
            }
            BroadcastPeerId peerId = new BroadcastPeerId() { source = source, pid = ppid };
            if (totalSize == 0 && partCount == 0 && partIdx == 0)
            {
                if (peers.ContainsKey(peerId))
                {
                    if (peers[peerId].lastSeq != -1)
                        AddMessage("Disconnect from " + source + ":" + ppid);
                    peers[peerId].lastSeq = -1;
                    // do not remove to keep fakePid around...
                    return "";
                }
                return null;
            }

            var now = DateTime.UtcNow;
            var timeout = now.AddSeconds(-20);
            if (!peers.TryGetValue(peerId, out BroadcastPeer peer) || peer.lastTime.CompareTo(timeout) < 0)
            {
                // remove timed out peers
                foreach (var x in peers.Keys.Where(x => peers[x].lastTime.CompareTo(timeout) < 0).ToList())
                {
                    activeFakePids.Remove(peers[x].fakePid);
                    peers.Remove(x);
                }
                var pid = -2000000000;
                while (activeFakePids.Contains(pid))
                    pid++;
                activeFakePids.Add(pid);
                AddMessage("New client " + source + ":" + ppid + " fakepid " + pid);
                peers.Add(peerId, peer = new BroadcastPeer() { lastSeq = -1, fakePid = pid });
            }
            //AddMessage("Got " + message.Length + " bytes from " + source + ":" + pid + " " + seq + " (" + c.lastSeq + ") tot " + totalSize + " " + partIdx + "/" + partCount);
            peer.lastTime = now;
            if (seq > peer.lastSeq)
            {
                peer.curParts = new byte[partCount][];
                peer.lastSeq = seq;
                AddMessage("Init new message from " + source + ":" + ppid);
            }
            if (peer.curParts != null)
            {
                peer.curParts[partIdx] = packet;
                if (peer.curParts.All(part => part != null))
                {
                    var buf = new byte[totalSize];
                    int ofs = 0;
                    foreach (var part in peer.curParts)
                    {
                        byte[] msg = part as byte[];
                        for (int i = 19; i < msg.Length; i++)
                            buf[ofs++] = (byte)(msg[i] ^ 204);
                    }
                    peer.curParts = null;
                    peer.lastSeq--; // allow receiving this seq again
                    uint hash = BitConverter.ToUInt32(packet, 8);
                    bool ok = xxHash.CalculateHash(buf) == hash;
                    string text = Encoding.UTF8.GetString(buf);
                    AddMessage("Got " + text + " chk=" + ok + " ppid=" + ppid + " mypid=" + myPid);
                    return ok ? text : null;
                }
            }
            return null;
        }

        Dictionary<int, int> pidSeq = new Dictionary<int, int>();
        List<byte[]> MessageToPackets(string msg, int pid = -1)
        {
            bool closePacket = msg.Length == 0;
            byte[] buf = Encoding.UTF8.GetBytes(msg);
            int maxPacketSize = 384;
            int packetCount = closePacket ? 1 : (buf.Length + maxPacketSize - 1) / maxPacketSize;
            if (pid == -1)
                pid = myPid;
            if (!pidSeq.TryGetValue(pid, out int seq))
                seq = -1;
            pidSeq[pid] = ++seq;
            uint hash = closePacket ? 0 : xxHash.CalculateHash(buf);
            var packets = new List<byte[]>();
            for (int packetIdx = 0; packetIdx < packetCount; packetIdx++)
            {
                int ofs = packetIdx * maxPacketSize;
                int len = buf.Length - ofs;
                if (len > maxPacketSize)
                {
                    len = maxPacketSize;
                }
                var packet = new byte[len + 19];
                for (int i = 0; i < len; i++)
                    packet[i + 19] = (byte)(buf[ofs + i] ^ 204);
                BitConverter.GetBytes(seq).CopyTo(packet, 0);
                BitConverter.GetBytes(pid).CopyTo(packet, 4);
                BitConverter.GetBytes(hash).CopyTo(packet, 8);
                BitConverter.GetBytes((short)buf.Length).CopyTo(packet, 12);
                BitConverter.GetBytes(closePacket ? 0 : (short)packetCount).CopyTo(packet, 14);
                BitConverter.GetBytes((short)packetIdx).CopyTo(packet, 16);

                byte chksum = 0;
                for (int i = 0; i < 18; i++)
                    chksum += packet[i];
                packet[18] = chksum;
                packets.Add(packet);
                //var sep = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 11, 255 }), 8000);
                //socket.Send(pbuf, pbuf.Length, ep);
                //HandlePacket(new IPEndPoint(IPAddress.Any, 12345), pbuf);
            }
            return packets;
        }

        public void SendMulti(string msg, Socket socket, IPEndPoint[] endPoints, int pid = -1)
        {
            var packets = MessageToPackets(msg, pid);
            foreach (var endPoint in endPoints)
                foreach (var packet in packets)
                    socket.SendTo(packet, endPoint);
        }

        public IPEndPoint[] FindMessageEndPoints(string msg)
        {
            string msgName = new Regex("\"name\":\"([^\"]+)\",\"uid\":").Match(msg).Groups[1].Value;
            if (msgName == "MMRequest") // client -> server
            {
                string password = new Regex(
                    "\\\\\"password\\\\\":\\{\\\\\"attributeType\\\\\":\\\\\"STRING_LIST\\\\\",\\\\\"valueAttribute\\\\\":\\[\\\\\"([^\"]+)\\\\\"\\]}"
                    ).Match(msg).Groups[1].Value;
                var i = password.IndexOf('_');
                var name = i == -1 ? password : password.Substring(0, i);
                if (!new Regex(@"\d{1,3}([.]\d{1,3}){3}").IsMatch(name) || !IPAddress.TryParse(name, out IPAddress adr)) {
                    var adrs = name.Contains('.') ? Dns.GetHostAddresses(name) : null;
                    if (adrs == null || adrs.Length == 0)
                        return null;
                    adr = adrs[0];
                }
                return new IPEndPoint[]{new IPEndPoint(adr, defaultRemotePort)};
            }
            if (msgName == "MMMatch") // server -> client
            {
                
            }
            return null;
        }

        string MkAnswer(string msg)
        {
            string ticketType = new Regex("\"mm_ticketType\":[{]\"S\":\"([^\"]+)\"[}]").Match(msg).Groups[1].Value;
            string ticket = new Regex("\"mm_ticket\":[{]\"S\":\"([^\"]+)\"[}]").Match(msg).Groups[1].Value;
            string time = new Regex("\"mm_createTime\":[{]\"S\":\"([^\"]+)\"[}]").Match(msg).Groups[1].Value;
            string players = new Regex("\"mm_players\":[{]\"S\":\"[[](.*)[]]\"[}]").Match(msg).Groups[1].Value;
            string player = new Regex("\\\\\"PlayerId\\\\\":\\\\\"([^\\\\]+)\\\\\"").Match(msg).Groups[1].Value;
            string tplayer = Guid.NewGuid().ToString();

            //string ip = "192.168.11.235";
            //int port = 7621;
            string ip = "159.69.83.128";
            int port = 7622;
            AddMessage($"ticket: {ticket} time: {time} players: {players} player: {player}");
            string res;
            if (ticketType == "request")
                res = $@"{{""max"":8,""name"":""MMMatch"",""uid"":""{uid}"",""attr"":{{""mm_ticketType"":{{""S"":""pending""}},""mm_claims"":{{""S"":""{ticket}""}},""mm_createTime"":{{""S"":""{time}""}}}}}}";
            else
                res = $"{{\"max\":8,\"name\":\"MMMatch\",\"uid\":\"{uid}\"," +
                    $"\"attr\":{{\"mm_ticketType\":{{\"S\":\"match\"}},\"mm_claims\":{{\"S\":\"{ticket}\"}}," +
                    $"\"mm_createTime\":{{\"S\":\"{time}\"}},\"internalIP\":{{\"S\":\"{ip}\"}},\"port\":{{\"I\":{port}}}," +
                    $"\"mm_mmGSArn\":{{\"S\":\"arn:aws:local:local-lan::gamesession/fleet-00000000-0000-0000-0000-000000000000/5112e4e8-140c-4612-99ae-a5591370f742\"}}," +
                    $"\"mm_mmTickets\":{{\"S\":\"{ticket}\"}},\"mm_mmPlayerIds\":{{\"S\":\"{ticket}:{player}={tplayer}\"}},\"mm_mmPlayers\":{{\"S\":\"[{players}]\"}}}}}}";
            AddMessage($"res: {res}");
            return res;
        }

        public void Run()
        {
            int listenPort = 8000;
            socket = new UdpClient();
            //socket.EnableBroadcast = true;
            //socket.DontFragment = true;
            //socket.ExclusiveAddressUse = false;
            socket.MulticastLoopback = false;
            //socket.EnableBroadcast = true;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
            //socket.Client.ExclusiveAddressUse = false;
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 0);
            socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            //socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));
            //socket.BeginReceive(new AsyncCallback(OnUdpData), this);
            AddMessage("Ready.");
            while (true)
            {
                var source = new IPEndPoint(0, 0);
                var message = socket.Receive(ref source);
                HandlePacket(source, message);
            }
        }

        private void AddMessage(string msg)
        {
            Console.WriteLine(msg);
        }

        public static void test() {
            var msg = @"{""max"":2,""name"":""MMRequest"",""uid"":""60d07295-31cd-4bf4-96e3-e5ad3c7fef90"",""attr"":{""mm_ticketType"":{""S"":""request""},""mm_name"":{""S"":""PRIVATE-Overload-PROD""},""mm_ticket"":{""S"":""5b83d89d-eab3-4744-9acd-132c71719e72""},""mm_version"":{""I"":11},""mm_players"":{""S"":""[{\""PlayerId\"":\""67e3e780-b368-11e8-a0d7-0d8a3d4259a5\"",\""Team\"":\""players\"",\""PlayerAttributes\"":{\""private_match_data\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""BGFybmWQGBAFAADAwAAoAmoAAAAAEJCAAA==\""},\""password\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""vps1.2ar.nl\""]},\""private_initiator\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""max_num_players\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""devId\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""PROD\""},\""version\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":11},\""playlists\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""private\""]},\""skill\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":500},\""platform_self\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\""]},\""platform_other\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\"",\""ps4\"",\""xbox\""]},\""uid\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""99ea9514-186e-423f-875b-78a1452b59e6\""}}}]""},""mm_createTime"":{""S"":""48D630E5F3E43ACA""}}}";
            var ep = new BroadcastHandler().FindMessageEndPoints(msg);
            Debug.WriteLine(ep == null ? "null" : ep[0].ToString());
            //DateTime.UtcNow.CompareTo(
        }
    }
}
