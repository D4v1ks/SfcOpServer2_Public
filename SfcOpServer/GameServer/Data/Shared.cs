using System;
using System.Collections.Generic;

namespace SfcOpServer
{
    public partial class GameServer
    {
        // hex map

        private static readonly int[] _classTypeIcons =
        [
            -1, // kClassShuttle
            -1, // kClassPseudoFighter

            2,  // kClassFreighter
            3,  // kClassFrigate
            14, // kClassDestroyer
            14, // kClassWarDestroyer
            4,  // kClassLightCruiser
            7,  // kClassHeavyCruiser
            7,  // kClassNewHeavyCruiser
            15, // kClassHeavyBattlecruiser
            10, // kClassCarrier
            11, // kClassDreadnought
            11, // kClassBattleship

            -1, // kClassListeningPost
            -1, // kClassBaseStation
            -1, // kClassBattleStation
            -1, // kClassStarBase

            1,  // kClassMonster

            -1, // kClassPlanets
            -1  // kClassSpecial
        ];

        // script map

        private static readonly string[] _mapTerrains =
        [
            "asteroids1",
            "asteroids2",
            "asteroids3",
            "asteroids4",
            "asteroids5",
            "asteroids6",

            "space1",
            "space2",
            "space3",
            "space4",
            "space5",
            "space6",

            "nebula1",
            "nebula2",
            "nebula3",
            "nebula4",
            "nebula5",
            "nebula6",

            "blackhole1",
            "blackhole2",
            "blackhole3",
            "blackhole4",
            "blackhole5",
            "blackhole6"
        ];

        // specs

        private static readonly string[] _races =
        [
            "Federation",
            "Klingon",
            "Romulan",
            "Lyran",
            "Hydran",
            "Gorn",
            "ISC",
            "Mirak",

            "OrionOrion",
            "OrionKorgath",
            "OrionPrime",
            "OrionTigerHeart",
            "OrionBeastRaiders",
            "OrionSyndicate",
            "OrionWyldeFire",
            "OrionCamboro",

            "Orion",
            "Monster",

            null,
            null,
            null,
            null,
            null,
            "Neutral",
            null
        ];

        private static readonly string[] _raceAbbreviations =
        [
            "F",
            "K",
            "R",
            "L",
            "H",
            "G",
            "I",
            "Z",

            "X",
            "Y",
            "P",
            "T",
            "B",
            "S",
            "W",
            "C",

            "N",
            "M",

            null,
            null,
            null,
            null,
            null,
            "N",
            null
        ];

        /// <summary>
        /// These abbreviations are meant to be unique and only used in the context of a chat command
        /// </summary>
        private static readonly string[] _realAbbreviations =
        [
            "F",
            "K",
            "R",
            "L",
            "H",
            "G",
            "I",
            "Z",

            "X",
            "Y",
            "P",
            "T",
            "B",
            "S",
            "W",
            "C",

            "O",
            "M",

            null,
            null,
            null,
            null,
            null,
            "N",
            null
        ];

        private static readonly string[] _raceNames =
        [
            "United Federation of Planets",
            "Klingon Empire",
            "Romulan Star Empire",
            "Lyran Star Empire",
            "Hydran Kingdom",
            "Gorn Confederation",
            "Interstellar Concordium",
            "Mirak Star League",

            "Orion Cartel",
            "Crimson Shadow Cartel",
            "Prime Industries Cartel",
            "Tiger Heart Cartel",
            "Beast Raiders Cartel",
            "The Syndicate Cartel",
            "Wyldefire Compact Cartel",
            "Camboro Cartel",

            "Orion Pirates",
            "Living Monsters",

            null,
            null,
            null,
            null,
            null,
            "Neutral",
            null,

            "Unknown" // Races.kNumberOfRaces
        ];

        private static readonly ConsoleColor[] _raceColors =
        [
            ConsoleColor.Blue,
            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Magenta,
            ConsoleColor.Cyan
        ];

        private static readonly string[] _hullTypes =
        [
            "FF",
            "DD",
            "CL",
            "CA",
            "DN",
            "F",

            "SB",  // star base
            "BS",  // base station
            "BT",  // battle station
            "AB",  // asteroid base (not used!)
            "KLP", // listening post (BASE)
            "KDP", // defense platform (BASE: weapons platform)
            "KSD", // star dock (fleet repair dock)

            "MAM", // astro miner
            "MSG", // sun glider
            "MDM", // doomsday machine
            "MLC", // living cage
            "MMT", // m_eater
            "MSS", // space shell
            "MIT", // intruder
            
            "BOX",
            "XMN", // mine hull
            "FTR", // pseudo fighter
            "SHT",

            "Planet",
            "Moon",
            "Star",
            "Asteroid",

            "Plasma",
            "Drone",

            null,  // bodies
            "ASF", // fissure
            "ABH", // black hole
            "AWH", // worm hole
            
            "FSH",
            "KSH",
            "RSH",
            "LSH",
            "HSH",
            "GSH",
            "ISH",
            "ZSH",
            "OSH",

            "Unknown"
        ];

        private static readonly string[] _classTypes =
        [
            "Unknown",

            "SHUTTLE",
            "PF",
            "FREIGHTER",
            "FRIGATE",
            "DESTROYER",
            "WAR_DESTROYER",
            "LIGHT_CRUISER",
            "HEAVY_CRUISER",
            "NEW_HEAVY_CRUISER",
            "HEAVY_BATTLECRUISER",
            "CARRIER",
            "DREADNOUGHT",
            "BATTLESHIP",
            "LISTENING_POST",
            "BASE_STATION",
            "BATTLE_STATION",
            "STARBASE",
            "MONSTER",
            "PLANET",
            "SPECIAL"
        ];

        private static readonly string[] _specialRoles =
        [
            string.Empty,

            "C",
            "D",
            "K",
            "KM",
            "M",
            "NT",
            "S",
            "V", // used in carriers

            "A",
            "E",
            "I",
            "L",
            "P",
            "Q",
            "R",

            "T"
        ];

        private static readonly string[] _weaponTypes =
        [
            string.Empty,

            "Phot", "PhoF", "PhoH",
            "Dis1", "Dis2", "Dis3", "Dis4", "DisF", "DisH",
            "Fus", "FusF",
            "HB", "Hellf",
            "PlaR", "PlaS", "PlaG", "PlaF", "PlaI", "PlaD", "PlaX", "PlaE",
            "ESG", "ESGL",
            "PPD",
            "MLR",
            "PhA", "PhB",
            "TRBL", "TRBH",
            null,
            "DroA", "DroB", "DroC", "DroD", "DroE", "DroF", "DroG", "DroH", "DroI", "DroVI", "DroM",
            "Opt", "OptW",
            "Ph1", "Ph2", "Ph3", "PhG", "PhG2", "Ph4", "PhX",
            "ADD6", "ADD12", "ADD30"
        ];

        private static readonly string[] _weaponArcs =
        [
            string.Empty,

            "1",
            "2",
            "3",

            "Arc360",
            "Arc240",
            "Arc210",
            "Arc180",
            "Arc120",
            "FA",
            "RS",
            "FRR",
            "FLL",
            "FH",
            "RH",
            "LS",
            "FX",
            "RX",
            "RRR",
            "LLR",
            "FAR",
            "FAL",
            "ALL",
            "RW",
            "LW",
            "FHR",
            "FHL",
            "RA",
            "RWX",
            "LWX",
            "RP",
            "LP",
            "RAR",
            "RAL",
            "FLLX",
            "FRRX",
            "FARA",
            "KFX",
            "MLR",
            "ESGL",
            "LR",
            "RRP",
            "RLP",
            "RSLF",
            "LSRF",
            "FALX",
            "FARX",
            "SFBL",
            "SFBR"
        ];

        private readonly Dictionary<WeaponTypes, int> _droneCapacities = new()
        {
            { WeaponTypes.DroneRackA, 4 },
            { WeaponTypes.DroneRackB, 6 },
            { WeaponTypes.DroneRackC, 4 },
            { WeaponTypes.DroneRackD, 0 }, // used to be 4
            { WeaponTypes.DroneRackE, 8 },
            { WeaponTypes.DroneRackF, 4 },
            { WeaponTypes.DroneRackG, 4 },
            { WeaponTypes.DroneRackH, 4 },

            { WeaponTypes.DroneFighter1, 4 },
            { WeaponTypes.DroneFighter4, 4 },

            { WeaponTypes.DroneRackMIRV, 4 }
        };

        private readonly int[] _missileSizes =
        [
            1, // Type1
            1, // Type2 (used to be TypeIV size 2)
            1, // Type3
            1,
            1,
            1,
            1
        ];

        private static readonly int[] _officerDefaults =
        [
            0x00,
            0x00, // kRookie
            0x05, // kJunior
            0x14, // kSenior
            0x32, // kVeteran
            0xc8  // kLegendary
        ];

        // drafts

        private static readonly string[] _missionNames =
        [
            "The Cage",
            "Where No Man Has Gone Before",
            "The Corbomite Maneuver",
            "Mudd's Women",
            "The Enemy Within",
            "The Man Trap",
            "The Naked Time",
            "Charlie X",
            "Balance of Terror",
            "What Are Little Girls Made Of?",
            "Dagger of the Mind",
            "Miri",
            "The Conscience of the King",
            "The Galileo Seven",
            "Court Martial",
            "The Menagerie, Parts I and II",
            "Shore Leave",
            "The Squire of Gothos",
            "Arena",
            "The Alternative Factor",
            "Tomorrow Is Yesterday",
            "The Return of the Archons",
            "A Taste of Armageddon",
            "Space Seed",
            "This Side of Paradise",
            "The Devil in the Dark",
            "Errand of Mercy",
            "The City on the Edge of Forever",
            "Operation: Annihilate!",
            "Catspaw",
            "Metamorphosis",
            "Friday's Child",
            "Who Mourns for Adonais?",
            "Amok Time",
            "The Doomsday Machine",
            "Wolf in the Fold",
            "The Changeling",
            "The Apple",
            "Mirror, Mirror",
            "The Deadly Years",
            "I, Mudd",
            "The Trouble with Tribbles",
            "Bread and Circuses",
            "Journey to Babel",
            "A Private Little War",
            "The Gamesters of Triskelion",
            "Obsession",
            "The Immunity Syndrome",
            "A Piece of the Action",
            "By Any Other Name",
            "Return to Tomorrow",
            "Patterns of Force",
            "The Ultimate Computer",
            "The Omega Glory",
            "Assignment: Earth",
            "Spectre of the Gun",
            "Elaan of Troyius",
            "The Paradise Syndrome",
            "The Enterprise Incident",
            "And the Children Shall Lead",
            "Spock's Brain",
            "Is There in Truth No Beauty?",
            "The Empath",
            "The Tholian Web",
            "For the World Is Hollow and I've Touched the Sky",
            "Day of the Dove",
            "Plato's Stepchildren",
            "Wink of an Eye",
            "That Which Survives",
            "Let That Be Your Last Battlefield",
            "Whom Gods Destroy",
            "The Mark of Gideon",
            "The Lights of Zetar",
            "The Cloud Minders",
            "The Way to Eden",
            "Requiem for Methuselah",
            "The Savage Curtain",
            "All Our Yesterdays",
            "Turnabout Intruder",
            "Encounter at Farpoint",
            "The Naked Now",
            "Code of Honor",
            "The Last Outpost",
            "Where No One Has Gone Before",
            "Lonely Among Us",
            "Justice",
            "The Battle",
            "Hide and Q",
            "Haven",
            "The Big Goodbye",
            "Datalore",
            "Angel One",
            "11001001",
            "Too Short a Season",
            "When the Bough Breaks",
            "Home Soil",
            "Coming of Age",
            "Heart of Glory",
            "The Arsenal of Freedom",
            "Symbiosis",
            "Skin of Evil",
            "We'll Always Have Paris",
            "Conspiracy",
            "The Neutral Zone",
            "The Child",
            "Where Silence Has Lease",
            "Elementary, Dear Data",
            "The Outrageous Okona",
            "Loud as a Whisper",
            "The Schizoid Man",
            "Unnatural Selection",
            "A Matter of Honor",
            "The Measure of a Man",
            "The Dauphin",
            "Contagion",
            "The Royale",
            "Time Squared",
            "The Icarus Factor",
            "Pen Pals",
            "Q Who",
            "Samaritan Snare",
            "Up the Long Ladder",
            "Manhunt",
            "The Emissary",
            "Peak Performance",
            "Shades of Gray",
            "Evolution",
            "The Ensigns of Command",
            "The Survivors",
            "Who Watches the Watchers",
            "The Bonding",
            "Booby Trap",
            "The Enemy",
            "The Price",
            "The Vengeance Factor",
            "The Defector",
            "The Hunted",
            "The High Ground",
            "Deja Q",
            "A Matter of Perspective",
            "Yesterday's Enterprise",
            "The Offspring",
            "Sins of the Father",
            "Allegiance",
            "Captain's Holiday",
            "Tin Man",
            "Hollow Pursuits",
            "The Most Toys",
            "Sarek",
            "Menage a Troi",
            "Transfigurations",
            "The Best of Both Worlds",
            "Family",
            "Brothers",
            "Suddenly Human",
            "Remember Me",
            "Legacy",
            "Reunion",
            "Future Imperfect",
            "Final Mission",
            "The Loss",
            "Data's Day",
            "The Wounded",
            "Devil's Due",
            "Clues",
            "First Contact",
            "Galaxy's Child",
            "Night Terrors",
            "Identity Crisis",
            "The Nth Degree",
            "Qpid",
            "The Drumhead",
            "Half a Life",
            "The Host",
            "The Mind's Eye",
            "In Theory",
            "Redemption",
            "Darmok",
            "Ensign Ro",
            "Silicon Avatar",
            "Disaster",
            "The Game",
            "Unification",
            "A Matter of Time",
            "New Ground",
            "Hero Worship",
            "Violations",
            "The Masterpiece Society",
            "Conundrum",
            "Power Play",
            "Ethics",
            "The Outcast",
            "Cause and Effect",
            "The First Duty",
            "Cost of Living",
            "The Perfect Mate",
            "Imaginary Friend",
            "I, Borg",
            "The Next Phase",
            "The Inner Light",
            "Time's Arrow",
            "Realm of Fear",
            "Man of the People",
            "Relics",
            "Schisms",
            "True Q",
            "Rascals",
            "A Fistful of Datas",
            "The Quality of Life",
            "Chain of Command",
            "Ship in a Bottle",
            "Aquiel",
            "Face of the Enemy",
            "Tapestry",
            "Birthright",
            "Starship Mine",
            "Lessons",
            "The Chase",
            "Frame of Mind",
            "Suspicions",
            "Rightful Heir",
            "Second Chances",
            "Timescape",
            "Descent",
            "Liaisons",
            "Interface",
            "Gambit",
            "Phantasms",
            "Dark Page",
            "Attached",
            "Force of Nature",
            "Inheritance",
            "Parallels",
            "The Pegasus",
            "Homeward",
            "Sub Rosa",
            "Lower Decks",
            "Thine Own Self",
            "Masks",
            "Eye of the Beholder",
            "Genesis",
            "Journey's End",
            "Firstborn",
            "Bloodlines",
            "Emergence",
            "Preemptive Strike",
            "All Good Things..."
        ];
    }
}
