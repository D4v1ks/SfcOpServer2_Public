#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum PlanetTypes
    {
        kPlanetNone = 0,

        kPlanetHomeWorld1 = 1 << 0,
        kPlanetHomeWorld2 = 1 << 1,
        kPlanetHomeWorld3 = 1 << 2,
        kPlanetCoreWorld1 = 1 << 3,
        kPlanetCoreWorld2 = 1 << 4,
        kPlanetCoreWorld3 = 1 << 5,
        kPlanetColony1 = 1 << 6,
        kPlanetColony2 = 1 << 7,
        kPlanetColony3 = 1 << 8,
        kPlanetAsteroidBase1 = 1 << 9,
        kPlanetAsteroidBase2 = 1 << 10,
        kPlanetAsteroidBase3 = 1 << 11,
    }
}
