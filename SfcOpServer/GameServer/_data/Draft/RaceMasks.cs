#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum RaceMasks
    {
        None = 0,

        // empires

        kFederation = 1 << Races.kFederation,
        kKlingon = 1 << Races.kKlingon,
        kRomulan = 1 << Races.kRomulan,
        kLyran = 1 << Races.kLyran,
        kHydran = 1 << Races.kHydran,
        kGorn = 1 << Races.kGorn,
        kISC = 1 << Races.kISC,
        kMirak = 1 << Races.kMirak,

        // cartels

        kOrionOrion = 1 << Races.kOrionOrion,
        kOrionKorgath = 1 << Races.kOrionKorgath,
        kOrionPrime = 1 << Races.kOrionPrime,
        kOrionTigerHeart = 1 << Races.kOrionTigerHeart,
        kOrionBeastRaiders = 1 << Races.kOrionBeastRaiders,
        kOrionSyndicate = 1 << Races.kOrionSyndicate,
        kOrionWyldeFire = 1 << Races.kOrionWyldeFire,
        kOrionCamboro = 1 << Races.kOrionCamboro,

        // NPC

        kOrion = 1 << Races.kOrion,
        kMonster = 1 << Races.kMonster,

        // other

        kTholian = 1 << Races.kTholian,
        kLDR = 1 << Races.kLDR,
        kWYN = 1 << Races.kWYN,
        kJindarian = 1 << Races.kJindarian,
        kAndro = 1 << Races.kAndro,

        kNeutralRace = 1 << Races.kNeutralRace,

        kMirror = 1 << Races.kMirror,

        // total

        AllRaces = (1 << Races.kNumberOfRaces) - 1,

        // helpers

        AllEmpires = kFederation | kKlingon | kRomulan | kLyran | kHydran | kGorn | kISC | kMirak,
        AllCartels = kOrionOrion | kOrionKorgath | kOrionPrime | kOrionTigerHeart | kOrionBeastRaiders | kOrionSyndicate | kOrionWyldeFire | kOrionCamboro
    }
}
