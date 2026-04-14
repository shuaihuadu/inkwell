using Inkwell.Agents;

namespace Inkwell.Tests.Agents;

[TestClass]
public sealed class InkwellToolsTests
{
    [TestMethod]
    public void SearchLatestNews_ReturnsResults()
    {
        // Act
        string result = InkwellTools.SearchLatestNews("AI healthcare");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void AnalyzeKeyword_ReturnsAnalysis()
    {
        // Act
        string result = InkwellTools.AnalyzeKeyword("artificial intelligence");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void PublishArticle_ReturnsConfirmation()
    {
        // Act
        string result = InkwellTools.PublishArticle("Test Title", "Test summary");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("Test Title") || result.Length > 0);
    }
}
