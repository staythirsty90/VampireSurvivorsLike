using UnityEngine;
using UnityEngine.EventSystems;
using ImGuiNET;

public class TouchControl : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler {
    public Vector3 Movement { get; private set; }

    public void OnBeginDrag(PointerEventData eventData) {
        if(IsImGui()) return;
        if(eventData.button == PointerEventData.InputButton.Middle) return;
    }

    private static bool IsImGui() {
        if(ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)) return true;
        if(ImGui.IsAnyItemActive()) return true;

        var cursor = ImGui.GetMouseCursor();
        return cursor == ImGuiMouseCursor.ResizeAll || cursor == ImGuiMouseCursor.ResizeEW
            || cursor == ImGuiMouseCursor.ResizeNESW || cursor == ImGuiMouseCursor.ResizeNS
            || cursor == ImGuiMouseCursor.ResizeNWSE;
    }

    public void OnDrag(PointerEventData eventData) {
        if(IsImGui()) return;

        if(eventData.button == PointerEventData.InputButton.Middle) return;

        var pos = eventData.position;
        var input = Camera.main.ScreenToWorldPoint(pos);
        input.z = 0;

        if(input.magnitude > 1) {
            input.Normalize();
        }

        Movement = input;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if(IsImGui()) return;
        Movement = Vector3.zero;
    }
}