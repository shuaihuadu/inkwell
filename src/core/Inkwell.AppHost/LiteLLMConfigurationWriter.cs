// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Inkwell.AppHost;

/// <summary>
/// 为本地 LiteLLM 容器生成不包含明文凭据的启动配置。
/// </summary>
internal static partial class LiteLLMConfigurationWriter
{
    /// <summary>
    /// 验证模型配置并将 LiteLLM YAML 写入指定路径。
    /// </summary>
    /// <param name="path">输出文件路径。</param>
    /// <param name="models">需要预置的模型部署。</param>
    internal static void Write(string path, IReadOnlyList<LiteLLMBootstrapModelOptions> models)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(models);

        List<Dictionary<string, object>> modelList = [];
        foreach (LiteLLMBootstrapModelOptions model in models)
        {
            Validate(model);

            Dictionary<string, object> parameters = new(StringComparer.Ordinal)
            {
                ["model"] = model.Model,
                ["api_key"] = $"os.environ/{model.ApiKeyEnvironmentVariable}",
            };
            if (!string.IsNullOrWhiteSpace(model.ApiBaseEnvironmentVariable))
            {
                parameters["api_base"] = $"os.environ/{model.ApiBaseEnvironmentVariable}";
            }

            if (!string.IsNullOrWhiteSpace(model.ApiVersion))
            {
                parameters["api_version"] = model.ApiVersion;
            }

            modelList.Add(new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["model_name"] = model.Name,
                ["litellm_params"] = parameters,
            });
        }

        Dictionary<string, object> configuration = new(StringComparer.Ordinal)
        {
            ["model_list"] = modelList,
            ["litellm_settings"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["drop_params"] = true,
            },
        };
        string yaml = new SerializerBuilder().Build().Serialize(configuration);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, yaml);
    }

    /// <summary>
    /// 验证单个模型部署配置。
    /// </summary>
    /// <param name="model">模型部署配置。</param>
    private static void Validate(LiteLLMBootstrapModelOptions model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ApiKeyParameter);
        ArgumentException.ThrowIfNullOrWhiteSpace(model.ApiKeyEnvironmentVariable);
        if (!EnvironmentVariableNameRegex().IsMatch(model.ApiKeyEnvironmentVariable))
        {
            throw new InvalidOperationException(
                $"LiteLLM model '{model.Name}' has an invalid API Key environment variable name.");
        }

        bool hasApiBaseParameter = !string.IsNullOrWhiteSpace(model.ApiBaseParameter);
        bool hasApiBaseEnvironmentVariable = !string.IsNullOrWhiteSpace(model.ApiBaseEnvironmentVariable);
        if (hasApiBaseParameter != hasApiBaseEnvironmentVariable)
        {
            throw new InvalidOperationException(
                $"LiteLLM model '{model.Name}' must configure both API Base parameter and environment variable names.");
        }

        if (hasApiBaseEnvironmentVariable
            && !EnvironmentVariableNameRegex().IsMatch(model.ApiBaseEnvironmentVariable!))
        {
            throw new InvalidOperationException(
                $"LiteLLM model '{model.Name}' has an invalid API Base environment variable name.");
        }
    }

    /// <summary>
    /// 获取跨平台环境变量名称校验表达式。
    /// </summary>
    /// <returns>环境变量名称校验表达式。</returns>
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentVariableNameRegex();
}