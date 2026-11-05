using PlaceholderIndices;

// 先验证当前中文资源；每对断言分别读取 TOML、YAML，起飞文案固定传入组号 4、飞机数量 49。
AssertEqual("确定对第 4 组共 49 架飞机执行起飞？", Lang.Current.Toml.Takeoff.ToString(4, 49));
AssertEqual("确定对第 4 组共 49 架飞机执行起飞？", Lang.Current.Yaml.Takeoff.ToString(4, 49));

// 同一索引重复出现只需要一个实参；若生成器仍按出现次数计算参数数量，这些调用会编译失败。
AssertEqual("第 4 组，再次确认第 4 组。", Lang.Current.Toml.Repeated.ToString(4));
AssertEqual("第 4 组，再次确认第 4 组。", Lang.Current.Yaml.Repeated.ToString(4));

// 乱序与重复可以同时存在，重复出现的 {1} 必须始终读取第二个实参。
AssertEqual("第 4 组共 49 架飞机，仍为 49 架。", Lang.Current.Toml.ReorderedAndRepeated.ToString(4, 49));
AssertEqual("第 4 组共 49 架飞机，仍为 49 架。", Lang.Current.Yaml.ReorderedAndRepeated.ToString(4, 49));

// 英文先出现 string 类型的 {1}，生成签名仍必须是 ToString(int, string)。
AssertEqual("第 4 组属于 Fleet。", Lang.Current.Toml.Typed.ToString(4, "Fleet"));
AssertEqual("第 4 组属于 Fleet。", Lang.Current.Yaml.Typed.ToString(4, "Fleet"));

// 带类型与不带类型的同索引占位符共用 int 参数，不受类型标注出现位置的影响。
AssertEqual("第 4 组，再次确认第 4 组。", Lang.Current.Toml.RepeatedTyped.ToString(4));
AssertEqual("第 4 组，再次确认第 4 组。", Lang.Current.Yaml.RepeatedTyped.ToString(4));

// 只使用 {2} 时仍保留前两个参数槽位，实际输出取第三个实参。
AssertEqual("飞机数量：49。", Lang.Current.Toml.Sparse.ToString(4, 0, 49));
AssertEqual("飞机数量：49。", Lang.Current.Yaml.Sparse.ToString(4, 0, 49));

// 原有命名占位符继续按出现顺序分配参数，数字索引修复不能破坏这种用法。
AssertEqual("Fleet 有 49 架飞机。", Lang.Current.Toml.Named.ToString("Fleet", 49));
AssertEqual("Fleet 有 49 架飞机。", Lang.Current.Yaml.Named.ToString("Fleet", 49));

// 混用时先保留数字索引 0，再将命名参数 count 分配到索引 1，避免英文中的先后顺序导致参数覆盖。
AssertEqual("确定对第 4 组共 49 架飞机执行起飞？", Lang.Current.Toml.Mixed.ToString(4, 49));
AssertEqual("确定对第 4 组共 49 架飞机执行起飞？", Lang.Current.Yaml.Mixed.ToString(4, 49));

// 没有占位符的文案仍生成无参数字符串，可直接读取 Value。
AssertEqual("准备就绪。", Lang.Current.Toml.Plain.Value);
AssertEqual("准备就绪。", Lang.Current.Yaml.Plain.Value);

// 切换语言后复用完全相同的参数顺序，验证英文语序变化不会改变参数含义。
Lang.SetCurrent("en");

AssertEqual("Take off 49 drone(s) in group 4?", Lang.Current.Toml.Takeoff.ToString(4, 49));
AssertEqual("Take off 49 drone(s) in group 4?", Lang.Current.Yaml.Takeoff.ToString(4, 49));
AssertEqual("Group 4, again 4.", Lang.Current.Toml.Repeated.ToString(4));
AssertEqual("Group 4, again 4.", Lang.Current.Yaml.Repeated.ToString(4));
AssertEqual("49 drone(s), group 4, still 49 drone(s).", Lang.Current.Toml.ReorderedAndRepeated.ToString(4, 49));
AssertEqual("49 drone(s), group 4, still 49 drone(s).", Lang.Current.Yaml.ReorderedAndRepeated.ToString(4, 49));
AssertEqual("Fleet belongs to group 4.", Lang.Current.Toml.Typed.ToString(4, "Fleet"));
AssertEqual("Fleet belongs to group 4.", Lang.Current.Yaml.Typed.ToString(4, "Fleet"));
AssertEqual("Group 4, again 4.", Lang.Current.Toml.RepeatedTyped.ToString(4));
AssertEqual("Group 4, again 4.", Lang.Current.Yaml.RepeatedTyped.ToString(4));
AssertEqual("Drone count: 49.", Lang.Current.Toml.Sparse.ToString(4, 0, 49));
AssertEqual("Drone count: 49.", Lang.Current.Yaml.Sparse.ToString(4, 0, 49));
AssertEqual("Fleet has 49 drone(s).", Lang.Current.Toml.Named.ToString("Fleet", 49));
AssertEqual("Fleet has 49 drone(s).", Lang.Current.Yaml.Named.ToString("Fleet", 49));
AssertEqual("Take off 49 drone(s) in group 4?", Lang.Current.Toml.Mixed.ToString(4, 49));
AssertEqual("Take off 49 drone(s) in group 4?", Lang.Current.Yaml.Mixed.ToString(4, 49));
AssertEqual("Ready.", Lang.Current.Toml.Plain.Value);
AssertEqual("Ready.", Lang.Current.Yaml.Plain.Value);

Console.WriteLine("PASS");

// 比较手写预期与生成代码的实际输出；不一致时抛出异常，使宿主测试通过非零退出码识别失败。
static void AssertEqual(string expected, string actual)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected: {expected}; Actual: {actual}.");
    }
}
