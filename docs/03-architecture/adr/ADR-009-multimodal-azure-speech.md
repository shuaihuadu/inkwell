---
id: ADR-009-multimodal-azure-speech
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-002
  - ADR-005
downstream: []
---

# ADR-009 多模态：Azure Speech 后端 ASR + 模型能力清单

## 上下文

[REQ-016 多模态](../../01-requirements/requirements.md) 要求支持："文本、麦克风语音输入、文件上传"三种输入；输出维持纯文本（v1 不实现 TTS）。

[OQ-003 closed §A](../../01-requirements/open-questions.md) 已锁"语音由后端 [Azure AI Speech](https://learn.microsoft.com/azure/ai-services/speech-service/) 做 ASR；图像 / 文件理解直接走支持多模态的 LLM"。

[EX-004 多模态降级](../../01-requirements/requirements.md) 要求："麦克风权限被拒 / Azure Speech 不可用时降级为文本输入"。

[ADR-005](./ADR-005-deployment-docker-compose-aks.md) 锁定 Azure 平台，Azure Speech 凭据通过 Kubernetes Secret 注入（[OQ-A006 closed §B](../open-questions-arch.md)：v1 不引入 Key Vault；详见 [RISK-013](../risk-analysis.md)）。

## 决策

**语音转文本（ASR）走 Azure AI Speech；图像 / PDF / Office 文件理解走支持多模态的 LLM（Azure OpenAI GPT-4o / GPT-5 类模型）；前端只做"录音 → 后端"的传输，不在客户端做 ASR。**

- 客户端：`MediaRecorder` API 采集 audio/webm；分块（每 200 ms）上传到后端；后端聚合后转 PCM 16 kHz 送 Azure Speech 流式接口。
- 后端：[Azure.AI.Speech SDK](https://www.nuget.org/packages/Microsoft.CognitiveServices.Speech) 封装在 `Inkwell.Multimodal.Speech` 模块；中文为默认识别语言（[OQ-015](../../01-requirements/open-questions.md) 已锁 zh-CN）。
- 文件输入：客户端上传到后端 [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md)（在当前部署下按配置路由到 LocalFileSystem / Azure Blob / MinIO 三 Provider 之一），得到预签名下载 URL；后端把该 URL 作为多模态消息的 image_url / file 元素传给 Azure OpenAI（仅当模型支持 vision 能力）。
- 降级（[EX-004](../../01-requirements/requirements.md)）：
  - 麦克风权限拒：UI 把麦克风按钮置灰 + tooltip 提示原因
  - Azure Speech 失败：UI 出现 toast "语音转文字失败，请改用文本输入"，输入框保持文本可用
- v1 不实现 TTS（[OQ-003](../../01-requirements/open-questions.md)）；不实现客户端本地 ASR。

## 备选项

### 备选 A（OQ-003 §B 模型多模态全包）：直接把 audio 文件丢给多模态 LLM 让模型自己 ASR

- **放弃理由**：(1) Azure OpenAI GPT-4o-audio 仍在 preview，不是稳定 GA 能力 — 不能作为 v1 唯一通道。(2) Azure OpenAI audio 单次调用计费高于 Azure Speech ASR 数倍。(3) 流式 ASR + 中间结果展示是 v1 体验关键，模型路径目前不支持 partial transcript。

### 备选 B（OQ-003 §C 双路并存）：Azure Speech 主路 + 模型多模态备路

- **放弃理由**：(1) 双路实现成本 ≈ 主路的 1.5 倍（路由 / 切换 / 监控）；(2) v1 没有用户场景需要双路 — Azure Speech 在 zh-CN 上经过广泛验证；(3) 等 Azure OpenAI audio GA 后再做。

### 备选 C：客户端 [Whisper.cpp](https://github.com/ggerganov/whisper.cpp) 本地 ASR

- **放弃理由**：(1) 客户端模型分发增加 [ADR-001](./ADR-001-client-runtime-electron-react.md) 包大小（Whisper small ≈ 460 MB）；(2) 客户端 CPU 占用高，对 macOS 风扇 / Windows 笔记本电池不友好；(3) 模型版本更新需要客户端升级，比后端切版本慢；(4) v1 NFR-001 已声明"必须联网"，不存在离线场景需求。

## 后果

### 正面

- 后端集中管理 ASR 凭据 → 客户端不需要 Speech SDK / 凭据，[NFR-006 安全](../../01-requirements/requirements.md) 收益明显。
- Azure Speech 的 zh-CN ASR 准确度业界领先，符合 [OQ-015](../../01-requirements/open-questions.md) v1 仅 zh-CN 语境。
- 客户端只做 `MediaRecorder` 录音 + 简单 audio 流上传，[ADR-001 Electron](./ADR-001-client-runtime-electron-react.md) 主进程 / 渲染进程 IPC 边界清晰。
- 多模态文件理解走"上传到 Blob → 给模型 SAS URL"的标准模式，与现有 OpenAI vision API 完全兼容。

### 负面

- Azure Speech 是按调用计费的服务，需要监控用量与告警；H3 / H4 详细设计需要把"调用量 + 时长"加入审计字段。详见 [RISK-005](../risk-analysis.md)。
- 不支持离线 / 隔离网络部署的客户 → 这类客户场景 v1 不在范围（[OQ-006 closed §A](../../01-requirements/open-questions.md)）。
- 客户端到后端的 audio 上传需要稳定网络 — [NFR-001](../../01-requirements/requirements.md) 已声明"必须联网"覆盖。

### 中性

- 文件理解能力受限于所选 LLM 是否支持 vision 输入；UI 应该在用户切换到非 vision 模型时禁用上传按钮（[H3 详细设计 UI 状态机](../../04-detailed-design/) 任务）。
- 麦克风录音格式（audio/webm Opus）转 PCM 16 kHz 在后端做，CPU 占用可接受。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-005](./ADR-005-deployment-docker-compose-aks.md) / [OQ-003](../../01-requirements/open-questions.md)
- **置信度**：high（OQ-003 closed；Azure Speech 在 zh-CN 上经过广泛验证）
