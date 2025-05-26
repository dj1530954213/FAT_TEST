# 修复源码文档页号跳跃问题
# 重新生成完整的前40页和后40页源码文档

param(
    [string]$SourcePath = "FatFullVersion\FatFullVersion",
    [string]$OutputPath = "FatFullVersion\FatFullVersion\NOTE\软著所需资料",
    [int]$LinesPerPage = 50,
    [int]$FrontPages = 40,
    [int]$BackPages = 40
)

Write-Host "开始修复源码文档页号跳跃问题..." -ForegroundColor Green

# 删除现有的页号文件
Get-ChildItem -Path $OutputPath -Filter "第*.txt" | Remove-Item -Force
Write-Host "已删除现有的页号文件" -ForegroundColor Yellow

# 获取所有源码文件（排除obj和bin目录）
$sourceFiles = Get-ChildItem -Path $SourcePath -Recurse -Include "*.cs","*.xaml" | 
    Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" } |
    Sort-Object FullName

Write-Host "找到 $($sourceFiles.Count) 个源码文件" -ForegroundColor Cyan

# 收集所有源码内容
$allLines = @()

foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    
    # 添加文件分隔符
    $allLines += "// =========================================="
    $allLines += "// 文件: $relativePath"
    $allLines += "// =========================================="
    
    try {
        $content = Get-Content $file.FullName -Encoding UTF8 -ErrorAction Stop
        if ($content) {
            $allLines += $content
        }
    }
    catch {
        Write-Warning "无法读取文件: $($file.FullName) - $($_.Exception.Message)"
        $allLines += "// 无法读取此文件内容"
    }
    
    # 添加空行分隔
    $allLines += ""
}

Write-Host "总共收集了 $($allLines.Count) 行代码" -ForegroundColor Cyan

# 计算总页数
$totalPages = [Math]::Ceiling($allLines.Count / $LinesPerPage)
Write-Host "总页数: $totalPages" -ForegroundColor Cyan

# 生成前40页
Write-Host "生成前40页源码文档..." -ForegroundColor Green
for ($page = 1; $page -le $FrontPages; $page++) {
    $startIndex = ($page - 1) * $LinesPerPage
    $endIndex = [Math]::Min($startIndex + $LinesPerPage - 1, $allLines.Count - 1)
    
    if ($startIndex -ge $allLines.Count) {
        break
    }
    
    $pageContent = @()
    $pageContent += "软件源代码文档 - 第 $page 页"
    $pageContent += "=================================================="
    $pageContent += "项目名称: FatFullVersion - 工业自动化测试系统"
    $pageContent += "文档生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $pageContent += "页码: $page / $totalPages"
    $pageContent += "=================================================="
    $pageContent += ""
    
    for ($i = $startIndex; $i -le $endIndex; $i++) {
        $lineNumber = $i + 1
        $pageContent += "{0:D4}: {1}" -f $lineNumber, $allLines[$i]
    }
    
    $fileName = "第{0:D2}页.txt" -f $page
    $filePath = Join-Path $OutputPath $fileName
    $pageContent | Out-File -FilePath $filePath -Encoding UTF8
    
    Write-Host "已生成: $fileName" -ForegroundColor Green
}

# 生成后40页
Write-Host "生成后40页源码文档..." -ForegroundColor Green
$backStartPage = $totalPages - $BackPages + 1
if ($backStartPage -le $FrontPages) {
    $backStartPage = $FrontPages + 1
}

for ($page = $backStartPage; $page -le $totalPages; $page++) {
    $startIndex = ($page - 1) * $LinesPerPage
    $endIndex = [Math]::Min($startIndex + $LinesPerPage - 1, $allLines.Count - 1)
    
    if ($startIndex -ge $allLines.Count) {
        break
    }
    
    $pageContent = @()
    $pageContent += "软件源代码文档 - 第 $page 页"
    $pageContent += "=================================================="
    $pageContent += "项目名称: FatFullVersion - 工业自动化测试系统"
    $pageContent += "文档生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $pageContent += "页码: $page / $totalPages"
    $pageContent += "=================================================="
    $pageContent += ""
    
    for ($i = $startIndex; $i -le $endIndex; $i++) {
        $lineNumber = $i + 1
        $pageContent += "{0:D4}: {1}" -f $lineNumber, $allLines[$i]
    }
    
    $fileName = "第{0:D3}页.txt" -f $page
    $filePath = Join-Path $OutputPath $fileName
    $pageContent | Out-File -FilePath $filePath -Encoding UTF8
    
    Write-Host "已生成: $fileName" -ForegroundColor Green
}

Write-Host "源码文档页号修复完成！" -ForegroundColor Green
Write-Host "前40页: 第01页.txt - 第40页.txt" -ForegroundColor Cyan
Write-Host "后40页: 第$($backStartPage.ToString('D3'))页.txt - 第$($totalPages.ToString('D3'))页.txt" -ForegroundColor Cyan 