using System.Threading;

namespace shrServices
{
    public sealed class GamespyUser(string email, string nick, string password)
    {
        private static int _lastId;

        public int Id { get; } = Interlocked.Increment(ref _lastId);
        public string Email { get; } = email;
        public string Nick { get; } = nick;
        public string Password { get; } = password;
        public string Username { get; } = $"{nick}@{email}";
    }
}
