using System.Buffers.Text;
using System.Security.Cryptography;

namespace Inkwell;

/// <summary>生成不可预测的随机会话 Token（供 <c>ICacheProvider</c> key 使用）。</summary>
internal static class SessionTokenGenerator
{
    private const int EntropyBytes = 32;

    public static string Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(EntropyBytes);

        return Base64Url.EncodeToString(bytes);
    }
}
