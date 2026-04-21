# 修改日志

## 2026-04-22 高度视图细化与地形参数调优

### 修改文件

#### Core/MainManager.cs
- 右下角视图切换按钮正式扩展为 4 个，加入 `高度视图` 按钮。

#### Managers/CellRenderer.cs
- 高度视图底图改为按量化后的高度分层绘制，并在分层边界处压暗形成清晰分层线。
- 新增 `GetAltitudeQuantizedLevel`、`GetAltitudeContourColor`、`EvaluateAltitudeGradient`、`EvaluateAltitudeGradientSegment`，统一高度分层与着色逻辑。
- 高度视图颜色带按分层中心采样，保证颜色带与分层边界对齐。
- 视图切换时每次都会重绘背景，温度/光照叠加层不再覆盖高度视图。

#### Core/SimulationConfig.cs
- 世界种子调整为 `11111`。
- 海平面与沙滩阈值下调为 `AltitudeThreshold = 450`、`AltitudeBeach = 475`。
- 地形噪声参数继续调优为 `NoiseBaseScale = 0.00090`、`NoiseLayerCount = 10`、`NoiseFrequencyMultiplier = 1.8`、`NoiseWeightDecay = 0.7`。
- 高度视图补充并收敛为一组真实地形锚点色，`AltitudeContourLevels` 提高到 `50`，并提高 `AltitudeContourDarken` 以增强分层边界。

#### Core/TerrainGenerator.cs
- 高度生成正式从单套 fBm 迁移为“低频主形状 + 高频细节叠加”的多层噪声结构。
- 增加 `SampleTiledPerlin`，使高度图左右边界与上下边界环绕连续。

#### Future.md
- 将当前目标收敛为“增加高度视图，并由 `SimulationConfig` 中的颜色配置统一控制”。

## 2026-04-22 地图边界改为环绕连续

### 修改文件

#### Core/TerrainGenerator.cs
- 重写 `GeneratePerlinHeight` 的单层噪声采样方式，使每层噪声都按地图宽高做周期采样
- 左边界与右边界现在会在高度图上连续衔接，上边界与下边界也会连续衔接
- 保留原有“低频主形状 + 高频细节叠加”结构，只改变每层的采样方式，不改海陆阈值和地形分类逻辑

## 2026-04-22 提高高度视图颜色对比度

### 修改文件

#### Core/SimulationConfig.cs
- 调整高度视图锚点色，使整体高差映射更强烈：
  - 深水区进一步压暗，浅水区更亮更蓝
  - 海岸带更亮，低地和平原更饱和
  - 高地、丘陵、山地之间的暖色跨度加大
  - 山顶接近纯白，拉大与中高海拔的亮度落差
- `AltitudeContourDarken` 提高到更明显的边界强度，使分层线更清晰

## 2026-04-22 高度生成改为低频主形状叠加高频细节

### 修改文件

#### Core/SimulationConfig.cs
- **删除**: `NoiseScale`, `NoiseOctaves`, `NoisePersistence`, `NoiseLacunarity`
- **新增**: `NoiseBaseScale`, `NoiseLayerCount`, `NoiseFrequencyMultiplier`, `NoiseWeightDecay`
- 新噪声参数语义调整为：最低频层决定大陆级轮廓，后续层频率逐层提高、权重逐层减小，仅用于补充地形细节

#### Core/TerrainGenerator.cs
- **重写** `GeneratePerlinHeight`
- 高度图不再直接使用单套 fBm 参数采样，而是改为：
  - 先从最低频噪声层开始建立大尺度地形骨架
  - 再按“频率递增、影响递减”的方式连续叠加多层高频噪声
  - 每层使用独立偏移，减少不同层细节重复对齐
- 最终仍归一化到 `0-1000`，不改动后续 `AltitudeThreshold` / `AltitudeBeach` 的地形分类逻辑

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

---

## 2026-04-19 完善基因设计表

### 修改文件

#### Genelist.txt
- 将原始占位式基因清单重写为完整策划文档，扩展到 **45 个基因**。
- 保留并整理 1-10 号基础/原始代谢基因，补全 9 号“原始光合作用”和 10 号“硫化作用”。
- 新增 11-45 号基因，覆盖：
  - 原始厌氧代谢路线（发酵、摄食、甲烷生成、铁循环、固氮、反硝化）
  - 环境耐受路线（抗 H2S、抗酸、抗紫外、热/冷休克、生物膜加厚）
  - 向产氧光合作用过渡的前置基因（光系统前体、水裂解前体、膜褶雏形、色素协同）
  - 终盘基因节点（光合作用、活性氧清除 I/II、细胞色素电子传递链、氧化呼吸）
  - 若干可选强力取舍基因（高亲和氧捕获、快速复制酶、资源掠夺酶、休眠复苏开关、群落分工信号、钙壳沉积）
- 为每个基因补全了：效果、繁殖消耗、开启消耗、联动、冲突/代价。
- 新增 5 条推荐演化路线：原始厌氧摄食型、硫循环厌氧型、原始自养光能型、产氧光合型、高氧呼吸型。
- 在文末追加“当前项目与策划的差异提醒”，明确指出：
  - 当前 `Gene` 结构只有单一 `energyCost` 字段，尚未拆分繁殖消耗/开启消耗。
  - 当前 `Species` 仅初始化了 1-5 号基因。
  - 当前行为系统仍只有繁殖、温度、光照、死亡四类基础逻辑，高阶化学代谢尚未落地。

---

## 2026-04-19 修正 10 号基因“硫化作用”定义

### 修改文件

#### Genelist.txt
- 将 10 号基因从错误的“分解有机物并继续排 H2S 的异养/还原型定义”修正为“以 H2S 为电子供体、固定 CO2 合成有机物的化能自养型定义”。
- 修正后的效果为：每回合消耗 `1 单位 H2S + 1 单位 CO2`，生成 `1 单位有机物`，并排出 `1 单位硫沉积前体`。
- 联动说明同步调整为：
  - 与 14、23 构成硫化化能自养路线。
  - 与 7 共同构成“上游产 H2S、下游吃 H2S”的硫循环生态。
- 将推荐路线 B 名称从“硫循环厌氧型”调整为“硫循环化能型”，避免与新定义冲突。
- 将 39 号“氧化呼吸”的冲突描述中移除 10 号基因，因为修正后的 10 号基因不再代表严格厌氧路线。

---

## 2026-04-19 收敛化学物质命名

### 修改文件

#### Genelist.txt
- 按“只合并功能相近物质，不简化关键物质名”的原则调整了化学名词。
- 保留原名不变的关键物质：`CO2`、`H2O`、`H2`、`O2`、`CH4`、`H2S`、`N2`、`Fe2+/Fe3+`。
- 合并了两组前台更适合统一显示的物质：
  - `可用氮` = `NH3`、`NO3` 等可直接利用的含氮物。
  - `酸化物` = `SO2`、`H2SO4` 等会提升酸化压力的物质。
- 将文中所有“某某前体”表述改为更直白的名称：
  - `硫沉积前体` → `固硫物`
  - `有机物前体` → `少量有机物`
  - `光系统前体` → `原始光系统`
  - `水裂解前体` → `水裂解雏形`
- 与氮循环相关的基因名称和描述同步简化：
  - `氨同化` → `快速氮同化`
  - `硝酸盐同化` → `稳定氮同化`
  - `反硝化` → `脱氮呼吸`

---

## 2026-04-21 增加高度视图

### 修改文件

#### Core/SimulationConfig.cs
- 新增高度视图颜色常量：`AltitudeColorLow`、`AltitudeColorMid`、`AltitudeColorHigh`。
- 新增高度归一化范围：`AltitudeMin = 0`、`AltitudeMax = 1000`。

#### Managers/CellRenderer.cs
- `ViewMode` 枚举新增 `Altitude` 模式。
- `UpdateBackgroundTexture()` 新增高度着色逻辑：
  - 按 `Envir.Height` 在 0-1000 范围归一化。
  - 使用低海拔/中海拔/高海拔三段颜色插值绘制高度底图。
- 视图切换时不再只在首次初始化时更新底图，而是每次切换都重绘背景，以支持地形视图和高度视图之间切换。
- 温度/光照叠加层渲染条件收紧为仅在这两种模式下启用，高度视图不使用叠加层。

#### Core/MainManager.cs
- 右下角视图切换按钮从 3 个扩展为 4 个。
- 新增按钮：`高度视图`。

---

## 2026-04-21 提高高度视图对比度

### 修改文件

#### Core/SimulationConfig.cs
- 调整高度视图三段颜色常量，使高度分层更明显：
  - `AltitudeColorLow` 改为更深的蓝色
  - `AltitudeColorMid` 改为更亮的黄绿色/土黄色过渡
  - `AltitudeColorHigh` 改为更接近纯白的高山颜色
- 本次仅修改颜色配置，不改动高度视图渲染逻辑。

---

## 2026-04-21 高度视图改为等高分层图风格

### 修改文件

#### Core/SimulationConfig.cs
- 将高度视图颜色从 3 段渐变改为 6 段离散色带：深低地、浅低地、平原、高原、山地、高峰。
- 新增 `AltitudeContourLevels = 6`，用于控制高度分层数量。

#### Managers/CellRenderer.cs
- 将高度视图着色逻辑从连续渐变改为按高度区间分层取色。
- 高度视图现在表现为离散色带，更接近等高分层图/地形分层图风格。

---

## 2026-04-21 高度分层扩展到12段

### 修改文件

#### Core/SimulationConfig.cs
- 将高度视图色带从 6 段扩展到 12 段，提供更细的高程分层。
- `AltitudeContourLevels` 从 `6` 调整为 `12`。
- 新增 `AltitudeColorLevel7` 到 `AltitudeColorLevel12`，并重配全部 12 段颜色，使其从深蓝低地逐步过渡到白色高峰。

#### Managers/CellRenderer.cs
- `switch(level)` 的高度着色分支从 6 档扩展到 12 档。
- 现在高度图的分层更细，更接近密集色带的等高分层图效果。

---

## 2026-04-21 高度图改为20段真实地形图风格

### 修改文件

#### Core/SimulationConfig.cs
- 将高度视图配置从 12 个硬编码分层色改为 8 个真实地形图锚点色：深水、浅水、海岸平原、低地、高地、山前坡、山地、雪顶。
- `AltitudeContourLevels` 从 `12` 调整为 `20`。

#### Managers/CellRenderer.cs
- 高度图渲染从“12 档 switch 取固定色”改为“20 段量化 + 分段插值”。
- 新增 `GetAltitudeContourColor(float altitudeT)`，先把高度量化为 20 段，再按真实地形颜色锚点插值取色。
- 视觉效果从单纯分层色带调整为更接近真实地形图的蓝-绿-黄-棕-灰-白序列。

---

## 2026-04-21 强化海岸带与伪等高线

### 修改文件

#### Core/SimulationConfig.cs
- 新增 `AltitudeColorCoast`，用于单独强化海平面附近的海岸带显示。
- 新增 `AltitudeCoastBandWidth = 24`，控制海岸带宽度。
- 新增 `AltitudeContourDarken = 0.18f`，控制伪等高线压暗程度。

#### Managers/CellRenderer.cs
- 新增 `GetAltitudeQuantizedLevel(int height)`，统一高度分层计算。
- 将 `GetAltitudeContourColor` 改为直接基于高度值取色。
- 海平面以下全部改为分级蓝色显示，并限制在水下蓝色序列中，不再混入陆地颜色。
- 海平面以上单独增加海岸带高亮，再进入陆地真实地形色带。
- 在高度分层边界处比较上下左右相邻格分层编号，并对边界像素做压暗处理，形成伪等高线效果。
