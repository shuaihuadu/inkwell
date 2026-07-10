// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 队列消息信封。<see cref="TraceParent"/> 满足 RISK-015 跨进程（WebApi → Worker）trace 不断链要求。
/// </summary>
public sealed record class MessageEnvelope<T>(
    string MessageId,
    T Payload,
    DateTimeOffset EnqueuedTime,
    int DeliveryCount,
    string? TraceParent);
