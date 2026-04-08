// Gene.cs - 基因数据类（struct，值拷贝无GC）

public struct Gene
{
    public int id;           // 基因编号，0表示空槽位
    public int energyCost;   // 每回合能量消耗

    public Gene(int id, int energyCost = 1)
    {
        this.id = id;
        this.energyCost = energyCost;
    }
}
