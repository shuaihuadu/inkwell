// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// Builder DSL 装配期异常：Provider 重复注册 / 缺失必需 Provider 等。
/// </summary>
public class InkwellBuilderException : Exception
{
    public InkwellBuilderException(string message) : base(message)
    {
    }

    public InkwellBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
