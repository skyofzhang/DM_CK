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

        [Header("怪物 Prefab（X_guai01）")]
        [SerializeField] private GameObject _monsterPrefab; // 如果为空使用Cube占位

        [Header("生成点（从场景中拖入）")]
        [SerializeField] private Transform _spawnLeft;
        [SerializeField] private Transform _spawnRight;
        [SerializeField] private Transform _spawnTop;

        [Header("城门目标")]
        [SerializeField] private Transform _cityGateTarget;

        [Header("怪物缩放（Prefab根节点已归一化为(1,1,1)，默认1.0）")]
        [SerializeField] private float _monsterScale = 1.0f;

        [Header("内置波次配置（服务器未推送时使用）")]
        [SerializeField] private WaveConfig[] _localWaveConfigs;

        private List<MonsterController> _activeMonsters = new List<MonsterController>();
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

        private void Start()
        {
            // 订阅白天开始事件：夜晚结束时强制清除场上残余怪物
            if (DayNightCycleManager.Instance != null)
                DayNightCycleManager.Instance.OnDayStarted += HandleDayStarted;
        }

        private void OnDestroy()
        {
            if (DayNightCycleManager.Instance != null)
                DayNightCycleManager.Instance.OnDayStarted -= HandleDayStarted;
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

        /// <summary>服务器推送 monster_wave → 生成怪物</summary>
        public void SpawnWave(Survival.MonsterWaveData data)
        {
            _currentDay = data.day;
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
                SpawnOneMonster(cfg);
                // 极短间隔：大批怪物同时涌出，呈现千军万马效果
                yield return new WaitForSeconds(0.05f + UnityEngine.Random.Range(0f, 0.05f));
            }
        }

        // ==================== Boss 生成（C：Boss视觉）====================

        /// <summary>生成Boss：使用普通怪Prefab但放大2.5倍，红色材质标识</summary>
        public void SpawnBoss(int day, int bossHp = 1000, int bossAtk = 10)
        {
            Vector3 spawnPos = GetSpawnPos("top");
            spawnPos += new Vector3(0, 0, -2f); // Boss稍微靠前，确保显眼

            GameObject go;
            if (_monsterPrefab != null)
            {
                go = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
                // Boss放大2.5倍
                float bossScale = Mathf.Max(0.001f, _monsterScale * 2.5f);
                go.transform.localScale = Vector3.one * bossScale;
            }
            else
            {
                // 占位：深红色Cube，体型大
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = spawnPos;
                go.transform.localScale = new Vector3(2f, 3f, 2f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0.6f, 0f, 0f);
                    rend.material = mat;
                }
            }

            string bossId = $"boss_{day}_{UnityEngine.Random.Range(1000, 9999)}";
            go.name = $"Boss_{bossId}";

            var ctrl = go.GetComponent<MonsterController>() ?? go.AddComponent<MonsterController>();
            // Boss血量极高，速度较慢
            float bossSpd = 1.2f + day * 0.05f;
            ctrl.Initialize(bossHp, bossAtk, bossSpd, _cityGateTarget);
            ctrl.SetMonsterIdAndType(bossId, DrscfZ.Monster.MonsterType.Boss);
            ctrl.OnDead += HandleMonsterDead;
            _activeMonsters.Add(ctrl);

            Debug.Log($"[WaveSpawner] Boss spawned: Day{day} HP={bossHp} ATK={bossAtk} Scale=2.5x");
        }

        private void SpawnOneMonster(WaveConfig cfg)
        {
            Vector3 spawnPos = GetSpawnPos(cfg.spawnSide);
            GameObject go;

            if (_monsterPrefab != null)
            {
                go = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
                // Prefab 根节点 scale 已归一化为 (1,1,1)，_monsterScale 默认 1.0
                // HPBarCanvas 的 scale 由 MonsterController.Initialize() 内部强制设置为 (0.01,0.01,0.01)
                float s = Mathf.Max(0.001f, _monsterScale);
                go.transform.localScale = Vector3.one * s;
                // 注意：不在此处修改 HPBarCanvas.localScale，由 MonsterController.Initialize() 负责
            }
            else
            {
                // 占位：红色 Cube
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = spawnPos;
                go.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0.8f, 0.15f, 0.15f);
                    rend.material = mat;
                }
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
