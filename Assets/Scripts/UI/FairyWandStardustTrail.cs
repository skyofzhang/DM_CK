using UnityEngine;
using DrscfZ.Survival;

namespace DrscfZ.UI
{
    /// <summary>
    /// §34 Layer 2 组 A B8b 仙女棒矿工星尘拖尾
    ///
    /// 订阅 SurvivalGameManager.OnGiftImpact，giftId == "fairy_wand" 时：
    ///   给发送者 Worker 挂一个 1.5s 的 ParticleSystem（金黄色粒子 + 短 Lifetime + Rate 30），
    ///   1.5s 后自动 Destroy。
    ///
    /// 实现细节：
    ///   - 挂任意常驻 GO；无 UI 节点；粒子系统运行时 Instantiate 到 Worker 子节点（跟随移动）。
    ///   - 连续触发多次 → 每次都新建一个粒子子节点，互不干扰（短生命自动销毁）。
    ///   - 同一 Worker 短时间内多次触发允许叠加（视觉更炫）。
    ///
    /// 粒子参数：
    ///   StartColor  = 金黄 (1, 0.85, 0.2, 1)
    ///   StartSize   = 0.1
    ///   Lifetime    = 0.5
    ///   Rate        = 30 / sec
    ///   Duration    = 1.5s（1.5s 后 Stop + Destroy）
    /// </summary>
    public class FairyWandStardustTrail : MonoBehaviour
    {
        private bool _subscribed;

        private const float TRAIL_DURATION_SEC = 1.5f;
        private const float PARTICLE_LIFETIME  = 0.5f;
        private const float PARTICLE_START_SZ  = 0.1f;
        private const float PARTICLE_RATE      = 30f;

        private void Start()    { TrySubscribe(); }
        private void OnEnable() { TrySubscribe(); }
        private void OnDisable(){ Unsubscribe(); }
        private void OnDestroy(){ Unsubscribe(); }

        private void Update()
        {
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm == null) return;
            sgm.OnGiftImpact += HandleGiftImpact;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var sgm = SurvivalGameManager.Instance;
            if (sgm != null) sgm.OnGiftImpact -= HandleGiftImpact;
            _subscribed = false;
        }

        private void HandleGiftImpact(GiftImpactData data)
        {
            if (data == null) return;
            if (data.giftId != "fairy_wand") return;

            var wm = WorkerManager.Instance;
            if (wm == null) return;
            var worker = wm.GetWorkerByPlayerId(data.playerId);
            if (worker == null) return;

            AttachStardust(worker.transform);
        }

        private void AttachStardust(Transform workerRoot)
        {
            // 子节点跟随 Worker 移动
            var go = new GameObject("FairyWandStardust");
            go.transform.SetParent(workerRoot, false);
            go.transform.localPosition = Vector3.up * 0.8f;  // 矿工腰部

            var ps = go.AddComponent<ParticleSystem>();

            // 主模块
            var main = ps.main;
            main.duration      = TRAIL_DURATION_SEC;
            main.loop          = false;
            main.startLifetime = PARTICLE_LIFETIME;
            main.startSize     = PARTICLE_START_SZ;
            main.startColor    = new Color(1f, 0.85f, 0.2f, 1f);
            main.startSpeed    = 0.4f;
            main.maxParticles  = 128;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 发射
            var emission = ps.emission;
            emission.rateOverTime = PARTICLE_RATE;

            // 形状：小球体
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            // 颜色/尺寸曲线（淡出）
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f),
                        new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(0.8f, 0.8f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // 简单的默认渲染器材质（内置 Default-Particle）
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();

            // 1.5s 后销毁整棵子节点
            Destroy(go, TRAIL_DURATION_SEC + PARTICLE_LIFETIME + 0.1f);
        }
    }
}
