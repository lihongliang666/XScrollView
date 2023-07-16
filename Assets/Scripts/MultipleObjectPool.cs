using System;
using System.Collections.Generic;

#if UNITY_2021_1_OR_NEWER
using UnityEngine.Pool;
#endif

namespace XUITools
{
    public class MultipleObjectPool<TKey, TVal> where TVal : class
    {
#if UNITY_2021_1_OR_NEWER
        private Dictionary<TKey, ObjectPool<TVal>> pools;
#else
        private Dictionary<TKey, UnityObjectPoolCopy<TVal>> pools;
#endif

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
#if UNITY_2021_1_OR_NEWER
            this.pools = new Dictionary<TKey, ObjectPool<TVal>>(dict.Count);
#else
            this.pools = new Dictionary<TKey, UnityObjectPoolCopy<TVal>>(dict.Count);
#endif

            foreach (var kvp in dict)
            {
                var key = kvp.Key;

#if UNITY_2021_1_OR_NEWER
                var pool = new ObjectPool<TVal>(() => onCreate(key), onGet, onRelease, onDestroy, false);
#else
                var pool = new UnityObjectPoolCopy<TVal>(() => onCreate(key),
                    onGet, onRelease, onDestroy, false);
#endif

                this.pools.Add(key, pool);
            }

            this.keyGetter = keyGetter;
        }

        public TVal Get(TKey key)
        {
            return pools.TryGetValue(key, out var pool) ? pool.Get() : null;
        }

        public void Release(TVal obj)
        {
            var key = this.keyGetter(obj);
            if (pools.TryGetValue(key, out var pool))
                pool.Release(obj);
        }

        public void Clear()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
        }
    }

    public static class DictionaryUtils
    {
#if !UNITY_2021_1_OR_NEWER
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return false;

            if (dict.ContainsKey(key))
                return false;

            dict.Add(key, value);
            return true;
        }
#endif
    }
}