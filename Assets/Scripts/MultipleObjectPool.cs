using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace XUITools
{
    public class MultipleObjectPool<TKey, TVal> where TVal : class
    {
        private Dictionary<TKey, ObjectPool<TVal>> poolObject;

        private Func<TVal, TKey> keyGetter;

        public static MultipleObjectPool<TKey, TVal> Create(IList<TVal> pooledObjs, Func<TVal, TKey> keyGetter,
            Func<TKey, TVal> onCreate, Dictionary<TKey, TVal> key2ValDict, Action<TVal> onGet = null,
            Action<TVal> onRelease = null, Action<TVal> onDestroy = null)
        {
            if (pooledObjs == null || pooledObjs.Count == 0 || keyGetter == null || onCreate == null ||
                key2ValDict == null)
                return null;

            foreach (var pooledObj in pooledObjs)
            {
                var key = keyGetter(pooledObj);
                if (!key2ValDict.TryAdd(key, pooledObj))
                    return null;
            }

            return new MultipleObjectPool<TKey, TVal>(key2ValDict, keyGetter, onCreate, onGet, onRelease, onDestroy);
        }

        private MultipleObjectPool(Dictionary<TKey, TVal> dict, Func<TVal, TKey> keyGetter, Func<TKey, TVal> onCreate,
            Action<TVal> onGet = null, Action<TVal> onRelease = null, Action<TVal> onDestroy = null)
        {
            this.poolObject = new Dictionary<TKey, ObjectPool<TVal>>(dict.Count);
            foreach (var kvp in dict)
            {
                var key = kvp.Key;
                var pool = new ObjectPool<TVal>(() => onCreate(key), onGet, onRelease, onDestroy, false);
                this.poolObject.Add(key, pool);
            }

            this.keyGetter = keyGetter;
        }

        public TVal Get(TKey key)
        {
            return poolObject.TryGetValue(key, out var pool) ? pool.Get() : null;
        }

        public void Release(TVal obj)
        {
            var key = this.keyGetter(obj);
            if (poolObject.TryGetValue(key, out var pool))
                pool.Release(obj);
        }

        public void Clear()
        {
            foreach (var pool in poolObject.Values)
            {
                pool.Clear();
            }
        }
    }
}