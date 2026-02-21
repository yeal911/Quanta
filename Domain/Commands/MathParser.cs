// ============================================================================
// 文件名: MathParser.cs
// 文件描述: 数学表达式解析器（递归下降），支持 +、-、*、/、%、^ 和括号
//           优先级（由低到高）：加减 < 乘除模 < 幂 < 一元符号 < 括号/数字
// ============================================================================

namespace Quanta.Services;

/// <summary>
/// 数学表达式解析器，使用递归下降算法解析数学表达式。
/// 支持 +、-、*、/、%、^ 运算符和括号。
/// </summary>
internal static class MathParser
{
    /// <summary>
    /// 解析并计算数学表达式
    /// </summary>
    /// <param name="expression">数学表达式字符串</param>
    /// <returns>计算结果</returns>
    public static double Evaluate(string expression)
    {
        string expr = expression.Replace(" ", "");
        int pos = 0;
        double result = ParseAddSub(expr, ref pos);
        if (pos != expr.Length)
            throw new FormatException($"Unexpected character '{expr[pos]}' at position {pos}");
        return result;
    }

    private static double ParseAddSub(string expr, ref int pos)
    {
        double result = ParseMulDiv(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-'))
        {
            char op = expr[pos++];
            double right = ParseMulDiv(expr, ref pos);
            result = op == '+' ? result + right : result - right;
        }
        return result;
    }

    private static double ParseMulDiv(string expr, ref int pos)
    {
        double result = ParsePow(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '*' || expr[pos] == '/' || expr[pos] == '%'))
        {
            char op = expr[pos++];
            double right = ParsePow(expr, ref pos);
            result = op == '*' ? result * right
                   : op == '/' ? result / right
                   : result % right;
        }
        return result;
    }

    private static double ParsePow(string expr, ref int pos)
    {
        double result = ParseUnary(expr, ref pos);
        if (pos < expr.Length && expr[pos] == '^')
        {
            pos++;
            double exp = ParsePow(expr, ref pos);
            result = Math.Pow(result, exp);
        }
        return result;
    }

    private static double ParseUnary(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '-') { pos++; return -ParseFactor(expr, ref pos); }
        if (pos < expr.Length && expr[pos] == '+') { pos++; }
        return ParseFactor(expr, ref pos);
    }

    private static double ParseFactor(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++;
            double val = ParseAddSub(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')') pos++;
            return val;
        }
        int start = pos;
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.')) pos++;
        if (pos == start) throw new FormatException($"Expected number at position {pos}");
        return double.Parse(expr.Substring(start, pos - start),
                            System.Globalization.CultureInfo.InvariantCulture);
    }
}
