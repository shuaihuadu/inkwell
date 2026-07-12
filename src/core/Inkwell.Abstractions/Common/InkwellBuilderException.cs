// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Builder DSL 装配期异常：Provider 重复注册 / 缺失必需 Provider 等。
/// </summary>
public class InkwellBuilderException : Exception
{
    /// <summary>
    /// 使用指定错误消息初始化异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public InkwellBuilderException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public InkwellBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
