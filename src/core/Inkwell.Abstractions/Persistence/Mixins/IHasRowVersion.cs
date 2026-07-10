namespace Inkwell;

/// <summary>
/// 实现该 mixin 的业务 Model 会被 EFCore base 自动加 RowVersion 列并启用乐观并发控制。
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; init; }
}
