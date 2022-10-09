using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace XUITools
{
    public class XScrollView : MonoBehaviour
    {
        #region Enum

        public enum ScrollDirection
        {
            Vertical,
            Horizontal,
        }
        
        public enum ScrollViewType
        {
            List,
            Grid,
            Table,
        }        

        #endregion

        #region SupportClass

        class XScrollViewItemData
        {
            public string identifier;
            public Vector2 size;
            public Vector2 pos;
        }

        #endregion

        #region Field

        #region SerializeField

        [SerializeField] private RectTransform[] items;

        [SerializeField] private int verticalSpacing;

        [SerializeField] private int horizontalSpacing;

        [SerializeField] private ScrollDirection scrollDir;
        
        [SerializeField] ScrollViewType scrollViewType;

        #endregion
        
        public Func<int, string> onGetItemIdentifier;
        
        public Action<int, string, GameObject> onItemRefresh;

        private ScrollRect scrollRect;

        private XGameObjectPool itemPool;

        private List<XScrollViewItemData> virtualList;

        // Size of prefab
        private Dictionary<string, Vector2> itemOriginalSize;

        private Dictionary<int, GameObject> itemEntities;

        private Vector2Int curViewportRange = new Vector2Int(-1, -1);

        private float cachedViewportStart;

        #endregion

        #region Property

        public int ItemCount => virtualList?.Count ?? 0;

        #endregion

        // Add Items at tail of List.
        public void Add(int count)
        {
            var c = this.ItemCount;
            for (int i = c; i < c + count; i++)
            {
                var item = GenVirtualItem(i);
                if (item == null)
                    continue;
                
                this.virtualList.Add(item);
            }

            UpdateContentSize();
            RefreshViewport();
        }

        public void Insert(int startIndex, int count = 1)
        {
            if (startIndex == this.virtualList.Count)
            {
                Add(count);
                return;
            }
            
            if (!IsValidListIndex(startIndex))
            {
                Debug.LogError("Insert index must be correctly!");
                return;
            }

            var insertCount = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                var item = GenVirtualItem(i);
                if (item == null)
                    continue;

                this.virtualList.Insert(startIndex + insertCount++, item);
            }

            UpdatePosAfterIndex(startIndex + insertCount);
            UpdateContentSize();
            RefreshViewport();
        }

        public void RemoveAt(int removeIndex)
        {
            if (!IsValidListIndex(removeIndex))
            {
                Debug.LogError("Remove index must be correctly!");
                return;
            }

            this.virtualList.RemoveAt(removeIndex);
            UpdatePosAfterIndex(removeIndex);
            UpdateContentSize();
            RefreshViewport();
        }
        
        public void Clear()
        {
            this.virtualList.Clear();
            UpdateContentSize();
            RefreshViewport();
        }

        public void Refresh(int startIndex, int endIndex = -1)
        {
            if (endIndex < startIndex)
                endIndex = startIndex;

            // out of range, abort.
            if (endIndex < this.curViewportRange.x || startIndex > this.curViewportRange.y)
                return;

            if (startIndex < this.curViewportRange.x)
                startIndex = this.curViewportRange.x;

            if (endIndex > this.curViewportRange.y)
                endIndex = this.curViewportRange.y;

            if (this.onItemRefresh != null)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    GameObject go;
                    if (this.itemEntities.TryGetValue(i, out go))
                        this.onItemRefresh(i, this.virtualList[i].identifier, go);
                }
            }
        }

        public void OnItemSizeChange(int index, Vector2 size)
        {
            if (!IsValidListIndex(index))
            {
                Debug.LogError("Change index must be correctly!");
                return;
            }

            this.virtualList[index].size = size;
            UpdatePosAfterIndex(index + 1);
            UpdateContentSize();
            RefreshViewport();
        }

        public void JumpToIndex(int index)
        {
            if (!IsValidListIndex(index))
            {
                Debug.LogError("Jump index must be correctly!");
                return;
            }

            if (this.scrollDir == ScrollDirection.Vertical)
                JumpVertical(index);
            else
                JumpHorizontal(index);
        }

        private void JumpVertical(int index)
        {
            var vHeight = this.scrollRect.viewport.rect.size.y;
            var cHeight = this.scrollRect.content.rect.size.y;
            if (cHeight <= vHeight)
                return;

            float targetPosY = -1;
            if (index <= this.curViewportRange.x)
                targetPosY = -this.virtualList[index].pos.y;
            else if (index >= this.curViewportRange.y)
                targetPosY = -this.virtualList[index].pos.y + this.virtualList[index].size.y - vHeight;

            if (Mathf.Approximately(targetPosY, -1))
                return;

            targetPosY = Math.Clamp(targetPosY, 0, cHeight - vHeight);
            var lp = this.scrollRect.content.localPosition;
            if (!Mathf.Approximately(lp.y, targetPosY))
            {
                lp.y = targetPosY;
                this.scrollRect.content.localPosition = lp;
            }
        }

        private void JumpHorizontal(int index)
        {
            
        }
        
        private void RefreshViewport()
        {
            var firstShowIndex = -1;
            var lastShowIndex = -1;
            if (this.virtualList.Count > 0)
            {
                float viewportStart, viewportEnd;
                if (this.scrollDir == ScrollDirection.Vertical)
                {
                    viewportStart = -this.scrollRect.content.localPosition.y;
                    viewportEnd = viewportStart - this.scrollRect.viewport.rect.size.y;
                    if (this.curViewportRange.x != -1)
                    {
                        firstShowIndex = this.curViewportRange.x;
                        // list move forward
                        if (viewportStart < cachedViewportStart)
                        {
                            for (int i = firstShowIndex; i < this.virtualList.Count; i++)
                            {
                                firstShowIndex = i;
                                var item = this.virtualList[i];
                                if (item.pos.y - item.size.y < viewportStart)
                                    break;
                            }
                        }
                        else if (viewportStart > cachedViewportStart)
                        {
                            for (int i = firstShowIndex; i >= 0; i--)
                            {
                                var item = this.virtualList[i];
                                if (item.pos.y - item.size.y >= viewportStart)
                                    break;

                                firstShowIndex = i;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < this.virtualList.Count; i++)
                        {
                            var item = this.virtualList[i];
                            if (item.pos.y - item.size.y < viewportStart)
                            {
                                firstShowIndex = i;
                                break;
                            }
                        }
                    }

                    for (int i = firstShowIndex; i < this.virtualList.Count; i++)
                    {
                        var item = this.virtualList[i];
                        if (item.pos.y <= viewportEnd)
                            break;

                        lastShowIndex = i;
                    }
                }
                else
                {
                    viewportStart = -this.scrollRect.content.localPosition.x;
                    viewportEnd = viewportStart + this.scrollRect.viewport.rect.size.x;
                    if (this.curViewportRange.x != -1)
                    {
                        firstShowIndex = this.curViewportRange.x;
                        // list move forward
                        if (viewportStart > cachedViewportStart)
                        {
                            for (int i = firstShowIndex; i < this.virtualList.Count; i++)
                            {
                                firstShowIndex = i;
                                var item = this.virtualList[i];
                                if (item.pos.x + item.size.x > viewportStart)
                                    break;
                            }
                        }
                        else if (viewportStart < cachedViewportStart)
                        {
                            for (int i = firstShowIndex; i >= 0; i--)
                            {
                                var item = this.virtualList[i];
                                if (item.pos.x + item.size.x <= viewportStart)
                                    break;

                                firstShowIndex = i;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < this.virtualList.Count; i++)
                        {
                            var item = this.virtualList[i];
                            if (item.pos.x + item.size.x > viewportStart)
                            {
                                firstShowIndex = i;
                                break;
                            }
                        }
                    }
                    
                    for (int i = firstShowIndex; i < this.virtualList.Count; i++)
                    {
                        var item = this.virtualList[i];
                        if (item.pos.x >= viewportEnd)
                            break;

                        lastShowIndex = i;
                    }
                }

                this.cachedViewportStart = viewportStart;
                
                var removeKeys = new List<int>();
                foreach (var item in this.itemEntities)
                {
                    // out of range, remove it.
                    if (item.Key < firstShowIndex || item.Key > lastShowIndex)
                    {
                        this.itemPool.RecycleGO(item.Value);
                        removeKeys.Add(item.Key);
                    }
                    // item incorrectly, remove it.
                    else if (item.Value.name != this.virtualList[item.Key].identifier)
                    {
                        this.itemPool.RecycleGO(item.Value);
                        removeKeys.Add(item.Key);
                    }
                }

                foreach (var key in removeKeys)
                    this.itemEntities.Remove(key);
                
                for (int i = firstShowIndex; i <= lastShowIndex; i++)
                {
                    var itemData = this.virtualList[i];
                    GameObject go;
                    if (!this.itemEntities.TryGetValue(i, out go))
                    {
                        go = this.itemPool.GetGO(itemData.identifier);
                        this.itemEntities.Add(i, go);
                    }

                    var rt = go.GetComponent<RectTransform>();
                    rt.anchoredPosition = itemData.pos;
                    if (rt.rect.size != itemData.size)
                        rt.sizeDelta = itemData.size;
                    
                    this.onItemRefresh?.Invoke(i, itemData.identifier, go);
                }
            }
            else
            {
                foreach (var kvp in this.itemEntities)
                    this.itemPool.RecycleGO(kvp.Value);

                this.itemEntities.Clear();
            }

            // record item entities index data
            this.curViewportRange.x = firstShowIndex;
            this.curViewportRange.y = lastShowIndex;
        }
        
        private void UpdateContentSize()
        {
            var contentRT = this.scrollRect.content;
            var oldSize = contentRT.sizeDelta;
            if (this.ItemCount == 0)
            {
                if (this.scrollDir == ScrollDirection.Vertical)
                    oldSize.y = 0;
                else
                    oldSize.x = 0;
            }
            else
            {
                var lastItem = this.virtualList[this.ItemCount - 1];
                if (this.scrollDir == ScrollDirection.Vertical)
                {
                    oldSize.y = Mathf.Abs(lastItem.pos.y) + lastItem.size.y;
                }
                else
                {
                    oldSize.x = lastItem.pos.x + lastItem.size.x;
                }   
            }

            contentRT.sizeDelta = oldSize;
        }

        private XScrollViewItemData GenVirtualItem(int index)
        {
            var id = this.onGetItemIdentifier?.Invoke(index);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("Identifier get error at index: " + index);
                return null;
            }
            
            return new XScrollViewItemData
            {
                identifier = id,
                size = this.itemOriginalSize[id],
                pos = GetItemPos(index)
            };
        }
        
        private bool IsValidListIndex(int index)
        {
            return index >= 0 && index < this.virtualList.Count;
        }
        
        private Vector2 GetItemPos(int index)
        {
            var preIndex = index - 1;
            if (this.scrollViewType == ScrollViewType.List)
            {
                if (!IsValidListIndex(preIndex))
                    return Vector2.zero;

                var lastItem = this.virtualList[preIndex];
                if (this.scrollDir == ScrollDirection.Vertical)
                    return new Vector2(lastItem.pos.x, lastItem.pos.y - lastItem.size.y - this.verticalSpacing);
                else
                    return new Vector2(lastItem.pos.x + lastItem.size.x + this.horizontalSpacing, lastItem.pos.y);
            }

            return Vector2.zero;
        }

        // Update item pos from start index to the end of list.
        private void UpdatePosAfterIndex(int startIndex)
        {
            if (this.virtualList.Count == 0 || startIndex >= this.virtualList.Count)
                return;

            if (startIndex < 0)
                startIndex = 0;

            for (int i = startIndex; i < this.virtualList.Count; i++)
                this.virtualList[i].pos = GetItemPos(i);
        }

        private void Awake()
        {
            this.virtualList = new List<XScrollViewItemData>();
            this.itemEntities = new Dictionary<int, GameObject>();
            
            this.itemOriginalSize = new Dictionary<string, Vector2>();
            var goList = new List<GameObject>();
            for (int i = 0; i < this.items.Length; i++)
            {
                var go = this.items[i].gameObject;
                this.itemOriginalSize.Add(go.name, this.items[i].rect.size);
                goList.Add(go);
            }
            
            this.itemPool = XGameObjectPool.CreateHidePool(goList);
            this.itemPool.onNewGameObject = o => o.transform.SetParent(this.scrollRect.content, false);
            
            this.scrollRect = GetComponent<ScrollRect>();
            this.scrollRect.onValueChanged.AddListener((v2) =>
            {
                RefreshViewport();
            });
        }
    }   
}
