# 教程 12：节点图序列化与版本迁移（Graph Versioning）

## 概述

本教程以一个**游戏数值平衡配置系统**为载体，介绍 GraphToolkit 资产的版本控制机制。随着项目迭代，节点的选项字段会增加或变更；本教程展示如何通过 `m_SchemaVersion` 跟踪资产版本，并在 `ScriptedImporter` 中编写迁移逻辑，使旧资产无需手动重新创建即可自动补全新字段。

本教程采用**纯 Pull 模型**数据流图：节点之间没有连线，Importer 直接按类型扫描所有节点，输出 `BalanceConfig` ScriptableObject 供运行时读取。

### 学习目标

- 理解 GraphToolkit 将节点选项（`INodeOption`）序列化为 YAML 键值的机制
- 掌握 `m_SchemaVersion` + `const CurrentSchemaVersion` 双版本追踪模式
- 学会在 `ScriptedImporter.OnImportAsset` 中识别旧资产并执行数据迁移
- 理解 `[ScriptedImporter(version, "ext")]` 版本号与 schema 版本号的职责分工
- 实践"无连线纯数据节点"设计——节点只有选项（Options），没有端口（Ports）

### 前置知识

- 完成教程 01（HelloGraph，必须）
- 完成教程 02（DataFlow/Pull 模型，推荐）
- 了解 Unity ScriptedImporter 基础

---

## 项目结构

```
Assets/Tutorials/12_GraphVersioning/
├─ Editor/
│  ├─ BalanceGraph.cs               # [Graph("balancegraph")] 图定义 + m_SchemaVersion + OnEnable 自动填充
│  ├─ BalanceGraphImporter.cs       # [ScriptedImporter(2,"balancegraph")] 含 V1→V2 迁移逻辑
│  ├─ Nodes/
│  │  ├─ IBalanceNode.cs            # 标记接口，供 Importer 按接口类型过滤
│  │  ├─ EnemyStatsNode.cs          # 敌人属性节点（4 个选项，无端口）
│  │  ├─ WeaponStatsNode.cs         # 武器属性节点（V1:3 选项 / V2:4 选项，TryGetRange）
│  │  └─ BalanceOutputNode.cs       # 标记节点（无端口/选项，仅作视觉标识）
│  └─ Unity.GraphToolkit.Tutorials.GraphVersioning.Editor.asmdef
├─ Runtime/
│  ├─ BalanceConfig.cs              # ScriptableObject 输出（EnemyConfig[] + WeaponConfig[]）
│  └─ Unity.GraphToolkit.Tutorials.GraphVersioning.Runtime.asmdef
└─ Examples/
   └─ Graphs/
      ├─ GameBalance_V2.balancegraph       # V2 格式，含 Range 字段（正常导入）
      └─ GameBalance_V1Legacy.balancegraph # V1 格式，无 Range 字段（演示迁移）
```

---

## 核心概念 1：GraphToolkit 的选项序列化规则

GraphToolkit 将每个 `INodeOption` 以 `__option_<名称>` 为键存储在资产的 YAML 文件中：

```yaml
# GameBalance_V2.balancegraph 中某个 WeaponStatsNode 的 YAML 片段
m_InputConstantsById:
  m_KeyList:
  - __option_Weapon Name      # → AddOption<string>("Weapon Name")
  - __option_Base Damage      # → AddOption<int>("Base Damage")
  - __option_Attack Speed     # → AddOption<float>("Attack Speed")
  - __option_Range            # → AddOption<float>("Range")  ← V2 新增
  m_ValueList:
  - rid: 302                  # Constant<string> "Iron Sword"
  - rid: 303                  # Constant<int>    20
  - rid: 304                  # Constant<float>  1.5
  - rid: 305                  # Constant<float>  1.0
```

而 V1 旧资产（`GameBalance_V1Legacy.balancegraph`）同一节点的 YAML：

```yaml
m_InputConstantsById:
  m_KeyList:
  - __option_Weapon Name
  - __option_Base Damage
  - __option_Attack Speed
  # 没有 __option_Range ← 这就是"旧资产"的特征
  m_ValueList:
  - rid: 202
  - rid: 203
  - rid: 204
```

**关键规则**：

| 操作 | 对旧资产的影响 |
|------|-------------|
| **新增**选项 | 旧资产 YAML 无该键 → `TryGetValue` 返回 `false` → 可在 Importer 中处理 |
| **重命名**选项 | 旧键的值丢失（旧键变成无效数据），等同于删除后新增 |
| **删除**选项 | 旧键的值被框架忽略，下次资产保存时清除 |

> **工程实践**：选项名称即序列化键，**永远不要重命名已发布资产使用的选项名称**。若必须重命名，需在 Importer 中手动读取旧键并写入新键（GraphToolkit 不提供自动重命名迁移）。

---

## 核心概念 2：双版本号机制

T12 使用两个相互配合但职责不同的版本号：

```
┌─────────────────────────────────────────────────────────┐
│  [ScriptedImporter(2, "balancegraph")]  ← Importer 版本 │
│                                                         │
│  功能：控制 Unity 是否对所有资产触发强制重导入           │
│  触发：递增此数字 → Unity 重新导入所有 .balancegraph     │
│  时机：迁移逻辑本身发生变化时递增                        │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  BalanceGraph.m_SchemaVersion  ← Schema 版本            │
│                                                         │
│  功能：记录该资产文件保存时的节点结构版本                │
│  读取：Importer 从已加载的 BalanceGraph 实例读取         │
│  判断：schemaVersion < CurrentSchemaVersion → 需迁移     │
└─────────────────────────────────────────────────────────┘
```

### 2.1 Importer 版本号

```csharp
[ScriptedImporter(2, "balancegraph")]
public class BalanceGraphImporter : ScriptedImporter { ... }
```

Unity 在资产数据库（Library/）中记录每个资产"上次导入时使用的 Importer 版本"。当该数字递增时，Unity 在下次打开项目或刷新时对**所有** `.balancegraph` 文件触发完整重导入。这确保了即便是从未被手动修改的旧资产，也会经过最新的迁移逻辑处理。

### 2.2 Schema 版本号

```csharp
[Graph("balancegraph", GraphOptions.Default)]
[Serializable]
public class BalanceGraph : Graph
{
    /// <summary>当前最新 schema 版本。修改节点结构后递增此常量。</summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// 本文件保存时的 schema 版本。
    /// 0 表示极旧格式（无版本字段），通常视同 V1。
    /// </summary>
    [SerializeField] private int m_SchemaVersion = 0;

    public int SchemaVersion => m_SchemaVersion;
}
```

**版本历史**：

| SchemaVersion | 节点结构描述 |
|---------------|------------|
| 0（缺省/未设置）| 极旧资产，不含版本字段，Importer 视同 V1 处理 |
| 1 | V1：`WeaponStatsNode` 有 Weapon Name / Base Damage / Attack Speed |
| 2 | V2：在 V1 基础上新增 `Range`（float）选项 |

---

## 核心概念 3：Importer 中的迁移逻辑

```csharp
[ScriptedImporter(2, "balancegraph")]
public class BalanceGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // 步骤1：加载图形，读取 schema 版本
        var graph = GraphDatabase.LoadGraphForImporter<BalanceGraph>(ctx.assetPath);
        int schemaVersion = graph.SchemaVersion;
        bool needsMigration = schemaVersion < BalanceGraph.CurrentSchemaVersion;

        // 步骤2：遍历节点，按类型收集数据
        var weapons = new List<WeaponConfig>();
        foreach (var node in graph.GetNodes())
        {
            if (node is WeaponStatsNode weaponNode)
            {
                var config = new WeaponConfig
                {
                    weaponName  = weaponNode.GetName(),
                    baseDamage  = weaponNode.GetBaseDamage(),
                    attackSpeed = weaponNode.GetAttackSpeed(),
                };

                // 步骤3：处理 V2 新增字段 —— 迁移核心
                if (weaponNode.TryGetRange(out float storedRange))
                {
                    // V2 资产：Range 键存在，直接读取
                    config.range = storedRange;
                }
                else
                {
                    // V1 资产：Range 键缺失，按业务规则推断
                    // 策略：BaseDamage >= 30 视为重型/远程武器，range = 2.5
                    config.range = config.baseDamage >= 30 ? 2.5f : 1.0f;
                }

                weapons.Add(config);
            }
        }

        // 步骤4：生成 BalanceConfig ScriptableObject 并注册为资产主对象
        var balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        balanceConfig.weapons = weapons.ToArray();
        ctx.AddObjectToAsset("balance_config", balanceConfig);
        ctx.SetMainObject(balanceConfig);
    }
}
```

**迁移策略选择原则**：
- 优先使用**数据驱动规则**（如"伤害高 → 远程武器"）而非魔法常量
- 迁移仅在 `TryGetValue` 失败时触发，对已设置值的 V2 资产没有任何影响
- 如果无法推断合理默认值，应 `Debug.LogWarning` 提示策划手动补填

---

## 节点实现详解

### EnemyStatsNode —— 纯数据节点

```csharp
[Node("Enemy Stats", "Balance")]
[UseWithGraph(typeof(BalanceGraph))]
[Serializable]
internal class EnemyStatsNode : Node, IBalanceNode
{
    private INodeOption m_NameOption;
    private INodeOption m_HealthOption;
    private INodeOption m_AttackOption;
    private INodeOption m_DefenseOption;

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        m_NameOption    = context.AddOption<string>("Enemy Name").Delayed().Build();
        m_HealthOption  = context.AddOption<int>("Health").Build();
        m_AttackOption  = context.AddOption<int>("Attack").Build();
        m_DefenseOption = context.AddOption<int>("Defense").Build();
    }
    // GetName() / GetHealth() / GetAttack() / GetDefense() 各自 TryGetValue + 返回默认值
}
```

**设计要点**：
- 无任何端口（`OnDefinePorts` 未重写）—— 平衡配置数据之间不存在依赖关系
- `string` 选项加 `.Delayed()` 防止每次按键都重建节点
- 每个 getter 提供合理默认值（如 `int v = 100`），确保新建节点直接可用

### WeaponStatsNode —— 版本迁移核心

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    m_NameOption        = context.AddOption<string>("Weapon Name").Delayed().Build();
    m_BaseDamageOption  = context.AddOption<int>("Base Damage").Build();
    m_AttackSpeedOption = context.AddOption<float>("Attack Speed").Build();
    m_RangeOption       = context.AddOption<float>("Range").Build();   // V2 新增
}

/// <summary>
/// 尝试读取 Range 选项。
/// V1 资产：YAML 中无 __option_Range 键，TryGetValue 返回 false。
/// V2 资产：正常读取已存储的值。
/// </summary>
public bool TryGetRange(out float range)
{
    range = 1.0f;
    return m_RangeOption != null && m_RangeOption.TryGetValue(out range);
}
```

关键理解：`m_RangeOption` 是 C# 运行时字段（始终存在），但 YAML 文件中的 `__option_Range` 键只有在 V2 资产中才存在。GraphToolkit 加载时如果找不到对应键，`TryGetValue` 自然返回 `false`，这是**框架的向前兼容机制**，无需任何特殊处理。

### BalanceOutputNode —— 标记节点

```csharp
[Node("Balance Output", "Balance")]
[UseWithGraph(typeof(BalanceGraph))]
[Serializable]
internal class BalanceOutputNode : Node, IBalanceNode { }
```

空类体，无端口、无选项。作用：
- 为图形提供视觉上的"终点"，帮助策划理解数据流向
- Importer **不依赖**此节点——即便不存在也能正常导入
- 对比 T02 的 `OutputNode`（必须存在才能触发求值）：T12 是纯扫描模式，无强制终点

---

## 运行时数据结构

```csharp
// Runtime/BalanceConfig.cs — 纯运行时 ScriptableObject，不引用任何 Editor API
namespace GraphToolkitTutorials.GraphVersioning.Runtime
{
    public class BalanceConfig : ScriptableObject
    {
        [SerializeField] public EnemyConfig[] enemies = Array.Empty<EnemyConfig>();
        [SerializeField] public WeaponConfig[] weapons = Array.Empty<WeaponConfig>();
    }

    [Serializable]
    public class EnemyConfig
    {
        public string enemyName;
        public int    health;
        public int    attack;
        public int    defense;
    }

    [Serializable]
    public class WeaponConfig
    {
        public string weaponName;
        public int    baseDamage;
        public float  attackSpeed;
        public float  range;      // V2 新增字段，V1 资产由 Importer 迁移补全
    }
}
```

### 游戏代码中读取 BalanceConfig

```csharp
using GraphToolkitTutorials.GraphVersioning.Runtime;
using UnityEngine;

public class GameBalanceReader : MonoBehaviour
{
    [SerializeField] private BalanceConfig m_Config;

    void Start()
    {
        // 打印所有敌人信息
        foreach (var enemy in m_Config.enemies)
        {
            Debug.Log($"[Enemy] {enemy.enemyName} — HP:{enemy.health} ATK:{enemy.attack} DEF:{enemy.defense}");
        }

        // 打印所有武器信息（range 字段在 V1 资产中也已由 Importer 自动补全）
        foreach (var weapon in m_Config.weapons)
        {
            Debug.Log($"[Weapon] {weapon.weaponName} — DMG:{weapon.baseDamage} SPD:{weapon.attackSpeed} RNG:{weapon.range}");
        }
    }
}
```

`BalanceConfig` 是 ScriptableObject，在 Inspector 中拖入 `m_Config` 字段即可使用，和普通数据资产完全相同。

---

## 示例文件说明

### GameBalance_V2.balancegraph（当前版本）

| 节点 | 类型 | 数据 |
|------|------|------|
| Goblin | EnemyStatsNode | HP=100, ATK=15, DEF=5 |
| Orc Warrior | EnemyStatsNode | HP=300, ATK=25, DEF=15 |
| Iron Sword | WeaponStatsNode | DMG=20, SPD=1.5, **Range=1.0** |
| Siege Cannon | WeaponStatsNode | DMG=50, SPD=0.3, **Range=5.0** |
| — | BalanceOutputNode | 标记节点 |

导入结果：`BalanceConfig.weapons[0].range = 1.0`（直接读取 YAML 存储值）

### GameBalance_V1Legacy.balancegraph（迁移演示）

| 节点 | 类型 | 数据 |
|------|------|------|
| Dragon | EnemyStatsNode | HP=500, ATK=50, DEF=30 |
| Ancient Cannon | WeaponStatsNode | DMG=45, SPD=0.4, **无 Range 字段** |
| — | BalanceOutputNode | 标记节点 |

导入结果：`weapons[0].baseDamage=45 ≥ 30` → **Importer 自动迁移** → `weapons[0].range = 2.5`

Console 输出：
```
[BalanceGraph] 检测到旧版 schema（v1），执行迁移 → v2：.../GameBalance_V1Legacy.balancegraph
[BalanceGraph] 武器 'Ancient Cannon' 迁移 Range → 2.5
[BalanceGraph] 导入完成：1 敌人，1 武器（schema v1）
```

---

## 与其他教程的对比

| 特性 | T02 DataFlow | T03 ExecutionFlow | T08 DialogueSystem | T12 GraphVersioning |
|------|-------------|-------------------|-------------------|---------------------|
| 图形范式 | Pull（连线求值） | Push（执行流） | Push（执行流） | Pull（扫描节点） |
| 节点连接 | 有（数据流） | 有（执行流） | 有（执行流） | **无**（纯数据集） |
| 节点数据 | 端口 + 选项 | 端口 + 选项 | 端口 + 选项 | **仅选项**（无端口） |
| 运行时 | 无 | MonoBehaviour | MonoBehaviour | **无**（ScriptableObject） |
| Importer 输出 | Texture2D | RuntimeGraph SO | DialogueRuntimeGraph SO | **BalanceConfig SO** |
| 版本控制 | 无 | 无 | 无 | **m_SchemaVersion + 迁移** |

---

## 配置步骤

### 使用示例文件

1. 打开 Project 窗口，进入 `Assets/Tutorials/12_GraphVersioning/Examples/Graphs/`
2. 选中 `GameBalance_V2.balancegraph`，确认右侧 Inspector 显示 `BalanceConfig` 预览（含 2 敌人 2 武器）
3. 选中 `GameBalance_V1Legacy.balancegraph`，查看 Console 是否有迁移日志
4. 点击 V1Legacy 资产，在 Inspector 中展开 `Weapons[0]` 确认 `range = 2.5`（迁移成功）

### 创建新图形

1. 在 Project 窗口右键 → **Create → Graph Toolkit → Balance Graph**
2. 双击打开，系统自动在 `OnEnable` 中填充一个示例 `EnemyStatsNode`、`WeaponStatsNode`、`BalanceOutputNode`
3. 选中节点，在右侧 Inspector（Node Properties）中填写各选项值
4. 按 **Ctrl+S** 保存，Unity 自动触发 `BalanceGraphImporter`，在同目录生成 `BalanceConfig` 资产
5. 在游戏对象上挂载 `GameBalanceReader`，将 `BalanceConfig` 拖入 `m_Config` 字段运行验证

### 模拟版本升级流程

1. 将 `WeaponStatsNode.cs` 中 `m_RangeOption` 定义注释掉（模拟"删除 V2 选项"），将 `CurrentSchemaVersion` 改回 1
2. 打开 V2 示例图，确认 Range 选项在 Inspector 消失（节点定义已移除）
3. 恢复代码，将 `CurrentSchemaVersion` 改为 3，在 Importer 中加入 V2→V3 迁移逻辑
4. 递增 `[ScriptedImporter(3, "balancegraph")]`，观察 Unity 对所有文件强制重导入

---

## 常见问题

### Q：为什么旧资产的 Range 不是 0 而是由 Importer 推断？

因为 `TryGetValue` 在找不到 YAML 键时返回 `false`（不修改 `out` 参数的值），而不是返回 `0`。Importer 通过检查返回值来区分"旧资产未填写"和"新资产明确设为 0"。如果两种情况都返回相同值，就无法区分是否需要迁移。

### Q：`SchemaVersion = 0` 的资产和 `SchemaVersion = 1` 的处理方式一样吗？

在本教程中是的。`m_SchemaVersion` 的默认值是 `0`，因此极旧的资产（完全没有版本字段）和显式保存了 `m_SchemaVersion = 0` 的资产，在条件 `schemaVersion < CurrentSchemaVersion` 下行为相同。如果需要区分，可以加 `schemaVersion == 0` 的特殊分支。

### Q：递增 Importer 版本号会影响性能吗？

会短暂影响——Unity 会在下次导入时重新处理所有 `.balancegraph` 文件。对于少量资产，这几乎是瞬间完成的。对于大型项目（数百个资产），应在开发周期低谷期（如 CI 流水线）执行版本升级。

### Q：可以不用 `m_SchemaVersion`，只凭 `TryGetValue` 的返回值判断吗？

可以，对于简单的"新增字段"迁移场景确实不需要显式版本号——`TryGetValue = false` 就足够判断。但 `m_SchemaVersion` 在以下情况不可缺少：
- 需要区分"字段缺失"和"字段被用户主动清空"
- 多个字段同时迁移，迁移顺序有依赖关系（V1→V2→V3 链式迁移）
- Importer 日志需要准确报告"从哪个版本升级"

---

## 练习

1. **新增字段**：向 `EnemyStatsNode` 增加 `Speed`（float）选项，将 `CurrentSchemaVersion` 改为 3，在 Importer 中添加 V2→V3 迁移逻辑（速度默认 `3.0f`）

2. **选项重命名测试**：将 `WeaponStatsNode` 中 `"Base Damage"` 改为 `"Damage"`，重新导入 V2 示例文件，观察 Inspector 中 `baseDamage` 字段是否变为 `0`（旧键的值丢失）

3. **强制重导入验证**：将 `[ScriptedImporter(2, ...)]` 改为 `[ScriptedImporter(3, ...)]`，观察 Unity 是否对所有 `.balancegraph` 触发重导入（Console 会再次输出所有文件的导入日志）

4. **链式迁移**：设计 V3 版本，向 `EnemyStatsNode` 新增 `DropRate`（float）。在 Importer 中实现 `if (schemaVersion < 2) { ... V1迁移 } if (schemaVersion < 3) { ... V2迁移 }` 的链式迁移结构

5. **运行时读取**：创建一个 `GameBalanceReader` MonoBehaviour，在 `Start()` 中读取 `BalanceConfig` 并打印每个敌人的"有效战斗力"（`attack × 2 + defense`）

---

## 小结

| 概念 | 实现位置 | 作用 |
|------|----------|------|
| Schema 版本 | `BalanceGraph.m_SchemaVersion` | 记录资产保存时的节点结构版本 |
| Importer 版本 | `[ScriptedImporter(2, "balancegraph")]` | 触发所有同类资产的强制重导入 |
| 选项序列化键 | `__option_<选项名称>` | 框架内部约定，重命名导致数据丢失 |
| 新增选项兼容 | `TryGetValue` 返回 `false` | 框架自然向前兼容，无需额外处理 |
| 迁移逻辑 | `BalanceGraphImporter.OnImportAsset` | 基于旧数据推算新字段值 |
| 无连线纯数据图 | `BalanceGraph` + 无端口节点 | 配置表不需要计算关系，Importer 按类型扫描 |
| 运行时输出 | `BalanceConfig` ScriptableObject | Runtime assembly，不含任何 Editor API |
