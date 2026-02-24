// ============================================================================
// 文件名: MathParser.cs
// 文件描述: 数学表达式解析器（递归下降），支持基础算术、常用函数与常量。
//           优先级（由低到高）：加减 < 乘除模 < 幂 < 一元符号 < 括号/函数/数字
// ============================================================================

namespace Quanta.Services;

/// <summary>
/// 数学表达式解析器，使用递归下降算法解析数学表达式。
/// 支持 +、-、*、/、%、^、括号、科学计数法，以及常用数学函数/常量。
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
        string expr = RemoveWhitespace(expression);
        if (expr.Length == 0)
            throw new FormatException("Expression is empty");

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
            if (pos >= expr.Length || expr[pos] != ')')
                throw new FormatException($"Missing ')' at position {pos}");

            pos++;
            return val;
        }

        if (pos < expr.Length && char.IsLetter(expr[pos]))
            return ParseIdentifier(expr, ref pos);

        return ParseNumber(expr, ref pos);
    }

    private static double ParseIdentifier(string expr, ref int pos)
    {
        int start = pos;
        while (pos < expr.Length && (char.IsLetterOrDigit(expr[pos]) || expr[pos] == '_')) pos++;
        string name = expr.Substring(start, pos - start).ToLowerInvariant();

        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++; // (
            var args = new List<double>();
            if (pos < expr.Length && expr[pos] != ')')
            {
                while (true)
                {
                    args.Add(ParseAddSub(expr, ref pos));
                    if (pos < expr.Length && expr[pos] == ',')
                    {
                        pos++;
                        continue;
                    }
                    break;
                }
            }

            if (pos >= expr.Length || expr[pos] != ')')
                throw new FormatException($"Missing ')' for function '{name}' at position {pos}");

            pos++; // )
            return EvaluateFunction(name, args);
        }

        return name switch
        {
            "pi" => Math.PI,
            "e" => Math.E,
            _ => throw new FormatException($"Unknown identifier '{name}' at position {start}")
        };
    }

    private static double EvaluateFunction(string name, List<double> args)
    {
        return name switch
        {
            // P1
            "abs" => RequireArgs(name, args, 1, a => Math.Abs(a[0])),
            "sign" => RequireArgs(name, args, 1, a => Math.Sign(a[0])),
            "floor" => RequireArgs(name, args, 1, a => Math.Floor(a[0])),
            "ceil" => RequireArgs(name, args, 1, a => Math.Ceiling(a[0])),
            "round" => RequireArgs(name, args, 1, a => Math.Round(a[0], MidpointRounding.AwayFromZero)),
            "min" => RequireMinArgs(name, args, 2, a => a.Min()),
            "max" => RequireMinArgs(name, args, 2, a => a.Max()),

            // P2
            "sqrt" => RequireArgs(name, args, 1, a => Math.Sqrt(a[0])),
            "log" => args.Count switch
            {
                1 => Math.Log(args[0]),
                2 => Math.Log(args[0], args[1]),
                _ => throw new FormatException($"Function '{name}' expects 1 or 2 arguments, got {args.Count}")
            },
            "ln" => RequireArgs(name, args, 1, a => Math.Log(a[0])),
            "log10" => RequireArgs(name, args, 1, a => Math.Log10(a[0])),
            "sin" => RequireArgs(name, args, 1, a => Math.Sin(a[0])),
            "cos" => RequireArgs(name, args, 1, a => Math.Cos(a[0])),
            "tan" => RequireArgs(name, args, 1, a => Math.Tan(a[0])),
            "asin" => RequireArgs(name, args, 1, a => Math.Asin(a[0])),
            "acos" => RequireArgs(name, args, 1, a => Math.Acos(a[0])),
            "atan" => RequireArgs(name, args, 1, a => Math.Atan(a[0])),
            "rad" => RequireArgs(name, args, 1, a => a[0] * Math.PI / 180d),
            "deg" => RequireArgs(name, args, 1, a => a[0] * 180d / Math.PI),

            _ => throw new FormatException($"Unknown function '{name}'")
        };
    }

    private static double RequireArgs(string name, List<double> args, int expected, Func<List<double>, double> evaluator)
    {
        if (args.Count != expected)
            throw new FormatException($"Function '{name}' expects {expected} arguments, got {args.Count}");
        return evaluator(args);
    }

    private static double RequireMinArgs(string name, List<double> args, int min, Func<List<double>, double> evaluator)
    {
        if (args.Count < min)
            throw new FormatException($"Function '{name}' expects at least {min} arguments, got {args.Count}");
        return evaluator(args);
    }

    private static double ParseNumber(string expr, ref int pos)
    {
        int start = pos;
        bool seenDigit = false;

        while (pos < expr.Length && char.IsDigit(expr[pos]))
        {
            seenDigit = true;
            pos++;
        }

        if (pos < expr.Length && expr[pos] == '.')
        {
            pos++;
            while (pos < expr.Length && char.IsDigit(expr[pos]))
            {
                seenDigit = true;
                pos++;
            }
        }

        if (!seenDigit)
            throw new FormatException($"Expected number at position {start}");

        // scientific notation: 1e-3
        if (pos < expr.Length && (expr[pos] == 'e' || expr[pos] == 'E'))
        {
            int expPos = pos;
            pos++;
            if (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-')) pos++;

            int expStart = pos;
            while (pos < expr.Length && char.IsDigit(expr[pos])) pos++;
            if (pos == expStart)
                throw new FormatException($"Invalid exponent at position {expPos}");
        }

        return double.Parse(expr.Substring(start, pos - start),
                            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RemoveWhitespace(string input)
    {
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (char ch in input)
        {
            if (!char.IsWhiteSpace(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }
}
