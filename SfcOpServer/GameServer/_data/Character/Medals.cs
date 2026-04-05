#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum Medals
    {
        kNoMedals = 0,

        kMedalRankOne = 1 << 0,
        kMedalRankTwo = 1 << 1,
        kMedalRankThree = 1 << 2,
        kMedalRankFour = 1 << 3,
        kMedalRankFive = 1 << 4,

        kMedalMissionOne = 1 << 5,
        kMedalMissionTwo = 1 << 6,
        kMedalMissionThree = 1 << 7,
        kMedalMissionFour = 1 << 8,

        kMedalSpecialOne = 1 << 9,
        kMedalSpecialTwo = 1 << 10,
        kMedalSpecialThree = 1 << 11,
        kMedalSpecialFour = 1 << 12,

        kAllMedals = kMedalRankOne | kMedalRankTwo | kMedalRankThree | kMedalRankFour | kMedalRankFive | kMedalMissionOne | kMedalMissionTwo | kMedalMissionThree | kMedalMissionFour | kMedalSpecialOne | kMedalSpecialTwo | kMedalSpecialThree | kMedalSpecialFour
    };
}
