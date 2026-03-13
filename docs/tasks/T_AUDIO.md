# 任务：T_AUDIO — 基础音效系统

| 属性 | 值 |
|------|-----|
| 任务ID | T_AUDIO |
| 负责方 | 子Claude（使用 Coplay MCP工具）|
| 优先级 | P2 |
| 前置依赖 | 无（可独立开发）|
| 预计时间 | 3-4小时 |
| 状态 | ✅ 已完成 |

---

## 目标

建立基础音效系统：白天/夜晚BGM切换 + 5个核心SFX。不要求完美音效，能播放即可（后期替换）。

---

## 详细音效规格

参考：`docs/modules/audio_spec.md`

---

## 工程文件范围

| 文件 | 操作 |
|------|------|
| `Assets/Scripts/Core/AudioManager.cs` | **扩展**（已存在，添加BGM切换+SFX方法）|
| `Assets/Audio/BGM/` | **新建目录 + 4个BGM文件** |
| `Assets/Audio/SFX/` | **新建目录 + 15个SFX文件** |

---

## AudioManager.cs 扩展规范

### 音效来源（Phase 2临时方案）

由于版权问题，Phase 2使用Unity内置音效或程序生成音效：

```csharp
// 临时方案：使用AudioSource.PlayOneShot + 程序生成简单音效
// 通过 AudioClip 从代码生成：

private AudioClip GenerateSimpleBeep(float frequency, float duration) {
    int sampleRate = 44100;
    int samples = (int)(sampleRate * duration);
    AudioClip clip = AudioClip.Create("beep", samples, 1, sampleRate, false);
    float[] data = new float[samples];
    for (int i = 0; i < samples; i++) {
        data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate)
                  * Mathf.Clamp01(1 - (float)i / samples); // 淡出
    }
    clip.SetData(data, 0);
    return clip;
}
```

**真实音效获取**（首选）：
1. freesound.org 搜索 CC0授权音效
2. Unity Asset Store "Free Sound Effects Pack"
3. 如果无法获取，用程序生成近似音效

### BGM切换方法

```csharp
public class AudioManager : MonoBehaviour {
    [Header("BGM")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioClip _bgmDay;
    [SerializeField] private AudioClip _bgmNight;

    // BGM淡入淡出切换
    public IEnumerator SwitchBGM(AudioClip newClip, float fadeDuration = 2f) {
        // 淡出当前
        float startVol = _bgmSource.volume;
        for (float t = 0; t < fadeDuration * 0.5f; t += Time.deltaTime) {
            _bgmSource.volume = Mathf.Lerp(startVol, 0, t / (fadeDuration * 0.5f));
            yield return null;
        }
        // 切换
        _bgmSource.clip = newClip;
        _bgmSource.Play();
        // 淡入
        for (float t = 0; t < fadeDuration * 0.5f; t += Time.deltaTime) {
            _bgmSource.volume = Mathf.Lerp(0, startVol, t / (fadeDuration * 0.5f));
            yield return null;
        }
        _bgmSource.volume = startVol;
    }

    // SFX播放
    public void PlaySFX(string sfxId, float volume = 1.0f) {
        if (_sfxClips.TryGetValue(sfxId, out AudioClip clip)) {
            _sfxSource.PlayOneShot(clip, volume);
        } else {
            Debug.LogWarning($"[Audio] SFX not found: {sfxId}");
        }
    }

    // 循环SFX（火焰/警报等）
    private Dictionary<string, AudioSource> _loopSources = new();
    public void StartLoopSFX(string sfxId) { /* ... */ }
    public void StopLoopSFX(string sfxId) { /* ... */ }
}
```

### 集成到昼夜系统

```csharp
// 在 DayNightCycleManager.cs 中添加：
void OnDayStart() {
    StartCoroutine(AudioManager.Instance.SwitchBGM(_bgmDay, 2f));
    AudioManager.Instance.PlaySFX("sfx_day_start");
}

void OnNightStart() {
    StartCoroutine(AudioManager.Instance.SwitchBGM(_bgmNight, 3f));
    AudioManager.Instance.PlaySFX("sfx_night_start");
}
```

---

## AudioMixer设置（Coplay操作）

1. 在 Assets/Audio/ 下创建 AudioMixer.mixer
2. 添加三个Group：Master/BGM/SFX
3. Exposed参数：BGM_Volume，SFX_Volume

---

## Phase 2 最低可接受标准

如果音效文件暂时无法获取，使用以下临时方案：
- **BGM**: Unity内置的 AudioClip 测试音或任意CC0 MP3 (freesound.org)
- **关键SFX**: 至少实现 sfx_gift_t5_airdrop 和 sfx_gate_alarm（这两个对玩家体验影响最大）
- **其他SFX**: 可用程序生成的单音调beep临时替代

---

## 验收标准

- [ ] Play Mode进入游戏 → 有背景音乐播放（无报错）
- [ ] 昼夜切换 → BGM在3秒内切换（有淡入淡出）
- [ ] 推送T5礼物 → 播放礼物音效（哪怕是简单beep）
- [ ] 城门HP≤30% → 警报音效循环
- [ ] AudioManager.cs 中无编译错误

## 完成后操作

更新 `docs/progress.md` → T_AUDIO: ✅

---

*创建：2026-02-24 | 负责：子Claude*
