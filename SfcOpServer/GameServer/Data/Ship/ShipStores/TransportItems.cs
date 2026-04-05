#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum TransportItems
    {
        kTransNothing,

        kTransGornEgg, // can't be used because it has priority over the spare parts in the "beam in" UI
        kTransSpareParts,
        kTransCaptain, // reserved by the game
        kTransBlueStar,
        kTransDilithiumCrystals,
        kTransGravTank,
        kTransMarineDude,
        kTransHarryMudd,
        kTransInfiltrator,
        kTransNovaMine,
        kTransRomulanAle,
        kTransTribbles,
        kTransAlienArtifact,
        kTransCaptainDude,
        kTransWeaponSchematic,
        kTransMedicalSupplies,
        kTransAwayTeamItem,
        kTransDiplomat,
        kTransInjuredDude,
        kTransMedicineJar,
        kTransScientists,
        kTransLifePodDude,
        kTransDeathPlague,
        kTransBlackBox,
        kTransPsionicDisruptor,
        kTransIonicProjector,
        kTransEngineers,
        kTransPrisoner,

        Total
    };
}
