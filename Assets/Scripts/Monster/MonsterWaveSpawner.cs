using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DrscfZ.Survival;

namespace DrscfZ.Monster
{
    /// <summary>
    /// 怪物波次生成器
    /// 波次参数来自服务器 monster_wave 消息 或 本地 WaveConfig
    /// 参考《极寒之夜》设计：beginning延迟 + 每 refreshTime 生成一批 baseCount 只
    /// </summary>
    public class MonsterWaveSpawner : MonoBehaviour
    {
        public static MonsterWaveSpawner Instance { get; private set; }

        [Header("怪物 Prefab")]
        [SerializeField] private GameObject _monsterPrefab;  // 怪物外观A（KuanggongMonster_03）
        [SerializeField] private GameObject _monsterPrefab2; // 怪物外观B（KuanggongMonster_04，随机选一）
        [SerializeField] private GameObject _bossPrefab;     // Boss专用外观（KuanggongBoss_05）

        [Header("生成点（从场景中拖入）")]
        [SerializeField] private Transform _spawnLeft;
        [SerializeField] private Transform _spawnRight;
        [SerializeField] private Transform _spawnTop;

        [Header("城门目标")]
        [SerializeField] private Transform _cityGateTarget;

        [Header("怪物缩放（Prefab根节点已归一化为(1,1,1)，默认1.0）")]
        [SerializeField] private float _monsterScale = 1.0f;

        [Header("性能：同屏最大活跃怪物数（超出时排队等死亡后再生成）")]
        [SerializeField] private int _maxAliveMonsters = 15;

        [Header("内置波次配置（服务器未推送时使用）")]
        [SerializeField] private WaveConfig[] _localWaveConfigs;

        private List<MonsterController> _activeMonsters = new List<MonsterController>();

        /// <summary>当前活跃怪物只读列表（供 WorkerController 避免每帧 FindObjectsOfType）</summary>
        public IReadOnlyList<MonsterController> ActiveMonsters => _activeMonsters;

        private int _currentDay = 0;
        private bool _spawning  = false;

        // 事件
        public event Action<int> OnWaveStarted;       // waveIndex
        public event Action<int> OnAllMonstersDead;   // day

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 初始化默认波次配置
            if (_localWaveConfigs == null || _localWaveConfigs.Length == 0)
            {
                _localWaveConfigs = DefaultWaveConfigs();
            }
        }

        // 🆕 Fix B (组 B Reviewer P0) §34B B3 heavy_fog：
        //   缓存上一次 hideMonsterHp 状态，仅在切换时刷新所有怪物血条，避免每次 resource_update 重复 SetActive。
        private bool _hideMonsterHpCached = false;

        private void Start()
        {
            // 订阅白天开始事件：夜晚结束时强制清除场上残余怪物
            if (DayNightCycleManager.Instance != null)
                DayNightCycleManager.Instance.OnDayStarted += HandleDayStarted;

            // 🆕 Fix B §34B B3 heavy_fog：订阅 resource_update，按 data.hideMonsterHp 切换所有存活怪物血条
            if (DrscfZ.Survival.SurvivalGameManager.Instance != null)
                DrscfZ.Survival.SurvivalGameManager.Instance.OnResourceUpdate += HandleResourceUpdate;
        }

        private void OnDestroy()
        {
            if (DayNightCycleManager.Instance != null)
                DayNightCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (DrscfZ.Survival.SurvivalGameManager.Instance != null)
                DrscfZ.Survival.SurvivalGameManager.Instance.OnResourceUpdate -= HandleResourceUpdate;
        }

        // 🆕 Fix B (组 B Reviewer P0) §34B B3：heavy_fog 期间强制隐藏所有怪物血条（30s），结束后恢复。
        //   新生成的怪物（OnResourceUpdate 之后的 SpawnWave）会因为 _hideMonsterHpCached=true 在 Initialize 完成后
        //   需要下一次 resource_update 触发来隐藏；重要：SpawnWithVariants/LegacySpawn 添加 _activeMonsters 后立即 apply 当前状态。
        private void HandleResourceUpdate(Survival.ResourceUpdateData ru)
        {
            if (ru == null) return;
            if (_hideMonsterHpCached == ru.hideMonsterHp) return;
            _hideMonsterHpCached = ru.hideMonsterHp;
            // 遍历存活怪物同步血条可见性
            for (int i = 0; i < _activeMonsters.Count; i++)
            {
                var m = _activeMonsters[i];
                if (m == null || m.IsDead) continue;
                m.SetHpBarVisible(!_hideMonsterHpCached);
            }
        }

        /// <summary>🆕 Fix B §34B B3：当前缓存的 hideMonsterHp 状态（供新生成怪物 apply 当前状态）</summary>
        public bool HideMonsterHp => _hideMonsterHpCached;

        /// <summary>🆕 Fix B §34B B3：新生成的怪物若当前处于 fog 期应立即隐藏血条（避免闪烁）</summary>
        private void ApplyInitialHpBarState(MonsterController m)
        {
            if (m == null) return;
            if (_hideMonsterHpCached) m.SetHpBarVisible(false);
        }

        private void HandleDayStarted(int day)
        {
            if (_activeMonsters.Count > 0)
            {
                Debug.Log($"[WaveSpawner] 白天到来，强制清除剩余 {_activeMonsters.Count} 只怪物");
                ResetAll();
            }
        }

        // ==================== 服务器消息驱动 ====================

        /// <summary>服务器推送 monster_wave → 生成怪物。
        /// 🆕 §31 多样性系统：优先走 monsters[] 数组（含 variant），否则回落到旧 count 路径。</summary>
        public void SpawnWave(Survival.MonsterWaveData data)
        {
            if (data == null) return;
            _currentDay = data.day;

            // 🆕 §31 新路径：data.monsters[] 非空 → 逐只按 variant 生成
            if (data.monsters != null && data.monsters.Length > 0)
            {
                StartCoroutine(SpawnWithVariants(data));
                return;
            }

            // 旧路径：仅按 count 生成 normal 怪（向后兼容）
            var config = new WaveConfig
            {
                monsterId  = data.monsterId ?? "X_guai01",
                count      = data.count,
                spawnSide  = data.spawnSide ?? "all",
                beginning  = 0,            // 服务器控制延迟
                refreshTime = 0,
                maxCount   = data.count
            };
            StartCoroutine(SpawnBatch(config, data.waveIndex));
        }

        /// <summary>🆕 §31 变种怪生成协程：逐只解析 MonsterSpawnInfo，传入 variant。
        /// isSummonSpawn=true 时：spawn 位置从屏幕中心随机偏移（迷你怪从死亡怪位置冒出的视觉占位）。</summary>
        private IEnumerator SpawnWithVariants(Survival.MonsterWaveData data)
        {
            OnWaveStarted?.Invoke(data.waveIndex);

            bool isSummon  = data.isSummonSpawn;
            string side    = data.spawnSide ?? "all";
            // 🔴 audit-r37 GAP-A37-02：bypassCap 守卫 — 主播轮盘 elite_raid 卡触发的精英怪不受 maxAliveMonsters 限制
            //   服务端 SurvivalDataTypes.cs:252 MonsterWaveData.bypassCap 字段 r35 已加但客户端 SpawnWave 路径绕过
            //   旧版当 _activeMonsters.Count >= 15 时排队 0.5s/tick，"30s deadline" 玩法体感失效
            //   r37 真闭环 — bypassCap=true 时跳过 maxAliveMonsters 检查，立即 SpawnOneVariantMonster
            bool bypassCap = data.bypassCap;

            foreach (var info in data.monsters)
            {
                // 同屏上限控制（与 SpawnBatch 一致）；bypassCap=true 时跳过（精英来袭专属）
                while (!bypassCap && _activeMonsters.Count >= _maxAliveMonsters)
                    yield return new WaitForSeconds(0.5f);

                // 变种解析：字符串 → 枚举（失败退回 Normal）
                MonsterVariant variant = ParseVariant(info != null ? info.variant : null);

                // spawn 位置：isSummonSpawn 时走地图中心附近随机；否则按 spawnSide
                Vector3 spawnPos = isSummon ? GetSummonSpawnPos() : GetSpawnPos(side);

                SpawnOneVariantMonster(info, variant, spawnPos);
                yield return new WaitForSeconds(0.05f + UnityEngine.Random.Range(0f, 0.05f));
            }
        }

        /// <summary>variant 字符串 → 枚举（大小写不敏感，未知值 → Normal）</summary>
        private static MonsterVariant ParseVariant(string s)
        {
            if (string.IsNullOrEmpty(s)) return MonsterVariant.Normal;
            switch (s.ToLowerInvariant())
            {
                case "rush":     return MonsterVariant.Rush;
                case "assassin": return MonsterVariant.Assassin;
                case "ice":      return MonsterVariant.Ice;
                case "summoner": return MonsterVariant.Summoner;
                case "guard":    return MonsterVariant.Guard;
                case "mini":     return MonsterVariant.Mini;
                default:         return MonsterVariant.Normal;
            }
        }

        /// <summary>🆕 §31 isSummonSpawn spawn 位置：地图中心区域（X=±3, Z=10±3）随机偏移。
        /// TODO: 理想做法是服务端提供召唤怪死亡位置（monsterId→pos 映射）；MVP 按中心硬编码，
        /// 如与实际场景偏差较大，PM 合并后在 main repo 内手测调整（Scene/Camera 视角校正）。</summary>
        private static Vector3 GetSummonSpawnPos()
        {
            return new Vector3(
                UnityEngine.Random.Range(-3f, 3f),
                0f,
                UnityEngine.Random.Range(7f, 13f));
        }

        /// <summary>🆕 §31 按 variant 生成单只怪物（与 SpawnOneMonster 同基础流程，差异在 Initialize 传入 variant/hp）。</summary>
        private void SpawnOneVariantMonster(Survival.MonsterSpawnInfo info, MonsterVariant variant, Vector3 spawnPos)
        {
            GameObject go;
            // 随机选择两套怪物外观（Guard 可复用 monsterPrefab/monsterPrefab2，美术后续可替换）
            var prefabToUse = (_monsterPrefab2 != null && UnityEngine.Random.value > 0.5f)
                              ? _monsterPrefab2 : _monsterPrefab;
            if (prefabToUse != null)
            {
                go = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                float s = Mathf.Max(0.001f, _monsterScale);
                go.transform.localScale = Vector3.one * s;
            }
            else
            {
                go = new GameObject("VariantMonster_Placeholder");
                go.transform.position = spawnPos;
                Debug.LogWarning("[WaveSpawner] Monster prefab missing! Variant monster uses invisible placeholder.");
            }

            // ID：优先用 info.monsterId，否则 fallback
            string monsterId = (info != null && !string.IsNullOrEmpty(info.monsterId))
                ? info.monsterId
                : $"variant_{variant}_{_activeMonsters.Count}_{UnityEngine.Random.Range(1000, 9999)}";
            go.name = $"Monster_{variant}_{monsterId}";

            var ctrl = go.GetComponent<MonsterController>() ?? go.AddComponent<MonsterController>();

            // HP：优先用服务端下发；否则按当前天数公式（与普通怪一致）
            int hp = (info != null && info.hp > 0)
                ? info.hp
                : Mathf.RoundToInt(20f + _currentDay * 8f);
            int   atk = Mathf.RoundToInt(3f + _currentDay * 1.5f);
            // 🆕 §31 速度：优先用服务端下发（已按 variant 乘倍率 Rush×1.6 / Ice×0.85 / Summoner×0.9）；
            //     info.speed<=0 时回退到旧公式（旧协议兼容）
            float spd = (info != null && info.speed > 0f)
                ? info.speed
                : 1.8f + _currentDay * 0.1f;

            // 🆕 §31 走 variant 重载（内部会 ApplyVariantTint + Rush 锁城门）
            ctrl.Initialize(hp, atk, spd, _cityGateTarget, variant);

            // MonsterType：服务端下发的 type 字符串 → 枚举
            MonsterType monsterType = ParseMonsterType(info != null ? info.type : null);
            ctrl.SetMonsterIdAndType(monsterId, monsterType);

            ctrl.OnDead += HandleMonsterDead;
            _activeMonsters.Add(ctrl);
            ApplyInitialHpBarState(ctrl); // Fix B §34B B3：heavy_fog 期内新生成怪物同步隐藏血条
        }

        /// <summary>type 字符串 → MonsterType 枚举（未知 → Normal）</summary>
        private static MonsterType ParseMonsterType(string s)
        {
            if (string.IsNullOrEmpty(s)) return MonsterType.Normal;
            switch (s.ToLowerInvariant())
            {
                case "elite": return MonsterType.Elite;
                case "boss":  return MonsterType.Boss;
                default:      return MonsterType.Normal;
            }
        }

        // ==================== 本地驱动（测试/演示用）====================

        /// <summary>夜晚开始时自动刷怪（本地Config）</summary>
        public void StartNightWaves(int day)
        {
            if (_spawning) return;
            _currentDay = day;
            _spawning   = true;
            StartCoroutine(RunLocalWaves(day));
        }

        private IEnumerator RunLocalWaves(int day)
        {
            // 找到对应天数的 Config（超出则用最后一个）
            int idx = Mathf.Min(day - 1, _localWaveConfigs.Length - 1);
            var cfg = _localWaveConfigs[idx];

            // beginning 延迟
            if (cfg.beginning > 0)
                yield return new WaitForSeconds(cfg.beginning);

            int batchIndex = 0;
            int totalSpawned = 0;

            while (totalSpawned < cfg.maxCount)
            {
                int batchCount = Mathf.Min(cfg.count, cfg.maxCount - totalSpawned);
                StartCoroutine(SpawnBatch(cfg, batchIndex));
                totalSpawned += batchCount;
                batchIndex++;

                if (totalSpawned < cfg.maxCount)
                    yield return new WaitForSeconds(cfg.refreshTime);
            }

            _spawning = false;
        }

        private IEnumerator SpawnBatch(WaveConfig cfg, int batchIdx)
        {
            OnWaveStarted?.Invoke(batchIdx);

            for (int i = 0; i < cfg.count; i++)
            {
                // 同屏怪物达上限时等待，死亡后自动续生
                while (_activeMonsters.Count >= _maxAliveMonsters)
                    yield return new WaitForSeconds(0.5f);

                SpawnOneMonster(cfg);
                yield return new WaitForSeconds(0.05f + UnityEngine.Random.Range(0f, 0.05f));
            }
        }

        // ==================== Boss 生成（C：Boss视觉）====================

        /// <summary>生成Boss：使用普通怪Prefab但放大2.5倍，红色材质标识</summary>
        public void SpawnBoss(int day, int bossHp = 1000, int bossAtk = 10, string bossIdOverride = null)
        {
            Vector3 spawnPos = GetSpawnPos("top");
            spawnPos += new Vector3(0, 0, -2f); // Boss稍微靠前，确保显眼

            GameObject go;
            // 优先使用专属Boss Prefab（KuanggongBoss_05，已内置2.5x大小）
            var bossPrefabToUse = _bossPrefab != null ? _bossPrefab : _monsterPrefab;
            if (bossPrefabToUse != null)
            {
                go = Instantiate(bossPrefabToUse, spawnPos, Quaternion.identity);
                // 若使用专属Boss Prefab则不额外放大；若降级用普通怪则放大2.5倍
                if (_bossPrefab == null)
                {
                    float bossScale = Mathf.Max(0.001f, _monsterScale * 2.5f);
                    go.transform.localScale = Vector3.one * bossScale;
                }
            }
            else
            {
                // Boss Prefab 缺失 fallback：用空 GameObject 挂载 MonsterController（不显示色块）
                go = new GameObject("Boss_Placeholder");
                go.transform.position = spawnPos;
                Debug.LogWarning("[WaveSpawner] Boss prefab missing! Using invisible placeholder.");
            }

            string bossId = string.IsNullOrEmpty(bossIdOverride)
                ? $"boss_{day}_{UnityEngine.Random.Range(1000, 9999)}"
                : bossIdOverride;
            go.name = $"Boss_{bossId}";

            var ctrl = go.GetComponent<MonsterController>() ?? go.AddComponent<MonsterController>();
            // Boss血量极高，速度较慢
            float bossSpd = 1.2f + day * 0.05f;
            ctrl.Initialize(bossHp, bossAtk, bossSpd, _cityGateTarget);
            ctrl.SetMonsterIdAndType(bossId, DrscfZ.Monster.MonsterType.Boss);
            ctrl.OnDead += HandleMonsterDead;
            _activeMonsters.Add(ctrl);
            ApplyInitialHpBarState(ctrl); // Fix B §34B B3：heavy_fog 期内新生成怪物同步隐藏血条

            Debug.Log($"[WaveSpawner] Boss spawned: Day{day} HP={bossHp} ATK={bossAtk} Scale=2.5x");
        }

        private void SpawnOneMonster(WaveConfig cfg)
        {
            Vector3 spawnPos = GetSpawnPos(cfg.spawnSide);
            GameObject go;

            // 随机选择两套怪物外观，增加视觉多样性
            var prefabToUse = (_monsterPrefab2 != null && UnityEngine.Random.value > 0.5f)
                              ? _monsterPrefab2 : _monsterPrefab;
            if (prefabToUse != null)
            {
                go = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                // Prefab 根节点 scale 已归一化为 (1,1,1)，_monsterScale 默认 1.0
                // HPBarCanvas 的 scale 由 MonsterController.Initialize() 内部强制设置为 (0.01,0.01,0.01)
                float s = Mathf.Max(0.001f, _monsterScale);
                go.transform.localScale = Vector3.one * s;
                // 注意：不在此处修改 HPBarCanvas.localScale，由 MonsterController.Initialize() 负责
            }
            else
            {
                // Monster Prefab 缺失 fallback：用空 GameObject 挂载 MonsterController（不显示色块）
                go = new GameObject("Monster_Placeholder");
                go.transform.position = spawnPos;
                Debug.LogWarning("[WaveSpawner] Monster prefab missing! Using invisible placeholder.");
            }

            string monsterId = $"{cfg.monsterId}_{_activeMonsters.Count}_{UnityEngine.Random.Range(1000,9999)}";
            go.name = $"Monster_{monsterId}";

            var ctrl = go.GetComponent<MonsterController>() ?? go.AddComponent<MonsterController>();

            // 怪物参数（简单线性缩放：天数越多越强）
            int hp     = Mathf.RoundToInt(20f + _currentDay * 8f);
            int atk    = Mathf.RoundToInt(3f + _currentDay * 1.5f);
            float spd  = 1.8f + _currentDay * 0.1f;
            ctrl.Initialize(hp, atk, spd, _cityGateTarget);
            // 设置唯一ID（供服务器消息查找）
            ctrl.SetMonsterIdAndType(monsterId, DrscfZ.Monster.MonsterType.Normal);
            ctrl.OnDead += HandleMonsterDead;
            _activeMonsters.Add(ctrl);
            ApplyInitialHpBarState(ctrl); // Fix B §34B B3：heavy_fog 期内新生成怪物同步隐藏血条
        }

        private Vector3 GetSpawnPos(string side)
        {
            Transform sp = side switch
            {
                "left"  => _spawnLeft,
                "right" => _spawnRight,
                "top"   => _spawnTop,
                _       => RandomSpawnPoint()
            };

            // 城门外侧刷怪（Z=-20 包围感）；fallback 取 Z=-20
            Vector3 base_ = sp != null ? sp.position : new Vector3(0, 0, -20f);
            return base_ + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-3f, 3f));
        }

        private Transform RandomSpawnPoint()
        {
            int r = UnityEngine.Random.Range(0, 3);
            return r == 0 ? _spawnLeft : (r == 1 ? _spawnRight : _spawnTop);
        }

        private void HandleMonsterDead(MonsterController m)
        {
            _activeMonsters.Remove(m);
            if (_activeMonsters.Count == 0 && !_spawning)
                OnAllMonstersDead?.Invoke(_currentDay);
        }

        /// <summary>服务器推送 monster_died → 通知波次生成器某怪已死</summary>
        public void OnMonsterDied(MonsterController monster)
        {
            _activeMonsters?.Remove(monster);
            Debug.Log($"[WaveSpawner] Monster died: {monster.MonsterId} ({monster.Type})");
        }

        // ==================== §35 跨直播间攻防战 — 远征怪生成 ====================

        /// <summary>红色 tint（MaterialPropertyBlock 复写 _BaseColor / _Color）</summary>
        private static readonly Color TribeWarTint = new Color(1f, 0.35f, 0.35f, 1f);

        /// <summary>
        /// §35.4 生成红色远征怪（Tribe War Expedition）。
        /// 复用现有怪物 Prefab / 初始化流程，差异点：
        ///   - 刷怪点固定为 SpawnTop（(5,0,28) 附近，与策划案 §35.4 对齐）
        ///   - 每只怪 Renderer 通过 MaterialPropertyBlock 覆盖 _BaseColor/_Color（不新建材质）
        ///   - 头顶名字显示 attackerName（MVP：若 MonsterController 未实现 name 字段，Debug.Log 占位）
        ///   - 属性与当前天数 (_currentDay) 的普通怪一致（与 SpawnOneMonster 同公式）
        ///   - 仍遵守 maxAliveMonsters = 15 上限（超上限时直接丢弃，避免阻塞协程）
        /// </summary>
        public void SpawnTribeWarExpedition(int count, string attackerName)
        {
            if (count <= 0) return;
            StartCoroutine(SpawnTribeWarExpeditionCoroutine(count, attackerName, null, null, null));
        }

        public void SpawnTribeWarExpedition(int count, string attackerName, string[] monsterIds)
        {
            if (count <= 0) return;
            StartCoroutine(SpawnTribeWarExpeditionCoroutine(count, attackerName, monsterIds, null, null));
        }

        // 🔴 audit-r46 GAP-M-02 codex 方案 D：新签名带 sessionId + monsters[].earliestHitAt
        //   远征怪 spawn 后调 MonsterController.SetTribeWarMetadata(sessionId, earliestHitAt)
        //   让 DoAttack 命中目标时能上报 tribe_war_expedition_hit 给服务端做实际结算
        public void SpawnTribeWarExpedition(int count, string attackerName, string[] monsterIds,
                                            string sessionId, Survival.TribeWarExpeditionMonsterData[] monsters)
        {
            if (count <= 0) return;
            StartCoroutine(SpawnTribeWarExpeditionCoroutine(count, attackerName, monsterIds, sessionId, monsters));
        }

        private IEnumerator SpawnTribeWarExpeditionCoroutine(int count, string attackerName, string[] monsterIds,
                                                              string sessionId, Survival.TribeWarExpeditionMonsterData[] monsters)
        {
            for (int i = 0; i < count; i++)
            {
                // 超上限时等待（与 SpawnBatch 一致）；但不阻塞太久——给出最长 5s 等待窗口
                float waitAcc = 0f;
                while (_activeMonsters.Count >= _maxAliveMonsters && waitAcc < 5f)
                {
                    yield return new WaitForSeconds(0.5f);
                    waitAcc += 0.5f;
                }
                if (_activeMonsters.Count >= _maxAliveMonsters)
                {
                    Debug.LogWarning($"[WaveSpawner] 远征怪超上限 {_maxAliveMonsters}，跳过 {count - i} 只（attacker={attackerName}）");
                    yield break;
                }
                string serverMonsterId = (monsterIds != null && i < monsterIds.Length) ? monsterIds[i] : null;
                long earliestHitAt = (monsters != null && i < monsters.Length) ? monsters[i].earliestHitAt : 0L;
                SpawnOneTribeWarMonster(attackerName, serverMonsterId, sessionId, earliestHitAt);
                yield return new WaitForSeconds(0.1f + UnityEngine.Random.Range(0f, 0.1f));
            }
        }

        private void SpawnOneTribeWarMonster(string attackerName, string serverMonsterId = null,
                                             string tribeWarSessionId = null, long earliestHitAt = 0L)
        {
            // 刷怪点固定为 top（策划案 §35.4）
            Vector3 spawnPos = GetSpawnPos("top");

            var prefabToUse = (_monsterPrefab2 != null && UnityEngine.Random.value > 0.5f)
                              ? _monsterPrefab2 : _monsterPrefab;

            GameObject go;
            if (prefabToUse != null)
            {
                go = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                float s = Mathf.Max(0.001f, _monsterScale);
                go.transform.localScale = Vector3.one * s;
            }
            else
            {
                go = new GameObject("TribeWarExpedition_Placeholder");
                go.transform.position = spawnPos;
                Debug.LogWarning("[WaveSpawner] Monster prefab missing! Tribe War expedition uses invisible placeholder.");
            }

            string monsterId = !string.IsNullOrEmpty(serverMonsterId)
                ? serverMonsterId
                : $"tw_exp_{_activeMonsters.Count}_{UnityEngine.Random.Range(1000,9999)}";
            go.name = $"TWExp_{monsterId}_{attackerName}";

            var ctrl = go.GetComponent<MonsterController>() ?? go.AddComponent<MonsterController>();

            // 与当前天数普通怪属性一致（§35.4）
            int hp    = Mathf.RoundToInt(20f + _currentDay * 8f);
            int atk   = Mathf.RoundToInt(3f + _currentDay * 1.5f);
            float spd = 1.8f + _currentDay * 0.1f;
            ctrl.Initialize(hp, atk, spd, _cityGateTarget);
            ctrl.SetMonsterIdAndType(monsterId, DrscfZ.Monster.MonsterType.Normal);
            // 🔴 audit-r46 GAP-M-02 codex 方案 D：传入 sessionId + earliestHitAt
            //   DoAttack 命中目标动画时上报 tribe_war_expedition_hit；服务端校验后做实际结算
            //   tribeWarSessionId 为 null 时退化为普通怪行为（不上报）
            if (!string.IsNullOrEmpty(tribeWarSessionId))
            {
                ctrl.SetTribeWarMetadata(tribeWarSessionId, earliestHitAt);
            }
            ctrl.OnDead += HandleMonsterDead;
            _activeMonsters.Add(ctrl);
            ApplyInitialHpBarState(ctrl); // Fix B §34B B3：heavy_fog 期内新生成怪物同步隐藏血条

            // 红色 tint（MaterialPropertyBlock，不新建材质）
            ApplyTribeWarRedTint(go);

            // MVP 占位：MonsterController 未实现 name 字段，Debug.Log 记录 attackerName
            // 后续补强 PlayerNameTag-style head tag 时可填充。
            if (!string.IsNullOrEmpty(attackerName))
            {
                Debug.Log($"[WaveSpawner] 远征怪生成：id={monsterId} attacker={attackerName} (头顶名字待视觉任务实现)");
            }
        }

        /// <summary>对 go 的所有 Renderer 应用红色 tint（MaterialPropertyBlock）。
        /// 兼容 URP Lit (_BaseColor) / Built-in Standard (_Color) 两种常见属性名。</summary>
        private static void ApplyTribeWarRedTint(GameObject go)
        {
            if (go == null) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                // URP Lit / Simple Lit
                mpb.SetColor("_BaseColor", TribeWarTint);
                // Built-in Standard（无 _BaseColor 时仍有 _Color；同时设不冲突）
                mpb.SetColor("_Color", TribeWarTint);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>根据MonsterId查找活跃的MonsterController</summary>
        public MonsterController FindMonsterById(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId)) return null;
            foreach (var m in _activeMonsters)
            {
                if (m != null && m.MonsterId == monsterId)
                    return m;
            }
            return null;
        }

        /// <summary>短别名（🆕 v1.22 §10 gate_effect_triggered 回调使用）</summary>
        public MonsterController FindById(string monsterId) => FindMonsterById(monsterId);

        public void StopAllWaves()
        {
            StopAllCoroutines();
            _spawning = false;
        }

        public void ResetAll()
        {
            StopAllWaves();
            foreach (var m in _activeMonsters)
            {
                if (m != null) Destroy(m.gameObject);
            }
            _activeMonsters.Clear();
        }

        // ==================== 默认波次配置（参考极寒之夜）====================

        private WaveConfig[] DefaultWaveConfigs()
        {
            return new WaveConfig[]
            {
                new WaveConfig { monsterId="X_guai01", count=5, maxCount=9,  beginning=5f, refreshTime=15f, spawnSide="all" },  // Day 1
                new WaveConfig { monsterId="X_guai01", count=4, maxCount=10, beginning=3f, refreshTime=12f, spawnSide="all" },  // Day 2
                new WaveConfig { monsterId="X_guai01", count=5, maxCount=15, beginning=3f, refreshTime=10f, spawnSide="all" },  // Day 3
                new WaveConfig { monsterId="X_guai01", count=6, maxCount=20, beginning=2f, refreshTime=8f,  spawnSide="all" },  // Day 4
                new WaveConfig { monsterId="X_guai01", count=6, maxCount=25, beginning=2f, refreshTime=7f,  spawnSide="all" },  // Day 5
                new WaveConfig { monsterId="X_guai01", count=7, maxCount=30, beginning=2f, refreshTime=6f,  spawnSide="all" },  // Day 6+
            };
        }
    }

    /// <summary>波次配置（参考极寒之夜 ConfigMonster）</summary>
    [System.Serializable]
    public class WaveConfig
    {
        public string monsterId   = "X_guai01";
        public int    count       = 3;    // 每批生成数量（baseCount）
        public int    maxCount    = 10;   // 总数量上限
        public float  beginning   = 5f;  // 开始前延迟（秒）
        public float  refreshTime = 15f; // 每批间隔（秒）
        public string spawnSide   = "all"; // left | right | top | all
    }
}
