namespace Inkwell;

/// <summary>
/// 通用配置错误：Options 校验失败、Builder 装配冲突等。
/// </summary>
public class InkwellConfigurationException : Exception
{
    public InkwellConfigurationException(string message) : base(message)
    {
    }

    public InkwellConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
