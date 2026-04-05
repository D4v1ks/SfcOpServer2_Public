#pragma warning disable IDE0130

using shrNet;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public sealed class Client27000 : DuplexClientTransport
    {
        public const int MinimumBufferSize = 4;
        public const int MaximumBufferSize = 262144;

        [Flags]
        public enum States
        {
            Accepted = 1,
            Registered = 2,
            Online = 4,

            IsOffline = 0,
            IsAccepted = Accepted,
            IsRegistered = Accepted | Registered,
            IsOnline = Accepted | Registered | Online
        }

        // references

        public string IPAddress;
        public bool IsReconnecting;

        public Character Character;
        public int LauncherId;

        // status

        public States State;

        public int[] Relays;
        public int[][] Requests;
        public int[] Other;

        public readonly Dictionary<int, object> IdList; // character.Id, <null>

        public int LastTurn;

        public long LastActivity;
        public long LastPingRequest;
        public long LastPingReply;

        public bool IconRequest;
        public readonly Dictionary<int, int> HexRequests;

        public Client27000(Socket sock) : base(sock, new DuplexQueue(isShared: false), dataMinSize: 4, dataMaxSize: 32768, dataDelimiter: null)
        {
            // references

            IPAddress = ((IPEndPoint)sock.RemoteEndPoint).Address.ToString();

            // status

            Relays = new int[(int)ClientRelays.Total];

            Array.Fill(Relays, -1);

            Requests = new int[(int)ClientRequests.Total][];

            // ... must match the order in "enum ClientRequests"

            Requests[0] = [(int)ClientRelays.MetaViewPortHandlerNameC, 0, 0, 0x00];
            Requests[1] = [(int)ClientRelays.MetaViewPortHandlerNameC, 0, 0, 0x0d];
            Requests[2] = [(int)ClientRelays.MetaViewPortHandlerNameC, 0, 0, 0x0f];
            Requests[3] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x02];
            Requests[4] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x03];
            Requests[5] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x04];
            Requests[6] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x05];
            Requests[7] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x06];
            Requests[8] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x07];
            Requests[9] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x08];
            Requests[10] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x0b];
            Requests[11] = [(int)ClientRelays.PlayerRelayC, 0, 0, 0x0c];
            Requests[12] = [(int)ClientRelays.MetaClientNewsPanel, 0, 0, 0x00];
            Requests[13] = [(int)ClientRelays.MetaClientPlayerListPanel, 0, 0, 0x00];
            Requests[14] = [(int)ClientRelays.MetaClientPlayerListPanel, 0, 0, 0x01];
            Requests[15] = [(int)ClientRelays.MetaClientShipPanel, 0, 0, 0x0d];
            Requests[16] = [(int)ClientRelays.MetaClientSupplyDockPanel, 0, 0, 0x0d];
            Requests[17] = [(int)ClientRelays.MetaClientSupplyDockPanel, 0, 0, 0x0e];

            // ... other

            Other =
            [
                -1, // set in Q_14_8() and used in Q_1_1()
                -1  // set in Q_D_2() and used in Q_1_1() 
            ];

            // ... request stuff

            IdList = [];

            LastPingRequest = -1;

            HexRequests = [];
        }
    }
}
