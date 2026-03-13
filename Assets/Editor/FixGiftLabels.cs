using UnityEditor;
using UnityEngine;
using TMPro;

public class FixGiftLabels
{
    public static string Execute()
    {
        var data = new (string path, string text)[]
        {
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier1/Label", "小温暖\n≤5分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier2/Label", "暖炉礼\n≤20分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier3/Label", "丰收篮\n≤50分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier4/Label", "守护盾\n≤100分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier5/Label", "暴风眼\n≤300分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier6/Label", "极地堡垒\n≤1000分"),
            ("Canvas/GameUIPanel/GiftIconBar/GiftTier7/Label", "神秘空投\n>1000分"),
        };

        int fixed_count = 0;
        foreach (var (path, text) in data)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                Debug.LogWarning($"[FixGiftLabels] Not found: {path}");
                continue;
            }
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogWarning($"[FixGiftLabels] No TMP on: {path}");
                continue;
            }
            tmp.text = text;
            // 确保字号可读
            if (tmp.fontSize < 18f)
                tmp.fontSize = 18f;
            // 强制白色确保可读
            tmp.color = Color.white;
            EditorUtility.SetDirty(go);
            fixed_count++;
        }

        Debug.Log($"[FixGiftLabels] Fixed {fixed_count} labels.");
        return $"Fixed {fixed_count} labels with proper newlines and white text.";
    }
}
