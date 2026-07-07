using UnityEngine;

public class ViewChange : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] canvasGroups;

    // 💡このような関数を1つ作っておき、ボタンから呼び出す設計にします
    public void SetTabVisibility(bool isVisible)
    {
        foreach (CanvasGroup canvas in canvasGroups) {
            canvas.alpha = isVisible ? 1f : 0f;
            canvas.interactable = isVisible;
            canvas.blocksRaycasts = isVisible;
        }
    }
}
