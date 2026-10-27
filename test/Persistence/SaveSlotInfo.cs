namespace test.Persistence;

public sealed class SaveSlotInfo
{
    public int Slot { get; }
    public SaveData Data { get; }
    public string Error { get; }
    public bool HasSave => Data != null;
    public bool IsCorrupt => Error != null;

    public SaveSlotInfo(int slot, SaveData data, string error)
    {
        Slot = slot;
        Data = data;
        Error = error;
    }
}
