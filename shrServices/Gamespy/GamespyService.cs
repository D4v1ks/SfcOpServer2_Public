using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

namespace shrServices
{
    public sealed class GamespyService
    {
        public const string ClientVersion = "2.5.6.5";

        public const string GameName = "sfc2op";
        public const string GameVersion = "1.6";

        private static readonly Lock _userLock;
        private static readonly Dictionary<string, int> _emails;
        private static readonly Dictionary<string, int> _nicks;
        private static readonly Dictionary<int, GamespyUser> _users;

        private static readonly Lock _gameLock;
        private static readonly Dictionary<int, GamespyGame> _games;

        static GamespyService()
        {
            _userLock = new();
            _emails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _nicks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _users = [];

            _gameLock = new();
            _games = [];
        }

        // IO

        public static void ReadFrom(BinaryReader r)
        {
            Contract.Assert(r != null);
        }

        public static void WriteTo(BinaryWriter w)
        {
            Contract.Assert(w != null);
        }

        // Profiles

        public static bool ContainsEmail(string email)
        {
            lock (_userLock)
                return _emails.ContainsKey(email);
        }

        public static int AddUser(string email, string nick, string password, out GamespyUser user)
        {
            user = new(email, nick, password);

            int id = user.Id;

            lock (_userLock)
            {
                if (_emails.TryAdd(email, id))
                {
                    if (_nicks.TryAdd(nick, id))
                    {
                        _users.Add(id, user);

                        return id;
                    }

                    _emails.Remove(email);

                    id = -2; // nick already exists
                }
                else
                    id = -1; // email already exists
            }

            user = null;

            return id;
        }

        public static void GetUser(string username, out GamespyUser user)
        {
            int i = username.IndexOf('@');

            if (i >= 0)
            {
                lock (_userLock)
                {
                    if (
                        _emails.TryGetValue(username[(i + 1)..], out int emailId) &&
                        _nicks.TryGetValue(username[..i], out int nickId) &&
                        emailId == nickId
                    )
                    {
                        user = _users[emailId];

                        return;
                    }
                }
            }

            user = null;
        }

        // Servers

        public static void AddGame(GamespyGame game)
        {
            lock (_gameLock)
                _games.Add(game.Id, game);
        }

        public static void RemoveGame(GamespyGame game)
        {
            lock (_gameLock)
                _games.Remove(game.Id);
        }

        public static void GetGame(int id, out GamespyGame game)
        {
            lock (_gameLock)
                _games.TryGetValue(id, out game);
        }

        public static void ListGames(ref Span<byte> destination)
        {
            lock (_gameLock)
            {
                foreach (KeyValuePair<int, GamespyGame> p in _games)
                    p.Value.AppendEndPoint(ref destination);
            }
        }
    }
}
