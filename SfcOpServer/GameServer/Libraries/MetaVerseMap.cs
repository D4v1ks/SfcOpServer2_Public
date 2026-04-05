#pragma warning disable IDE0130, IDE0290, IDE1006

using shrGF;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public sealed class MetaVerseMap
    {
        public enum eVersion
        {
            None,

            Sfc2Eaw,
            Sfc2Op,
            Sfc3,

            Total
        }

        public enum eClass
        {
            Regions,
            CartelRegions,
            Terrain,
            Planets,
            Bases,

            Total
        }

        public struct tCell
        {
            public int Economic;
            public float Impedence;
            public int Strength;

            public int Region;
            public int CartelRegion;

            public int Terrain;
            public int Planet;
            public int Base;

            public tCell(BinaryReader r, GFFile mvm, eVersion version)
            {
                Economic = Math.Min(Math.Max(r.ReadInt32(), 0), 100);
                Impedence = Math.Min(Math.Max(r.ReadSingle(), 0.01f), 2.00f);
                Strength = Math.Min(Math.Max(r.ReadInt32(), 0), 200);

                Region = GetNormalizedIndex(mvm, version, eClass.Regions, r.ReadInt32());
                CartelRegion = GetNormalizedIndex(mvm, version, eClass.CartelRegions, r.ReadInt32());

                Terrain = GetNormalizedIndex(mvm, version, eClass.Terrain, r.ReadInt32());
                Planet = GetNormalizedIndex(mvm, version, eClass.Planets, r.ReadInt32());
                Base = GetNormalizedIndex(mvm, version, eClass.Bases, r.ReadInt32());
            }
        }

        public static readonly string[][][] Defaults =
        [
            [[/* None */]],

            [[/* Sfc2Eaw */]],
            [
                ["Neutral", "Federation", "Klingon", "Romulan", "Lyran", "Hydran", "Gorn", "ISC", "Mirak"],
                ["Neutral", "OrionOrion", "OrionKorgath", "OrionPrime", "OrionTigerHeart", "OrionBeastRaiders", "OrionSyndicate", "OrionWyldeFire", "OrionCamboro"],
                ["Space 1", "Space 2", "Space 3", "Space 4", "Space 5", "Space 6", "Asteroid 1", "Asteroid 2", "Asteroid 3", "Asteroid 4", "Asteroid 5", "Asteroid 6", "Nebula 1", "Nebula 2", "Nebula 3", "Nebula 4", "Nebula 5", "Nebula 6", "Blackhole1", "Blackhole2", "Blackhole3", "Blackhole4", "Blackhole5", "Blackhole6"],
                ["(none)", "Homeworld 1", "Homeworld 2", "Homeworld 3", "Core World 1", "Core World 2", "Core World 3", "Colony 1", "Colony 2", "Colony 3", "Asteroid Base 1", "Asteroid Base 2", "Asteroid Base 3"],
                ["(none)", "Starbase", "Battle Station", "Base Station", "Weapons Platform", "Listening Post"]
            ],
            [[/* Sfc3 */]]
        ];
        public static readonly string[] Classes =
        [
            "Regions",
            "CartelRegions",
            "Terrain",
            "Planets",
            "Bases"
        ];

        public readonly List<tCell> Cells;

        public eVersion Version;
        public int Width;
        public int Height;

        private static int GetNormalizedIndex(GFFile mvm, eVersion version, eClass classType, int index)
        {
            if (mvm.TryGetValue("Classes/" + Classes[(int)classType], index.ToString(CultureInfo.InvariantCulture), out string value, out _))
                return TryGetIndex(version, classType, value);

            return 0;
        }

        private static int TryGetIndex(eVersion version, eClass classType, string value)
        {
            ref string[] defaults = ref Defaults[(int)version][(int)classType];

            for (int i = 0; i < defaults.Length; i++)
            {
                if (defaults[i].Equals(value, StringComparison.Ordinal))
                    return i;
            }

            return 0;
        }

        public MetaVerseMap()
        {
            Cells = [];
        }

        public bool Load(string filename)
        {
            // tries to read the settings

            GFFile mvm = new();

            if (!mvm.Load(filename))
                return false;

            // tries to read the objects

            FileStream f = null;
            byte[] buffer;

            try
            {
                f = new(filename, FileMode.Open, FileAccess.Read);

                int c = (int)f.Length;

                if (c == 0)
                    return false;

                buffer = new byte[c];

                f.ReadExactly(buffer, 0, c);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                f?.Close();
            }

            // tries to parse the objects

            MemoryStream m = null;
            BinaryReader r = null;

            try
            {
                ReadOnlySpan<byte> objectHeader = "[Objects]\r\n"u8;

                int i = new ReadOnlySpan<byte>(buffer).IndexOf(objectHeader);

                if (i == 0)
                    return false;

                i += objectHeader.Length;

                while (buffer[i] == 0)
                    i++;

                int width = BitConverter.ToInt32(buffer, i);

                i += 4;

                if (width < 8 || width > 1000)
                    return false;

                int height = BitConverter.ToInt32(buffer, i);

                i += 4;

                if (height < 8 || height > 1000)
                    return false;

                int count = BitConverter.ToInt32(buffer, i);

                i += 4;

                int size = buffer.Length - i;
                eVersion version;

                if (size == count * 32)
                {
                    if (mvm.ContainsKey("Classes/CartelRegions", "1"))
                    {
                        if (count > 2728)
                            return false; // the server can't handle more

                        version = eVersion.Sfc2Op;
                    }
                    else
                        throw new NotImplementedException("version = eVersion.Sfc3");
                }
                else if (size == count * 28)
                    throw new NotImplementedException("version = eVersion.Sfc2Eaw");
                else
                    return false;

                m = new(buffer, i, size);
                r = new(m, Encoding.UTF8, true);

                for (i = 0; i < count; i++)
                    Cells.Add(new tCell(r, mvm, version));

                Version = version;

                Width = width;
                Height = height;

                return true;
            }
            catch (Exception)
            {
                Clear();

                return false;
            }
            finally
            {
                r?.Dispose();
                m?.Dispose();
            }
        }

        public void Clear()
        {
            Cells.Clear();

            Version = eVersion.None;
            Width = 0;
            Height = 0;
        }
    }
}
