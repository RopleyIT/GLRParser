using CalculatorDemo;
namespace CalculatorDemoMsTests;

[TestClass]
public class GLRCalcTests
{
    [TestMethod]
    public void TestGLRIntExpressions()
    {
        string result = GLRCalculator.Calculate("3 * 2");
        Assert.AreEqual("6", result);
        result = GLRCalculator.Calculate("3 - 1");
        Assert.AreEqual("2", result);
    }

    [TestMethod]
    public void TestGLRSimpleExpressions()
    {
        string result = GLRCalculator.Calculate("3.3E2 * 2");
        Assert.AreEqual("660", result);
        result = GLRCalculator.Calculate("3000 - 100");
        Assert.AreEqual("2900", result);
    }

    [TestMethod]
    public void TestGLRTripleExpressions()
    {
        string result = GLRCalculator.Calculate("3.3E2 - 2 + 3");
        Assert.AreEqual("331", result);
        result = GLRCalculator.Calculate("3000 * 100 / 30");
        Assert.AreEqual("10000", result);
    }

    [TestMethod]
    public void TestGLRParenExpressions()
    {
        string result = GLRCalculator.Calculate("3.3E2 - (2 + 3)");
        Assert.AreEqual("325", result);
        result = GLRCalculator.Calculate("3000 * (100 - 30)");
        Assert.AreEqual("210000", result);
    }

    [TestMethod]
    public void TestGLRPrecedenceExpressions()
    {
        string result = GLRCalculator.Calculate("3.3E2 - 2 * 3");
        Assert.AreEqual("324", result);
        result = GLRCalculator.Calculate("3.3E2 * 2 - 3");
        Assert.AreEqual("657", result);
        result = GLRCalculator.Calculate("3.3E2 * 2 - 3 * 7");
        Assert.AreEqual("639", result);
        result = GLRCalculator.Calculate("3.3E2 - 2 * 3 + 7");
        Assert.AreEqual("331", result);
    }

    [TestMethod]
    public void TestGLRUnaryMinusExpressions()
    {
        string result = GLRCalculator.Calculate("-3.3E2 - (2 + 3)");
        Assert.AreEqual("-335", result);
        result = GLRCalculator.Calculate("-3000 * -(100 - 30)");
        Assert.AreEqual("210000", result);
        result = GLRCalculator.Calculate("3000 - -(100 - 30)");
        Assert.AreEqual("3070", result);
    }
}
