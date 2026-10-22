# 项目说明

这是一个使用 MonoGame 和 C# 编写的 Windows 2D 横版动作游戏原型。游戏当前包含主菜单、双场景切换、跟随镜头、玩家移动与跳跃、敌人 AI，以及可配置的武器连招。

## 运行与操作

项目文件：[`test/test.csproj`](./test/test.csproj)
入口：[`test/Program.cs`](./test/Program.cs)

在仓库根目录构建和运行：

```powershell
dotnet build .\test\test.csproj
dotnet run --project .\test\test.csproj
```

| 操作 | 按键 |
| --- | --- |
| 移动 | A/D 或左右方向键 |
| 跑步 | 按住 Shift |
| 跳跃 | Space、W 或上方向键 |
| 攻击/连招 | J |
| 与 NPC 对话 | 靠近 NPC 后按 E |
| 弹反 | 按住 Ctrl |
| 返回主菜单 | 游戏中按 Esc |

主菜单支持鼠标点击，也可以用上下方向键或 W/S 选择、Enter/空格确认。设置页可以选择 1280×720、1600×900 或 1920×1080，然后选择“应用”。菜单的“操作说明”页也列出了游戏操作。

## 代码结构

```text
test/
├── Game1.cs                    # MonoGame 生命周期、游戏状态和系统装配
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
│   └── Camera2D.cs              # 摄像机跟随及背景视差变换
├── Entities/
│   ├── Player.cs                # 玩家移动、跳跃、动画、生命值和冲刺
│   └── Enemy.cs                 # 敌人生命值、追击和近战攻击
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

1. `LoadContent` 从 MonoGame 内容管线载入角色、武器、场景和菜单资源，并创建玩家、武器、摄像机和场景。
2. 游戏状态在主菜单与游戏中切换。菜单动作由 `MainMenu.Update` 返回，`Game1` 决定开始游戏、应用分辨率或退出。
3. 游戏状态下，每帧依次更新玩家、武器、当前场景敌人和碰撞，再更新摄像机。
4. `Draw` 分批绘制视差背景、世界对象和固定在屏幕上的生命条。世界对象使用摄像机变换，UI 不使用变换。

按 `J` 后，`Weapon` 从 `WeaponDefinition` 的招式列表中选择当前招式。招式的攻击动画和实际命中检查使用相同的武器姿态，以保持视觉效果与判定一致。

### 玩家和场景

- [`Player.cs`](./test/Entities/Player.cs) 负责水平移动加速度/减速度、跳跃重力、平台落地、角色朝向、动画帧、生命值和招式触发的冲刺。
- [`Scene.cs`](./test/World/Scene.cs) 保存场景边界、出生点、平台、障碍物和敌人，并负责绘制这些内容。
- 当前场景由 `Game1` 中的场景数组和活动场景索引管理。第一场景走到右边缘进入第二场景，第二场景走到左边缘返回第一场景。
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

在 [`Game1.cs`](./test/Game1.cs) 的 `LoadContent` 中创建 `WeaponDefinition`：

- `Texture`：武器贴图。
- `GripOrigin`：武器贴图中刀柄/握持点的像素坐标，也是旋转轴。
- `IdleAngle`：未攻击时的角度。
- `Attacks`：按顺序执行的 `AttackDefinition` 列表。
- `ComboResetDelay`：两次攻击输入间隔超过此值后，连招回到第一段。

`Weapon` 会根据武器贴图读取透明度数据，用于像素级命中。为使用另一张贴图，可先将图片登记到 `Content.mgcb` 并加载，再构造新的 `WeaponDefinition`。

### 添加或调整一个招式

每个 [`AttackDefinition.cs`](./test/Combat/AttackDefinition.cs) 配置包含：

- `InputKey`：启动招式的按键。当前连招使用同一个按键，因此各段的按键应保持一致。
- `Damage`：命中伤害。
- `Duration`、`Cooldown`：招式持续时间及下一次攻击允许启动前的冷却。
- `ActiveStart`、`ActiveEnd`：从招式开始计时的有效命中区间。
- `StartAngle`、`EndAngle`：挥击姿态的起止角度，单位为弧度。
- `DashDistance`：冲刺招式的移动距离；设为 0 则不冲刺。
- `HitRule`：用于判定目标是否被武器命中。

当前 `Game1` 配置了三段 J 连招：

1. 挥砍：30 点伤害，透明像素判定。
2. 刺击：35 点伤害，旋转后的贴图外接矩形判定。
3. 冲刺攻击：50 点伤害，向角色朝向冲刺 150 像素，透明像素判定。

连招索引在每次有效按键时前进，到列表末尾后循环；若超过 `ComboResetDelay` 没有继续输入，下一击重新从第一段开始。调整现有招式时，修改 `Game1.cs` 中 `swordAttacks` 的相应参数；在数组中添加配置即可增加连招段数。

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

主菜单提供“继续”和“新游戏”。继续游戏会恢复当前场景、玩家位置与生命值，以及第二场景敌人的生命值和位置；新游戏会重置角色和敌人，并覆盖现有存档。游戏开始、剧情结束、场景切换和返回主菜单时会自动保存；游戏中按 `F5` 可手动保存。存档包含版本号，读取时会校验数据；存档缺失、损坏或无法读取时，错误会显示在菜单状态提示区域。

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
