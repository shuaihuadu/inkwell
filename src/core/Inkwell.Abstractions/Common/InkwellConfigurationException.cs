// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 通用配置错误：Options 校验失败、Builder 装配冲突等。
/// </summary>
public class InkwellConfigurationException : Exception
{
    /// <summary>
    /// 使用指定错误消息初始化异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public InkwellConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public InkwellConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
