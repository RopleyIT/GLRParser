using CalculatorDemo;
using Xunit;

namespace CalculatorDemoTest
{
    public class CalcTests
    {
        [Fact]
        public void TestSimpleExpressions()
        {
            string result = Calculator.Calculate("3.3E2 * 2");
            Assert.Equal("660", result);
            result = Calculator.Calculate("3000 - 100");
            Assert.Equal("2900", result);
        }

        [Fact]
        public void TestTripleExpressions()
        {
            string result = Calculator.Calculate("3.3E2 - 2 + 3");
            Assert.Equal("331", result);
            result = Calculator.Calculate("3000 * 100 / 30");
            Assert.Equal("10000", result);
        }

        [Fact]
        public void TestParenExpressions()
        {
            string result = Calculator.Calculate("3.3E2 - (2 + 3)");
            Assert.Equal("325", result);
            result = Calculator.Calculate("3000 * (100 - 30)");
            Assert.Equal("210000", result);
        }

        [Fact]
        public void TestPrecedenceExpressions()
        {
            string result = Calculator.Calculate("3.3E2 - 2 * 3");
            Assert.Equal("324", result);
            result = Calculator.Calculate("3.3E2 * 2 - 3");
            Assert.Equal("657", result);
            result = Calculator.Calculate("3.3E2 * 2 - 3 * 7");
            Assert.Equal("639", result);
            result = Calculator.Calculate("3.3E2 - 2 * 3 + 7");
            Assert.Equal("331", result);
        }

        [Fact]
        public void TestUnaryMinusExpressions()
        {
            string result = Calculator.Calculate("-3.3E2 - (2 + 3)");
            Assert.Equal("-335", result);
            result = Calculator.Calculate("-3000 * -(100 - 30)");
            Assert.Equal("210000", result);
            result = Calculator.Calculate("3000 - -(100 - 30)");
            Assert.Equal("3070", result);
        }
    }
}
