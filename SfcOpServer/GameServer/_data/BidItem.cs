#pragma warning disable IDE0130

using shrServices;
using System.IO;

namespace SfcOpServer
{
    public sealed class BidItem
    {
        public readonly static BidItem Empty;

        public int Id;
        public int LockID;

        public byte BiddingHasBegun;

        public string ShipClassName;
        public int ShipId;
        public int ShipBPV;

        public int AuctionValue;
        public double AuctionRate;

        public int TurnOpened;
        public int TurnToClose;
        public int CurrentBid;

        public int BidOwnerID;
        public int TurnBidMade;
        public int BidMaximum;

        static BidItem()
        {
            Empty = new()
            {
                ShipClassName = string.Empty,
                AuctionRate = 1.0
            };
        }

        public BidItem()
        { }

        public BidItem(BinaryReader r)
        {
            Id = r.ReadInt32();
            LockID = r.ReadInt32();

            BiddingHasBegun = r.ReadByte();

            Utils.Read(r, false, out ShipClassName);

            ShipId = r.ReadInt32();
            ShipBPV = r.ReadInt32();

            AuctionValue = r.ReadInt32();
            AuctionRate = r.ReadDouble();

            TurnOpened = r.ReadInt32();
            TurnToClose = r.ReadInt32();
            CurrentBid = r.ReadInt32();

            BidOwnerID = r.ReadInt32();
            TurnBidMade = r.ReadInt32();
            BidMaximum = r.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(Id);
            w.Write(LockID);

            w.Write(BiddingHasBegun);

            Utils.Write(w, false, ShipClassName);

            w.Write(ShipId);
            w.Write(ShipBPV);

            w.Write(AuctionValue);
            w.Write(AuctionRate);

            w.Write(TurnOpened);
            w.Write(TurnToClose);
            w.Write(CurrentBid);

            w.Write(BidOwnerID);
            w.Write(TurnBidMade);
            w.Write(BidMaximum);
        }
    }
}
