using System;

namespace test.Persistence;

public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public int SceneIndex { get; set; }
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public int PlayerHealth { get; set; }
    public int EnemyHealth { get; set; }
    public int EnemyX { get; set; }
    public int[] EnemyHealths { get; set; } = System.Array.Empty<int>();
    public int[] EnemyXs { get; set; } = System.Array.Empty<int>();
    public int[][] SceneEnemyHealths { get; set; } = System.Array.Empty<int[]>();
    public int[][] SceneEnemyXs { get; set; } = System.Array.Empty<int[]>();
    public DateTime SavedAtUtc { get; set; }
}
