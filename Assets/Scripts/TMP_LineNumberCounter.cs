using UnityEngine;
using TMPro;

public class TMP_LineNumberCounter : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI lineNumberText;

    private RectTransform inputFieldRect;
    private RectTransform lineNumbersRect;

    void Start()
    {
        if (inputField == null || lineNumberText == null) return;

        inputFieldRect = inputField.textComponent.GetComponent<RectTransform>();
        lineNumbersRect = lineNumberText.GetComponent<RectTransform>();

        // 文字が変更された（増えた・消えた）ときのイベント
        inputField.onValueChanged.AddListener(OnTextChanged);

        // 初回実行
        OnTextChanged(inputField.text);
    }

    void OnTextChanged(string text)
    {
        // 1. 行を消した瞬間に、本物のテキストのメッシュとレイアウトを強制的に今すぐ再計算させる
        inputField.textComponent.ForceMeshUpdate();

        // 2. 正確に更新された行数を取得
        int lineCount = inputField.textComponent.textInfo.lineCount;
        if (lineCount <= 0) lineCount = 1;

        // 3. 行数テキストの作成
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine(i.ToString());
        }
        lineNumberText.text = sb.ToString();

        // 4. 行数テキスト側も即座に再計算させて位置ズレを防ぐ
        lineNumberText.ForceMeshUpdate();

        // 5. その場で位置を完全に同期させる
        SyncPosition();
    }

    void LateUpdate()
    {
        // スクロール時の追従用
        SyncPosition();
    }

    private void SyncPosition()
    {
        if (inputFieldRect != null && lineNumbersRect != null)
        {
            Vector2 anchoredPos = lineNumbersRect.anchoredPosition;
            anchoredPos.y = inputFieldRect.anchoredPosition.y;
            lineNumbersRect.anchoredPosition = anchoredPos;
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnTextChanged);
        }
    }
}