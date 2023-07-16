using System;
using System.Collections.Generic;

namespace XUITools
{
    // Copied from Unity2021 ObjectPool.cs for previous versions.
    public class UnityObjectPoolCopy<T> : IDisposable, IObjectPool<T> where T : class
    {
        internal readonly List<T> m_List;
        private readonly Func<T> m_CreateFunc;
        private readonly Action<T> m_ActionOnGet;
        private readonly Action<T> m_ActionOnRelease;
        private readonly Action<T> m_ActionOnDestroy;
        private readonly int m_MaxSize;
        internal bool m_CollectionCheck;

        public int CountAll { get; private set; }

        public int CountActive => this.CountAll - this.CountInactive;

        public int CountInactive => this.m_List.Count;

        public UnityObjectPoolCopy(
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 10000)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));
            if (maxSize <= 0)
                throw new ArgumentException("Max Size must be greater than 0", nameof(maxSize));
            this.m_List = new List<T>(defaultCapacity);
            this.m_CreateFunc = createFunc;
            this.m_MaxSize = maxSize;
            this.m_ActionOnGet = actionOnGet;
            this.m_ActionOnRelease = actionOnRelease;
            this.m_ActionOnDestroy = actionOnDestroy;
            this.m_CollectionCheck = collectionCheck;
        }

        public T Get()
        {
            T obj;
            if (this.m_List.Count == 0)
            {
                obj = this.m_CreateFunc();
                ++this.CountAll;
            }
            else
            {
                int index = this.m_List.Count - 1;
                obj = this.m_List[index];
                this.m_List.RemoveAt(index);
            }

            Action<T> actionOnGet = this.m_ActionOnGet;
            if (actionOnGet != null)
                actionOnGet(obj);
            return obj;
        }

        public PooledObject<T> Get(out T v) => new PooledObject<T>(v = this.Get(), (IObjectPool<T>)this);

        public void Release(T element)
        {
            if (this.m_CollectionCheck && this.m_List.Count > 0)
            {
                for (int index = 0; index < this.m_List.Count; ++index)
                {
                    if ((object)element == (object)this.m_List[index])
                        throw new InvalidOperationException(
                            "Trying to release an object that has already been released to the pool.");
                }
            }

            Action<T> actionOnRelease = this.m_ActionOnRelease;
            if (actionOnRelease != null)
                actionOnRelease(element);
            if (this.CountInactive < this.m_MaxSize)
            {
                this.m_List.Add(element);
            }
            else
            {
                Action<T> actionOnDestroy = this.m_ActionOnDestroy;
                if (actionOnDestroy != null)
                    actionOnDestroy(element);
            }
        }

        public void Clear()
        {
            if (this.m_ActionOnDestroy != null)
            {
                foreach (T obj in this.m_List)
                    this.m_ActionOnDestroy(obj);
            }

            this.m_List.Clear();
            this.CountAll = 0;
        }

        public void Dispose() => this.Clear();
    }

    public struct PooledObject<T> : IDisposable where T : class
    {
        private readonly T m_ToReturn;
        private readonly IObjectPool<T> m_Pool;

        internal PooledObject(T value, IObjectPool<T> pool)
        {
            this.m_ToReturn = value;
            this.m_Pool = pool;
        }

        void IDisposable.Dispose() => this.m_Pool.Release(this.m_ToReturn);
    }

    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }

        T Get();

        PooledObject<T> Get(out T v);

        void Release(T element);

        void Clear();
    }
}