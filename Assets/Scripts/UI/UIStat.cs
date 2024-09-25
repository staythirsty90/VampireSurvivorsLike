using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStat : MonoBehaviour {

    public System.Guid StatToRepresent;
    Image icon;
    TextMeshProUGUI nameText;
    TextMeshProUGUI valueText;
    Stat stat;
    float _previousValue;

    public bool IsDirty() {
        return _previousValue != stat.value;
    }

    void Awake() {
        nameText = transform.Find("Name Text").GetComponent<TextMeshProUGUI>();
        valueText = transform.Find("Value Text").GetComponent<TextMeshProUGUI>();
        icon = transform.Find("Icon Stat").GetComponent<Image>();
        Debug.Assert(nameText != null && valueText != null && icon != null);
        //nameText.gameObject.SetActive(false);
    }

    public void SetStat(Stat stat) {
        this.stat = stat;
        icon.sprite = SpriteDB.Instance.Get(stat.iconName);
        nameText.SetText(stat.name.ToString());
        _previousValue = stat.value;
        UpdateValueText();
    }

    public void UpdateValueText() {
        if(stat.Equals(default(Stat))) {
            Debug.LogWarning("UpdateValueText() stat is default!");
            return;
        }

        string str;
        if(stat.value > 0) {
            str = $"<color=#FFFF22>{stat.value:N0}";
        }
        else {
            str = "—";
        }

        valueText.SetText(str);
        _previousValue = stat.value;
    }
}