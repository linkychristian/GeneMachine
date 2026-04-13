// CellRenderer.cs - 细胞渲染器，分批DrawMeshInstanced
using UnityEngine;
using System.Collections.Generic;

public class CellRenderer : MonoBehaviour
{
    private Mesh quadMesh;
    private Material playerMaterial;  // 玩家细胞材质(绿)
    private Material npcMaterial;     // NPC细胞材质(棕)
    private Texture2D circleTexture;
    private CameraController cameraController;

    // 颜色定义
    private static readonly Color PlayerColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    private static readonly Color NPCColor = new Color(0.8f, 0.5f, 0.2f, 1f);
    private static readonly Color GridLineColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    // 渲染批次
    private const int MAX_BATCH = 1023;
    private List<Matrix4x4> playerMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> npcMatrices = new List<Matrix4x4>();

    // 网格线
    private Material lineMaterial;

    // 缓存网格线数据
    private int cachedMinX, cachedMaxX, cachedMinY, cachedMaxY;
    private bool drawGrid = false;

    // 视图模式
    public enum ViewMode { Terrain = 0, Temperature = 1, Light = 2 }
    public static ViewMode currentViewMode = ViewMode.Terrain;
    private ViewMode lastRenderedMode = (ViewMode)(-1);

    // 背景渲染
    private Texture2D bgTexture;
    private Material bgMaterial;
    private Mesh bgMesh;

    // 叠加层（温度/光照热力图）
    private Texture2D overlayTexture;
    private Material overlayMaterial;
    private Mesh overlayMesh;
    private bool overlayDirty = true;

    void Start()
    {
        cameraController = FindObjectOfType<CameraController>();

        CreateCircleTexture();
        CreateQuadMesh();
        CreateMaterials();
        CreateLineMaterial();
        CreateBackground();
        CreateOverlay();
    }

    void CreateCircleTexture()
    {
        int size = 64;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius - 1f)
                    pixels[y * size + x] = Color.white;
                else if (dist <= radius)
                    pixels[y * size + x] = new Color(1, 1, 1, radius - dist);
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
    }

    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        quadMesh.RecalculateNormals();
    }

    void CreateMaterials()
    {
        Shader shader = Shader.Find("Sprites/Default");

        playerMaterial = new Material(shader);
        playerMaterial.mainTexture = circleTexture;
        playerMaterial.color = PlayerColor;

        npcMaterial = new Material(shader);
        npcMaterial.mainTexture = circleTexture;
        npcMaterial.color = NPCColor;
    }

    void CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    void CreateBackground()
    {
        int size = SimulationConfig.EnvirSize;
        bgTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        bgTexture.filterMode = FilterMode.Point;
        bgTexture.wrapMode = TextureWrapMode.Clamp;

        Shader shader = Shader.Find("Sprites/Default");
        bgMaterial = new Material(shader);
        bgMaterial.mainTexture = bgTexture;
        bgMaterial.color = Color.white;

        float worldSize = size * SimulationConfig.PixelPerEnvir;
        bgMesh = new Mesh();
        bgMesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 1),
            new Vector3(worldSize, 0, 1),
            new Vector3(worldSize, worldSize, 1),
            new Vector3(0, worldSize, 1)
        };
        bgMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        bgMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        bgMesh.RecalculateNormals();
    }

    void CreateOverlay()
    {
        int size = SimulationConfig.EnvirSize;
        overlayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        overlayTexture.filterMode = FilterMode.Bilinear; // 虚化效果
        overlayTexture.wrapMode = TextureWrapMode.Clamp;

        Shader shader = Shader.Find("Sprites/Default");
        overlayMaterial = new Material(shader);
        overlayMaterial.mainTexture = overlayTexture;
        overlayMaterial.color = Color.white;

        float worldSize = size * SimulationConfig.PixelPerEnvir;
        overlayMesh = new Mesh();
        overlayMesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0.5f),
            new Vector3(worldSize, 0, 0.5f),
            new Vector3(worldSize, worldSize, 0.5f),
            new Vector3(0, worldSize, 0.5f)
        };
        overlayMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        overlayMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        overlayMesh.RecalculateNormals();
    }

    void UpdateBackgroundTexture()
    {
        if (SimulationCore.EnvirData == null) return;
        int size = SimulationConfig.EnvirSize;
        Color32[] pixels = new Color32[size * size];

        // 始终绘制地形底图
        for (int y = 1; y <= size; y++)
        {
            for (int x = 1; x <= size; x++)
            {
                Color c;
                Envir env = SimulationCore.EnvirData[x, y];
                switch (env.Topography)
                {
                    case 0: c = SimulationConfig.TerrainColorOcean; break;
                    case 2: c = SimulationConfig.TerrainColorBeach; break;
                    case 3: c = SimulationConfig.TerrainColorRiver; break;
                    case 4: c = SimulationConfig.TerrainColorLake; break;
                    default: c = SimulationConfig.TerrainColorLand; break;
                }
                pixels[(y - 1) * size + (x - 1)] = c;
            }
        }

        bgTexture.SetPixels32(pixels);
        bgTexture.Apply();
    }

    void UpdateOverlayTexture()
    {
        if (SimulationCore.EnvirData == null) return;
        int size = SimulationConfig.EnvirSize;
        Color32[] pixels = new Color32[size * size];
        float overlayAlpha = SimulationConfig.OverlayAlpha;

        for (int y = 1; y <= size; y++)
        {
            for (int x = 1; x <= size; x++)
            {
                Envir env = SimulationCore.EnvirData[x, y];
                Color c;

                if (currentViewMode == ViewMode.Temperature)
                {
                    float t = Mathf.InverseLerp(SimulationConfig.TempMin, SimulationConfig.TempMax, env.Temp);
                    c = Color.Lerp(SimulationConfig.TempColorCold, SimulationConfig.TempColorHot, t);
                }
                else
                {
                    float l = Mathf.InverseLerp(SimulationConfig.LightMin, SimulationConfig.LightMax, env.Light);
                    c = Color.Lerp(SimulationConfig.LightColorDark, SimulationConfig.LightColorBright, l);
                }

                c.a = overlayAlpha;
                pixels[(y - 1) * size + (x - 1)] = c;
            }
        }

        overlayTexture.SetPixels32(pixels);
        overlayTexture.Apply();
        overlayDirty = false;
    }

    void LateUpdate()
    {
        if (SimulationCore.EnvirData == null || cameraController == null) return;

        if (currentViewMode != lastRenderedMode)
        {
            if (lastRenderedMode == (ViewMode)(-1))
                UpdateBackgroundTexture(); // 首次初始化地形底图
            overlayDirty = true;
            lastRenderedMode = currentViewMode;
        }

        // 始终渲染地形底图
        if (bgMesh != null && bgMaterial != null)
            Graphics.DrawMesh(bgMesh, Matrix4x4.identity, bgMaterial, 0);

        // 温度/光照模式：渲染半透明叠加层
        if (currentViewMode != ViewMode.Terrain)
        {
            if (overlayDirty)
                UpdateOverlayTexture();
            if (overlayMesh != null && overlayMaterial != null)
                Graphics.DrawMesh(overlayMesh, Matrix4x4.identity, overlayMaterial, 0);
        }

        int minX, maxX, minY, maxY;
        cameraController.GetVisibleGridRange(out minX, out maxX, out minY, out maxY);

        int visibleGrids = (maxX - minX + 1) * (maxY - minY + 1);
        bool optimizedMode = visibleGrids > SimulationConfig.GridOptimizeThreshold;

        RenderCells(minX, maxX, minY, maxY, optimizedMode);

        // 缓存网格线范围供OnRenderObject使用
        drawGrid = visibleGrids < 2500;
        cachedMinX = minX; cachedMaxX = maxX;
        cachedMinY = minY; cachedMaxY = maxY;
    }

    void OnRenderObject()
    {
        if (drawGrid && lineMaterial != null)
        {
            DrawGridLines(cachedMinX, cachedMaxX, cachedMinY, cachedMaxY);
        }
    }

    /// <summary>
    /// 渲染可见区域内的细胞
    /// </summary>
    void RenderCells(int minX, int maxX, int minY, int maxY, bool optimizedMode)
    {
        float ppe = SimulationConfig.PixelPerEnvir;
        playerMatrices.Clear();
        npcMatrices.Clear();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Envir env = SimulationCore.GetEnvir(x, y);
                if (env == null || env.CellNum == 0) continue;

                if (optimizedMode)
                {
                    Cell best = env.GetHighestPriorityCell();
                    if (best == null) continue;

                    float worldX = (x - 0.5f) * ppe;
                    float worldY = (y - 0.5f) * ppe;
                    float scale = ppe * 0.8f;

                    Matrix4x4 m = Matrix4x4.TRS(
                        new Vector3(worldX, worldY, 0),
                        Quaternion.identity,
                        new Vector3(scale, scale, 1));

                    if (best.isPlayer) playerMatrices.Add(m);
                    else npcMatrices.Add(m);
                }
                else
                {
                    RenderCellsInGrid(env, x, y, ppe);
                }
            }
        }

        // 分批渲染玩家和NPC
        FlushBatches(playerMatrices, playerMaterial);
        FlushBatches(npcMatrices, npcMaterial);
    }

    /// <summary>
    /// 在单个环境格内排列并渲染所有细胞
    /// 细胞表现为圆形，优先级越大圆形越大，等比缩放使其恰好塞满方格
    /// </summary>
    void RenderCellsInGrid(Envir env, int gx, int gy, float ppe)
    {
        int n = env.CellNum;
        if (n == 0) return;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
        int rows = Mathf.CeilToInt((float)n / cols);

        float cellWidth = ppe / cols;
        float cellHeight = ppe / rows;
        float baseSize = Mathf.Min(cellWidth, cellHeight);

        int maxPriority = 1;
        for (int i = 1; i <= n; i++)
        {
            if (env.CellList[i] != null && env.CellList[i].priority > maxPriority)
                maxPriority = env.CellList[i].priority;
        }

        float originX = (gx - 1) * ppe;
        float originY = (gy - 1) * ppe;

        int idx = 0;
        for (int i = 1; i <= n; i++)
        {
            Cell cell = env.CellList[i];
            if (cell == null) continue;

            int col = idx % cols;
            int row = idx / cols;

            float cx = originX + (col + 0.5f) * cellWidth;
            float cy = originY + (row + 0.5f) * cellHeight;

            float priorityRatio = (float)cell.priority / maxPriority;
            float scale = baseSize * (0.4f + 0.55f * priorityRatio);

            Matrix4x4 m = Matrix4x4.TRS(
                new Vector3(cx, cy, 0),
                Quaternion.identity,
                new Vector3(scale, scale, 1));

            if (cell.isPlayer) playerMatrices.Add(m);
            else npcMatrices.Add(m);

            idx++;
        }
    }

    void FlushBatches(List<Matrix4x4> matricesList, Material mat)
    {
        if (matricesList.Count == 0) return;

        for (int i = 0; i < matricesList.Count; i++)
        {
            Graphics.DrawMesh(quadMesh, matricesList[i], mat, 0);
        }
    }

    /// <summary>
    /// 绘制环境格网格线
    /// </summary>
    void DrawGridLines(int minX, int maxX, int minY, int maxY)
    {
        float ppe = SimulationConfig.PixelPerEnvir;

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(GridLineColor);

        // 竖线
        for (int x = minX; x <= maxX + 1; x++)
        {
            float wx = (x - 1) * ppe;
            GL.Vertex3(wx, (minY - 1) * ppe, 0);
            GL.Vertex3(wx, maxY * ppe, 0);
        }

        // 横线
        for (int y = minY; y <= maxY + 1; y++)
        {
            float wy = (y - 1) * ppe;
            GL.Vertex3((minX - 1) * ppe, wy, 0);
            GL.Vertex3(maxX * ppe, wy, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}
