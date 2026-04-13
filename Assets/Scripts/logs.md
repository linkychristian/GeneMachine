# 修改日志

## 2026-04-13 地形生成 + 视图切换

### 新增文件
- **Core/TerrainGenerator.cs** — 地形生成器（静态类）
  - `GeneratePerlinHeight`: 6八度柏林噪声生成 1000×1000 高度图（0-1000）
  - `HydraulicErosion`: 粒子水力侵蚀（70000次迭代），自然雕刻河谷地形
  - `FlowAccumulation`: D8方向流量累积算法，流量 ≥ 阈值的陆地格标记为河流
  - `Generate`: 整合上述三步，根据 AltitudeThreshold/AltitudeBeach 分类地形（海洋/沙滩/陆地/河流），写入 Envir.Height 和 Envir.Topography

### 修改文件

#### Core/SimulationConfig.cs
- 添加 `using UnityEngine;`
- 新增地形生成参数：NoiseScale, NoiseOctaves, NoisePersistence, NoiseLacunarity, ErosionIterations, ErosionMaxLifetime, RiverFlowThreshold
- 新增地形视图颜色常量：TerrainColorOcean(深蓝), TerrainColorLand(棕色), TerrainColorBeach(淡黄), TerrainColorRiver(浅蓝)
- 新增温度视图颜色常量：TempColorCold(蓝), TempColorHot(红)
- 新增光照视图颜色常量：LightColorDark(黑), LightColorBright(白)
- 新增热力图归一化范围：TempMin/TempMax, LightMin/LightMax

#### Core/SimulationCore.cs — InitWorld()
- 在 Envir 创建循环之后调用 `TerrainGenerator.Generate()`
- 玩家出生点：从地图中心螺旋搜索最近陆地
- 玩家细胞生成：跳过海洋格（Topography==0）
- NPC 群落生成：随机选点时重试直到找到陆地，个体也跳过海洋格

#### Managers/CellRenderer.cs
- 新增 `ViewMode` 枚举（Terrain/Temperature/Light）及 `currentViewMode` 静态字段
- 新增背景渲染资源：1000×1000 Texture2D（Point过滤）、Material、覆盖全世界的 Mesh（z=1，在细胞z=0之后）
- `CreateBackground()`: 创建背景纹理/材质/网格
- `UpdateBackgroundTexture()`: 根据当前视图模式填充像素（地形=按Topography着色, 温度=蓝红插值, 光照=黑白插值）
- `LateUpdate()` 开头：检测视图模式变更时重建纹理，每帧 DrawMesh 渲染背景

#### Core/MainManager.cs — OnGUI()
- 添加 `DrawViewModeUI()` 调用
- `DrawViewModeUI()`: 屏幕右下角绘制3个按钮（地形视图/温度视图/光照视图），选中按钮高亮显示，点击切换 CellRenderer.currentViewMode

### 未修改的文件
- Entities/Cell.cs, Gene.cs, Species.cs
- 所有 Behavior 文件
- CameraController.cs

---

## 2026-04-13 水系算法重写（水源寻径 + 湖泊）

### 修改文件

#### Core/TerrainGenerator.cs — 完全重写水系部分
- **删除**: `HydraulicErosion`(粒子侵蚀)、`SampleHeight`、`CalcGradient`、`FlowAccumulation`(D8流量累积)
- **新增**: `GenerateRiversAndLakes` — 水源寻径河流 + 湖泊生成
  - 随机选取 300 个陆地水源点（高于海拔阈值+20）
  - 每个水源沿最陡下降方向追踪路径，直到：到达海洋 / 汇入已有河流 / 汇入已有湖泊 → 正常终止
  - 若所有邻域都更高（极低点）→ BFS 填充湖泊，水位=极低点高度+汇入源数×LakeRisePerSource
  - 路径写入 flowCount，后续汇入同一湖泊的水源会抬升该湖泊水位并重新 BFS 扩展湖面
- **新增**: `FillLake` — BFS 洪水填充湖泊区域（高度≤水位的连通区标记为湖泊）
- **新增**: `PaintRiver` — 根据 flowCount 按半径涂河流宽度（汇入越多越粗）
- `Generate` 方法更新：调用新的 `GenerateRiversAndLakes`，Topography 增加 4=湖泊判定

#### Core/SimulationConfig.cs
- **删除**: ErosionIterations, ErosionMaxLifetime, RiverFlowThreshold
- **新增**: RiverSourceCount=300, RiverMaxLength=2000, RiverMinLength=15, RiverWidenPerFlow=3
- **新增**: LakeRisePerSource=8f
- **新增**: TerrainColorLake(深蓝偏暗)

#### Entities/Envir.cs
- Topography 注释更新：增加 4=湖泊

#### Managers/CellRenderer.cs
- UpdateBackgroundTexture: Topography switch 新增 case 4 → TerrainColorLake

---

## 2026-04-13 湖泊溢出算法修复 + 河流初始宽度

### 修改文件

#### Core/TerrainGenerator.cs — 湖泊溢出机制完全重写
- **删除**: 旧的 `FillLake`（BFS 洪水填充，导致大量陆地被淹没）
- **重写** `GenerateRiversAndLakes`:
  - 新增 `RiverSource` 结构体（x, y, flow, fromLake）
  - 改为队列驱动：初始 300 个随机水源入队，湖泊溢出产生的新水源也入队继续追踪
  - 极低点处理：找溢出口（未访问邻域中最低点），创建湖泊后从溢出口生成新水源（继承流量）
  - 汇入已有湖泊时：从该湖泊溢出口生成新水源继续向下
  - visited 数组改为代数标记（visitedGen + currentGen），避免每个水源分配 bool 数组
- **新增**: `FillLakeBasin` — 仅填充高度 < 溢出口高度的连通区域（面积受地形约束，不会无限扩展）

#### Core/SimulationConfig.cs
- **删除**: LakeRisePerSource
- **新增**: RiverBaseWidth=2（初始河流宽度 2 格）
- 河流宽度公式改为: `RiverBaseWidth + flowCount / RiverWidenPerFlow`

---

## 2026-04-13 地图扩大 + 河流禁用 + 沙滩阈值调整

### 修改文件

#### Core/SimulationConfig.cs
- `EnvirSize`: 1000 → **2000**
- `AltitudeBeach`: 550 → **525**（海平面 500 + 25）

#### Core/TerrainGenerator.cs
- `GenerateRiversAndLakes` 调用已注释掉（`// TODO: 河流生成暂时禁用`）

---

## 2026-04-13 温度/光照视图改为叠加层

### 修改文件

#### Managers/CellRenderer.cs
- 地形底图始终渲染（不再根据视图模式切换底图内容）
- `UpdateBackgroundTexture()` 简化：只绘制地形颜色
- **新增** 叠加层系统：
  - `overlayTexture`: Bilinear 过滤的 Texture2D（虚化效果）
  - `overlayMesh`: z=0.5（在地形 z=1 和细胞 z=0 之间）
  - `CreateOverlay()`: 创建叠加层资源
  - `UpdateOverlayTexture()`: 根据温度/光照模式填充半透明像素（alpha=OverlayAlpha）
  - `LateUpdate()`: 温度/光照模式时额外渲染叠加层

#### Core/SimulationConfig.cs
- **新增**: `OverlayAlpha = 0.55f`（叠加层透明度）
