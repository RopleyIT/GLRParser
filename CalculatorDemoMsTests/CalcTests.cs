using CalculatorDemo;
namespace CalculatorDemoMsTests;

[TestClass]
public sealed class CalcTests
{
    [TestMethod]
    public void TestSimpleExpressions()
    {
        string result = Calculator.Calculate("3.3E2 * 2");
        Assert.AreEqual("660", result);
        result = Calculator.Calculate("3000 - 100");
        Assert.AreEqual("2900", result);
    }

    [TestMethod]
    public void TestTripleExpressions()
    {
        string result = Calculator.Calculate("3.3E2 - 2 + 3");
        Assert.AreEqual("331", result);
        result = Calculator.Calculate("3000 * 100 / 30");
        Assert.AreEqual("10000", result);
    }

    [TestMethod]
    public void TestParenExpressions()
    {
        string result = Calculator.Calculate("3.3E2 - (2 + 3)");
        Assert.AreEqual("325", result);
        result = Calculator.Calculate("3000 * (100 - 30)");
        Assert.AreEqual("210000", result);
    }

    [TestMethod]
    public void TestPrecedenceExpressions()
    {
        string result = Calculator.Calculate("3.3E2 - 2 * 3");
        Assert.AreEqual("324", result);
        result = Calculator.Calculate("3.3E2 * 2 - 3");
        Assert.AreEqual("657", result);
        result = Calculator.Calculate("3.3E2 * 2 - 3 * 7");
        Assert.AreEqual("639", result);
        result = Calculator.Calculate("3.3E2 - 2 * 3 + 7");
        Assert.AreEqual("331", result);
    }

    [TestMethod]
    public void TestUnaryMinusExpressions()
    {
        string result = Calculator.Calculate("-3.3E2 - (2 + 3)");
        Assert.AreEqual("-335", result);
        result = Calculator.Calculate("-3000 * -(100 - 30)");
        Assert.AreEqual("210000", result);
        result = Calculator.Calculate("3000 - -(100 - 30)");
        Assert.AreEqual("3070", result);
    }
}
