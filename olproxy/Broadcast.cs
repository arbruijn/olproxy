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
    class BroadcastPeer<PeerInfoType>
    {
        public BroadcastPeerId peerId;
        public int lastSeq;
        public byte[][] curParts;
        public DateTime lastTime;
        public int fakePid;
        public int lastNewSeq;
        public uint lastHash;
        public PeerInfoType peerInfo;
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


    class BroadcastHandler<PeerInfoType>
    {
        public Dictionary<BroadcastPeerId, BroadcastPeer<PeerInfoType>> peers =
            new Dictionary<BroadcastPeerId, BroadcastPeer<PeerInfoType>>();
        public HashSet<int> activeFakePids = new HashSet<int>();

        public Action<string> AddMessage;

        public static int GetPacketPID(byte[] packet)
        {
            return BitConverter.ToInt32(packet, 4);
        }

        public string HandlePacket(IPEndPoint source, byte[] packet, out bool isNew, out BroadcastPeer<PeerInfoType> peer)
        {
            isNew = false;
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
                isNew = false;
                peer = null;
                return null;
            }
            BroadcastPeerId peerId = new BroadcastPeerId() { source = source, pid = ppid };

            var now = DateTime.UtcNow;
            var timeout = now.AddSeconds(-5);

            if (totalSize == 0 && partCount == 0 && partIdx == 0) // special disconnect packet
            {
                if (peers.TryGetValue(peerId, out peer))
                    if (peer.lastTime < timeout)
                    {
                        peers.Remove(peerId);
                    }
                    else
                    {
                        if (peers[peerId].lastSeq != -1)
                            AddMessage("Disconnect from " + source + ":" + ppid);
                        isNew = peers[peerId].lastSeq != -1;
                        peers[peerId].lastSeq = -1;
                        // do not remove to keep fakePid around...
                        return "";
                    }
                isNew = false;
                peer = null;
                return null;
            }

            if (!peers.TryGetValue(peerId, out peer) || peer.lastTime < timeout)
            {
                // remove timed out peers
                foreach (var x in peers.Keys.Where(x => peers[x].lastTime < timeout).ToList())
                {
                    activeFakePids.Remove(peers[x].fakePid);
                    peers.Remove(x);
                }
                var pid = -2000000000;
                while (activeFakePids.Contains(pid))
                    pid++;
                activeFakePids.Add(pid);
                AddMessage("New client " + source + ":" + ppid + " fakepid " + pid);
                peers.Add(peerId, peer = new BroadcastPeer<PeerInfoType>() {
                    lastSeq = -1, lastNewSeq = -1, fakePid = pid, peerId = peerId });
            }
            //AddMessage("Got " + message.Length + " bytes from " + source + ":" + pid + " " + seq + " (" + c.lastSeq + ") tot " + totalSize + " " + partIdx + "/" + partCount);
            if (seq >= peer.lastSeq)
                peer.lastTime = now;
            if (seq > peer.lastSeq)
            {
                peer.curParts = new byte[partCount][];
                peer.lastSeq = seq;
                AddMessage("Init new message from " + source + ":" + ppid + " #" + seq);
            }
            if (peer.curParts != null)
            {
                peer.curParts[partIdx] = packet;
                if (peer.curParts.All(part => part != null))
                {
                    var buf = new byte[totalSize];
                    int ofs = 0;
                    foreach (var part in peer.curParts)
                        for (int i = 19; i < part.Length; i++)
                            buf[ofs++] = (byte)(part[i] ^ 204);
                    peer.curParts = null;

                    uint hash = BitConverter.ToUInt32(packet, 8);
                    if (hash != xxHash.CalculateHash(buf))
                        return null;

                    isNew = seq != peer.lastNewSeq || hash != peer.lastHash;
                    peer.lastHash = hash;
                    peer.lastNewSeq = seq;
                    peer.lastSeq--; // allow receiving this seq again
                    string text = Encoding.UTF8.GetString(buf);
                    //AddMessage("Got " + text + " chk=" + ok + " ppid=" + ppid + " mypid=" + myPid);
                    AddMessage(text);
                    return text;
                }
            }
            return null;
        }

        Dictionary<int, int> pidSeq = new Dictionary<int, int>();
        List<byte[]> MessageToPackets(string msg, int pid, bool isNew)
        {
            bool closePacket = msg.Length == 0;
            byte[] buf = Encoding.UTF8.GetBytes(msg);
            int maxPacketSize = 384;
            int packetCount = closePacket ? 1 : (buf.Length + maxPacketSize - 1) / maxPacketSize;
            if (!pidSeq.TryGetValue(pid, out int seq))
                seq = 0;
            if (isNew)
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
            }
            return packets;
        }

        public void SendMulti(string msg, Socket socket, IEnumerable<IPEndPoint> endPoints, int pid, bool isNew)
        {
            var packets = MessageToPackets(msg, pid, isNew);
            foreach (var endPoint in endPoints)
                foreach (var packet in packets)
                    socket.SendTo(packet, endPoint);
        }

        public void Send(string msg, Socket socket, IPEndPoint endPoint, int pid, bool isNew)
        {
            SendMulti(msg, socket, new [] { endPoint }, pid, isNew);
        }
    }
}
