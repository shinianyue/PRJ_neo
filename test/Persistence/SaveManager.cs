using System;
using System.IO;
using System.Text.Json;

namespace test.Persistence;

public sealed class SaveManager
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _savePath;

    public SaveManager(string savePath)
    {
        _savePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
    }

    public bool HasSave => File.Exists(_savePath);

    public SaveData Load()
    {
        var saveData = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(_savePath));
        if (saveData == null ||
            saveData.Version != CurrentVersion ||
            saveData.SceneIndex < 0 ||
            !float.IsFinite(saveData.PlayerX) ||
            !float.IsFinite(saveData.PlayerY) ||
            saveData.PlayerHealth < 0)
        {
            throw new InvalidDataException($"存档格式无效或版本不受支持：{_savePath}");
        }

        return saveData;
    }

    public void Save(SaveData saveData)
    {
        ArgumentNullException.ThrowIfNull(saveData);

        var directory = Path.GetDirectoryName(_savePath)
            ?? throw new InvalidOperationException("无法确定存档目录。");
        Directory.CreateDirectory(directory);
        saveData.Version = CurrentVersion;
        saveData.SavedAtUtc = DateTime.UtcNow;

        var temporaryPath = _savePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(saveData, JsonOptions));
        File.Move(temporaryPath, _savePath, overwrite: true);
    }
}
