#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum BaseTypes
    {
        kBaseNone = 0,

        kBaseStarbase = 1 << 0,
        kBaseBattleStation = 1 << 1,
        kBaseBaseStation = 1 << 2,
        kBaseWeaponsPlatform = 1 << 3,
        kBaseListeningPost = 1 << 4,
    }
}
