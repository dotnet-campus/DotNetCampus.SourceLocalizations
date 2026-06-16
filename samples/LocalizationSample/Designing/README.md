# 源生成器设计方案

本文件夹包含本地化源生成器的设计目标文件（伪代码），以及完整的决策记录。
新会话实现时应参考本文档。

## 设计约束与决策记录

### 决策 1：GenerationMode（Dictionary vs Compiled）

- **Dictionary**：运行时从 `Dictionary<string,string>` 中查找，支持动态 key 拼接。适合应用层。
- **Compiled**：每种语言的每个 key 编译为字面量属性，零分配零字典。适合库项目。
- Compiled 牺牲动态拼接能力换取极致性能，这是合理的需求取舍，不需要退化方案。

### 决策 2：Compiled 的实现方式 — 单类 + 显式接口实现

每种语言只生成一个类，通过显式接口实现所有层级接口。导航属性（`A`、`B`）返回 `this`。
好处：
- 每语言仅一次对象分配（单例）
- 无对象树，无层层 new
- 显式接口天然避免不同层级同名属性冲突

### 决策 3：WPF 兼容性

**WPF 不认显式接口实现**（绑定路径 `A.B.C` 时，WPF 只能通过公开属性/索引器找到成员）。
Avalonia / WinUI / Uno Platform 均可正确绑定显式接口属性。

结论：
- **Dictionary 模式必须照顾 WPF** → 保留多类树方案（每节点一个类，公开属性实现接口）
- **Compiled 模式无需照顾 WPF** → 使用单类 + 显式接口方案

### 决策 4：NotificationMode

| 模式 | 行为 |
|------|------|
| `InitOnly` | 所有属性只读，切换语言时直接替换 `_current` 实例，不发通知 |
| `CurrentCulturePropertyChanged` | 切换语言时发出属性变更通知，UI 自动更新 |
| `LocalizationItemPropertyChanged` | 单个语言项可变（基本无业务场景，仅 Dictionary 支持） |

#### INPC 机制差异

**Dictionary + INPC**：
- 使用递归 `SetProvider()` 方案（保持 `_current` 对象引用不变，就地替换 provider）
- 每个节点独立实现 `INotifyPropertyChanged`，各自 raise 自己的叶子属性
- 原因：WPF 绑定 `{Binding A.B.C, Source={x:Static l:Lang.Current}}` 时，`x:Static` 只求值一次，
  之后 WPF 逐层监听每个对象的 PropertyChanged。如果替换 `_current` 实例，WPF 不会重新取 Source。

**Compiled + INPC**：
- 使用 `SetInner()` 方案（`NotifiableLocalizedValues` 单类持有 `_inner` 引用，切换时替换并 raise）
- 由于 Compiled 不照顾 WPF，非 WPF 框架中所有导航属性返回 `this`（同一对象），
  只要在该对象上 raise 所有叶子属性名即可触发 UI 更新。

#### 非法组合

- `Compiled + LocalizationItemPropertyChanged` → 源生成器应报诊断错误。
  Compiled 无 provider，不存在"单个语言项变更"的概念。

### 决策 5：DependencyMode

| 模式 | 行为 |
|------|------|
| `Library` | 基础类型（`LocalizedString`、`ILocalizedStringProvider` 等）来自 `DotNetCampus.Localizations.dll` |
| `NestedSource` | 所有类型生成为 `partial class {Type}` 的内部类，无运行时依赖 |

NestedSource 时：
- 接口树包裹在 `partial class {Type} {` ... `}` 中，访问级别为 `internal`
- 额外生成 `LocalizedString` 等结构体作为内部类型
- 所有类型引用改为相对路径（无需 `global::DotNetCampus.Localizations.`）

### 决策 6：static 修饰符

用户声明 `static partial class Lang` 时，`Default`/`Current`/`SetCurrent` 等保持 static（当前就是如此）。
非 static 时允许实例化（适合 DI 注入）。差异极小，仅修饰符不同。

---

## 有效组合矩阵

| # | GenerationMode | NotificationMode | DependencyMode | 有效？ |
|---|---|---|---|---|
| 1 | Dictionary | InitOnly | Library | ✅ |
| 2 | Dictionary | InitOnly | NestedSource | ✅ |
| 3 | Dictionary | CurrentCulturePropertyChanged | Library | ✅ |
| 4 | Dictionary | CurrentCulturePropertyChanged | NestedSource | ✅ |
| 5 | Dictionary | LocalizationItemPropertyChanged | Library | ✅ |
| 6 | Dictionary | LocalizationItemPropertyChanged | NestedSource | ✅ |
| 7 | Compiled | InitOnly | Library | ✅ |
| 8 | Compiled | InitOnly | NestedSource | ✅ |
| 9 | Compiled | CurrentCulturePropertyChanged | Library | ✅ |
| 10 | Compiled | CurrentCulturePropertyChanged | NestedSource | ✅ |
| 11 | Compiled | LocalizationItemPropertyChanged | * | ❌ 报诊断错误 |

---

## 生成文件清单

| 生成文件 | 生成条件 | 内容概要 |
|----------|----------|----------|
| **主类** `{Type}.g.cs` | 始终 | partial class：Default/Current/SetCurrent/Create + 工厂 |
| **接口树** `ILocalizedValues.g.cs` | 始终 | 接口层级（由 key 树结构决定，与配置无关） |
| **Immutable Values** | Dictionary | 多类树：`ImmutableLocalizedValues` + `_A` + `_A_B` ...（公开属性） |
| **Compiled Values** `LocalizedValues_{Tag}.g.cs` | Compiled（每语言一个文件） | 单类 + 显式接口 + 字面量 |
| **Notifiable Values (Dictionary)** | Dictionary + INPC | 多类树 + 递归 SetProvider + 每节点 INPC |
| **Notifiable Values (Compiled)** | Compiled + INPC | 单类包装 + SetInner + raise 所有叶子 |
| **Provider** `Strings.{tag}.g.cs` | Dictionary（每语言一个文件） | `LocalizedStringProvider_{Tag}`：Dictionary + indexer + fallback |
| **LocalizedString** `{Type}.LocalizedString.g.cs` | NestedSource | 基础结构体（LocalizedString / LocalizedString&lt;T&gt;） |

---

## 场景示例文件

| 文件前缀 | 配置 | 说明 |
|----------|------|------|
| `CompiledLang` | Compiled + InitOnly + NestedSource | 最纯粹编译模式 |
| `CompiledNotifiable` | Compiled + INPC + Library | 编译 + 切换通知 |
| `DictLang` | Dictionary + InitOnly + Library | 最简字典模式 |
| `DictNotifiable` | Dictionary + INPC + Library | 当前 Lang 的行为 |

### 简化 Key 树（用于所有示例）

```
A.A1 = "Words" / "文本"                  （无参 LocalizedString）
A.A2 = "Error code: {0}" / "错误码：{0}"  （一个 int 参数 LocalizedString<int>）
A.B.B1 = "Hello" / "你好"                （无参，二级嵌套）
```

---

## 源生成器实现规划

### Generator 列表（已确认 ✅）

当前的 `LocalizationTypeGenerator` 和 `StringsGenerator` 将被删除，替换为以下 6 个 Generator：

| Generator | 职责 | 输出文件 | 触发条件 |
|-----------|------|----------|----------|
| **LocalizationTypeGenerator** | 主类 partial | `{Type}.g.cs` | 始终 |
| **InterfaceTreeGenerator** | 接口树 | `ILocalizedValues.g.cs` | 始终 |
| **DictionaryValuesGenerator** | Dictionary 模式的实现类（多类树） | `LocalizedValues.immutable.g.cs`<br>`LocalizedValues.notifiable.g.cs` | GenerationMode = Dictionary |
| **CompiledValuesGenerator** | Compiled 模式的实现类（单类+显式接口） | `LocalizedValues_{Tag}.g.cs`<br>`NotifiableLocalizedValues.g.cs` | GenerationMode = Compiled |
| **StringsProviderGenerator** | Dictionary 的 Provider | `Strings.{tag}.g.cs` | GenerationMode = Dictionary |
| **NestedTypesGenerator** | NestedSource 的基础类型 | `{Type}.LocalizedString.g.cs` | DependencyMode = NestedSource |

#### 关于 NestedSource 外壳

`DependencyMode = NestedSource` 时，各 Generator 自行在输出外围包裹 `partial class {Type} { ... }`。
不单独抽 Generator，因为逻辑简单（一行判断），且项目已有 `DotNetCampus.CodeAnalysisUtils` 提供辅助。

#### 关于 InterfaceTreeGenerator 独立

接口树是唯一一个所有组合都生成且内容与配置无关（仅由 key 树结构决定）的产物。
独立后 DictionaryValuesGenerator / CompiledValuesGenerator 不需要重复处理接口生成逻辑。

### 各 Generator 内部判断逻辑

```
LocalizationTypeGenerator:
├─ NotificationMode → 选择主类模板
│  ├─ InitOnly → SetCurrent 为 void，_current 直接替换
│  └─ INPC → SetCurrent 调用 SetProvider/SetInner
├─ GenerationMode → 工厂方法实现
│  ├─ Dictionary → new Provider → new ImmutableLocalizedValues(provider)
│  └─ Compiled → 返回 LocalizedValues_{Tag}.Instance
├─ DependencyMode → NestedSource 时包裹外壳
└─ static → 修饰符

InterfaceTreeGenerator:
├─ DependencyMode
│  ├─ Library → namespace DotNetCampus.Localizations, public
│  └─ NestedSource → 包裹在 partial class, internal
└─ 接口内容本身与配置无关，仅由 key 树决定

DictionaryValuesGenerator:（仅 GenerationMode = Dictionary 时）
├─ 始终生成 ImmutableLocalizedValues 多类树（公开属性，WPF 兼容）
├─ NotificationMode != InitOnly → 额外生成 NotifiableLocalizedValues 多类树（递归 SetProvider + 每节点 INPC）
└─ DependencyMode → NestedSource 时包裹外壳

CompiledValuesGenerator:（仅 GenerationMode = Compiled 时）
├─ 每语言生成一个单类（显式接口实现 + 字面量 + 单例）
├─ NotificationMode = INPC → 额外生成 NotifiableLocalizedValues 单类包装（SetInner + raise 所有叶子）
└─ DependencyMode → NestedSource 时包裹外壳

StringsProviderGenerator:（仅 GenerationMode = Dictionary 时）
├─ 每语言一个 Provider 文件（Dictionary<string,string> + indexer + fallback）
└─ DependencyMode → NestedSource 时包裹外壳

NestedTypesGenerator:（仅 DependencyMode = NestedSource 时）
└─ 生成 LocalizedString / LocalizedString<T1> / ... 系列结构体（包裹在 partial class 内）
```

---

## 代码生成方式选择：模板 vs SourceTextBuilder

### 判断规则

| 条件 | 选择 | 原因 |
|------|------|------|
| 生成的代码**结构固定**，仅个别值（类型名、语言标签列表、switch 分支等）随配置变化 | **模板**（EmbeddedSourceFile + Replace/FlagReplace） | 代码可读性高，修改时直接改 `.g.cs` 模板文件即可，无需在 C# 中拼接大段逻辑 |
| 生成的代码**结构由数据驱动**（类型数量、成员数量取决于输入数据如 key 树） | **SourceTextBuilder** | 避免递归拼字符串，天然管理缩进，且 Library/NestedSource 两种模式可通过 `IAllowTypeDeclaration` 共享同一段逻辑 |

### 各 Generator 的选择

| Generator | 方式 | 理由 |
|-----------|------|------|
| **LocalizationTypeGenerator** | 模板 | 主类结构固定（Default/Current/SetCurrent/Create/工厂），仅语言标签列表和少量分支随配置变化 |
| **InterfaceTreeGenerator** | SourceTextBuilder | 接口数量完全由 key 树结构决定 |
| **DictionaryValuesGenerator** | SourceTextBuilder | 实现类数量由 key 树决定，每类的成员也由子节点决定 |
| **CompiledValuesGenerator** | SourceTextBuilder | 同上（虽然编译模式是单类，但显式接口成员数量仍由 key 树决定） |
| **StringsProviderGenerator** | SourceTextBuilder | 虽然每文件结构固定，但内容简单且需要统一处理 NestedSource 包裹 |
| **NestedTypesGenerator** | 模板或 SourceTextBuilder 均可 | 结构固定且简单，取决于实现便利性 |
