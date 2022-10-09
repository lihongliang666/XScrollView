using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUITools;

public class DemoController : MonoBehaviour
{
    private class ListData
    {
        public string itemName;
        public string content;
    }

    public XScrollView scrollView;

    public Button insertBtn;

    public InputField insertIndexInput;

    public InputField insertCountInput;

    public Button addBtn;

    public InputField addCountInput;

    public Button removeBtn;

    public InputField removeIndexInput;

    public Button refreshBtn;

    public InputField refreshIndexInput;

    public Button changeSizeBtn;

    public InputField changeSizeInput1;

    public InputField changeSizeInput2;

    public InputField changeSizeInput3;

    public Button clearBtn;

    public Button jumpBtn;

    public InputField jumpInput;

    private List<ListData> myData;

    private static readonly string[] tengwanggexu = new string[]
    {
        "时维九月，序属三秋。",
        "潦水尽而寒潭清，烟光凝而暮山紫。",
        "俨骖騑于上路，访风景于崇阿。",
        "临帝子之长洲，得天人之旧馆。",
        "层峦耸翠，上出重霄；",
        "飞阁流丹，下临无地。",
        "鹤汀凫渚，穷岛屿之萦回；",
        "桂殿兰宫，即冈峦之体势。",
        "披绣闼，俯雕甍，山原旷其盈视，川泽纡其骇瞩。",
        "闾阎扑地，钟鸣鼎食之家；",
        "舸舰弥津，青雀黄龙之舳。",
        "云销雨霁，彩彻区明。",
        "落霞与孤鹜齐飞，秋水共长天一色。",
        "渔舟唱晚，响穷彭蠡之滨，雁阵惊寒，声断衡阳之浦。"
    };

    // Start is called before the first frame update
    void Start()
    {
        InitControl();
        InitList();
    }

    private void InitControl()
    {
        this.insertBtn.onClick.AddListener(() =>
        {
            int startIndex, count;
            if (!int.TryParse(this.insertIndexInput.text, out startIndex) ||
                !int.TryParse(this.insertCountInput.text, out count))
                return;
            
            var insertItem = new List<ListData>();
            for (int i = 0; i < count; i++)
            {
                insertItem.Add(new ListData
                {
                    itemName = "Item" + (i % 5 + 1),
                    content = tengwanggexu[Random.Range(0, tengwanggexu.Length)]
                });
            }

            this.myData.InsertRange(startIndex, insertItem);
            this.scrollView.Insert(startIndex, count);
        });

        this.addBtn.onClick.AddListener(() =>
        {
            int count;
            if (!int.TryParse(this.addCountInput.text, out count))
                return;

            var c = this.myData.Count;
            for (int i = c; i < c + count; i++)
            {
                this.myData.Add(new ListData
                {
                    itemName = "Item" + (i % 5 + 1),
                    content = tengwanggexu[i % tengwanggexu.Length]
                });
            }

            this.scrollView.Add(count);
        });
        
        this.removeBtn.onClick.AddListener(() =>
        {
            int index;
            if (!int.TryParse(this.removeIndexInput.text, out index))
                return;
            
            this.myData.RemoveAt(index);
            this.scrollView.RemoveAt(index);
        });
        
        this.refreshBtn.onClick.AddListener(() =>
        {
            int index;
            if (!int.TryParse(this.refreshIndexInput.text, out index))
                return;

            this.myData[index].content = "[已变更] " + this.myData[index].content;
            this.scrollView.Refresh(index);
        });
        
        this.changeSizeBtn.onClick.AddListener(() =>
        {
            int index, x, y;
            if (!int.TryParse(this.changeSizeInput1.text, out index) ||
                !int.TryParse(this.changeSizeInput2.text, out x) || !int.TryParse(this.changeSizeInput3.text, out y))
                return;

            this.scrollView.OnItemSizeChange(index, new Vector2(x, y));
        });
        
        this.clearBtn.onClick.AddListener(() =>
        {
            this.myData.Clear();
            this.scrollView.Clear();
        });
        
        this.jumpBtn.onClick.AddListener(() =>
        {
            int index;
            if (!int.TryParse(this.jumpInput.text, out index))
                return;
            
            this.scrollView.JumpToIndex(index);
        });
    }

    private void InitList()
    {
        var count = 5;
        this.myData = new List<ListData>(count);
        for (int i = 0; i < count; i++)
        {
            this.myData.Add(new ListData
            {
                itemName = "Item" + (i % 5 + 1),
                content = tengwanggexu[i % tengwanggexu.Length]
            });
        }

        this.scrollView.onGetItemIdentifier += (index) => this.myData[index].itemName;

        this.scrollView.onItemRefresh += (index, identifier, gameObject) =>
        {
            gameObject.GetComponentInChildren<Text>().text = this.myData[index].content + "Index: " + index;
        };

        this.scrollView.Add(this.myData.Count);
    }
}