using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TalentUI : MonoBehaviour {
    public Button button;
    Image icon;
    TextMeshProUGUI nameText;
    TextMeshProUGUI descText;
    TextMeshProUGUI costText;
    TextMeshProUGUI rankText;

    private void Awake() {
        button = GetComponent<Button>();
        Debug.Assert(button != null);
        icon = transform.Find("Icon").GetComponent<Image>();
        Debug.Assert(icon != null);
        nameText = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        Debug.Assert(nameText != null);
        descText = transform.Find("Desc").GetComponent<TextMeshProUGUI>();
        Debug.Assert(descText != null);
        costText = transform.Find("Cost").GetComponent<TextMeshProUGUI>();
        Debug.Assert(costText != null);
        rankText = transform.Find("Rank").GetChild(1).GetComponent<TextMeshProUGUI>();
        Debug.Assert(rankText != null);
    }

    public void UpdateTalent(in Talent talent, in float PlayerGold) {
        icon.sprite = SpriteDB.Instance.Get(talent.Icon.ToString());
        nameText.SetText(talent.Name.ToString());
        descText.SetText(talent.Desc.ToString());
        costText.SetText(talent.Cost.ToString());
        rankText.SetText($"{talent.CurrentRank}/{talent.MaximumRank}");
        
        if(talent.CurrentRank == talent.MaximumRank) {
            costText.gameObject.SetActive(false);
            rankText.color = Color.yellow;
            icon.raycastTarget = false;
            button.interactable = false;
            nameText.color = Color.white;
            return;
        }

        if (PlayerGold < talent.Cost) {
            costText.color = Color.red;
            rankText.color = Color.gray;
            descText.color = Color.gray;
            icon.raycastTarget = false;
            button.interactable = false;
            nameText.color = Color.gray;
            icon.color = Color.gray;
            return;
        }

        costText.gameObject.SetActive(true);
        costText.color = Color.white;
        rankText.color = Color.green;
        descText.color = Color.white;
        icon.raycastTarget = true;
        icon.color = Color.white;
        button.interactable = true;
        nameText.color = Color.yellow;
    }
}