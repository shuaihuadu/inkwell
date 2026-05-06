#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Harness Engineering · 卸载脚本（基于 manifest）。

.DESCRIPTION
    根据 <TargetRepo>/.harness-engineering/manifest.json 反向移除安装时落下的文件。

    安全策略：
    - 优先用 manifest 精确卸载（按 sha256 比对）。
    - 文件已不存在 → 跳过（幂等）。
    - 文件存在且 sha256 与 manifest 一致 → 默认删除（用户未修改）。
    - 文件存在但 sha256 不一致 → 默认保留并列出告警，须加 -Force 才删。
    - 自下而上清理只剩空的目录；非空目录绝不递归删。
    - 全程支持 -DryRun。

    Manifest 缺失时的兜底（残留清理）：
    - 例如 install 中途按 Ctrl+C，manifest 还没写入但 .harness-engineering/
      或 .github/ 下已经有部分文件落地。
    - 此时脚本不报错退出，而是启动 best-effort 残留扫描：
      * 整体清理 <TargetRepo>/.harness-engineering/ 目录（纯属本工具产物）。
      * 对 .github/copilot-instructions.md / .github/instructions/*.instructions.md /
        .github/agents/*.agent.md，仅当文件内含 "Harness Engineering" 标记时才删。
      * 交互模式下会列出候选并请求确认；非交互模式必须显式 -Force 才执行删除。

.PARAMETER TargetRepo
    采用方仓库根目录的绝对路径。
    省略时：若脚本位于 <TargetRepo>/.harness-engineering/ 下（即装到目标的副本），自动取脚本所在目录的父目录；
    否则报错要求显式传入。

.PARAMETER Force
    对内容已被本地修改的文件也执行删除；在 manifest 缺失的兜底路径下，等价于
    "不再需要交互确认，直接删除全部候选"。

.PARAMETER DryRun
    只打印将要执行的动作，不写盘。

.EXAMPLE
    # 源仓库直接卸载某采用方
    ./uninstall.ps1 -TargetRepo D:\Path\To\YourRepo
    # 在采用方仓库内一键卸载（脚本已被装到 .harness-engineering/ 下）
    pwsh -File .\.harness-engineering\uninstall.ps1
#>

[CmdletBinding()]
param(
    [string]$TargetRepo,

    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# TargetRepo 智能默认：若脚本位于 .harness-engineering/ 内，取上级目录
if (-not $TargetRepo) {
    $hereLeaf = Split-Path -Leaf $PSScriptRoot
    if ($hereLeaf -eq '.harness-engineering') {
        $TargetRepo = Split-Path -Parent $PSScriptRoot
    }
    else {
        throw '请用 -TargetRepo <path> 指定采用方仓库根，或把脚本放到 <TargetRepo>/.harness-engineering/ 下再运行'
    }
}

if (-not (Test-Path $TargetRepo -PathType Container)) {
    throw "TargetRepo 不存在或不是目录：$TargetRepo"
}
$TargetRepo = (Resolve-Path $TargetRepo).Path

$manifestDir = Join-Path $TargetRepo '.harness-engineering'
$manifestPath = Join-Path $manifestDir 'manifest.json'

# 如果脚本是在 .harness-engineering\ 里被启动的，这个目录会被记录为进程的
# 当前工作目录，最后一步删自身会被 Windows 拒以 "文件正在被使用"。
# 提前切出去（同时同步进程级 cwd）。
$startCwd = (Get-Location).ProviderPath
if ($startCwd -like "$manifestDir*") {
    Set-Location -LiteralPath $TargetRepo
    [System.Environment]::CurrentDirectory = $TargetRepo
}

# ----------------------------------------------------------------------------
# 公共：sha256 / 残留清理（manifest 缺失时的兜底）
# ----------------------------------------------------------------------------
function Get-Sha256Hex([string]$Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

# 已渲染产物中始终包含 "Harness Engineering" 字面量（来自模板的 H1 / 引用脚注），
# 用作残留清理的归属标记，避免误删用户写的同名文件。
function Test-HarnessMarker([string]$Path) {
    if (-not (Test-Path $Path -PathType Leaf)) { return $false }
    try {
        $content = Get-Content -LiteralPath $Path -Raw -ErrorAction Stop
        return ($content -match 'Harness Engineering' -or $content -match 'harness-engineering')
    }
    catch { return $false }
}

function Invoke-BestEffortCleanup {
    param(
        [string]$TargetRepo,
        [string]$ManifestDir,
        [bool]$DryRun,
        [bool]$Force
    )

    Write-Host ''
    Write-Host '[!] 未发现 manifest.json：' -ForegroundColor Yellow -NoNewline
    Write-Host " $ManifestDir\manifest.json"
    Write-Host '    安装可能在写入 manifest 前被中断（如 Ctrl+C）。' -ForegroundColor Yellow
    Write-Host '    启动 best-effort 残留扫描 / Best-effort residual scan。' -ForegroundColor Yellow

    $candidates = New-Object System.Collections.Generic.List[object]

    # 1) .harness-engineering/ 目录：完全是本工具产物，整目录可删
    if (Test-Path $ManifestDir -PathType Container) {
        $entry = New-Object PSObject -Property @{
            Kind   = 'dir'
            Path   = $ManifestDir
            Rel    = '.harness-engineering/'
            Reason = '整目录（本工具专属）'
            Safe   = $true
        }
        [void]$candidates.Add($entry)
    }

    # 2) .github/ 下的已知文件名 —— 必须含 Harness 标记才视为本工具产物
    $githubKnown = @(
        '.github/copilot-instructions.md'
    )
    foreach ($glob in @('.github/instructions/*.instructions.md', '.github/agents/*.agent.md')) {
        $dir = Join-Path $TargetRepo (Split-Path -Parent $glob)
        if (Test-Path $dir -PathType Container) {
            $pattern = Split-Path -Leaf $glob
            foreach ($f in Get-ChildItem -LiteralPath $dir -Filter $pattern -File -ErrorAction SilentlyContinue) {
                $rel = (Resolve-Path $f.FullName).Path.Substring($TargetRepo.Length).TrimStart('\', '/')
                $githubKnown += ($rel -replace '\\', '/')
            }
        }
    }
    foreach ($rel in $githubKnown | Select-Object -Unique) {
        $abs = Join-Path $TargetRepo $rel
        if (-not (Test-Path $abs -PathType Leaf)) { continue }
        $hasMarker = Test-HarnessMarker $abs
        if ($hasMarker) { $reasonText = '含 Harness 标记 ✓' } else { $reasonText = '无标记 (需 -Force)' }
        $entry = New-Object PSObject -Property @{
            Kind   = 'file'
            Path   = $abs
            Rel    = $rel
            Reason = $reasonText
            Safe   = $hasMarker
        }
        [void]$candidates.Add($entry)
    }

    if ($candidates.Count -eq 0) {
        Write-Host ''
        Write-Host '    未发现任何残留 / No residual files found.' -ForegroundColor Green
        return
    }

    Write-Host ''
    Write-Host '    候选 / Candidates:' -ForegroundColor Cyan
    foreach ($c in $candidates) {
        if ($c.Safe) { $color = 'Gray' } else { $color = 'Yellow' }
        Write-Host ("      [{0}] {1,-55} {2}" -f $c.Kind, $c.Rel, $c.Reason) -ForegroundColor $color
    }

    # 确认：交互模式默认询问；-Force 跳过询问；非交互且非 Force → 仅打印不执行
    $interactive = -not [System.Console]::IsInputRedirected
    $proceed = $false
    if ($DryRun) {
        $proceed = $false  # DryRun 下走预览路径
    }
    elseif ($Force) {
        $proceed = $true
    }
    elseif ($interactive) {
        Write-Host ''
        $ans = Read-Host '继续删除上述候选？/ Proceed? [y/N]'
        $proceed = ($ans -and ($ans.Trim().ToLowerInvariant() -in @('y', 'yes')))
    }
    else {
        Write-Host ''
        Write-Host '    非交互模式且未传 -Force：仅预览，不执行删除。' -ForegroundColor Yellow
        Write-Host '    Non-interactive without -Force: preview only.' -ForegroundColor Yellow
    }

    $deleted = 0
    $kept = 0
    foreach ($c in $candidates) {
        $shouldDelete = $c.Safe -or $Force
        $rel = $c.Rel
        if (-not $shouldDelete) {
            Write-Host "   keep   $rel (无 Harness 标记，使用 -Force 强制删除)" -ForegroundColor Yellow
            $kept++
            continue
        }
        if ($DryRun -or -not $proceed) {
            Write-Host "   dryrun-delete $rel" -ForegroundColor DarkGray
            continue
        }
        if ($c.Kind -eq 'dir') {
            Remove-Item -LiteralPath $c.Path -Recurse -Force
            Write-Host "   delete $rel (recursive)" -ForegroundColor Magenta
        }
        else {
            Remove-Item -LiteralPath $c.Path -Force
            Write-Host "   delete $rel" -ForegroundColor Magenta
            $parent = Split-Path -Parent $c.Path
            while ($parent -and ($parent.Length -gt $TargetRepo.Length) -and (Test-Path $parent -PathType Container)) {
                $items = @(Get-ChildItem -LiteralPath $parent -Force)
                if ($items.Count -gt 0) { break }
                Remove-Item -LiteralPath $parent -Force
                Write-Host "   rmdir  $parent" -ForegroundColor DarkMagenta
                $parent = Split-Path -Parent $parent
            }
        }
        $deleted++
    }

    Write-Host ''
    if ($DryRun) {
        Write-Host "DryRun 完成（best-effort）。预计删除 $($candidates.Count - $kept) / 保留 $kept" -ForegroundColor Cyan
    }
    elseif (-not $proceed) {
        Write-Host '已取消（未删除任何文件）/ Cancelled (nothing removed).' -ForegroundColor DarkYellow
    }
    else {
        Write-Host "完成（best-effort）。删除 $deleted / 保留 $kept" -ForegroundColor Cyan
    }
}

if (-not (Test-Path $manifestPath)) {
    Invoke-BestEffortCleanup -TargetRepo $TargetRepo -ManifestDir $manifestDir -DryRun:$DryRun -Force:$Force
    Write-Host ''
    return
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if (-not $manifest.files) {
    Write-Host "manifest 中无 files 记录，仅清理 manifest 自身。" -ForegroundColor DarkYellow
}

Write-Host ''
Write-Host "==> Harness Engineering · 卸载" -ForegroundColor Cyan
Write-Host "    目标仓库：$TargetRepo"
Write-Host "    manifest 版本：$($manifest.harness_version)（commit: $($manifest.harness_commit))"
Write-Host "    待处理文件数：$(@($manifest.files).Count)"
Write-Host ''

$deleted = 0
$skipped = 0
$kept = 0
$missing = 0
$modifiedKept = New-Object System.Collections.Generic.List[string]
$touchedDirs = New-Object System.Collections.Generic.HashSet[string]

foreach ($entry in @($manifest.files)) {
    $relPath = $entry.path
    $abs = Join-Path $TargetRepo $relPath

    if (-not (Test-Path $abs)) {
        Write-Host "   miss   $relPath (已不存在)" -ForegroundColor DarkGray
        $missing++
        continue
    }

    $currentSha = Get-Sha256Hex $abs
    $clean = ($currentSha -eq $entry.sha256)

    if (-not $clean -and -not $Force) {
        Write-Host "   keep   $relPath (本地已修改，使用 -Force 强制删除)" -ForegroundColor Yellow
        $modifiedKept.Add($relPath) | Out-Null
        $kept++
        continue
    }

    if ($DryRun) {
        Write-Host "   dryrun-delete $relPath" -ForegroundColor DarkGray
    }
    else {
        Remove-Item -LiteralPath $abs -Force
        Write-Host "   delete $relPath" -ForegroundColor Magenta
    }
    $deleted++
    $parent = Split-Path -Parent $abs
    if ($parent) { [void]$touchedDirs.Add($parent) }
}

# 自下而上清理空目录（仅清空，不递归非空）
if (-not $DryRun) {
    $sortedDirs = @($touchedDirs) | Sort-Object -Property Length -Descending
    foreach ($dir in $sortedDirs) {
        $cur = $dir
        while ($cur -and ($cur.Length -gt $TargetRepo.Length) -and (Test-Path $cur -PathType Container)) {
            $items = @(Get-ChildItem -LiteralPath $cur -Force)
            if ($items.Count -gt 0) { break }
            Remove-Item -LiteralPath $cur -Force
            Write-Host "   rmdir  $cur" -ForegroundColor DarkMagenta
            $cur = Split-Path -Parent $cur
        }
    }
}

# manifest 自身：用户未修改且未保留任何 entry → 删 manifest + 目录
$shouldRemoveManifest = ($modifiedKept.Count -eq 0)
if ($DryRun) {
    Write-Host ''
    Write-Host "DryRun 完成。删除 $deleted / 保留 $kept / 缺失 $missing" -ForegroundColor Cyan
}
else {
    # 写一行 uninstall 记录到 install.log（与 install.ps1 复用同一审计日志）
    $logPath = Join-Path $manifestDir 'install.log'
    $tsIso = (Get-Date).ToString('o')
    $commitTag = if ($manifest.harness_commit) { $manifest.harness_commit } else { 'unknown' }
    $logLine = "[$tsIso] uninstall · harness@$commitTag · deleted=$deleted · kept=$kept · missing=$missing`n"
    if (Test-Path $manifestDir -PathType Container) {
        [System.IO.File]::AppendAllText($logPath, $logLine, [System.Text.UTF8Encoding]::new($false))
    }

    if ($shouldRemoveManifest) {
        Remove-Item -LiteralPath $manifestPath -Force
        Write-Host "   delete .harness-engineering/manifest.json" -ForegroundColor Magenta
        # install.log 是审计日志、不在 manifest 里；clean uninstall 时一并移除以让目录归零
        if (Test-Path $logPath -PathType Leaf) {
            Remove-Item -LiteralPath $logPath -Force
            Write-Host "   delete .harness-engineering/install.log" -ForegroundColor Magenta
        }
        if ((Test-Path $manifestDir) -and -not @(Get-ChildItem -LiteralPath $manifestDir -Force)) {
            try {
                Remove-Item -LiteralPath $manifestDir -Force -ErrorAction Stop
                Write-Host "   rmdir  .harness-engineering" -ForegroundColor DarkMagenta
            }
            catch {
                # 例如脚本是从 .harness-engineering\ 内部被启动的，进程仍持有该 cwd 句柄，
                # 内核会拒绝删除。此时不报错退出，只提示手动收尾。
                Write-Host "   [!] .harness-engineering/ 仍被本脚本进程占用，无法自动删除" -ForegroundColor Yellow
                Write-Host "       退出后手动执行：" -ForegroundColor Yellow
                Write-Host "         cd '$TargetRepo'; Remove-Item .harness-engineering -Force" -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host ''
        Write-Host "[!] 以下文件被本地修改过，已保留；manifest 与 install.log 也保留：" -ForegroundColor Yellow
        foreach ($p in $modifiedKept) { Write-Host "    $p" }
        Write-Host '    若要强制删除：-Force；若确认保留请手动删除 manifest 自身。' -ForegroundColor Yellow
    }

    Write-Host ''
    Write-Host "完成。删除 $deleted / 保留 $kept / 缺失 $missing" -ForegroundColor Cyan
}
Write-Host ''
