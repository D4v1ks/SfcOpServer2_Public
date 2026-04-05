#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum Races
    {
        kNoRace = -1,

        // empires

        kFederation,
        kKlingon,
        kRomulan,
        kLyran,
        kHydran,
        kGorn,
        kISC,
        kMirak,

        // cartels

        kOrionOrion,
        kOrionKorgath,
        kOrionPrime,
        kOrionTigerHeart,
        kOrionBeastRaiders,
        kOrionSyndicate,
        kOrionWyldeFire,
        kOrionCamboro,

        // NPC

        kOrion,
        kMonster,

        // neutral

        kTholian,
        kLDR,
        kWYN,
        kJindarian,
        kAndro,
        kNeutralRace,
        kMirror,

        // total

        kNumberOfRaces,

        // helpers

        kFirstEmpire = kFederation,
        kLastEmpire = kMirak,

        kFirstCartel = kOrionOrion,
        kLastCartel = kOrionCamboro,

        kFirstNPC = kOrion,
        kLastNPC = kMonster,

        kFirstNeutral = kTholian,
        kLastNeutral = kMirror
    };
}
