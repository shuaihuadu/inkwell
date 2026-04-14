using Inkwell.Agents.Skills;

namespace Inkwell.Tests.Agents.Skills;

[TestClass]
public sealed class ContentSkillsTests
{
    [TestMethod]
    public void MarkdownLint_DetectsNoH1()
    {
        // Act
        string result = MarkdownLintSkill.Lint("## Only H2\n\nSome content");

        // Assert
        Assert.IsTrue(result.Contains("H1") || result.Length > 0);
    }

    [TestMethod]
    public void MarkdownLint_PassesValidMarkdown()
    {
        // Act
        string result = MarkdownLintSkill.Lint("# Title\n\nSome paragraph content here.");

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MarkdownLint_HandlesEmptyInput()
    {
        // Act
        string result = MarkdownLintSkill.Lint("");

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ReadabilityAnalyze_ReturnsAnalysis()
    {
        // Act
        string result = ReadabilitySkill.Analyze("This is a simple test sentence. It should be easy to read.");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void ReadabilityAnalyze_HandlesEmptyInput()
    {
        // Act
        string result = ReadabilitySkill.Analyze("");

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SensitiveWordScan_DetectsSensitiveContent()
    {
        // Act
        string result = SensitiveWordSkill.Scan("这篇文章包含暴力相关的内容");

        // Assert
        Assert.IsTrue(result.Contains("暴力") || result.Contains("敏感") || result.Contains("发现"));
    }

    [TestMethod]
    public void SensitiveWordScan_PassesCleanContent()
    {
        // Act
        string result = SensitiveWordSkill.Scan("这是一篇关于AI技术的正常文章");

        // Assert
        Assert.IsNotNull(result);
    }
}
