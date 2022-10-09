using System;
using System.Collections.Generic;
using UnityEngine;

namespace XUITools
{
    class XGameObjectPool1 : XGameObjectPool
    {
        public override GameObject GetGO(string key, object arg = null)
        {
            var go = base.GetGO(key, arg);
            if (go != null)
            {
                go.SetActive(true);
            }

            return go;
        }

        public override void RecycleGO(GameObject go)
        {
            base.RecycleGO(go);
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }

    class XGameObjectPool2 : XGameObjectPool
    {
        internal Vector3 poolPos;
        internal bool isWorldPos;
        
        public override GameObject GetGO(string key, object arg = null)
        {
            var go = base.GetGO(key, arg);
            if (go != null && arg != null)
            {
                if (isWorldPos)
                {
                    go.transform.position = (Vector3) arg;
                }
                else
                {
                    go.transform.localPosition = (Vector3) arg;   
                }
            }

            return go;
        }

        public override void RecycleGO(GameObject go)
        {
            base.RecycleGO(go);
            if (go != null)
            {
                if (isWorldPos)
                {
                    go.transform.position = poolPos;
                }
                else
                {
                    go.transform.localPosition = poolPos;   
                }
            }
        }
    }

    class XGameObjectPool3 : XGameObjectPool
    {
        internal Transform poolTran;
        
        public override GameObject GetGO(string key, object arg = null)
        {
            var go = base.GetGO(key, arg);
            if (go != null && arg != null)
            {
                go.transform.SetParent((Transform) arg, false);
            }

            return go;
        }

        public override void RecycleGO(GameObject go)
        {
            base.RecycleGO(go);
            if (go != null)
            {
                go.transform.SetParent(poolTran, false);
            }
        }
    }
    
    public class XGameObjectPool
    {
        public enum ReturnPoolType
        {
            Hide,
            Move,
            Root,
        }

        public Action<GameObject> onNewGameObject;

        protected Dictionary<string, GameObject> prefabs;

        protected Dictionary<string, List<GameObject>> pooledGOs = new Dictionary<string, List<GameObject>>();

        private ReturnPoolType type;

        public static XGameObjectPool CreateHidePool(IList<GameObject> prefabs)
        {
            XGameObjectPool1 ret = new XGameObjectPool1();
            ret.type = ReturnPoolType.Hide;
            ret.InitPool(prefabs);
            return ret;
        }
        
        public static XGameObjectPool CreateMovePool(IList<GameObject> prefabs, Vector3 poolPos, bool isWorldPos)
        {
            XGameObjectPool2 ret = new XGameObjectPool2();
            ret.type = ReturnPoolType.Move;
            ret.poolPos = poolPos;
            ret.isWorldPos = isWorldPos;
            ret.InitPool(prefabs);
            return ret;
        }

        public static XGameObjectPool CreateRootPool(IList<GameObject> prefabs, Transform poolRoot)
        {
            XGameObjectPool3 ret = new XGameObjectPool3();
            ret.poolTran = poolRoot;
            ret.InitPool(prefabs);
            return ret;
        }

        public virtual GameObject GetGO(string key, object arg = null)
        {
            GameObject ret = null;
            List<GameObject> cachedList;
            if (this.pooledGOs.TryGetValue(key, out cachedList) && cachedList.Count > 0)
            {
                var i = cachedList.Count - 1;
                ret = cachedList[i];
                cachedList.RemoveAt(i);
            }

            if (ret == null && this.prefabs.ContainsKey(key))
            {
                ret = GameObject.Instantiate(this.prefabs[key]);
                ret.name = key;
                this.onNewGameObject?.Invoke(ret);
            }

            return ret;
        }

        public virtual void RecycleGO(GameObject go)
        {
            if (go == null)
                return;

            var n = go.name;
            List<GameObject> cachedList;
            if (!this.pooledGOs.TryGetValue(n, out cachedList))
            {
                cachedList = new List<GameObject>();
                this.pooledGOs.Add(n, cachedList);
            }

            cachedList.Add(go);
        }
        
        private void InitPool(IList<GameObject> prefabs)
        {
            if (prefabs == null)
                return;

            var c = prefabs.Count;
            if (c == 0)
                return;
            
            this.prefabs = new Dictionary<string, GameObject>(c);
            for (int i = 0; i < c; i++)
            {
                if (prefabs[i] == null)
                {
                    Debug.LogError("Prefab is null at index: " + i);
                    continue;
                }

                var n = prefabs[i].name;
                if (this.prefabs.ContainsKey(n))
                {
                    Debug.LogError("Duplicate prefab name at index: " + i);
                    continue;
                }

                this.prefabs.Add(n, prefabs[i]);
            }
        }
    }
}