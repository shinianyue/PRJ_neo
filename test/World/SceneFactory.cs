using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using test.Configuration;
using test.Entities;
using test.Story;

namespace test.World;

public sealed class SceneFactory
{
    private const int PlayerHeight = 58;
    private readonly GameConfiguration _configuration;
    private readonly Texture2D _wallTexture;
    private readonly Texture2D _obstacleTexture;
    private readonly Texture2D _npcTexture;

    public SceneFactory(
        GameConfiguration configuration,
        Texture2D wallTexture,
        Texture2D obstacleTexture,
        Texture2D npcTexture)
    {
        _configuration = configuration;
        _wallTexture = wallTexture;
        _obstacleTexture = obstacleTexture;
        _npcTexture = npcTexture;
    }

    public IReadOnlyList<Scene> Build(Viewport viewport)
    {
        return _configuration.Scenes
            .Select(scene => BuildScene(scene, viewport))
            .ToArray();
    }

    private Scene BuildScene(SceneConfiguration configuration, Viewport viewport)
    {
        var sceneWidth = (int)(viewport.Width * configuration.WidthInViewports);
        var bounds = new Rectangle(0, 0, sceneWidth, viewport.Height);
        var ground = new Rectangle(
            bounds.Left,
            bounds.Bottom - _wallTexture.Height,
            bounds.Width,
            _wallTexture.Height);

        var platforms = new List<Rectangle>(configuration.Platforms.Count + 1)
        {
            ground
        };
        platforms.AddRange(configuration.Platforms.Select(platform => new Rectangle(
            (int)(platform.XRatio * viewport.Width),
            (int)(platform.YRatio * viewport.Height),
            (int)(platform.WidthRatio * viewport.Width),
            _wallTexture.Height)));

        var obstacles = configuration.Obstacles.Select(obstacle => new Rectangle(
            (int)(obstacle.XRatio * viewport.Width),
            ground.Top - _obstacleTexture.Height,
            _obstacleTexture.Width,
            _obstacleTexture.Height)).ToArray();

        var landmarks = configuration.Landmarks.Select(landmark => new Rectangle(
            (int)(landmark.XRatio * viewport.Width),
            ground.Top - landmark.Height,
            landmark.Width,
            landmark.Height)).ToArray();

        var enemies = configuration.Enemies.Select(spawn =>
        {
            var enemyConfiguration = _configuration.EnemyArchetypes[spawn.Archetype];
            var boundsForEnemy = new Rectangle(
                (int)(spawn.XRatio * viewport.Width),
                ground.Top - enemyConfiguration.Height,
                enemyConfiguration.Width,
                enemyConfiguration.Height);
            return _configuration.CreateEnemy(spawn.Archetype, boundsForEnemy);
        }).ToArray();

        DialogueNpc npc = null;
        if (configuration.Npc != null)
        {
            npc = new DialogueNpc(
                configuration.Npc.Name,
                new Rectangle(
                    (int)(configuration.Npc.XRatio * viewport.Width),
                    ground.Top - PlayerHeight,
                    _npcTexture.Width,
                    PlayerHeight),
                configuration.Npc.SequenceId);
            npc.SetActive(configuration.Npc.Active);
        }

        return new Scene(
            bounds,
            new Vector2(
                configuration.PlayerStartXRatio * viewport.Width,
                ground.Top - PlayerHeight),
            platforms,
            obstacles,
            landmarks,
            enemies,
            npc);
    }
}
