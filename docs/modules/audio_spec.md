# 冬日生存法则 — 音效规格 v1.0

> 技术：Unity AudioMixer（Master → BGM/SFX/UI三轨）
> 格式：OGG Vorbis，44100Hz，单声道（SFX）/ 立体声（BGM）
> 来源：Unity Asset Store 免费包 或 freesound.org CC0授权

---

## 1. AudioMixer层级

```
Master (-0dB)
├── BGM_Group (-3dB)  — 背景音乐，支持淡入淡出
├── SFX_Group (0dB)   — 游戏音效
└── UI_Group (-3dB)   — UI音效（按钮/Toast）
```

**切换规则**：
- 白天→夜晚：BGM_Group Exposed参数淡出至 -80dB (3s) → 切换clip → 淡入至 -3dB (3s)
- 夜晚→白天：同上，2s淡出淡入

---

## 2. 背景音乐（BGM）

| ID | 文件名 | BPM | 时长 | 风格描述 | 循环 |
|----|--------|-----|------|---------|------|
| BGM_DAY | `bgm_day_winter.ogg` | 100 BPM | 90秒 | 轻松冬日氛围：木吉他拨弦 + 玻璃钟声 + 轻微鼓点；感觉安全但有寒意 | 是 |
| BGM_NIGHT | `bgm_night_danger.ogg` | 140 BPM | 60秒 | 紧张危机感：弦乐紧促 + 低音鼓 + 金属撞击；营造"怪物来了"紧迫感 | 是 |
| BGM_WIN | `bgm_win.ogg` | — | 10秒 | 凯旋：号角+鼓点+升调，简短有力 | 否 |
| BGM_LOSE | `bgm_lose.ogg` | — | 8秒 | 失落：单钢琴+低沉弦乐，不要太悲，留一点希望感 | 否 |

### 搜索关键词（freesound.org）
- BGM_DAY: "winter ambient loop" / "cozy cabin music" / "peaceful snow"
- BGM_NIGHT: "survival horror loop" / "tense ambient" / "night danger"
- BGM_WIN: "fanfare short" / "victory trumpet"
- BGM_LOSE: "sad piano short"

---

## 3. 游戏SFX清单（15条）

| ID | 文件名 | 触发时机 | 描述 | 音量 | 优先级 |
|----|--------|---------|------|------|-------|
| SFX_COLLECT_FOOD | `sfx_collect_food.ogg` | 指令1有效时，每次聚合窗口200ms触发 | 轻快"叮"声，像钓鱼线收线 | 0.6 | 低 |
| SFX_COLLECT_COAL | `sfx_collect_coal.ogg` | 指令2有效时 | 沉闷镐击声，有回响 | 0.5 | 低 |
| SFX_COLLECT_ORE | `sfx_collect_ore.ogg` | 指令3有效时 | 金属碰撞声，比煤炭更清脆 | 0.5 | 低 |
| SFX_FIRE_START | `sfx_fire_start.ogg` | 指令4首次触发时 | 点火声，木材噼啪1次 | 0.7 | 中 |
| SFX_FIRE_LOOP | `sfx_fire_loop.ogg` | 有人在生火时循环播放 | 持续噼啪火焰声（循环） | 0.4 | 低 |
| SFX_MONSTER_HIT | `sfx_monster_hit.ogg` | 怪物被玩家攻击时 | 怪物受击嚎叫声 | 0.7 | 中 |
| SFX_MONSTER_ATTACK | `sfx_monster_attack.ogg` | 怪物攻击城门时 | 轰鸣撞击声，低频 | 0.9 | 高 |
| SFX_GATE_ALARM | `sfx_gate_alarm.ogg` | 城门HP≤30%，循环至HP恢复 | 急促警报声（2s循环） | 0.8 | 高 |
| SFX_COLD_ALARM | `sfx_cold_alarm.ogg` | 炉温≤-80℃ | 寒风呼啸 + 低沉警报（3s循环） | 0.8 | 高 |
| SFX_GIFT_T1 | `sfx_gift_t1_ding.ogg` | T1礼物 | 清脆铃声×1，0.5s | 0.5 | 低 |
| SFX_GIFT_T2 | `sfx_gift_t2_bubble.ogg` | T2礼物 | 魔法泡泡声，有回响，2s | 0.7 | 中 |
| SFX_GIFT_T3 | `sfx_gift_t3_boom.ogg` | T3礼物 | 礼炮声，短促有力，3s | 0.9 | 高 |
| SFX_GIFT_T4 | `sfx_gift_t4_electric.ogg` | T4礼物 | 电能充充声 + 爆鸣，5s序列 | 1.0 | 高 |
| SFX_GIFT_T5 | `sfx_gift_t5_airdrop.ogg` | T5礼物（完整序列） | 飞机引擎声(2s)→着地爆炸(1s)→金色烟花(2s)→胜利号角(3s)，8s序列 | 1.0 | 最高 |
| SFX_BROADCASTER_BOOST | `sfx_broadcaster_boost.ogg` | 主播点⚡加速 | 能量激活音，有电感，1s | 0.9 | 高 |
| SFX_DAY_START | `sfx_day_start.ogg` | 白天开始 | 鸟鸣+清脆钟声，1.5s | 0.7 | 中 |
| SFX_NIGHT_START | `sfx_night_start.ogg` | 夜晚开始 | 狼嚎 + 警报，2s | 0.9 | 高 |
| SFX_RANK_UP | `sfx_rank_up.ogg` | 守护者排名上升时 | 上升音调（钢琴+叮声），0.5s | 0.6 | 低 |
| SFX_RANK_DOWN | `sfx_rank_down.ogg` | 排名下降时 | 下降音调，0.5s | 0.4 | 低 |

### 搜索关键词（freesound.org）
- SFX_COLLECT_FOOD: "fishing reel" / "water splash light"
- SFX_COLLECT_COAL: "pickaxe hit" / "mining sound"
- SFX_MONSTER_ATTACK: "impact heavy" / "gate slam"
- SFX_GATE_ALARM: "alarm loop short" / "warning siren"
- SFX_GIFT_T5: "airplane fly" + "explosion fireworks" + "fanfare"（三个文件合并）

---

## 4. UI音效

| ID | 触发时机 | 描述 | 音量 |
|----|---------|------|------|
| UI_BTN_CLICK | 主播按钮点击 | 通用点击声，轻快 | 0.5 |
| UI_TOAST | Toast提示出现 | 轻微"嗒"声 | 0.3 |
| UI_SETTLEMENT | 结算面板出现 | 翻页声 | 0.6 |
| UI_BOBAO_SPECIAL | T5礼物bobao | 特殊播报（比普通bobao更响） | 0.8 |

---

## 5. 实现规范

### AudioManager.cs（已存在文件）扩展方式
```csharp
// 在现有 AudioManager.cs 中添加：
public void PlaySFX(string sfxId, float volume = 1.0f)
// 从 Resources/Audio/SFX/ 加载对应文件

// BGM切换方法：
public IEnumerator SwitchBGM(AudioClip newClip, float fadeDuration = 3f)
// 淡出当前 → 切换 → 淡入

// 循环SFX控制：
private Dictionary<string, AudioSource> _loopSources = new();
public void StartLoopSFX(string sfxId)
public void StopLoopSFX(string sfxId)
```

### 文件目录结构
```
Assets/
└── Audio/
    ├── BGM/
    │   ├── bgm_day_winter.ogg
    │   ├── bgm_night_danger.ogg
    │   ├── bgm_win.ogg
    │   └── bgm_lose.ogg
    ├── SFX/
    │   ├── sfx_collect_food.ogg
    │   ├── sfx_collect_coal.ogg
    │   ├── sfx_collect_ore.ogg
    │   ├── sfx_fire_start.ogg
    │   ├── sfx_fire_loop.ogg
    │   ├── sfx_monster_hit.ogg
    │   ├── sfx_monster_attack.ogg
    │   ├── sfx_gate_alarm.ogg
    │   ├── sfx_cold_alarm.ogg
    │   ├── sfx_gift_t1_ding.ogg
    │   ├── sfx_gift_t2_bubble.ogg
    │   ├── sfx_gift_t3_boom.ogg
    │   ├── sfx_gift_t4_electric.ogg
    │   ├── sfx_gift_t5_airdrop.ogg
    │   ├── sfx_broadcaster_boost.ogg
    │   ├── sfx_day_start.ogg
    │   ├── sfx_night_start.ogg
    │   ├── sfx_rank_up.ogg
    │   └── sfx_rank_down.ogg
    └── UI/
        ├── ui_btn_click.ogg
        ├── ui_toast.ogg
        └── ui_settlement.ogg
```

---

*文档维护：策划Claude | 更新：2026-02-24*
