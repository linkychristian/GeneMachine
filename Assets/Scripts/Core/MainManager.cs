// MainManager.cs - Unity主管理器，负责启动模拟和UI显示
using UnityEngine;

public class MainManager : MonoBehaviour
{
    [Header("模拟设置")]
    [Range(1, 10)] public int simulationSpeed = 1;  // 1x=1步/秒, 10x=10步/秒

    private bool showUI = true;

    void Start()
    {
        // 启动独立计算线程
        SimulationCore.SetSpeedMultiplier(simulationSpeed);
        SimulationCore.StartCalculationThread();
        Debug.Log("基因自动机已启动！计算线程独立运行中...");
    }

    void Update()
    {
        // 快捷键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (SimulationCore.IsPaused())
                SimulationCore.ResumeSimulation();
            else
                SimulationCore.PauseSimulation();
        }

        // 切换UI显示
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showUI = !showUI;
        }
    }

    void OnDestroy()
    {
        SimulationCore.StopCalculation();
        Debug.Log("模拟已停止");
    }

    void OnGUI()
    {
        if (!showUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float y = 10;
        float lineH = 22;

        string pauseText = SimulationCore.IsPaused() ? " [已暂停]" : "";

        string[] lines = new string[]
        {
            string.Format("基因自动机{0}", pauseText),
            string.Format("计算频率: {0} 步/秒 | 渲染帧率: {1:F0} FPS", SimulationCore.stepsPerSecond, 1f / Time.deltaTime),
            string.Format("存活细胞: {0} | 总步数: {1}", SimulationCore.aliveCellCount, SimulationCore.totalSteps),
            string.Format("模拟速度: {0}x", SimulationCore.speedMultiplier),
            "",
            "操作: 空格=暂停/继续  WASD=移动  滚轮=缩放  右键拖拽=平移  F1=隐藏UI"
        };

        for (int i = 0; i < lines.Length; i++)
        {
            // 文字阴影
            GUI.Label(new Rect(11, y + 1, 600, lineH), lines[i], shadowStyle);
            GUI.Label(new Rect(10, y, 600, lineH), lines[i], style);
            y += lineH;
        }

        DrawSpeedControl(style, shadowStyle);
        DrawViewModeUI();
    }

    void DrawSpeedControl(GUIStyle style, GUIStyle shadowStyle)
    {
        float panelWidth = 250f;
        float panelHeight = 92f;
        float x = Screen.width - panelWidth - 16f;
        float y = 16f;

        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");

        GUI.Label(new Rect(x + 13, y + 11, panelWidth - 20, 22), "游戏速度", shadowStyle);
        GUI.Label(new Rect(x + 12, y + 10, panelWidth - 20, 22), "游戏速度", style);

        int currentSpeed = SimulationCore.speedMultiplier;
        float sliderValue = GUI.HorizontalSlider(new Rect(x + 12, y + 42, panelWidth - 24, 20), currentSpeed, 1f, 10f);
        int newSpeed = Mathf.RoundToInt(sliderValue);
        if (newSpeed != currentSpeed)
        {
            SimulationCore.SetSpeedMultiplier(newSpeed);
        }

        string speedText = string.Format("{0}x  ({1:F1}秒/步)", SimulationCore.speedMultiplier, 1f / SimulationCore.speedMultiplier);
        GUI.Label(new Rect(x + 13, y + 60, panelWidth - 20, 22), speedText, shadowStyle);
        GUI.Label(new Rect(x + 12, y + 59, panelWidth - 20, 22), speedText, style);
    }

    void DrawViewModeUI()
    {
        float btnWidth = 100f;
        float btnHeight = 28f;
        float spacing = 4f;
        float totalHeight = btnHeight * 3 + spacing * 2;
        float x = Screen.width - btnWidth - 16f;
        float y = Screen.height - totalHeight - 16f;

        string[] labels = { "地形视图", "温度视图", "光照视图" };
        Color origBg = GUI.backgroundColor;

        for (int i = 0; i < 3; i++)
        {
            bool selected = (int)CellRenderer.currentViewMode == i;
            GUI.backgroundColor = selected ? new Color(0.3f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), labels[i]))
            {
                CellRenderer.currentViewMode = (CellRenderer.ViewMode)i;
            }
            y += btnHeight + spacing;
        }

        GUI.backgroundColor = origBg;
    }
}
