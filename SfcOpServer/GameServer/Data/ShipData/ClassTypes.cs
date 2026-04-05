#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum ClassTypes
    {
        kNoClassType = -1, // used for stars (NONE)

        // ships

        kClassShuttle,
        kClassPseudoFighter,

        kClassFreighter,
        kClassFrigate,
        kClassDestroyer,
        kClassWarDestroyer,
        kClassLightCruiser,
        kClassHeavyCruiser,
        kClassNewHeavyCruiser,
        kClassHeavyBattlecruiser,
        kClassCarrier,
        kClassDreadnought,
        kClassBattleship,

        // bases

        kClassListeningPost,
        kClassBaseStation,
        kClassBattleStation,
        kClassStarBase,

        // other

        kClassMonster,
        kClassPlanets,
        kClassSpecial,

        // total

        kMaxClasses
    };
}
