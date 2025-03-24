# 优化编译脚本
Write-Host "开始优化编译FatFullVersion程序..." -ForegroundColor Green

# 指定项目路径
$projectPath = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\FatFullVersion.csproj"
$outputPath = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\bin\Release"

# 创建优化版本的项目配置文件
$tempConfigFile = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\optimized.csproj.props"

@"
<Project>
  <PropertyGroup>
    <!-- 启用所有优化选项 -->
    <Optimize>true</Optimize>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    
    <!-- 启用高级优化 -->
    <TieredCompilation>true</TieredCompilation>
    <TieredCompilationQuickJit>false</TieredCompilationQuickJit>
    <TieredCompilationQuickJitForLoops>false</TieredCompilationQuickJitForLoops>
    
    <!-- 启用裁剪 -->
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    
    <!-- 启用AOT编译 -->
    <PublishAot>false</PublishAot>
    
    <!-- 启用ReadyToRun -->
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishReadyToRunEmitSymbols>false</PublishReadyToRunEmitSymbols>
    
    <!-- 启用单文件发布 -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    
    <!-- 控制GC模式 -->
    <ServerGarbageCollection>false</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
    <RetainVMGarbageCollection>false</RetainVMGarbageCollection>
    
    <!-- 其他优化标志 -->
    <PublishWithAspNetCoreTargetManifest>false</PublishWithAspNetCoreTargetManifest>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
</Project>
"@ | Out-File -FilePath $tempConfigFile -Encoding utf8

# 添加GC优化的代码片段
$gcOptimizerFile = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\GCOptimizer.cs"

@"
using System;
using System.Runtime;

namespace FatFullVersion
{
    /// <summary>
    /// GC优化器，用于管理垃圾回收
    /// </summary>
    public static class GCOptimizer
    {
        /// <summary>
        /// 初始化GC优化设置
        /// </summary>
        public static void Initialize()
        {
            // 设置GCLatencyMode为批处理模式，降低GC暂停频率
            GCSettings.LatencyMode = GCLatencyMode.Batch;

            // 启用Concurrent GC
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            
            // 注册内存压力事件
            AddMemoryPressureHandler();
        }
        
        /// <summary>
        /// 执行内存清理
        /// </summary>
        public static void CleanupMemory()
        {
            // 强制执行GC收集
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            
            // 释放未使用的内存回操作系统
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                NativeMethods.SetProcessWorkingSetSize(
                    System.Diagnostics.Process.GetCurrentProcess().Handle,
                    -1, -1);
            }
        }
        
        /// <summary>
        /// 添加内存压力监控
        /// </summary>
        private static void AddMemoryPressureHandler()
        {
            // 可以添加自定义内存压力监控逻辑
        }
        
        /// <summary>
        /// 本机方法调用
        /// </summary>
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            public static extern bool SetProcessWorkingSetSize(
                IntPtr hProcess, 
                int dwMinimumWorkingSetSize, 
                int dwMaximumWorkingSetSize);
        }
    }
}
"@ | Out-File -FilePath $gcOptimizerFile -Encoding utf8

# 修改App.cs添加GC优化器初始化
$appFile = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\App.xaml.cs"
$appContent = Get-Content -Path $appFile -Raw

if (-not $appContent.Contains("GCOptimizer.Initialize()")) {
    $newContent = $appContent -replace "protected override Window CreateShell\(\)\s*{", @"protected override Window CreateShell()
    {
        // 初始化GC优化
        GCOptimizer.Initialize();"
    
    $newContent | Out-File -FilePath $appFile -Encoding utf8
    Write-Host "已添加GC优化器初始化代码" -ForegroundColor Green
}

# 执行优化编译
Write-Host "开始优化编译..." -ForegroundColor Yellow
$publishCommand = "dotnet publish `"$projectPath`" -c Release -o `"$outputPath`" /p:DefineConstants=`"TRACE;RELEASE;NET8_0;OPTIMIZED`" /p:DebugType=None /p:DebugSymbols=false /p:Optimize=true /property:PropsFile=`"$tempConfigFile`""

# 执行编译命令
Invoke-Expression $publishCommand

# 创建优化版启动脚本
$optimizedBatFile = "D:\GIT\Git\code\FAT_TEST\FAT_TEST\RunOptimizedFatFullVersion.bat"

@"
@echo off
cd /d D:\GIT\Git\code\FAT_TEST\FAT_TEST\FatFullVersion\FatFullVersion\bin\Release\net8.0-windows7.0
start FatFullVersion.exe -gcConserve -gcServer-
"@ | Out-File -FilePath $optimizedBatFile -Encoding ascii

Write-Host "优化完成，可通过以下脚本启动优化版：$optimizedBatFile" -ForegroundColor Green 