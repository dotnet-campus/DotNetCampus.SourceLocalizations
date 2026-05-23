# 源生成器重构任务大纲

本次重构将当前的 2 个 Generator 替换为 6 个，每次只完成一个子任务，验证后再继续下一个。

验证命令：
```
dotnet build samples/LocalizationSample/LocalizationSample.csproj -p:EmitCompilerGeneratedFiles=true
```

生成文件输出在：`artifacts/obj/LocalizationSample/debug/generated/DotNetCampus.Localizations.Analyzer/`

---

## 任务 0：准备工作
- 扩展 `LocalizationGeneratingModel`，解析 `GenerationMode`、`NotificationMode`、`DependencyMode` 属性
- 确保模型能正确读取 `[LocalizedConfiguration]` 上的所有新属性
- 验证：编译通过，属性值正确传递到各 Generator

## 任务 1：InterfaceTreeGenerator
- 从当前 `StringsGenerator` 中提取接口树生成逻辑，独立为 `InterfaceTreeGenerator`
- 支持 DependencyMode：Library → 顶层 public；NestedSource → 包裹 partial class, internal
- 验证：生成的 `ILocalizedValues.g.cs` 与当前输出一致（Library 模式）

## 任务 2：StringsProviderGenerator
- 从当前 `StringsGenerator` 中提取 Provider 生成逻辑
- 仅 GenerationMode = Dictionary 时触发
- 支持 DependencyMode 外壳
- 验证：生成的 `Strings.{tag}.g.cs` 与当前输出一致

## 任务 3：DictionaryValuesGenerator
- 从当前 `StringsGenerator` 中提取 Immutable/Notifiable 实现类生成
- 多类树方案（公开属性，WPF 兼容）
- NotificationMode 决定是否生成 Notifiable 系列
- 验证：生成的 `LocalizedValues.immutable.g.cs` / `notifiable.g.cs` 与当前输出一致

## 任务 4：LocalizationTypeGenerator 重写
- 重写主类生成，支持 GenerationMode + NotificationMode + DependencyMode + static 的各种组合
- 验证：Dictionary 模式下 `{Type}.g.cs` 输出与当前一致

## 任务 5：CompiledValuesGenerator（新功能）
- 实现 Compiled 模式：每语言一个单类 + 显式接口实现 + 字面量
- NotificationMode = INPC 时额外生成 NotifiableLocalizedValues 单类包装
- 验证：对照 Designing/CompiledLang.Values.cs 和 CompiledNotifiable.Values.cs

## 任务 6：NestedTypesGenerator
- DependencyMode = NestedSource 时生成 LocalizedString 系列结构体
- 验证：对照 Designing/CompiledLang.LocalizedString.cs

## 任务 7：清理
- 删除旧的 `StringsGenerator`
- 确保 Sample 项目中 `Lang`（Dictionary+INPC）和 `CompiledLang`（Compiled+InitOnly）均正确编译运行
- 非法组合（Compiled + LocalizationItemPropertyChanged）报诊断错误

---

## 注意事项

- 项目使用 IIncrementalGenerator
- 已有 DotNetCampus.CodeAnalysisUtils 库提供 SourceTextBuilder 等辅助
- 任务 1-3 可以先与旧 Generator 共存（避免一次性破坏太多），任务 7 统一清理
- 每个任务完成后等用户确认再继续
