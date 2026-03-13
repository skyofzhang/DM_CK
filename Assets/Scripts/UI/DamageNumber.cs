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

    public void Init(int damage, Color color)
    {
        _tmp = gameObject.AddComponent<TextMeshPro>();

        // R2：绑定中文字体，防止中文乱码
        var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
        if (font != null) _tmp.font = font;

        _tmp.text      = "-" + damage.ToString();
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
