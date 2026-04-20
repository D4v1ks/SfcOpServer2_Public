#pragma warning disable IDE0079
#pragma warning disable IDE0305

using shrServices;

using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private bool TryCreateScriptINI(Mission mission, in int totalAllied, in int totalEnemy, in int totalNeutral, in int planetBits, in int baseBits, in int specialBits, MapTemplate mapTemplate)
        {
            Contract.Assert(totalAllied > 0);

            string ini =
                "Flags = %fl%\n" +
                "TeamID = %ti%\n" +
                "\n" +
                "[Teams]\n" +
                "%ts%" +
                "\n" +
                "[Ships]\n" +
                "%ss%" +
                "\n" +
                "[Map/Lines]\n" +
                "%ml%" +
                "\n" +
                "[Map/Overrides]\n" +
                "%mo%" +
                "\n" +
                "[Prestige]\n" +
                "LeftEarly = -200\n" +
                "AstoundingVictory = 1000\n" +
                "Victory = 750\n" +
                "Draw = 0\n" +
                "Defeat = -50\n" +
                "DevastatingDefeat = -100\n" +
                "\n" +
                "[Messages]\n" +
                "%ms%"
            ;

            StringBuilder s = new(1024);

            // flags

            bool isTeamDeathMatch = totalEnemy + totalNeutral > 0;

            if (isTeamDeathMatch)
                s.Append('1'); // fl_NewServer
            else
                s.Append('3'); // fl_NewServer | fl_FriendlyMatch

            ini = ini.Replace("%fl%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // teams setup (0 - 19)

            foreach (var ts in mission.Teams)
            {
                Team team1 = ts.Value;

                s.Append((int)team1.Id);
                s.Append(" = ");
                s.AppendHex((byte)(((int)team1.Type << 4) | (int)team1.Tag));

                foreach (var p in mission.Teams)
                {
                    Team team2 = p.Value;

                    if (team1.Tag == team2.Tag)
                    {
                        if (team1.Id == team2.Id)
                            s.Append("010a");
                        else
                            s.Append("020a");
                    }
                    else
                        s.Append("360a");
                }

                s.AppendLine();
            }

            ini = ini.Replace("%ts%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // ships setup (0 - 59)

            int c = 0;

            foreach (var ts in mission.Teams)
            {
                Team team = ts.Value;

                foreach (var ss in team.Ships)
                {
                    Ship ship = _ships[ss.Key];

                    mission.CustomIds.Add(c + Mission.FirstCustomId, ship.Id);

                    s.Append(c);
                    s.Append(" = ");

                    c++;

                    // meta id

                    s.AppendHex((uint)ship.Id);

                    // start position

                    s.AppendHex((uint)ShipOptions.kStartPositionCanBeOffset);

                    s.AppendHex((byte)0); // startX
                    s.AppendHex((byte)0); // startY

                    s.AppendHex((byte)0); // endX
                    s.AppendHex((byte)0); // endY

                    // transport items

                    ship.Stores.TransportItems.Remove(TransportItems.kTransSpareParts);

                    if (ship.Stores.DamageControl.CurrentQuantity > 0)
                        ship.Stores.TransportItems.Add(TransportItems.kTransSpareParts, ship.Stores.DamageControl.CurrentQuantity);

                    foreach (var p in ship.Stores.TransportItems)
                    {
                        Contract.Assert(p.Key > TransportItems.kTransNothing && p.Key < TransportItems.Total && p.Value > 0 && p.Value <= 255);

                        s.AppendHex((byte)p.Key);
                        s.AppendHex((byte)p.Value);
                    }

                    s.Append("00");

                    // patrol points

                    s.Append("00"); // number of points

                    /*
                        s.AppendHex((byte)Math.Truncate(1.0)); // detection range (min 1.0)
                        s.AppendHex((byte)AIPriorities.kAbsolutePriority); // AI priority

                        s.AppendHex((byte)14); // x0
                        s.AppendHex((byte)18); // y0

                        s.AppendHex((byte)14); // x1
                        s.AppendHex((byte)16); // y1

                        s.AppendHex((byte)16); // x2
                        s.AppendHex((byte)16); // y2

                        s.AppendHex((byte)16); // x3
                        s.AppendHex((byte)18); // y3
                    */

                    // initial flags

                    if (isTeamDeathMatch)
                        s.AppendLine("01"); // ship is controlled
                    else
                        s.AppendLine("00"); // ship is idle
                }
            }

            ini = ini.Replace("%ss%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // 0 = +___________________+\n

            int line = 0;

            s.Append(line);
            s.Append(" = +");
            s.Append('_', mapTemplate.Width);
            s.AppendLine("+");

            // 1..40 = |...................|\n

            for (int i = 0; i < mapTemplate.Height; i++)
            {
                line++;

                s.Append(line);
                s.Append(" = |");
                s.Append(mapTemplate.GetLine(i));
                s.AppendLine("|");
            }

            // 41 = +-------------------+\n

            line++;

            s.Append(line);
            s.Append(" = +");
            s.Append('-', mapTemplate.Width);
            s.AppendLine("+");

            // map lines

            ini = ini.Replace("%ml%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // map overrides (G -> Z)

            foreach (var mo in mission.Teams)
            {
                Team team = mo.Value;

                s.Append(char.ConvertFromUtf32((int)team.Id + 71));
                s.Append(" = ");

                int teamBit = 1 << (int)team.Id;

                if (team.Tag == TeamTags.kTagA)
                {
                    if (team.Id == TeamIds.kTeam1)
                        s.Append((int)MapObjectTypes.kObjectPlayerShip);
                    else if ((baseBits & teamBit) != 0)
                        s.Append((int)MapObjectTypes.kObjectPlayerBase);
                    else if ((planetBits & teamBit) != 0)
                        s.Append((int)MapObjectTypes.kObjectPlanet);
                    else
                        s.Append((int)MapObjectTypes.kObjectAlliedShip);
                }
                else if ((baseBits & teamBit) != 0)
                    s.Append((int)MapObjectTypes.kObjectEnemyBase);
                else if ((planetBits & teamBit) != 0)
                    s.Append((int)MapObjectTypes.kObjectPlanet);
                else
                    s.Append((int)MapObjectTypes.kObjectEnemyShip);

                s.AppendLine();
            }

            // ... 0 -> 9 and PL1 -> PL0

            if (mapTemplate.MapOverrides.Length != 0)
                s.Append(mapTemplate.MapOverrides);

            ini = ini.Replace("%mo%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // messages

            s.Append("0 = Mission Briefing ");
            s.AppendLine(_rand.NextUInt32().ToString("X8"));

            s.Append("1 = Mission Description ");
            s.AppendLine(_rand.NextUInt32().ToString("X8"));

            s.AppendLine("2 = Left Early");
            s.AppendLine("3 = Astounding Victory");
            s.AppendLine("4 = Victory");
            s.AppendLine("5 = Draw");
            s.AppendLine("6 = Defeat");
            s.AppendLine("7 = Devastating Defeat");

            ini = ini.Replace("%ms%", s.ToString(), StringComparison.Ordinal);

            s.Clear();

            // checks if we need to override the mission background

            if (mapTemplate.Background != null)
                mission.Background = mapTemplate.Background;

            // sets the config

            mission.Config = ini;
            mission.Musics = mapTemplate.Musics.ToArray();

            return true;
        }
    }
}
