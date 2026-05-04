using System;
using System.Collections.Generic;

namespace Shizuku.Core
{
    /// <summary>
    /// 通用对象池。线程不安全，适用于主线程使用。
    /// </summary>
    public class ShizukuObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly int _maxSize;

        public int CountAll { get; private set; }
        public int CountActive => CountAll - _pool.Count;
        public int CountInactive => _pool.Count;

        public ShizukuObjectPool(Action<T> onGet = null, Action<T> onRelease = null, int defaultCapacity = 16, int maxSize = 1024)
        {
            _pool = new Stack<T>(defaultCapacity);
            _onGet = onGet;
            _onRelease = onRelease;
            _maxSize = maxSize;
        }

        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = new T();
                CountAll++;
            }
            _onGet?.Invoke(obj);
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            _onRelease?.Invoke(obj);
            if (_pool.Count < _maxSize)
                _pool.Push(obj);
        }

        public void Clear()
        {
            _pool.Clear();
            CountAll = 0;
        }
    }
}
