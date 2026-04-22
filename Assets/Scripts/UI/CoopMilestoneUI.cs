using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34.4 E3b 协作里程碑 —— 全服累计贡献进度条
    ///
    /// 订阅：
    ///   - SurvivalGameManager.OnResourceUpdate：从 ResourceUpdateData.totalContribution 推进度条
    ///   - SurvivalGameManager.OnCoopMilestone：阈值达成时全屏公告 + 刷新下一目标
    ///
    /// 文案模板（策划案 5298-5306）：
    ///   进度条下方："{current}/{nextTarget} — 再 {gap} 解锁 {nextReward}"
    ///   达标公告全屏："{name} 解锁！{desc}"（3s，大字，淡入淡出）
    ///
    /// 里程碑表（客户端镜像，与服务端 §34.4 E3b 对齐）：
    ///   500    众志成城  — 全员效率 +10%
    ///   2000   钢铁意志  — 城门修复速度 x2
    ///   5000   极地奇迹  — 全矿工 HP +50
    ///   10000  传说降临  — 所有效果 +20%
    ///   20000  不朽证明  — 一次免费死亡豁免
    ///
    /// 挂载规则：
    ///   挂 Canvas/GameUIPanel/CoopMilestoneBar（常驻激活）。
    ///
    /// Inspector 必填：
    ///   _progressFill      — 进度条填充 Image（Type=Filled，fillMethod=Horizontal）
    ///   _progressText      — 进度文字 TMP（current/target + gap 提示）
    ///   _milestoneNameText — 当前目标里程碑名（可选，用于进度条上方标题）
    ///   _announceRoot      — 达标全屏公告容器
    ///   _announceCanvasGroup — 公告 CanvasGroup
    ///   _announceNameText  — 公告主文字
    ///   _announceDescText  — 公告副文字
    /// </summary>
    public class CoopMilestoneUI : MonoBehaviour
    {
        [Header("进度条")]
        [SerializeField] private Image           _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _milestoneNameText;

        [Header("达标全屏公告")]
        [SerializeField] private RectTransform   _announceRoot;
        [SerializeField] private CanvasGroup     _announceCanvasGroup;
        [SerializeField] private TextMeshProUGUI _announceNameText;
        [SerializeField] private TextMeshProUGUI _announceDescText;

        // ── 里程碑表（客户端镜像，用于 nextTarget=0 时兜底下一级计算） ────
        private static readonly (int target, string name, string reward)[] MILESTONES = new[]
        {
            (500,   "众志成城", "全员效率 +10%"),
            (2000,  "钢铁意志", "城门修复速度 x2"),
            (5000,  "极地奇迹", "全矿工 HP +50"),
            (10000, "传说降临", "所有效果 +20%"),
            (20000, "不朽证明", "一次免费死亡豁免"),
        };

        private const float ANNOUNCE_DUR = 3.0f;
        private const float ANNOUNCE_FADE_IN  = 0.3f;
        private const float ANNOUNCE_FADE_OUT = 0.4f;

        // ── 内部状态 ──────────────────────────────────────────────────────

        private int    _currentContribution = 0;
        private int    _currentNextTarget   = 500;   // 默认第一个目标
        private string _currentNextName     = "众志成城";
        private string _currentNextReward   = "全员效率 +10%";

        private Coroutine _announceCoroutine;
        private bool      _subscribed = false;

        // ── 生命周期 ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_announceRoot != null) _announceRoot.gameObject.SetActive(false);
            if (_announceCanvasGroup != null) _announceCanvasGroup.alpha = 0f;

            TrySubscribe();
            RefreshProgress();
        }

        private void OnEnable()  { TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnResourceUpdate += HandleResourceUpdate;
            sgm.OnCoopMilestone  += HandleCoopMilestone;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null)
            {
                sgm.OnResourceUpdate -= HandleResourceUpdate;
                sgm.OnCoopMilestone  -= HandleCoopMilestone;
            }
            _subscribed = false;
        }

        // ── 事件回调 ──────────────────────────────────────────────────────

        private void HandleResourceUpdate(ResourceUpdateData data)
        {
            if (data == null) return;
            _currentContribution = Mathf.Max(0, data.totalContribution);
            RefreshProgress();
        }

        private void HandleCoopMilestone(CoopMilestoneData d)
        {
            if (d == null) return;

            // 全屏公告
            if (_announceCoroutine != null) StopCoroutine(_announceCoroutine);
            _announceCoroutine = StartCoroutine(PlayAnnounce(d.name, d.desc));

            // 更新下一目标（服务端下发 >0 时采用；=0 或 <0 视为已封顶）
            // 后端 null → JsonUtility 解析 0；策划约定 -1 也表示封顶（防御性处理）
            if (d.nextTarget > 0)
            {
                _currentNextTarget = d.nextTarget;
                // 查本地表找名称/奖励（若服务端没单独传）
                var (hitName, hitReward) = FindMilestoneByTarget(d.nextTarget);
                _currentNextName   = hitName   ?? "下一目标";
                _currentNextReward = hitReward ?? "";
            }
            else
            {
                // 0 或 -1 = 已达最高层级，隐藏"下一级"提示
                _currentNextTarget = 0;
                _currentNextName   = "";
                _currentNextReward = "";
            }

            RefreshProgress();
        }

        // ── 刷新 ──────────────────────────────────────────────────────────

        private void RefreshProgress()
        {
            // 进度条
            if (_progressFill != null)
            {
                if (_currentNextTarget > 0)
                {
                    float ratio = Mathf.Clamp01((float)_currentContribution / _currentNextTarget);
                    _progressFill.fillAmount = ratio;
                }
                else
                {
                    _progressFill.fillAmount = 1f;
                }
            }

            // 进度文字
            if (_progressText != null)
            {
                if (_currentNextTarget > 0)
                {
                    int gap = Mathf.Max(0, _currentNextTarget - _currentContribution);
                    _progressText.text = $"{_currentContribution}/{_currentNextTarget} — 再 {gap} 解锁 {_currentNextReward}";
                }
                else
                {
                    _progressText.text = $"{_currentContribution} (已达最高层级)";
                }
            }

            if (_milestoneNameText != null)
            {
                _milestoneNameText.text = _currentNextTarget > 0
                    ? $"协作目标：{_currentNextName}"
                    : "协作目标：已全部解锁";
            }
        }

        private (string, string) FindMilestoneByTarget(int target)
        {
            foreach (var m in MILESTONES)
            {
                if (m.target == target) return (m.name, m.reward);
            }
            return (null, null);
        }

        // ── 公告动画 ──────────────────────────────────────────────────────

        private IEnumerator PlayAnnounce(string name, string desc)
        {
            if (_announceRoot == null || _announceCanvasGroup == null) yield break;

            _announceRoot.gameObject.SetActive(true);

            if (_announceNameText != null) _announceNameText.text = $"{name} 解锁！";
            if (_announceDescText != null) _announceDescText.text = desc ?? "";

            // 淡入
            float t = 0f;
            while (t < ANNOUNCE_FADE_IN)
            {
                t += Time.unscaledDeltaTime;
                _announceCanvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / ANNOUNCE_FADE_IN));
                yield return null;
            }
            _announceCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(ANNOUNCE_DUR);

            // 淡出
            t = 0f;
            while (t < ANNOUNCE_FADE_OUT)
            {
                t += Time.unscaledDeltaTime;
                _announceCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / ANNOUNCE_FADE_OUT));
                yield return null;
            }
            _announceCanvasGroup.alpha = 0f;
            _announceRoot.gameObject.SetActive(false);
            _announceCoroutine = null;
        }
    }
}
