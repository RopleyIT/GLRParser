using CalculatorDemo;
using Xunit;

namespace CalculatorDemoTest
{
    public class GLRCalcTests
    {
        [Fact]
        public void TestGLRIntExpressions()
        {
            string result = GLRCalculator.Calculate("3 * 2");
            Assert.Equal("6", result);
            result = GLRCalculator.Calculate("3 - 1");
            Assert.Equal("2", result);
        }

        [Fact]
        public void TestGLRSimpleExpressions()
        {
            string result = GLRCalculator.Calculate("3.3E2 * 2");
            Assert.Equal("660", result);
            result = GLRCalculator.Calculate("3000 - 100");
            Assert.Equal("2900", result);
        }

        [Fact]
        public void TestGLRTripleExpressions()
        {
            string result = GLRCalculator.Calculate("3.3E2 - 2 + 3");
            Assert.Equal("331", result);
            result = GLRCalculator.Calculate("3000 * 100 / 30");
            Assert.Equal("10000", result);
        }

        [Fact]
        public void TestGLRParenExpressions()
        {
            string result = GLRCalculator.Calculate("3.3E2 - (2 + 3)");
            Assert.Equal("325", result);
            result = GLRCalculator.Calculate("3000 * (100 - 30)");
            Assert.Equal("210000", result);
        }

        [Fact]
        public void TestGLRPrecedenceExpressions()
        {
            string result = GLRCalculator.Calculate("3.3E2 - 2 * 3");
            Assert.Equal("324", result);
            result = GLRCalculator.Calculate("3.3E2 * 2 - 3");
            Assert.Equal("657", result);
            result = GLRCalculator.Calculate("3.3E2 * 2 - 3 * 7");
            Assert.Equal("639", result);
            result = GLRCalculator.Calculate("3.3E2 - 2 * 3 + 7");
            Assert.Equal("331", result);
        }

        [Fact]
        public void TestGLRUnaryMinusExpressions()
        {
            string result = GLRCalculator.Calculate("-3.3E2 - (2 + 3)");
            Assert.Equal("-335", result);
            result = GLRCalculator.Calculate("-3000 * -(100 - 30)");
            Assert.Equal("210000", result);
            result = GLRCalculator.Calculate("3000 - -(100 - 30)");
            Assert.Equal("3070", result);
        }
    }
}
