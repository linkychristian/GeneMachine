// SimulationCore.cs - 完全独立的计算模块，运行在后台线程
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Random = System.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

public static class SimulationCore
{
    // ========== 世界数据 ==========
    public static Envir[,] EnvirData;
    public static List<Cell> AllCells = new List<Cell>();
    public static Random Rng;
    [ThreadStatic] public static Random ThreadRng;
    public static int _rngSeedCounter;

    // ========== 行为系统（各命名空间静态类） ==========
    // Multiply.Behavior, Temperature.Behavior, Light.Behavior, Death.Behavior

    // ========== 线程控制 ==========
    private static bool isRunning = false;
    private static bool isPaused = false;
    private static Thread simulationThread;
    private static Stopwatch timer = new Stopwatch();

    // ========== 统计数据 ==========
    public static long stepsPerSecond = 0;
    public static long totalSteps = 0;
    public static int aliveCellCount = 0;

    // ========== 模拟速度 ==========
    public static int speedMultiplier = 1; // 1x = 1步/秒, 10x = 10步/秒

    // ========== 八方向偏移 ==========
    public static readonly int[] DX = { -1, -1, -1, 0, 0, 1, 1, 1 };
    public static readonly int[] DY = { -1, 0, 1, -1, 1, -1, 0, 1 };

    public static void InitWorld()
    {
        Rng = new Random(SimulationConfig.WorldSeed);
        _rngSeedCounter = SimulationConfig.WorldSeed + 1000;
        int size = SimulationConfig.EnvirSize;
        EnvirData = new Envir[size + 2, size + 2];
        for (int x = 1; x <= size; x++)
        {
            for (int y = 1; y <= size; y++)
            {
                EnvirData[x, y] = new Envir();
                EnvirData[x, y].Temp = 22 + (int)(6.0 * Math.Sin(Math.PI * y / size));
                EnvirData[x, y].Light = 30 + (int)(40.0 * Math.Sin(Math.PI * x / size));
            }
        }
        AllCells.Clear();
        // 生成地形（柏林噪声 + 侵蚀 + 河流）
        TerrainGenerator.Generate(EnvirData, SimulationConfig.WorldSeed);

        // 从世界中心搜索最近陆地作为玩家出生点
        int cx = size / 2, cy = size / 2;
        for (int r = 0; r <= 200 && EnvirData[cx, cy].Topography == 0; r++)
        {
            bool found = false;
            for (int dx = -r; dx <= r && !found; dx++)
                for (int dy = -r; dy <= r && !found; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                    int tx = cx + dx, ty = cy + dy;
                    if (tx >= 1 && tx <= size && ty >= 1 && ty <= size && EnvirData[tx, ty].Topography != 0)
                    { cx = tx; cy = ty; found = true; }
                }
        }
        for (int i = 0; i < SimulationConfig.InitialPlayerCells; i++)
        {
            int px = cx + Rng.Next(-5, 6);
            int py = cy + Rng.Next(-5, 6);
            px = Math.Max(1, Math.Min(size, px));
            py = Math.Max(1, Math.Min(size, py));
            if (EnvirData[px, py].Topography == 0) continue;
            Cell cell = new Cell(px, py, true);
            Species.InitPlayerCell(cell);
            if (EnvirData[px, py].AddCell(cell))
                AllCells.Add(cell);
        }
        for (int c = 0; c < SimulationConfig.InitialNPCClusters; c++)
        {
            int clusterX = Rng.Next(50, size - 50);
            int clusterY = Rng.Next(50, size - 50);
            int cAttempts = 0;
            while (EnvirData[clusterX, clusterY].Topography == 0 && cAttempts++ < 200)
            {
                clusterX = Rng.Next(50, size - 50);
                clusterY = Rng.Next(50, size - 50);
            }
            if (EnvirData[clusterX, clusterY].Topography == 0) continue;
            for (int i = 0; i < SimulationConfig.InitialNPCPerCluster; i++)
            {
                int px = clusterX + Rng.Next(-3, 4);
                int py = clusterY + Rng.Next(-3, 4);
                px = Math.Max(1, Math.Min(size, px));
                py = Math.Max(1, Math.Min(size, py));
                if (EnvirData[px, py].Topography == 0) continue;
                Cell cell = new Cell(px, py, false);
                Species.InitNPCCell(cell, Rng);
                if (EnvirData[px, py].AddCell(cell))
                    AllCells.Add(cell);
            }
        }
        // 初始化各行为命名空间
        Multiply.Behavior.Init();
        Temperature.Behavior.Init();
        Light.Behavior.Init();
        Death.Behavior.Init();

        aliveCellCount = AllCells.Count;
    }

    public static void StartCalculationThread()
    {
        if (isRunning) return;
        InitWorld();
        isRunning = true;
        isPaused = false;
        simulationThread = new Thread(CalculationLoop);
        simulationThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        simulationThread.IsBackground = true;
        simulationThread.Start();
    }

    private static void CalculationLoop()
    {
        ThreadRng = new Random(Interlocked.Increment(ref _rngSeedCounter));
        timer.Reset();
        timer.Start();
        long stepCount = 0;
        long lastStatsMs = 0;
        long nextStepAtMs = 1000;
        while (isRunning)
        {
            long nowMs = timer.ElapsedMilliseconds;

            if (isPaused)
            {
                nextStepAtMs = nowMs + 1000 / Math.Max(1, speedMultiplier);
                Thread.Sleep(20);
                continue;
            }

            int currentSpeed = Math.Max(1, speedMultiplier);
            long intervalMs = 1000 / currentSpeed;
            if (intervalMs <= 0) intervalMs = 1;

            if (nowMs >= nextStepAtMs)
            {
                try
                {
                    SimulateOneStep();
                }
                catch (Exception ex)
                {
                    isRunning = false;
                    Debug.LogException(ex);
                    break;
                }
                stepCount++;
                totalSteps++;

                long updatedNowMs = timer.ElapsedMilliseconds;
                nextStepAtMs += intervalMs;
                if (nextStepAtMs < updatedNowMs)
                    nextStepAtMs = updatedNowMs + intervalMs;
            }
            else
            {
                int sleepMs = (int)Math.Min(10, nextStepAtMs - nowMs);
                if (sleepMs > 0)
                    Thread.Sleep(sleepMs);
            }

            nowMs = timer.ElapsedMilliseconds;
            if (nowMs - lastStatsMs >= 1000)
            {
                Interlocked.Exchange(ref stepsPerSecond, stepCount);
                Interlocked.Exchange(ref aliveCellCount, AllCells.Count);
                stepCount = 0;
                lastStatsMs = nowMs;
            }
        }
    }

    private static void SimulateOneStep()
    {
        // 不再排序AllCells —— 各行为Pre独立于遍历顺序，Multiply.Apply自行排序缓冲区
        // 原Sort O(N log N) 在100万细胞时约占50%时间，移除后直接翻倍

        Multiply.Behavior.PreParallel();
        Temperature.Behavior.PreParallel();
        Light.Behavior.PreParallel();
        Death.Behavior.PreParallel();
        
        Multiply.Behavior.Apply();
        Temperature.Behavior.Apply();
        Light.Behavior.Apply();
        Death.Behavior.Apply();

        // 能量消耗并行执行
        var allCells = AllCells;
        int count = allCells.Count;
        Parallel.ForEach(Partitioner.Create(0, count), range =>
        {
            if (ThreadRng == null)
                ThreadRng = new Random(Interlocked.Increment(ref _rngSeedCounter));
            for (int idx = range.Item1; idx < range.Item2; idx++)
            {
                Cell cell = allCells[idx];
                if (!cell.alive) continue;
                cell.energy -= cell.GetTotalEnergyCost();
                if (cell.energy <= 0) cell.alive = false;
            }
        });

        CleanupDeadCells();
    }

    // O(N)清理：前向压缩代替逐个RemoveAt（原为O(N²)最坏情况）
    private static void CleanupDeadCells()
    {
        int writeIdx = 0;
        for (int i = 0; i < AllCells.Count; i++)
        {
            Cell cell = AllCells[i];
            if (cell.alive)
            {
                AllCells[writeIdx++] = cell;
            }
            else
            {
                Envir env = EnvirData[cell.px, cell.py];
                for (int j = 1; j <= env.CellNum; j++)
                {
                    if (env.CellList[j] == cell)
                    {
                        env.RemoveCell(j);
                        break;
                    }
                }
            }
        }
        if (writeIdx < AllCells.Count)
            AllCells.RemoveRange(writeIdx, AllCells.Count - writeIdx);
    }

    public static bool InBounds(int x, int y)
    {
        return x >= 1 && x <= SimulationConfig.EnvirSize && y >= 1 && y <= SimulationConfig.EnvirSize;
    }

    public static int GetNeighborCellCount(int px, int py)
    {
        int count = 0;
        for (int d = 0; d < 8; d++)
        {
            int nx = px + DX[d];
            int ny = py + DY[d];
            if (InBounds(nx, ny))
                count += EnvirData[nx, ny].CellNum;
        }
        return count;
    }

    public static void PauseSimulation() { isPaused = true; }
    public static void ResumeSimulation() { isPaused = false; }
    public static bool IsPaused() { return isPaused; }
    public static Random EnsureThreadRng()
    {
        if (ThreadRng == null)
            ThreadRng = new Random(Interlocked.Increment(ref _rngSeedCounter));
        return ThreadRng;
    }

    public static void SetSpeedMultiplier(int value)
    {
        speedMultiplier = Math.Max(1, Math.Min(10, value));
    }

    public static void StopCalculation()
    {
        isRunning = false;
        simulationThread?.Join(2000);
    }

    public static Envir GetEnvir(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return EnvirData[x, y];
    }
}
