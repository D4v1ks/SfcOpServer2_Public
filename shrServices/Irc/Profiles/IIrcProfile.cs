namespace shrServices
{
    public interface IIrcProfile
    {
        int Id { get; }

        string LocalIP { get; }
        string Nick { get; set; }
        string Name { get; set; }
        string User { get; set; }
        long Modes { get; set; }

        long LastTick { get; set; }
    }
}
