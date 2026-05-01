#!/usr/bin/env pwsh
<#
.SYNOPSIS
    [DEPRECATED] 子目录入口，已合并至仓库根 install.ps1。

.DESCRIPTION
    本文件保留仅为向后兼容，所有参数转发到仓库根 install.ps1。
    新用户请直接使用：
        ./install.ps1 -TargetRepo <path> -Targets copilot ...
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$TargetRepo,

    [string]$ProjectName,
    [string]$ProjectOneLiner,
    [string]$PrimaryLanguage,
    [string]$TechStack,
    [string]$TestCommand,
    [string]$LintCommand,
    [string]$HarnessRepoRef,

    [string]$VendorHarnessTo = '.harness-engineering',
    [switch]$NoVendor,

    [string[]]$Chatmodes,

    [switch]$Force,
    [switch]$NoDelete,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

Write-Host '提示：本子目录脚本已弃用，请改用仓库根 ./install.ps1（等价并支持多目标）。' -ForegroundColor DarkYellow

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$rootScript = Join-Path $repoRoot 'install.ps1'

$forwarded = @{
    TargetRepo      = $TargetRepo
    Targets         = @('copilot')
    VendorHarnessTo = $VendorHarnessTo
}
if ($ProjectName) { $forwarded.ProjectName = $ProjectName }
if ($ProjectOneLiner) { $forwarded.ProjectOneLiner = $ProjectOneLiner }
if ($PrimaryLanguage) { $forwarded.PrimaryLanguage = $PrimaryLanguage }
if ($TechStack) { $forwarded.TechStack = $TechStack }
if ($TestCommand) { $forwarded.TestCommand = $TestCommand }
if ($LintCommand) { $forwarded.LintCommand = $LintCommand }
if ($HarnessRepoRef) { $forwarded.HarnessRepoRef = $HarnessRepoRef }
if ($Chatmodes) { $forwarded.Chatmodes = $Chatmodes }
if ($NoVendor) { $forwarded.NoVendor = $true }
if ($Force) { $forwarded.Force = $true }
if ($NoDelete) { $forwarded.NoDelete = $true }
if ($DryRun) { $forwarded.DryRun = $true }

& $rootScript @forwarded
