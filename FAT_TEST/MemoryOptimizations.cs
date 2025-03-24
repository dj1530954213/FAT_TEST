using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FatFullVersion.Optimizations
{
    /// <summary>
    /// 内存优化工具类，提供各种减少内存使用的方法
    /// </summary>
    public static class MemoryOptimizations
    {
        private static Timer _memoryMonitorTimer;
        private static readonly object _lockObject = new object();
        private static bool _isCleaningUp = false;
        private static WeakReference<Window> _mainWindowRef;

        /// <summary>
        /// 启用所有内存优化
        /// </summary>
        /// <param name="mainWindow">应用程序主窗口</param>
        public static void EnableOptimizations(Window mainWindow)
        {
            // 保存主窗口的弱引用
            _mainWindowRef = new WeakReference<Window>(mainWindow);
            
            // 优化GC设置
            OptimizeGarbageCollection();
            
            // 启动内存监控
            StartMemoryMonitoring();
            
            // 注册应用程序事件
            RegisterApplicationEvents();
            
            // 优化图像缓存
            OptimizeImageCache();
            
            // 显示优化提示
            Debug.WriteLine("已启用内存优化");
        }
        
        /// <summary>
        /// 清理不再使用的内存
        /// </summary>
        public static void CleanupMemory()
        {
            if (_isCleaningUp)
                return;
                
            lock (_lockObject)
            {
                if (_isCleaningUp) 
                    return;
                    
                _isCleaningUp = true;
                
                try
                {
                    // 清理图像缓存
                    CleanupImageCache();
                    
                    // 执行GC收集
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    
                    // 回收物理内存
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                    
                    // 输出清理结果
                    var memoryMB = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                    Debug.WriteLine($"内存清理完成，当前内存使用：{memoryMB} MB");
                }
                finally
                {
                    _isCleaningUp = false;
                }
            }
        }
        
        /// <summary>
        /// 创建优化的ObservableCollection，自动处理大型集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <returns>优化的ObservableCollection</returns>
        public static ObservableCollection<T> CreateOptimizedCollection<T>() where T : class
        {
            var collection = new ObservableCollection<T>();
            
            // 使用CollectionView优化
            var view = CollectionViewSource.GetDefaultView(collection);
            if (view != null)
            {
                view.IsLiveSorting = true;
                view.IsLiveSortingRequested = true;
                view.IsLiveFiltering = true;
                view.IsLiveFilteringRequested = true;
            }
            
            return collection;
        }
        
        /// <summary>
        /// 优化ListView/DataGrid以处理大量数据
        /// </summary>
        /// <param name="listView">要优化的ListView</param>
        public static void OptimizeListView(ListView listView)
        {
            // 启用UI虚拟化
            VirtualizingPanel.SetIsVirtualizing(listView, true);
            VirtualizingPanel.SetVirtualizationMode(listView, VirtualizationMode.Recycling);
            
            // 启用容器复用
            VirtualizingPanel.SetCacheLength(listView, new VirtualizationCacheLength(1, 1));
            VirtualizingPanel.SetCacheLengthUnit(listView, VirtualizationCacheLengthUnit.Page);
            
            // 禁用滚动时的项渲染
            ScrollViewer.SetIsDeferredScrollingEnabled(listView, true);
            
            // 减少视觉树中的节点数量
            listView.UseLayoutRounding = true;
        }
        
        /// <summary>
        /// 优化DataGrid以处理大量数据
        /// </summary>
        /// <param name="dataGrid">要优化的DataGrid</param>
        public static void OptimizeDataGrid(DataGrid dataGrid)
        {
            // 启用UI虚拟化
            VirtualizingPanel.SetIsVirtualizing(dataGrid, true);
            VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);
            
            // 启用容器复用
            VirtualizingPanel.SetCacheLength(dataGrid, new VirtualizationCacheLength(1, 1));
            VirtualizingPanel.SetCacheLengthUnit(dataGrid, VirtualizationCacheLengthUnit.Page);
            
            // 延迟加载行详情
            dataGrid.EnableRowVirtualization = true;
            dataGrid.EnableColumnVirtualization = true;
            
            // 使用固定列宽提高性能
            dataGrid.AutoGenerateColumns = false;
            
            // 减少视觉树中的节点数量
            dataGrid.UseLayoutRounding = true;
            dataGrid.SnapsToDevicePixels = true;
        }
        
        /// <summary>
        /// 分批加载数据到集合中，避免一次性加载大量数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="collection">目标集合</param>
        /// <param name="allItems">所有要加载的数据</param>
        /// <param name="batchSize">每批加载的数据量</param>
        /// <param name="delayBetweenBatchesMs">批次之间的延迟时间(毫秒)</param>
        public static void LoadItemsInBatches<T>(
            ObservableCollection<T> collection, 
            IList<T> allItems, 
            int batchSize = 100, 
            int delayBetweenBatchesMs = 10)
        {
            if (collection == null || allItems == null || allItems.Count == 0)
                return;
                
            // 清空集合
            collection.Clear();
            
            // 计算批次数
            int batchCount = (allItems.Count + batchSize - 1) / batchSize;
            int currentBatch = 0;
            
            // 创建计时器加载数据
            Timer batchTimer = null;
            batchTimer = new Timer(_ => 
            {
                if (currentBatch >= batchCount)
                {
                    batchTimer?.Dispose();
                    return;
                }
                
                int start = currentBatch * batchSize;
                int count = Math.Min(batchSize, allItems.Count - start);
                
                // 在UI线程上更新集合
                Application.Current.Dispatcher.Invoke(() => 
                {
                    for (int i = 0; i < count; i++)
                    {
                        collection.Add(allItems[start + i]);
                    }
                });
                
                currentBatch++;
            }, null, 0, delayBetweenBatchesMs);
        }
        
        #region 私有辅助方法
        
        private static void OptimizeGarbageCollection()
        {
            // 设置GC模式
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            
            // 减少LOH分配导致的碎片
            Debug.WriteLine("已优化GC设置");
        }
        
        private static void StartMemoryMonitoring()
        {
            _memoryMonitorTimer = new Timer(_ => 
            {
                var proc = Process.GetCurrentProcess();
                var memoryMB = proc.PrivateMemorySize64 / (1024 * 1024);
                
                Debug.WriteLine($"内存使用: {memoryMB} MB");
                
                // 如果内存超过1.5GB，执行清理
                if (memoryMB > 1500)
                {
                    Debug.WriteLine("内存使用超过阈值，执行清理...");
                    CleanupMemory();
                }
            }, null, 0, 30000); // 每30秒检查一次
        }
        
        private static void RegisterApplicationEvents()
        {
            // 在应用程序失去焦点时执行内存清理
            Application.Current.Deactivated += (s, e) => 
            {
                Debug.WriteLine("应用程序失去焦点，执行内存清理");
                CleanupMemory();
            };
            
            // 在应用程序最小化时执行清理
            if (_mainWindowRef != null && _mainWindowRef.TryGetTarget(out Window mainWindow))
            {
                mainWindow.StateChanged += (s, e) =>
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        Debug.WriteLine("应用程序最小化，执行内存清理");
                        CleanupMemory();
                    }
                };
            }
        }
        
        private static void OptimizeImageCache()
        {
            // 限制图像缓存大小
            ImageCache.SetCacheLimit(50000000); // 约50MB的图像缓存
        }
        
        private static void CleanupImageCache()
        {
            // 清理图像缓存
            ImageCache.BitmapCache.Clear();
        }
        
        // 图像缓存工具类
        private static class ImageCache
        {
            private static int _cacheLimitBytes = 100000000; // 默认100MB
            public static Dictionary<string, WeakReference<BitmapSource>> BitmapCache { get; } = new Dictionary<string, WeakReference<BitmapSource>>();
            
            public static void SetCacheLimit(int limitBytes)
            {
                _cacheLimitBytes = limitBytes;
            }
            
            public static void Clear()
            {
                BitmapCache.Clear();
            }
        }
        
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        
        #endregion
    }
} 