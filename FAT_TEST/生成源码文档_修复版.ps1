# 生成源码文档脚本 - 修复版
# 用于软著申请的源码文档生成

param(
    [string]$SourcePath = "FatFullVersion\FatFullVersion",
    [string]$OutputPath = "FatFullVersion\FatFullVersion\NOTE\软著所需资料",
    [int]$LinesPerPage = 50,
    [int]$FrontPages = 40,
    [int]$BackPages = 40
)

Write-Host "开始生成源码文档..." -ForegroundColor Green

# 确保输出目录存在
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "创建输出目录: $OutputPath" -ForegroundColor Yellow
}

# 获取所有源码文件（排除obj和bin目录）
$sourceFiles = Get-ChildItem -Path $SourcePath -Recurse -Include "*.cs","*.xaml" | 
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" } |
    Sort-Object FullName

Write-Host "找到 $($sourceFiles.Count) 个源码文件" -ForegroundColor Cyan

# 收集所有源码内容
$allLines = @()

foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $fileHeader = "// =========================================="
    $fileHeader += "`n// 文件: $relativePath"
    $fileHeader += "`n// =========================================="
    
    $allLines += $fileHeader.Split("`n")
    
    try {
        $content = Get-Content $file.FullName -Encoding UTF8
        if ($content) {
            $allLines += $content
        }
        $allLines += ""  # 空行分隔
        Write-Host "处理文件: $relativePath ($($content.Count) 行)" -ForegroundColor Gray
    }
    catch {
        Write-Host "警告: 无法读取文件 $relativePath - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "总共收集了 $($allLines.Count) 行代码" -ForegroundColor Cyan

# 计算总页数
$totalPages = [Math]::Ceiling($allLines.Count / $LinesPerPage)
Write-Host "总页数: $totalPages" -ForegroundColor Cyan

# 生成前40页
Write-Host "生成前 $FrontPages 页..." -ForegroundColor Green
for ($page = 1; $page -le $FrontPages; $page++) {
    $startLine = ($page - 1) * $LinesPerPage
    $endLine = [Math]::Min($startLine + $LinesPerPage - 1, $allLines.Count - 1)
    
    if ($startLine -lt $allLines.Count) {
        $pageContent = @()
        $pageContent += "软件源代码文档 - 第 $page 页"
        $pageContent += "=" * 50
        $pageContent += "项目名称: FatFullVersion - 工业自动化测试系统"
        $pageContent += "文档生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $pageContent += "页码: $page / $totalPages"
        $pageContent += "=" * 50
        $pageContent += ""
        
        for ($i = $startLine; $i -le $endLine; $i++) {
            if ($i -lt $allLines.Count) {
                $lineNumber = $i + 1
                $lineText = if ($allLines[$i]) { $allLines[$i] } else { "" }
                $pageContent += "$($lineNumber.ToString().PadLeft(4, '0')): $lineText"
            }
        }
        
        $fileName = "第$($page.ToString().PadLeft(2, '0'))页.txt"
        $filePath = Join-Path $OutputPath $fileName
        $pageContent | Out-File -FilePath $filePath -Encoding UTF8
        Write-Host "生成: $fileName" -ForegroundColor Green
    }
}

# 生成后40页
if ($totalPages -gt $FrontPages) {
    Write-Host "生成后 $BackPages 页..." -ForegroundColor Green
    $startBackPage = [Math]::Max($totalPages - $BackPages + 1, $FrontPages + 1)
    
    for ($page = $startBackPage; $page -le $totalPages; $page++) {
        $startLine = ($page - 1) * $LinesPerPage
        $endLine = [Math]::Min($startLine + $LinesPerPage - 1, $allLines.Count - 1)
        
        if ($startLine -lt $allLines.Count) {
            $pageContent = @()
            $pageContent += "软件源代码文档 - 第 $page 页"
            $pageContent += "=" * 50
            $pageContent += "项目名称: FatFullVersion - 工业自动化测试系统"
            $pageContent += "文档生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            $pageContent += "页码: $page / $totalPages"
            $pageContent += "=" * 50
            $pageContent += ""
            
            for ($i = $startLine; $i -le $endLine; $i++) {
                if ($i -lt $allLines.Count) {
                    $lineNumber = $i + 1
                    $lineText = if ($allLines[$i]) { $allLines[$i] } else { "" }
                    $pageContent += "$($lineNumber.ToString().PadLeft(4, '0')): $lineText"
                }
            }
            
            $fileName = "第$($page.ToString().PadLeft(2, '0'))页.txt"
            $filePath = Join-Path $OutputPath $fileName
            $pageContent | Out-File -FilePath $filePath -Encoding UTF8
            Write-Host "生成: $fileName" -ForegroundColor Green
        }
    }
}

# 生成文件清单
$manifestContent = @()
$manifestContent += "FatFullVersion 源码文件清单"
$manifestContent += "=" * 50
$manifestContent += "生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$manifestContent += "总文件数: $($sourceFiles.Count)"
$manifestContent += "总行数: $($allLines.Count)"
$manifestContent += "总页数: $totalPages"
$manifestContent += "=" * 50
$manifestContent += ""

foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $content = Get-Content $file.FullName -Encoding UTF8 -ErrorAction SilentlyContinue
    $lineCount = if ($content) { $content.Count } else { 0 }
    $manifestContent += "$relativePath ($lineCount 行)"
}

$manifestPath = Join-Path $OutputPath "源码文件清单.txt"
$manifestContent | Out-File -FilePath $manifestPath -Encoding UTF8

Write-Host "源码文档生成完成！" -ForegroundColor Green
Write-Host "输出目录: $OutputPath" -ForegroundColor Cyan
Write-Host "生成了前 $FrontPages 页和后 $BackPages 页的源码文档" -ForegroundColor Cyan 