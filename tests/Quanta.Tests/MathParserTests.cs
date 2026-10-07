// ============================================================================
// 文件名: MathParserTests.cs
// 文件描述: 数学表达式解析器单元测试。
//           覆盖常规表达式求值、嵌套深度限制边界（超深嵌套抛可捕获异常而非栈溢出）、
//           错误输入处理。
// ============================================================================

using Quanta.Domain.Commands;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// <see cref="MathParser"/> 的单元测试。
/// </summary>
public class MathParserTests
{
    [Theory]
    [InlineData("1+2", 3)]
    [InlineData("2*3", 6)]
    [InlineData("1+2*3", 7)]            // 乘法优先于加法
    [InlineData("(1+2)*3", 9)]          // 括号改变优先级
    [InlineData("7/2", 3.5)]
    [InlineData("10%3", 1)]
    [InlineData("2^10", 1024)]
    [InlineData("2^3^2", 512)]          // 幂运算右结合：2^(3^2)
    [InlineData("-5+3", -2)]
    [InlineData("2*-3", -6)]            // 一元负号
    [InlineData(" 1 + 2 ", 3)]          // 空白容忍
    [InlineData("1e-3", 0.001)]         // 科学计数法
    [InlineData("1.5e2", 150)]
    [InlineData("pi", 3.141592653589793)]
    [InlineData("e", 2.718281828459045)]
    [InlineData("sqrt(16)", 4)]
    [InlineData("abs(-3)", 3)]
    [InlineData("sign(-2)", -1)]
    [InlineData("floor(1.7)", 1)]
    [InlineData("ceil(1.2)", 2)]
    [InlineData("round(2.5)", 3)]       // AwayFromZero
    [InlineData("max(1,5,3)", 5)]
    [InlineData("min(2,7)", 2)]
    [InlineData("log(8,2)", 3)]
    [InlineData("log(e)", 1)]
    [InlineData("log10(1000)", 3)]
    [InlineData("sin(0)", 0)]
    [InlineData("cos(0)", 1)]
    [InlineData("rad(180)", 3.141592653589793)]
    [InlineData("deg(pi)", 180)]
    public void Evaluate_ComputesStandardExpressions(string expression, double expected)
    {
        Assert.Equal(expected, MathParser.Evaluate(expression), precision: 10);
    }

    [Fact]
    public void Evaluate_HandlesNesting_JustUnderDepthLimit()
    {
        // 100 层括号嵌套仍在上限内，正常求值
        var expr = new string('(', 100) + "1" + new string(')', 100);
        Assert.Equal(1, MathParser.Evaluate(expr), precision: 10);
    }

    [Fact]
    public void Evaluate_DeepParenNesting_ThrowsCatchableException()
    {
        // P0 修复回归点：超深嵌套不再触发无法捕获的 StackOverflowException，
        // 而是抛出可捕获的 MathParserException
        var expr = new string('(', 10_000) + "1" + new string(')', 10_000);
        Assert.Throws<MathParserException>(() => MathParser.Evaluate(expr));
    }

    [Fact]
    public void Evaluate_DeepFunctionNesting_ThrowsCatchableException()
    {
        var expr = string.Concat(Enumerable.Repeat("sin(", 10_000)) + "0" + new string(')', 10_000);
        Assert.Throws<MathParserException>(() => MathParser.Evaluate(expr));
    }

    [Fact]
    public void Evaluate_DeepPowChain_ThrowsCatchableException()
    {
        var expr = "2" + string.Concat(Enumerable.Repeat("^2", 10_000));
        Assert.Throws<MathParserException>(() => MathParser.Evaluate(expr));
    }

    [Theory]
    [InlineData("")]          // 空表达式
    [InlineData("   ")]       // 仅空白
    [InlineData("1+")]        // 缺右操作数
    [InlineData("(1+2")]      // 缺右括号
    [InlineData("1+2)")]      // 多余字符
    [InlineData("foo")]       // 未知标识符
    [InlineData("foo(1)")]    // 未知函数
    [InlineData("abs()")]     // 参数个数不符
    [InlineData("1..2")]      // 非法数字
    [InlineData("1e")]        // 非法指数
    public void Evaluate_ThrowsFormatException_ForInvalidInput(string expression)
    {
        Assert.Throws<FormatException>(() => MathParser.Evaluate(expression));
    }
}
