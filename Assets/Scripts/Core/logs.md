# 修改日志

## 2026-04-04 初始实现

### 新建文件
1. `Core/SimulationConfig.cs` - 全局配置常量（世界大小、基因数量、行为编号等）
2. `Entities/Gene.cs` - 基因数据类（id + 能量消耗）
3. `Entities/Cell.cs` - 细胞数据类（位置、能量、优先级、主干/自由基因列表）
4. `Entities/Envir.cs` - 环境格数据类（温度、光照、细胞容量、增删细胞方法）
5. `Entities/Species.cs` - 物种初始化（玩家细胞主干基因 + NPC细胞自由基因）
6. `Core/BehaviorSystem.cs` - 行为系统（繁殖pre/apply、温度检查、光照检查、死亡检查、能量消耗）
7. `Managers/CameraController.cs` - 相机控制器（WASD+鼠标平移、滚轮缩放、视野范围计算）
8. `Managers/CellRenderer.cs` - 细胞渲染器（GPU Instancing圆形渲染 + 网格线 + 优化模式）

### 重写文件
- `Core/SimulationCore.cs` - 从示例代码重写为完整的后台线程模拟引擎
- `Core/MainManager.cs` - 从示例代码重写为游戏主管理器 + UI显示

### 架构保持
- 保留原有的"计算线程 + Unity渲染线程"分离架构
- 保留扳机(Trigger)系统的设计思想：基因位于特定扳机位置，行为分pre/apply两阶段
- 保留优先级排序机制

### 修复
- 将 `架构.cs` 重命名为 `架构.cs.bak`，该文件是旧草稿，包含重复类定义和语法错误，导致Unity整个项目无法编译

## 架构重写 - 行为系统命名空间化

### 原因
旧的 `BehaviorSystem.cs` 使用单体类 + if链判断基因ID，违反了核心架构设计。
正确架构为：每个行为独立命名空间，使用 `Dictionary<(int,int), Action<Cell>>` + `HashSet<int>[]` 实现O(1)基因查找和函数指针调用。

### 删除文件
- `Core/BehaviorSystem.cs` - 旧的单体行为系统

### 新建文件
1. `Core/MultiplyBehavior.cs` - `namespace Multiply`：繁殖行为（扳机1, 基因1）
2. `Core/TemperatureBehavior.cs` - `namespace Temperature`：温度增益行为（扳机2, 基因2）
3. `Core/LightBehavior.cs` - `namespace Light`：光照光合作用行为（扳机3, 基因3）
4. `Core/DeathBehavior.cs` - `namespace Death`：致死判定行为（扳机2-5, 基因2-5）

### 修改文件
- `Core/SimulationCore.cs`：
  - 移除 `public static BehaviorSystem Behaviors` 引用
  - `InitWorld()` 中添加四个命名空间的 `Init()` 调用
  - `SimulateOneStep()` 改为调用 `Multiply.Behavior.Pre/Apply()`、`Temperature.Behavior.Pre()`、`Light.Behavior.Pre()`、`Death.Behavior.Pre()`
  - 新增 `EnergyConsumption()` 私有方法（原在BehaviorSystem中，不符合扳机模式，直接内联）

### FuncList 映射对照
| 基因ID | 名称 | 所在行为命名空间 |
|--------|------|-----------------|
| 1 | 基础繁殖 | Multiply（扳机1） |
| 2 | 基础温度耐受 | Temperature（扳机2增益） + Death（扳机2致死） |
| 3 | 基础光照需求 | Light（扳机3产能） + Death（扳机3预留） |
| 4 | 基础寿命 | Death（扳机4, 5%死亡） |
| 5 | 拥挤 | Death（扳机5, >6邻居死亡） |
