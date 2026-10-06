[CmdletBinding()]
param(
    # 编译器路径。留空则用随模组打包的那一份（Assets\AutoloadedEffects\Compiler\fxc.exe）。
    [string] $CompilerPath = '',
    # 模组根目录。留空则取脚本所在目录的上一级。
    [string] $Root = '',
    # 即使产物比源文件新也强制重编。
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = (Resolve-Path -LiteralPath (Join-Path $scriptRoot '..')).Path
}

$shaderRoot = Join-Path $Root 'Assets\Effects'
if (-not (Test-Path -LiteralPath $shaderRoot)) {
    Write-Host "没有着色器源目录，跳过：$shaderRoot"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($CompilerPath)) {
    $CompilerPath = Join-Path $Root 'Assets\AutoloadedEffects\Compiler\fxc.exe'
}
if (-not (Test-Path -LiteralPath $CompilerPath)) {
    throw "找不到着色器编译器：$CompilerPath（可用 -CompilerPath 指定，或把 fxc.exe 放回 Assets\AutoloadedEffects\Compiler）。"
}
$compiler = (Resolve-Path -LiteralPath $CompilerPath).Path

# 必须是**完整效果**目标。tML 的 FxcReader 走的是 new Effect(device, bytes)，
# 而裸的 ps_3_0 字节码（旧写法）虽然能编译，Effect 构造时会解析失败：
# 实测 /T ps_3_0 的产物头是 00 03 FF FF（ps_3_0 版本令牌），
# 而 /T fx_2_0 的产物头是 01 09 FF FE（D3DX9 effect 签名）。
$profile = 'fx_2_0'

$compiled = 0
$skipped = 0
Get-ChildItem -LiteralPath $shaderRoot -Filter '*.fx' -File -Recurse | ForEach-Object {
    $source = $_
    $output = [IO.Path]::ChangeExtension($source.FullName, '.fxc')

    if (-not $Force -and (Test-Path -LiteralPath $output) -and
        ((Get-Item -LiteralPath $output).LastWriteTimeUtc -gt $source.LastWriteTimeUtc)) {
        $skipped++
        return
    }

    # fxc **不接受 BOM**：带 BOM 的 .fx 会在 (1,1) 报
    # "X3000: Illegal character in shader file"（错误位置在文件最开头，很容易被误读成语法错）。
    # 而不少编辑器保存 .fx 时会加 BOM，所以这里统一规范化。只落临时文件，不碰源文件。
    $inputPath = $source.FullName
    $head = [IO.File]::ReadAllBytes($inputPath)
    if ($head.Length -ge 3 -and $head[0] -eq 0xEF -and $head[1] -eq 0xBB -and $head[2] -eq 0xBF) {
        $normalized = Join-Path ([IO.Path]::GetTempPath()) ("mono-nobom-" + $source.Name)
        [IO.File]::WriteAllBytes($normalized, $head[3..($head.Length - 1)])
        $inputPath = $normalized
    }
    elseif ($head.Length -ge 2 -and (($head[0] -eq 0xFF -and $head[1] -eq 0xFE) -or ($head[0] -eq 0xFE -and $head[1] -eq 0xFF))) {
        $normalized = Join-Path ([IO.Path]::GetTempPath()) ("mono-nobom-" + $source.Name)
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [IO.File]::WriteAllText($normalized, [IO.File]::ReadAllText($inputPath), $utf8NoBom)
        $inputPath = $normalized
    }

    & $compiler /nologo /T $profile /Fo $output $inputPath
    if ($LASTEXITCODE -ne 0) {
        throw "着色器编译失败：$($source.FullName)"
    }
    $compiled++
    Write-Host "已编译 $($source.Name) -> $([IO.Path]::GetFileName($output))"
}

Write-Host "着色器编译完成：$compiled 个已更新，$skipped 个已是最新。"
