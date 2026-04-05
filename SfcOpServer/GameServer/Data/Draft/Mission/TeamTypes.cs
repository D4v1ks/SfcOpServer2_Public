#pragma warning disable IDE0130

namespace SfcOpServer
{
    public enum TeamTypes
    {
        kNoTeamType = -1,

        // Only run by the AI

        kNPCTeam,

        // Intended for human play, although there is a chance that an AI can fill the slot if the scripter wants it

        kPlayableTeam,

        kPrimaryTeam,
        kPrimaryOpponentTeam,

        // Used to indicate a team that has left the universe and must be removed

        kDeadTeam
    };
}
