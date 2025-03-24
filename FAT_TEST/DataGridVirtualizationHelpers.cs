using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace FatFullVersion.Optimizations
{
    /// <summary>
    /// 提供用于DataGrid虚拟化的辅助类和扩展方法
    /// </summary>
    public static class DataGridVirtualizationHelpers
    {
        /// <summary>
        /// 应用DataGrid虚拟化优化
        /// </summary>
        /// <param name="dataGrid">要优化的DataGrid</param>
        public static void ApplyVirtualization(this DataGrid dataGrid)
        {
            if (dataGrid == null) return;
            
            // 启用UI虚拟化
            VirtualizingPanel.SetIsVirtualizing(dataGrid, true);
            VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);
            VirtualizingPanel.SetCacheLength(dataGrid, new VirtualizationCacheLength(1, 1));
            VirtualizingPanel.SetCacheLengthUnit(dataGrid, VirtualizationCacheLengthUnit.Page);
            
            // 启用行/列虚拟化
            dataGrid.EnableRowVirtualization = true;
            dataGrid.EnableColumnVirtualization = true;
            
            // 更多渲染优化
            dataGrid.UseLayoutRounding = true;
            dataGrid.SnapsToDevicePixels = true;
            
            // 调整滚动行为
            ScrollViewer.SetIsDeferredScrollingEnabled(dataGrid, true);
            ScrollViewer.SetCanContentScroll(dataGrid, true);
            
            // 添加Unloaded事件处理以清理资源
            dataGrid.Unloaded += (s, e) => 
            {
                // 手动清理内存
                dataGrid.ItemsSource = null;
                dataGrid.Items.Clear();
                dataGrid.Columns.Clear();
                GC.Collect();
            };
        }
        
        /// <summary>
        /// 将现有集合转换为虚拟化集合
        /// </summary>
        /// <typeparam name="T">集合项类型</typeparam>
        /// <param name="existingCollection">现有集合</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>虚拟化集合</returns>
        public static VirtualizingCollection<T> ToVirtualizingCollection<T>(
            this IEnumerable existingCollection, 
            int pageSize = 100) where T : class
        {
            if (existingCollection == null)
                return null;
                
            var sourceList = existingCollection.Cast<T>().ToList();
            var provider = new ListItemsProvider<T>(sourceList);
            return new VirtualizingCollection<T>(provider, pageSize);
        }
    }
    
    /// <summary>
    /// 虚拟化集合，支持大数据量的高效显示
    /// </summary>
    /// <typeparam name="T">集合项类型</typeparam>
    public class VirtualizingCollection<T> : IList, INotifyCollectionChanged, INotifyPropertyChanged where T : class
    {
        private readonly IItemsProvider<T> _itemsProvider;
        private readonly int _pageSize;
        private readonly Dictionary<int, List<T>> _itemCache = new Dictionary<int, List<T>>();
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private int _count = -1;
        
        // 事件
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// 初始化虚拟化集合
        /// </summary>
        /// <param name="itemsProvider">数据项提供程序</param>
        /// <param name="pageSize">页大小</param>
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize = 100)
        {
            _itemsProvider = itemsProvider ?? throw new ArgumentNullException(nameof(itemsProvider));
            _pageSize = pageSize > 0 ? pageSize : 100;
        }
        
        #region IList接口实现
        
        public int Count
        {
            get
            {
                if (_count < 0)
                {
                    _count = _itemsProvider.FetchCount();
                }
                return _count;
            }
        }
        
        public bool IsReadOnly => true;
        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        
        public object this[int index]
        {
            get => GetItem(index);
            set => throw new NotSupportedException("虚拟化集合不支持修改操作");
        }
        
        public int Add(object value) => throw new NotSupportedException("虚拟化集合不支持添加操作");
        public void Clear() => throw new NotSupportedException("虚拟化集合不支持清空操作");
        public bool Contains(object value) => IndexOf(value) != -1;
        
        public int IndexOf(object value)
        {
            if (value is T item)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (Equals(GetItem(i), item))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        
        public void Insert(int index, object value) => throw new NotSupportedException("虚拟化集合不支持插入操作");
        public void Remove(object value) => throw new NotSupportedException("虚拟化集合不支持删除操作");
        public void RemoveAt(int index) => throw new NotSupportedException("虚拟化集合不支持删除操作");
        
        public void CopyTo(Array array, int index)
        {
            for (int i = 0; i < Count && index < array.Length; i++, index++)
            {
                array.SetValue(GetItem(i), index);
            }
        }
        
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return GetItem(i);
            }
        }
        
        #endregion
        
        /// <summary>
        /// 获取指定索引的项
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>数据项</returns>
        private T GetItem(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            int pageIndex = index / _pageSize;
            int pageOffset = index % _pageSize;
            
            var page = GetPage(pageIndex);
            return page[pageOffset];
        }
        
        /// <summary>
        /// 获取指定页的数据
        /// </summary>
        /// <param name="pageIndex">页索引</param>
        /// <returns>页数据</returns>
        private List<T> GetPage(int pageIndex)
        {
            // 尝试从缓存获取
            if (_itemCache.TryGetValue(pageIndex, out List<T> cachedItems))
            {
                return cachedItems;
            }
            
            // 缓存未命中，从数据源加载
            _cacheLock.Wait();
            try
            {
                // 再次检查缓存，避免竞态条件
                if (_itemCache.TryGetValue(pageIndex, out cachedItems))
                {
                    return cachedItems;
                }
                
                // 加载数据
                var startIndex = pageIndex * _pageSize;
                var count = Math.Min(_pageSize, Count - startIndex);
                var items = _itemsProvider.FetchRange(startIndex, count);
                
                // 更新缓存
                _itemCache[pageIndex] = items;
                
                // 缓存管理：如果页面过多，移除最早的页面
                if (_itemCache.Count > 10)
                {
                    var oldestPage = _itemCache.Keys.Min();
                    if (oldestPage != pageIndex)
                    {
                        _itemCache.Remove(oldestPage);
                    }
                }
                
                return items;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        
        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cacheLock.Wait();
            try
            {
                _itemCache.Clear();
                _count = -1;
                
                // 通知集合已改变
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        
        /// <summary>
        /// 引发CollectionChanged事件
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 引发PropertyChanged事件
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
    
    /// <summary>
    /// 数据项提供程序接口
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public interface IItemsProvider<T> where T : class
    {
        /// <summary>
        /// 获取数据项总数
        /// </summary>
        /// <returns>数据项总数</returns>
        int FetchCount();
        
        /// <summary>
        /// 获取指定范围的数据项
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">数量</param>
        /// <returns>数据项列表</returns>
        List<T> FetchRange(int startIndex, int count);
    }
    
    /// <summary>
    /// 基于列表的数据项提供程序
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public class ListItemsProvider<T> : IItemsProvider<T> where T : class
    {
        private readonly List<T> _source;
        
        /// <summary>
        /// 初始化提供程序
        /// </summary>
        /// <param name="source">源数据列表</param>
        public ListItemsProvider(List<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }
        
        /// <summary>
        /// 获取数据项总数
        /// </summary>
        /// <returns>数据项总数</returns>
        public int FetchCount()
        {
            return _source.Count;
        }
        
        /// <summary>
        /// 获取指定范围的数据项
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">数量</param>
        /// <returns>数据项列表</returns>
        public List<T> FetchRange(int startIndex, int count)
        {
            return _source.Skip(startIndex).Take(count).ToList();
        }
    }
} 