#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum TerrainTypes
    {
        kTerrainNone = 0,

        kTerrainSpace1 = 1 << 0,
        kTerrainSpace2 = 1 << 1,
        kTerrainSpace3 = 1 << 2,
        kTerrainSpace4 = 1 << 3,
        kTerrainSpace5 = 1 << 4,
        kTerrainSpace6 = 1 << 5,

        kTerrainAsteroids1 = 1 << 6,
        kTerrainAsteroids2 = 1 << 7,
        kTerrainAsteroids3 = 1 << 8,
        kTerrainAsteroids4 = 1 << 9,
        kTerrainAsteroids5 = 1 << 10,
        kTerrainAsteroids6 = 1 << 11,

        kTerrainNebula1 = 1 << 12,
        kTerrainNebula2 = 1 << 13,
        kTerrainNebula3 = 1 << 14,
        kTerrainNebula4 = 1 << 15,
        kTerrainNebula5 = 1 << 16,
        kTerrainNebula6 = 1 << 17,

        kTerrainBlackHole1 = 1 << 18,
        kTerrainBlackHole2 = 1 << 19,
        kTerrainBlackHole3 = 1 << 20,
        kTerrainBlackHole4 = 1 << 21,
        kTerrainBlackHole5 = 1 << 22,
        kTerrainBlackHole6 = 1 << 23,

        kTerrainDustclouds = 1 << 24,
        kTerrainShippingLane = 1 << 25,

        kAnySpace = kTerrainSpace1 | kTerrainSpace2 | kTerrainSpace3 | kTerrainSpace4 | kTerrainSpace5 | kTerrainSpace6,
        kAnyAsteroids = kTerrainAsteroids1 | kTerrainAsteroids2 | kTerrainAsteroids3 | kTerrainAsteroids4 | kTerrainAsteroids5 | kTerrainAsteroids6,
        kAnyNebula = kTerrainNebula1 | kTerrainNebula2 | kTerrainNebula3 | kTerrainNebula4 | kTerrainNebula5 | kTerrainNebula6,
        kAnyBlackHole = kTerrainBlackHole1 | kTerrainBlackHole2 | kTerrainBlackHole3 | kTerrainBlackHole4 | kTerrainBlackHole5 | kTerrainBlackHole6
    }
}
