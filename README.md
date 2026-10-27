# 项目说明

这是一个使用 MonoGame 和 C# 编写的 Windows 2D 横版动作游戏原型。游戏当前包含主菜单、多场景切换、跟随镜头、玩家移动与跳跃、敌人 AI，以及可配置的武器连招。敌人、招式和关卡布置由外部 JSON 驱动，`Game1` 主要承担系统装配和游戏流程协调。

## 运行与操作

项目文件：[`test/test.csproj`](./test/test.csproj)
入口：[`test/Program.cs`](./test/Program.cs)

在仓库根目录构建和运行：

```powershell
dotnet build .\test\test.csproj
dotnet run --project .\test\test.csproj
```

| 操作        | 按键                |
| ----------- | ------------------- |
| 移动        | A/D 或左右方向键    |
| 跑步        | 按住 Shift          |
| 跳跃        | Space、W 或上方向键 |
| 攻击/连招   | J                   |
| 特殊技      | U                   |
| 与 NPC 对话 | 靠近 NPC 后按 E     |
| 弹反        | 点按 Ctrl 或 K      |
| 返回主菜单  | 游戏中按 Esc        |

主菜单支持鼠标点击，也可以用上下方向键或 W/S 选择、Enter/空格确认。设置页可以选择 1280×720、1600×900 或 1920×1080，然后选择“应用”。菜单的“操作说明”页也列出了游戏操作。
方向键支持短延迟的按住重复，页面切换动画也缩短；鼠标只在移动或点击时改变焦点，避免静止鼠标位置覆盖键盘选择。
“减少动态效果”设置会关闭菜单粒子、战斗火花和过场淡入淡出。

## 代码结构

```text
test/
├── Game1.cs                    # MonoGame 生命周期、游戏状态和系统装配
├── Configuration/
│   └── GameConfiguration.cs     # JSON 数据模型、校验和运行时定义装配
├── Data/
│   └── game.json                # 武器招式、敌人原型和场景布置
├── UI/
│   └── MainMenu.cs              # 主菜单、操作说明和分辨率设置
├── Persistence/
│   ├── SaveData.cs              # 存档数据结构
│   └── SaveManager.cs           # JSON 存档读写和校验
├── Story/
│   ├── StorySequence.cs         # 剧情序列数据结构
│   ├── StoryLine.cs             # 单条对白数据结构
│   └── StoryPlayer.cs           # 剧情载入、推进和对话框绘制
├── World/
│   ├── Scene.cs                 # 场景数据、平台/障碍物/敌人和背景绘制
│   ├── SceneFactory.cs          # 根据 JSON 和视口尺寸生成场景实体
│   └── Camera2D.cs              # 摄像机跟随及背景视差变换
├── Entities/
│   ├── Player.cs                # 玩家移动、跳跃、动画、生命值和冲刺
│   ├── Enemy.cs                 # 敌人运行时状态、生命值和近战攻击
│   ├── EnemyDefinition.cs       # 敌人运行时配置定义
│   └── EnemyAi.cs               # AI 接口、实现和选择工厂
└── Combat/
    ├── Weapon.cs                # 武器运行时状态、连招输入、绘制和命中
    ├── WeaponDefinition.cs      # 武器贴图、握持点、招式列表等静态配置
    ├── AttackDefinition.cs      # 单个招式的伤害、时序、动作和判定配置
    └── HitDetection/
        ├── IHitRule.cs          # 命中判定扩展接口
        ├── PixelPerfectHitRule.cs
        └── BoundsHitRule.cs
```

### 游戏初始化和主循环

[`Game1.cs`](./test/Game1.cs) 是各系统的组合入口：

1. `LoadContent` 载入图像资源与 `Data/game.json`，由配置装配器和 `SceneFactory` 创建武器、敌人和场景。
2. 游戏状态在主菜单与游戏中切换。菜单动作由 `MainMenu.Update` 返回，`Game1` 决定开始游戏、应用分辨率或退出。
3. 游戏状态下，每帧依次更新玩家、武器、当前场景敌人和碰撞，再更新摄像机。
4. `Draw` 分批绘制视差背景、世界对象和固定在屏幕上的生命条。世界对象使用摄像机变换，UI 不使用变换。

这种 glue 结构把“数据描述”与“行为实现”分开：JSON 描述内容，配置层负责校验并转换为运行时定义，场景工厂负责创建对象；`Game1` 只连接系统和处理游戏状态，不再内嵌敌人/招式/平台数值表。

按 `J` 后，`Weapon` 从 `WeaponDefinition` 的招式列表中选择当前招式。招式的攻击动画和实际命中检查使用相同的武器姿态，以保持视觉效果与判定一致。

### 玩家和场景

- [`Player.cs`](./test/Entities/Player.cs) 负责水平移动加速度/减速度、跳跃重力、平台落地、角色朝向、动画帧、生命值和招式触发的冲刺。
- [`Scene.cs`](./test/World/Scene.cs) 保存场景边界、出生点、平台、障碍物和敌人，并负责绘制这些内容。
- 场景数组由 `SceneFactory` 根据配置生成，`Game1` 只保留活动场景索引和相邻场景切换逻辑。各场景平台、障碍物、地标、敌人出生点和 NPC 配置均位于 `Data/game.json`。
- [`Camera2D.cs`](./test/World/Camera2D.cs) 水平跟随玩家，并把背景位移缩放到前景镜头位移的 35%，产生视差效果。摄像机移动范围限制在场景边界内。
- [`Enemy.cs`](./test/Entities/Enemy.cs) 是简单的近战敌人：玩家距离较远时追击，进入攻击距离后先显示攻击前摇，再尝试造成伤害。
- 第一场景的守门人会提供操作引导；第二场景敌人会持有武器并在攻击前摇时举起武器。

### 主菜单与分辨率

[`MainMenu.cs`](./test/UI/MainMenu.cs) 负责菜单页面、选择项及键鼠输入，并通过 `MenuAction` 把操作结果交给 `Game1`。分辨率列表目前在 `MainMenu.Resolutions` 中定义；选择并应用后，`Game1` 调用 `GraphicsDeviceManager.ApplyChanges`，再按新的视口尺寸重建平台和场景。

若要添加一种固定分辨率，在 `Resolutions` 数组中添加一个 `Point(width, height)` 即可。若需要允许任意尺寸，需同时修改该选择 UI 和分辨率应用逻辑。

### 背景和内容资源

图片、字体等资源由 [`Content.mgcb`](./test/Content/Content.mgcb) 交给 MonoGame Content Builder 编译。代码通过 `Content.Load<T>` 使用相对于内容根目录的资源名；例如，磁盘路径 `test/Content/images/Other_Images/Backgrounds/Underworld 3.png` 对应资源名 `images/Other_Images/Backgrounds/Underworld 3`。

增加图片或字体资源时：

1. 将文件放到 `test/Content/` 下合适的目录。
2. 在 `Content.mgcb` 添加相应的 importer、processor 和 build 项（可参考同类型资源已有配置）。
3. 使用 `Content.Load<Texture2D>` 或 `Content.Load<SpriteFont>` 加载编译后的资源。

当前关卡背景按图像纵横比缩放至场景高度，再横向平铺；背景绘制时使用 `Camera2D.BackgroundTransform`。

## 添加武器或招式

战斗实现按“武器运行时 + 武器配置 + 招式配置 + 判定策略”拆分，适合替换贴图和尝试不同招式。

### 调整现有武器

编辑 [`game.json`](./test/Data/game.json) 中的 `weapon` 对象来调整连招。每个 `attacks` 元素对应一个 `AttackDefinition`：

- `Texture`：武器贴图。
- `GripOrigin`：武器贴图中刀柄/握持点的像素坐标，也是旋转轴。
- `IdleAngle`：未攻击时的角度。
- `Attacks`：按顺序执行的 `AttackDefinition` 列表。
- `ComboResetDelay`：两次攻击输入间隔超过此值后，连招回到第一段。

`Weapon` 会根据武器贴图读取透明度数据，用于像素级命中。配置中的 `hitRule` 支持 `pixelPerfect` 和 `bounds`；`inputKey` 使用 MonoGame 的键名，例如 `J`、`U`。

### 添加或调整一个招式

每个 [`AttackDefinition.cs`](./test/Combat/AttackDefinition.cs) 配置包含：

- `InputKey`：启动招式的按键。当前连招使用同一个按键，因此各段的按键应保持一致。
- `Damage`：命中伤害。
- `Duration`、`Cooldown`：招式持续时间及下一次攻击允许启动前的冷却。
- `ActiveStart`、`ActiveEnd`：从招式开始计时的有效命中区间。
- `StartAngle`、`EndAngle`：挥击姿态的起止角度，单位为弧度。
- `DashDistance`：冲刺招式的移动距离；设为 0 则不冲刺。
- `HitRule`：用于判定目标是否被武器命中。

当前配置在 `game.json` 中定义五段 J 连招和两段 U 特殊技。各段伤害、时序、冲刺距离、判定类型与刀光颜色都由数据控制。

连招索引在每次有效按键时前进，到列表末尾后循环；若超过 `comboResetDelay` 没有继续输入，下一击重新从第一段开始。修改 JSON 中对应招式，或在 `attacks` 数组中添加元素，即可调整/增加招式。加载时会校验输入键、判定类型、颜色和招式数值；未知配置会以明确错误中止加载。

### 调整敌人和场景

- `enemyArchetypes` 定义敌人的风格、生命/架势、架势崩溃时长、移速、尺寸、颜色与 AI 名称。目前 AI 名称支持 `default` 和 `aggressive`；增加行为实现时，在 `EnemyAiFactory` 注册新的标识符。
- 每个敌人原型的 `attacks` 是按循环顺序使用的攻击策略列表。策略包含触发距离、伤害/架势伤害、前摇/冷却、武器握点/缩放/预备与挥击角度，以及矩形或圆形 hitbox 的尺寸和位置；长枪兵、迅捷剑士和祭坛守卫因此能使用不同攻击动作。
- 攻击前摇会按配置绘制 hitbox 预警，命中检测使用同一个几何定义；调 hitbox 时能直接看到预警形状和真实判定区域保持一致。`weaponKind` 目前用于区分占位武器的绘制色，武器握点、缩放和挥击姿态可独立调整。
- `scenes` 按顺序定义关卡。平台的 `xRatio`、`yRatio`、`widthRatio` 相对当前视口宽高；地面由工厂自动创建。障碍物、地标和敌人出生点用 `xRatio` 指定横向位置。
- 敌人出生点通过 `archetype` 引用原型，不需要在代码里构造 `Enemy`。NPC 可通过 `name`、`sequenceId`、`xRatio` 和 `active` 配置。
- 添加场景时，在 `scenes` 数组末尾添加配置即可；场景顺序同时决定向右前进时的连接顺序，存档使用场景索引恢复位置。

### 更换命中判定

[`IHitRule.cs`](./test/Combat/HitDetection/IHitRule.cs) 定义了可插拔判定接口：

- `PixelPerfectHitRule`：检查贴图中非透明像素经翻转、旋转和原点变换后是否覆盖目标矩形，适合需要贴合实际图形的招式。
- `BoundsHitRule`：计算旋转贴图的轴对齐外接矩形后检测相交，判定更宽松、计算更简单。

要尝试新的判定方式，可新增一个实现 `IHitRule` 的类，并在对应招式的 `hitRule` 参数中传入实例。接口参数包含贴图、预读取的像素数据、目标矩形和当前武器绘制姿态。

## 构建

在仓库根目录运行：

```powershell
dotnet build .\test\test.csproj
```

内容文件由构建任务调用 MGCB 处理。构建生成的 `bin/` 和 `obj/` 文件是派生输出，通常不需要手动修改。

## 存档管理

存档由 [`SaveManager.cs`](./test/Persistence/SaveManager.cs) 以 JSON 格式保存到：

```text
%LOCALAPPDATA%\ProjectSny\save.json
```

主菜单提供“继续”和“新游戏”。继续游戏会恢复当前场景、玩家位置与生命值，以及各场景敌人的生命值和位置；新游戏会重置角色和敌人，并覆盖现有存档。游戏开始、剧情结束、场景切换和返回主菜单时会自动保存；游戏中按 `F5` 可手动保存。存档包含版本号，读取时会校验数据；存档缺失、损坏或无法读取时，错误会显示在菜单状态提示区域。损坏的槽位可在新游戏页面确认覆盖，也可在存档页面按 Delete 确认删除。

需要调整存档字段时，修改 [`SaveData.cs`](./test/Persistence/SaveData.cs)，并同步更新 `Game1` 中保存/恢复这些字段的代码。修改存档结构后应递增 `SaveManager` 的版本号，或添加从旧版本迁移数据的逻辑，避免把旧结构误当作新格式。

## 剧情与对话

剧情数据放在 [`Content/Stories`](./test/Content/Stories/) 目录，每个 JSON 文件定义一个序列。项目会在启动时载入该目录中的所有 JSON，并要求序列 ID 唯一、至少有一条非空台词。第一场景的守门人会引导操作；靠近后按 E 对话。示例见 [`tutorial.json`](./test/Content/Stories/tutorial.json)：

```json
{
  "id": "tutorial",
  "lines": [
    { "speaker": "旁白", "text": "旅程开始了。" }
  ]
}
```

按 Enter 或空格推进对话，按 Esc 跳过。要在其他游戏事件中触发剧情，可在 [`Game1.cs`](./test/Game1.cs) 中调用 `_storyPlayer.Start("序列ID")`，并在剧情播放期间将游戏状态切换到 `Story`。台词会按对话框宽度自动换行。

剧情 JSON 会由项目文件复制到构建输出目录。添加或修改中文对白时，还需要在 [`Menu.spritefont`](./test/Content/Fonts/Menu.spritefont) 的 `CharacterRegions` 中加入所用汉字，否则字体不会包含对应字形；英文及常用 ASCII 字符已包含。
