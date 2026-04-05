#pragma warning disable IDE0130

namespace SfcOpServer
{
    public sealed class PlaceHolder(int id) : IObject
    {
        public int Id { get; set; } = id;
    }
}
