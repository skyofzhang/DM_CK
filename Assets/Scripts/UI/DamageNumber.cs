using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 伤害飘字：在世界坐标显示伤害数值，上浮并淡出后销毁。
/// 使用 TextMeshPro（世界空间），绑定中文字体（Rule R2）。
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private TextMeshPro _tmp;

    /// <summary>在指定世界坐标显示飘字（静态工厂方法）</summary>
    public static void Show(Vector3 worldPos, int damage, Color color)
    {
        var go = new GameObject("DmgNum_" + damage);
        go.transform.position = worldPos + Vector3.up * 1.5f;
        var dn = go.AddComponent<DamageNumber>();
        dn.Init(damage, color);
    }

    /// <summary>🆕 v1.22 §10 显示任意字符串飘字（供反伤/冲击波等特殊伤害渠道使用）</summary>
    public static void Show(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("DmgNum_" + text);
        go.transform.position = worldPos + Vector3.up * 1.5f;
        var dn = go.AddComponent<DamageNumber>();
        dn.InitText(text, color);
    }

    /// <summary>
    /// 🆕 v1.22 §10 显示带减伤的伤害飘字（格式 "-{actual}(-{raw})"）。
    ///   - actual == raw 时等同普通 Show
    ///   - actual != raw 时显示括号内原始伤害，便于玩家感知城门减伤效果
    /// </summary>
    public static void Show(Vector3 worldPos, int actualDamage, int rawDamage, Color actualColor)
    {
        if (rawDamage == actualDamage)
        {
            Show(worldPos, actualDamage, actualColor);
            return;
        }
        Show(worldPos, $"-{actualDamage}(-{rawDamage})", actualColor);
    }

    public void Init(int damage, Color color)
    {
        InitText("-" + damage.ToString(), color);
    }

    /// <summary>用自定义文本初始化（🆕 v1.22 §10）</summary>
    public void InitText(string text, Color color)
    {
        _tmp = gameObject.AddComponent<TextMeshPro>();

        // R2：绑定中文字体，防止中文乱码
        var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font != null) _tmp.font = font;

        _tmp.text      = text;
        _tmp.color     = color;
        _tmp.fontSize  = 5f;
        _tmp.fontStyle = FontStyles.Bold;
        _tmp.alignment = TextAlignmentOptions.Center;

        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        var   startPos = transform.position;
        float duration = 1.2f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = startPos + Vector3.up * (t * 2f);

            if (_tmp != null)
            {
                var c = _tmp.color;
                c.a = 1f - t;
                _tmp.color = c;
            }

            // 始终面向摄像机
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            yield return null;
        }

        Destroy(gameObject);
    }
}
