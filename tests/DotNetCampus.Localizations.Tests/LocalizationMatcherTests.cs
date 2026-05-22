using DotNetCampus.Localizations.Helpers;

namespace DotNetCampus.Localizations.Tests;

[TestClass]
public class LocalizationMatcherTests
{
    [TestMethod(DisplayName = "直接精确匹配")]
    public void TC01_ExactMatch()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hans-CN",
            ["zh-Hans-CN", "en-US"]);

        Assert.AreEqual("zh-Hans-CN", match);
    }

    [TestMethod(DisplayName = "不区分大小写的精确匹配")]
    public void TC02_CaseInsensitiveMatch()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-hans-cn",
            ["zh-Hans-CN", "en-US"]);

        Assert.AreEqual("zh-Hans-CN", match);
    }

    [TestMethod(DisplayName = "旧版代码自动转换")]
    public void TC03_LegacyCodeConversion()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-CHS",
            ["zh-Hans-CN", "en-US"]);

        Assert.AreEqual("zh-Hans-CN", match);
    }

    [TestMethod(DisplayName = "简体中文区域回退")]
    public void TC04_SimplifiedChineseRegionFallback()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hans-CN",
            ["zh-Hans", "en-US"]);

        Assert.AreEqual("zh-Hans", match);
    }

    [TestMethod(DisplayName = "繁体中文区域回退")]
    public void TC05_TraditionalChineseRegionFallback()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hant-TW",
            ["zh-Hant", "en-US"]);

        Assert.AreEqual("zh-Hant", match);
    }

    [TestMethod(DisplayName = "中性文化优先于普通语言")]
    public void TC06_NeutralCulturePriority()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            ["en", "en-GB"]);

        Assert.AreEqual("en", match);
    }

    [TestMethod(DisplayName = "同语言不同区域回退")]
    public void TC07_SameLanguageRegionFallback()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            ["en-GB", "fr-FR"]);

        Assert.AreEqual("en-GB", match);
    }

    [TestMethod(DisplayName = "特定区域到语言代码回退")]
    public void TC08_SpecificToLanguageFallback()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            ["en", "fr"]);

        Assert.AreEqual("en", match);
    }

    [TestMethod(DisplayName = "无匹配项返回null")]
    public void TC09_NoMatchReturnsNull()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            ["fr-FR", "de-DE"]);

        Assert.IsNull(match);
    }

    [TestMethod(DisplayName = "无效文化代码处理")]
    public void TC10_InvalidCultureHandling()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "invalid-code",
            ["en-US", "zh-CN"]);

        Assert.AreEqual("en-US", match);
    }

    [TestMethod(DisplayName = "两字母语言代码匹配")]
    public void TC11_TwoLetterLanguageCodeMatch()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh",
            ["zh-Hans-CN", "en-US"]);

        Assert.AreEqual("zh-Hans-CN", match);
    }

    [TestMethod(DisplayName = "繁体中文区域变体优先级")]
    public void TC12_TraditionalChineseRegionVariants()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hant",
            ["zh-Hant-TW", "zh-Hant-HK", "en-US"]);

        Assert.AreEqual("zh-Hant-TW", match);
    }

    [TestMethod(DisplayName = "简体中文区域映射")]
    public void TC13_SimplifiedChineseRegionMapping()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-CN",
            ["zh-Hans-CN", "en-US"]);

        Assert.AreEqual("zh-Hans-CN", match);
    }

    [TestMethod(DisplayName = "繁体中文区域映射")]
    public void TC14_TraditionalChineseRegionMapping()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-TW",
            ["zh-Hant-TW", "en-US"]);

        Assert.AreEqual("zh-Hant-TW", match);
    }

    [TestMethod(DisplayName = "空请求处理")]
    public void TC15_EmptyRequestHandling()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "",
            ["en-US", "zh-CN"]);

        Assert.IsNull(match);
    }

    [TestMethod(DisplayName = "空白请求处理")]
    public void TC16_WhitespaceRequestHandling()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "   ",
            ["en-US", "zh-CN"]);

        Assert.IsNull(match);
    }

    [TestMethod(DisplayName = "区域变体优先于父级文化")]
    public void TC17_RegionVariantPriority()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            ["en-GB", "en"]);

        Assert.AreEqual("en-GB", match);
    }

    [TestMethod(DisplayName = "空候选列表处理")]
    public void TC18_EmptyCandidateList()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "en-US",
            []);

        Assert.IsNull(match);
    }

    [TestMethod(DisplayName = "旧版繁体代码转换")]
    public void TC19_LegacyTraditionalCodeConversion()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-CHT",
            ["zh-Hant-TW", "en-US"]);

        Assert.AreEqual("zh-Hant-TW", match);
    }

    [TestMethod(DisplayName = "复杂回退链测试")]
    public void TC20_ComplexFallbackChain()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hans-CN",
            ["fr", "de", "zh", "en-US"]);

        Assert.AreEqual("zh", match);
    }

    [TestMethod(DisplayName = "简体中文脚本到语言代码回退")]
    public void TC21_SimplifiedScriptToLanguageCode()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hans",
            ["zh", "en-US"]);

        Assert.AreEqual("zh", match);
    }

    [TestMethod(DisplayName = "繁体中文脚到区域变体选择")]
    public void TC22_TraditionalScriptToRegionVariant()
    {
        var match = LocalizationHelper.MatchWithFallback(
            "zh-Hant",
            ["zh", "zh-TW", "en-US"]);

        Assert.AreEqual("zh-TW", match);
    }
}
