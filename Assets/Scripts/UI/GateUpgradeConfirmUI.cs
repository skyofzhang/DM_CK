using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DrscfZ.UI
{
    /// <summary>
    /// 城门升级确认弹窗（🆕 v1.22 §10 城门升级系统 v2）
    ///
    /// 挂载规则（Rule #6 / #7）：
    ///   - 本脚本挂在常驻 Canvas 的子 GameObject "GateUpgradeConfirmUI" 上（always-active）
    ///   - _panel 字段是挂载对象下的子 Panel（Image 遮罩），Awake 中 SetActive(false) 合法
    ///   - 禁止在 Awake 中 SetActive(false) 破坏脚本自身（本类不这样做）
    ///
    /// 触发流程：
    ///   1. BroadcasterPanel 点击"升级城门" → GateUpgradeConfirmUI.ShowConfirm(...)
    ///   2. 用户点击确认 → onConfirm 回调发送 upgrade_gate 消息
    ///   3. 用户点击取消 → 仅隐藏面板
    /// </summary>
    public class GateUpgradeConfirmUI : MonoBehaviour
    {
        public static GateUpgradeConfirmUI Instance { get; private set; }

        [Header("面板根（子 Panel，Awake 中 SetActive(false) 合法）")]
        [SerializeField] private GameObject      _panel;

        [Header("文本（Editor 工具 CreateGateUpgradeUI 自动绑定）")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _currentLevelText;
        [SerializeField] private TextMeshProUGUI _nextLevelText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _featuresText;

        [Header("按钮")]
        [SerializeField] private Button _btnConfirm;
        [SerializeField] private Button _btnCancel;

        private Action _onConfirm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 子 Panel 默认隐藏（不是自身，符合 Rule #6）
            if (_panel != null)
                _panel.SetActive(false);

            if (_btnConfirm != null)
                _btnConfirm.onClick.AddListener(OnConfirm);
            if (_btnCancel != null)
                _btnCancel.onClick.AddListener(OnCancel);
        }

        /// <summary>
        /// 显示升级确认弹窗。
        /// </summary>
        /// <param name="currentLevel">当前城门等级</param>
        /// <param name="nextLevel">升级后等级</param>
        /// <param name="cost">矿石消耗</param>
        /// <param name="nextTierName">下一级层级名（"铁栅"/.../"巨龙要塞"）</param>
        /// <param name="newFeatureDesc">新特性说明文本</param>
        /// <param name="onConfirm">确认回调（发送 upgrade_gate 消息）</param>
        public void ShowConfirm(int currentLevel, int nextLevel, int cost,
                                string nextTierName, string newFeatureDesc, Action onConfirm)
        {
            _onConfirm = onConfirm;

            if (_titleText != null)
                _titleText.text = "升级城门";
            if (_currentLevelText != null)
                _currentLevelText.text = $"当前 Lv.{currentLevel}";
            if (_nextLevelText != null)
            {
                string tierLabel = string.IsNullOrEmpty(nextTierName) ? "" : $"「{nextTierName}」";
                _nextLevelText.text = $"→ Lv.{nextLevel}{tierLabel}";
            }
            if (_costText != null)
                _costText.text = $"消耗矿石 × {cost}";
            if (_featuresText != null)
                _featuresText.text = newFeatureDesc ?? "";

            // §17.16 A 类 modal
            if (_panel != null)
                ModalRegistry.TryOpenModalA(_panel);
        }

        /// <summary>外部主动关闭（例如等级变化导致按钮失效时）</summary>
        public void Hide()
        {
            if (_panel != null)
                ModalRegistry.CloseModalA(_panel);
            _onConfirm = null;
        }

        private void OnConfirm()
        {
            if (_panel != null)
                ModalRegistry.CloseModalA(_panel);
            var cb = _onConfirm;
            _onConfirm = null;
            cb?.Invoke();
        }

        private void OnCancel()
        {
            if (_panel != null)
                ModalRegistry.CloseModalA(_panel);
            _onConfirm = null;
        }
    }
}
