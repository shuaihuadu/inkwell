// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

public class AzureOpenAICredential
{
    public string Endpoint { get; init; } = string.Empty;

    public string? ApiKey { get; init; }

    public string DeploymentName { get; init; } = string.Empty;
}