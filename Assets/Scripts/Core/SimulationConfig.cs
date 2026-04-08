// SimulationConfig.cs - 全局配置常量
public static class SimulationConfig
{
    public const int CellMaxNum = 20;           // 每个环境格能容纳细胞的最大数
    public const int CellTotalMaxNum = 2000000; // 全局细胞总数最大值
    public const int GeneNum = 1000;            // 基因种类数上限
    public const int WorldSeed = 12345;         // 世界种子
    public const int EnvirSize = 1000;          // 环境格总大小 EnvirSize x EnvirSize
    public const float PixelPerEnvir = 1.0f;    // 每个环境格对应的世界单位大小

    // 物种基因槽位
    public const int MaxMainGene = 50;           // 主干基因槽位数
    public const int MaxSubGene = 100;           // 自由基因槽位数

    // 初始参数
    public const int InitialEnergy = 200;       // 细胞初始能量
    public const int InitialPlayerCells = 50;   // 初始玩家细胞数
    public const int InitialNPCClusters = 20;   // 初始NPC群落数
    public const int InitialNPCPerCluster = 30; // 每个NPC群落细胞数

    // 行为编号 (Behavior ID)
    public const int BehaviorMultiply = 1;
    public const int BehaviorTemperature = 2;
    public const int BehaviorLight = 3;
    public const int BehaviorDeath = 4;

    // 环境默认值
    public const int DefaultTemp = 27;          // 默认温度
    public const int DefaultLight = 50;         // 默认光照

    // 视野优化阈值
    public const int GridOptimizeThreshold = 10000; // 超过此数量的可见格子时进入优化渲染
}
