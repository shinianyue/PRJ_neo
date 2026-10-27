namespace test.Persistence;

public sealed class GameSettings
{
    public int ResolutionIndex { get; set; }
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
    public bool ReduceMotion { get; set; }

    public GameSettings Copy() => new()
    {
        ResolutionIndex = ResolutionIndex,
        Fullscreen = Fullscreen,
        VSync = VSync,
        ReduceMotion = ReduceMotion
    };
}
