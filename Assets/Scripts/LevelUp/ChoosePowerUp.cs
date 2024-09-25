using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ChoosePowerUp : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    public bool IsClicked { get; set; }
    public int Index { get; private set; }
    
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI badgeText;
    [SerializeField] Color hoverColor;

    Color originalColor;
    bool isDisabled = false;

    private void Awake() {
        originalColor = background.color;
    }

    private void OnEnable() {
        OnPointerExit(null);
    }

    public void SetData(int index, string spriteName = "", string name = "", string description = "", string badge = "") {
        if(string.IsNullOrEmpty(name)) {
            Index = -1;
            isDisabled = true;
            icon.enabled = false;
            nameText.SetText(string.Empty);
            descriptionText.SetText($"Try increasing your luck for another reward!");
            badgeText.SetText(string.Empty);
            background.color = Color.clear;
            return;
        }
        
        icon.sprite = SpriteDB.Instance.Get(spriteName);
        icon.enabled = true;

        icon.preserveAspect = true;
        icon.SetNativeSize();
        icon.rectTransform.sizeDelta = new Vector2(80, 80);

        background.color = originalColor;
        isDisabled = false;
        
        nameText.SetText(name);
        descriptionText.SetText(description);
        badgeText.SetText(badge);
        Index = index;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if(isDisabled) return;
        background.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData) {
        if(isDisabled) return;
        background.color = originalColor;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if(isDisabled) return;
        IsClicked = true;
    }
}