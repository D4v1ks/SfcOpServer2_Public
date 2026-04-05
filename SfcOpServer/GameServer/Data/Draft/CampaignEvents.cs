#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum CampaignEvents
    {
        kNone = 0,

        kRetire = 1 << 1,
        kLost = 1 << 2,
    };
}
