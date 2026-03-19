using System.Collections;
using UnityEngine;

namespace DrscfZ.Survival
{
    /// <summary>
    /// Worker视觉控制组件 - 管理Worker的材质颜色和外观状态
    /// 挂在Worker GameObject上（与WorkerController同一对象）
    /// </summary>
    public class WorkerVisual : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer _bodyRenderer;

        [Header("Materials")]
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _glowMaterial;   // 金色发光（666/boost）
        [SerializeField] private Material _frozenMaterial; // 冰蓝（冻结）

        private Color _currentColor = Color.white;

        // 工位颜色映射（索引对应 commandId）
        private static readonly Color[] WORK_COLORS = {
            Color.white,                                    // [0] unused
            new Color(0.267f, 0.533f, 1.0f),               // [1] 采食物 #4488FF 蓝
            new Color(0.4f,   0.4f,   0.4f),               // [2] 挖煤   #666666 深灰
            new Color(0.533f, 0.8f,   1.0f),               // [3] 挖矿   #88CCFF 冰蓝
            new Color(1.0f,   0.408f, 0.125f),             // [4] 生火   #FF6820 橙红
            Color.white,                                    // [5] unused
            new Color(1.0f,   0.133f, 0.0f),               // [6] 打怪   #FF2200 鲜红
        };

        private void Awake()
        {
            if (_bodyRenderer == null)
                _bodyRenderer = GetComponent<Renderer>()
                             ?? GetComponentInChildren<Renderer>(true);
        }

        /// <summary>设置工作颜色（根据指令类型）</summary>
        public void SetWorkColor(int cmdType)
        {
            if (cmdType < 0 || cmdType >= WORK_COLORS.Length) return;
            _currentColor = WORK_COLORS[cmdType];
            ApplyColor(_currentColor);
        }

        /// <summary>激活金色光晕（666弹幕/主播加速），duration秒后自动恢复</summary>
        public void ActivateGlow(float duration)
        {
            CancelInvoke(nameof(RestoreNormalMaterial));
            if (_glowMaterial != null)
                _bodyRenderer.material = _glowMaterial;
            Invoke(nameof(RestoreNormalMaterial), duration);
        }

        /// <summary>激活冻结状态，duration秒后自动恢复</summary>
        public void ActivateFrozen(float duration)
        {
            CancelInvoke(nameof(RestoreNormalMaterial));
            if (_frozenMaterial != null)
                _bodyRenderer.material = _frozenMaterial;
            Invoke(nameof(RestoreNormalMaterial), duration);
        }

        private void RestoreNormalMaterial()
        {
            if (_bodyRenderer == null) return;
            if (_normalMaterial != null)
                _bodyRenderer.material = _normalMaterial;
            ApplyColor(_currentColor);
        }

        private void ApplyColor(Color color)
        {
            if (_bodyRenderer == null) return;
            // 动态创建材质实例，避免修改共享材质
            var mat = _bodyRenderer.material;
            mat.color = color;
        }

        /// <summary>手动设置冻结外观（不自动恢复，由外部调用 SetFrozen(false) 恢复）</summary>
        public void SetFrozen(bool frozen)
        {
            if (_bodyRenderer == null) return;
            if (frozen)
            {
                CancelInvoke(nameof(RestoreNormalMaterial));
                if (_frozenMaterial != null)
                    _bodyRenderer.material = _frozenMaterial;
            }
            else
            {
                RestoreNormalMaterial();
            }
        }

        /// <summary>重置为默认外观（白色，正常材质）</summary>
        public void Reset()
        {
            CancelInvoke();
            _currentColor = Color.white;
            RestoreNormalMaterial();
        }

        /// <summary>被指派工作时短暂白色闪烁（T214）</summary>
        public void TriggerAssignmentFlash()
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(AssignmentFlashCoroutine());
        }

        private Coroutine _flashCoroutine;

        private IEnumerator AssignmentFlashCoroutine()
        {
            if (_bodyRenderer == null) yield break;
            Color saved = _currentColor;
            ApplyColor(Color.white);
            yield return new WaitForSeconds(0.15f);
            ApplyColor(saved);
            yield return new WaitForSeconds(0.1f);
            ApplyColor(Color.white);
            yield return new WaitForSeconds(0.1f);
            ApplyColor(saved);
            _flashCoroutine = null;
        }
    }
}
