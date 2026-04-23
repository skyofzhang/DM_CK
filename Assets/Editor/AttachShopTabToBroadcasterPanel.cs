using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Reflection;
using DrscfZ.UI;

namespace DrscfZ.Editor
{
    /// <summary>
    /// Tools → DrscfZ → Attach Shop Tab to BroadcasterPanel
    ///
    /// 查找场景中 BroadcasterPanel，将其 _shopTabButton 的 onClick 绑定到
    /// ShopUI.OpenPanel("A") （Persistent Listener，避免运行时 OnClick 丢失）。
    ///
    /// 若：
    ///   - BroadcasterPanel 未找到 → LogError 终止（必须先跑 SetupBroadcasterPanel 等前置脚本）
    ///   - _shopTabButton 未绑定 → 委托 SetupSection36UI 已有的占位按钮创建逻辑（若未执行则 LogWarning）
    ///   - ShopUI 未找到 → LogWarning 继续（按钮可手动绑，但推荐先跑 SetupShopUI）
    ///
    /// 使用 UnityEventTools.AddVoidPersistentListener（参考 BindBtnSettings.cs）。
    /// </summary>
    public static class AttachShopTabToBroadcasterPanel
    {
        [MenuItem("Tools/DrscfZ/Attach Shop Tab to BroadcasterPanel")]
        public static void Execute()
        {
            // 1. 找 BroadcasterPanel
            var bp = Object.FindObjectOfType<BroadcasterPanel>(true);
            if (bp == null)
            {
                Debug.LogError("[AttachShopTab] 场景中找不到 BroadcasterPanel 组件，终止。");
                return;
            }

            // 2. 找 ShopUI
            var shopUI = Object.FindObjectOfType<ShopUI>(true);
            if (shopUI == null)
            {
                Debug.LogWarning("[AttachShopTab] 场景中找不到 ShopUI（请先跑 Setup Section 39 Shop UI）。跳过 Persistent Listener 绑定。");
                return;
            }

            // 3. 反射拿 _shopTabButton（私有 SerializeField）
            var field = typeof(BroadcasterPanel).GetField("_shopTabButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogError("[AttachShopTab] BroadcasterPanel 未定义 _shopTabButton 字段。");
                return;
            }

            var btn = field.GetValue(bp) as Button;
            if (btn == null)
            {
                Debug.LogWarning("[AttachShopTab] _shopTabButton 未绑定（Setup36UI 占位按钮可能未建）。请先跑 Tools/DrscfZ/Setup Section36 UI。");
                return;
            }

            // 4. 清旧监听 + 加 Persistent Listener（Inspector 可见）
            btn.onClick.RemoveAllListeners();

            // 清理旧的 persistent calls 避免重复
            var soBtn = new SerializedObject(btn);
            var calls = soBtn.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null) calls.arraySize = 0;
            soBtn.ApplyModifiedPropertiesWithoutUndo();

            // 用 OpenPanel("A") 作为默认入口（ShopUI.OpenPanel 有默认参数 "A"，但 Persistent
            // 绑定需要带参数版本以便写入常量参数到 serializedEvent）
            var method = typeof(ShopUI).GetMethod("OpenPanel",
                BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogError("[AttachShopTab] ShopUI 未定义 OpenPanel(string) 方法。");
                return;
            }

            // UnityEventTools.AddStringPersistentListener → 1 个 string 参数的 UnityAction<string>
            var action = (UnityEngine.Events.UnityAction<string>)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction<string>), shopUI, method);
            UnityEventTools.AddStringPersistentListener(btn.onClick, action, "A");

            EditorUtility.SetDirty(btn);
            EditorUtility.SetDirty(bp);
            EditorSceneManager.MarkSceneDirty(bp.gameObject.scene);

            Debug.Log($"[AttachShopTab] 已绑定：BroadcasterPanel._shopTabButton.onClick → ShopUI.OpenPanel(\"A\")（Persistent）");
        }
    }
}
