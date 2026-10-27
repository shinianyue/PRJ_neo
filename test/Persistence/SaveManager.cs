using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace test.Persistence;

public sealed class SaveManager
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _savePath;
    private readonly string _saveDirectory;

    public SaveManager(string savePath)
    {
        _savePath = savePath ?? throw new ArgumentNullException(nameof(savePath));
        _saveDirectory = Path.GetDirectoryName(_savePath)
            ?? throw new ArgumentException("存档路径必须包含目录。", nameof(savePath));
    }

    public const int SlotCount = 3;

    public SaveSlotInfo[] GetSlots()
    {
        var slots = new SaveSlotInfo[SlotCount];
        for (var slot = 1; slot <= SlotCount; slot++)
        {
            var slotPath = GetSlotPath(slot);
            var hasLegacySlot = slot == 1 && File.Exists(_savePath);
            if (!File.Exists(slotPath) && !hasLegacySlot)
            {
                slots[slot - 1] = new SaveSlotInfo(slot, null, null);
                continue;
            }

            try
            {
                slots[slot - 1] = new SaveSlotInfo(slot, LoadSlot(slot), null);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is InvalidDataException ||
                exception is UnauthorizedAccessException ||
                exception is JsonException)
            {
                slots[slot - 1] = new SaveSlotInfo(slot, null, exception.Message);
            }
        }

        return slots;
    }

    public SaveData LoadSlot(int slot)
    {
        ValidateSlot(slot);
        var path = GetSlotPath(slot);
        if (!File.Exists(path) && slot == 1 && File.Exists(_savePath))
        {
            path = _savePath;
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"存档槽位 {slot} 为空。", path);
        }

        var saveData = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path));
        if (saveData == null ||
            saveData.Version != CurrentVersion ||
            saveData.SceneIndex < 0 ||
            !float.IsFinite(saveData.PlayerX) ||
            !float.IsFinite(saveData.PlayerY) ||
            saveData.PlayerHealth < 0 ||
            saveData.PlayerHealth > 100 ||
            saveData.EnemyHealth < 0 ||
            saveData.EnemyHealths == null ||
            saveData.EnemyXs == null ||
            saveData.SceneEnemyHealths == null ||
            saveData.SceneEnemyXs == null ||
            saveData.EnemyHealths.Any(health => health < 0) ||
            saveData.SceneEnemyHealths.Any(scene => scene == null || scene.Any(health => health < 0)) ||
            saveData.SceneEnemyXs.Any(scene => scene == null))
        {
            throw new InvalidDataException($"存档格式无效或版本不受支持：{path}");
        }

        return saveData;
    }

    public void SaveSlot(int slot, SaveData saveData)
    {
        ValidateSlot(slot);
        ArgumentNullException.ThrowIfNull(saveData);
        Directory.CreateDirectory(_saveDirectory);
        saveData.Version = CurrentVersion;
        saveData.SavedAtUtc = DateTime.UtcNow;

        var path = GetSlotPath(slot);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(saveData, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    public void DeleteSlot(int slot)
    {
        ValidateSlot(slot);
        var path = GetSlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (slot == 1 && File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }
    }

    public GameSettings LoadSettings()
    {
        var path = Path.Combine(_saveDirectory, "settings.json");
        if (!File.Exists(path))
        {
            return new GameSettings();
        }

        var settings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path));
        if (settings == null ||
            settings.ResolutionIndex < 0 ||
            settings.ResolutionIndex > 2)
        {
            throw new InvalidDataException($"设置文件格式无效：{path}");
        }

        return settings;
    }

    public void SaveSettings(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(_saveDirectory);
        var path = Path.Combine(_saveDirectory, "settings.json");
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetSlotPath(int slot) =>
        Path.Combine(_saveDirectory, $"save-slot-{slot}.json");

    private static void ValidateSlot(int slot)
    {
        if (slot < 1 || slot > SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}
