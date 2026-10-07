// ============================================================================
// 文件名: MathParserException.cs
// 文件描述: 数学表达式解析器异常。当表达式嵌套深度超过 MathParser 的上限时抛出。
//           与无法捕获的 StackOverflowException 不同，本异常可被上层 catch 正常
//           拦截，从而将错误转为界面提示项而不是导致整个进程崩溃。
// ============================================================================

namespace Quanta.Domain.Commands;

/// <summary>
/// 数学表达式解析器异常。表达式嵌套深度（括号/函数调用/幂运算层级）超过
/// 解析器上限时抛出，由上层调用方捕获并转为错误结果项展示。
/// </summary>
public class MathParserException : InvalidOperationException
{
    /// <summary>默认构造函数</summary>
    public MathParserException() { }

    /// <summary>使用指定错误消息构造异常</summary>
    /// <param name="message">错误消息</param>
    public MathParserException(string message) : base(message) { }
}
