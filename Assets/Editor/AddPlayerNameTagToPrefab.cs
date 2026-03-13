using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrscfZ.UI;

/// <summary>
/// 为 CowWorker Prefab 添加头顶名字标签（PlayerNameTag + WorldSpace Canvas）
/// 执行入口：Execute()
/// </summary>
public class AddPlayerNameTagToPrefab
{
    private const string PrefabPath = "Assets/Prefabs/Characters/CowWorker.prefab";

    public static string Execute()
    {
        // 加载 Prefab
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null)
            return "ERROR: 找不到 Prefab：" + PrefabPath;

        // 打开 Prefab 编辑模式
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            var root = scope.prefabContentsRoot;

            // --- 1. 检查是否已存在 PlayerNameTag ---
            var existingTag = root.GetComponentInChildren<PlayerNameTag>(true);
            if (existingTag != null)
                return "PlayerNameTag 已存在（GameObject: " + existingTag.gameObject.name + "），无需重复添加。";

            // --- 2. 创建 NameTag GameObject（WorldSpace Canvas 载体） ---
            var nameTagGO = new GameObject("NameTag");
            nameTagGO.transform.SetParent(root.transform, false);
            nameTagGO.transform.localPosition = new Vector3(0f, 2.5f, 0f); // 头顶偏移
            nameTagGO.transform.localRotation = Quaternion.identity;
            nameTagGO.transform.localScale = Vector3.one;
            nameTagGO.SetActive(false); // 初始隐藏，由 Initialize() 激活

            // --- 3. 添加 WorldSpace Canvas ---
            var canvas = nameTagGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            // 设置 Canvas 的 RectTransform 尺寸和缩放
            var canvasRect = nameTagGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 120f);
            nameTagGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // WorldSpace 缩放

            // --- 4. 添加 CanvasScaler（可选，WorldSpace 下作用有限，但保持规范） ---
            var scaler = nameTagGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // --- 5. 创建头像 RawImage 子对象 ---
            var avatarGO = new GameObject("AvatarImage");
            avatarGO.transform.SetParent(nameTagGO.transform, false);
            var avatarRect = avatarGO.AddComponent<RectTransform>();
            avatarRect.anchoredPosition = new Vector2(-150f, 0f);
            avatarRect.sizeDelta = new Vector2(80f, 80f);
            var rawImage = avatarGO.AddComponent<RawImage>();
            rawImage.color = new Color(1f, 1f, 1f, 0f); // 初始透明，加载后显示

            // --- 6. 创建名字 TMP_Text 子对象 ---
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(nameTagGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchoredPosition = new Vector2(20f, 0f);
            nameRect.sizeDelta = new Vector2(280f, 80f);

            var tmpText = nameGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = "玩家名";
            tmpText.fontSize = 36f;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpText.overflowMode = TextOverflowModes.Ellipsis;

            // 绑定中文字体（必须）
            var font = Resources.Load<TMP_FontAsset>("Fonts/ChineseFont SDF");
            if (font != null)
            {
                tmpText.font = font;
                Debug.Log("[AddPlayerNameTagToPrefab] 已绑定 ChineseFont SDF");
            }
            else
            {
                Debug.LogWarning("[AddPlayerNameTagToPrefab] 未找到 Fonts/ChineseFont SDF，中文可能乱码");
            }

            // --- 7. 在 NameTag GameObject 上挂 PlayerNameTag 脚本并注入引用 ---
            var nameTag = nameTagGO.AddComponent<PlayerNameTag>();

            // 通过 SerializedObject 注入私有 [SerializeField] 字段
            var so = new SerializedObject(nameTag);
            so.FindProperty("_nameText").objectReferenceValue  = tmpText;
            so.FindProperty("_avatarImage").objectReferenceValue = rawImage;
            so.FindProperty("_canvas").objectReferenceValue    = canvas;
            so.FindProperty("_offset").vector3Value            = new Vector3(0f, 2.5f, 0f);
            so.ApplyModifiedProperties();

            Debug.Log("[AddPlayerNameTagToPrefab] PlayerNameTag 已添加到 CowWorker Prefab，字段注入完毕。");
        }

        // 刷新资源数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return "SUCCESS: PlayerNameTag 已成功添加到 " + PrefabPath;
    }
}
