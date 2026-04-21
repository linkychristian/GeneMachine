// SimulationConfig.cs - 全局配置常量
using UnityEngine;

public static class SimulationConfig
{
    public const int CellMaxNum = 20;           // 每个环境格能容纳细胞的最大数
    public const int CellTotalMaxNum = 2000000; // 全局细胞总数最大值
    public const int GeneNum = 1000;            // 基因种类数上限
    public const int WorldSeed = 11111;         // 世界种子
    public const int EnvirSize = 2000;          // 环境格总大小 EnvirSize x EnvirSize
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
    public const int AltitudeThreshold = 450;          // 海拔阈值，低于此高度为水域

    public const int AltitudeBeach = 475; //若非海，且低于沙滩高度(海平面+25)，则地形为沙滩。

    // 视野优化阈值
    public const int GridOptimizeThreshold = 10000; // 超过此数量的可见格子时进入优化渲染

    // 地形生成
        public const float NoiseBaseScale = 0.00090f;       // 最低频底形噪声，决定大陆级轮廓
        public const int NoiseLayerCount = 10;               // 低频到高频共叠加几层
        public const float NoiseFrequencyMultiplier = 1.8f; // 每层频率递增倍数
        public const float NoiseWeightDecay = 0.7f;         // 每层权重递减系数

    // 河流生成
    public const int RiverSourceCount = 300;       // 随机水源数量
    public const int RiverMaxLength = 2000;        // 单条河流最大追踪步数
    public const int RiverMinLength = 15;          // 路径太短不算河流
    public const int RiverBaseWidth = 2;           // 初始河流宽度（格）
    public const int RiverWidenPerFlow = 3;        // 每多几条汇入增加1格宽度

    // 地形视图颜色
    public static readonly Color TerrainColorOcean = new Color(0.05f, 0.12f, 0.55f, 1f);
    public static readonly Color TerrainColorLand = new Color(0.55f, 0.38f, 0.18f, 1f);
    public static readonly Color TerrainColorBeach = new Color(0.93f, 0.87f, 0.58f, 1f);
    public static readonly Color TerrainColorRiver = new Color(0.30f, 0.60f, 0.90f, 1f);
    public static readonly Color TerrainColorLake = new Color(0.15f, 0.35f, 0.70f, 1f);

    // 温度视图颜色
    public static readonly Color TempColorCold = new Color(0.0f, 0.2f, 0.9f, 1f);
    public static readonly Color TempColorHot = new Color(0.9f, 0.1f, 0.0f, 1f);

    // 光照视图颜色
    public static readonly Color LightColorDark = new Color(0f, 0f, 0f, 1f);
    public static readonly Color LightColorBright = new Color(1f, 1f, 1f, 1f);

    // 高度视图颜色（真实地形图风格锚点色）
        public static readonly Color AltitudeColorDeepWater = new Color(0.01f, 0.07f, 0.28f, 1f);
        public static readonly Color AltitudeColorShallowWater = new Color(0.10f, 0.42f, 0.82f, 1f);
        public static readonly Color AltitudeColorCoast = new Color(0.97f, 0.88f, 0.56f, 1f);
        public static readonly Color AltitudeColorCoastalPlain = new Color(0.22f, 0.60f, 0.24f, 1f);
        public static readonly Color AltitudeColorLowland = new Color(0.46f, 0.78f, 0.20f, 1f);
        public static readonly Color AltitudeColorHighland = new Color(0.82f, 0.70f, 0.18f, 1f);
        public static readonly Color AltitudeColorUpland = new Color(0.73f, 0.46f, 0.18f, 1f);
        public static readonly Color AltitudeColorMountain = new Color(0.34f, 0.26f, 0.24f, 1f);
        public static readonly Color AltitudeColorSnow = new Color(0.99f, 0.99f, 0.99f, 1f);

    // 热力图归一化范围
    public const float TempMin = 15f;
    public const float TempMax = 35f;
    public const float LightMin = 0f;
    public const float LightMax = 100f;
    public const float AltitudeMin = 0f;
    public const float AltitudeMax = 1000f;
    public const int AltitudeContourLevels = 50;
    public const int AltitudeCoastBandWidth = 24;
    public const float AltitudeContourDarken = 0.28f;

    // 叠加层透明度（温度/光照视图覆盖在地形上方）
    public const float OverlayAlpha = 0.55f;
}
