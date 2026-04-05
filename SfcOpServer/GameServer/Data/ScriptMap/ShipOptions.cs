#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum ShipOptions
    {
        kNoShipOptions,

        // 0x0000000f

        kStartPositionCanBeOffset = 1 << 0,

        // 0x000000f0

        kStartPositionOffsetE = 1 << 4,
        kStartPositionOffsetW = 2 << 4,
        kStartPositionOffsetSE = 3 << 4,
        kStartPositionOffsetSW = 4 << 4,
        kStartPositionOffsetS = 5 << 4,
        kStartPositionOffsetN = 6 << 4,
        kStartPositionOffsetNE = 7 << 4,
        kStartPositionOffsetNW = 8 << 4,
        kStartPositionOffsetOrigin = 9 << 4,

        // 0x00fff000

        kCanAddToFleetUponCapture = 1 << 12,

        kDeathUponCapture = 1 << 13,
        kDeathUponDisengage = 1 << 14
    };
}
