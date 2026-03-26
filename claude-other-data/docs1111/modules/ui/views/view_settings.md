# 设置面板 — view_settings

> Requires: ui_theme.md
> Canvas: L_PANEL (20)
> Priority: P3
> CachePolicy: Disposable
> Entry: MainLobby[设置]
> Exit: @CloseButton → MainLobby (关闭时自动保存)
> Animation: A_POPUP_IN 进入; A_POPUP_OUT 退出
> ShowCondition: NavigationStack.Push触发

---

## 元素表

### 容器层

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Mask | Image | root | (0,0)→(1,1) | 0,0,0,0 | full | C_BG_OVERLAY_50 | 半透明遮罩 |
| Card | Panel | root | (0.5,0.5) | -450,-480,450,480 | 900×960 | C_BG_PANEL 圆角16px | 设置卡片 |
| @CloseButton | — | Card | — | — | — | — | 关闭按钮 |

### Card 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Title | TMP | Card | (0.5,1) | -120,-60,120,-20 | 240×40 | F_H1 C_TITLE align=center | "设置" |
| SfxRow | Panel | Card | (0,0.72)→(1,0.88) | 32,0,-32,0 | fill | — | 音效行 |
| MusicRow | Panel | Card | (0,0.52)→(1,0.68) | 32,0,-32,0 | fill | — | 音乐行 |
| QualityRow | Panel | Card | (0,0.32)→(1,0.48) | 32,0,-32,0 | fill | — | 画质行 |
| VersionText | TMP | Card | (0.5,0) | -200,24,200,52 | 400×28 | F_SMALL C_TEXT_DIM align=center | "v1.0.0 \| 你修仙很牛吗" |

### SfxRow / MusicRow 子元素 (相同结构)

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Label | TMP | Row | (0,0.5) | 0,-14,120,14 | 120×28 | F_BODY C_TEXT | "音效"/"音乐" |
| Toggle | Toggle | Row | (0,0.5) | 128,-20,176,20 | 48×40 | — | 开关 |
| Slider | Slider | Row | (0,0.5) | 192,-18,700,18 | 508×36 | C_BG_CARD Fill=C_TITLE thumb=48×48圆形 | 音量滑块 |
| Value | TMP | Row | (1,0.5) | -80,-14,0,14 | 80×28 | F_NUM C_TEXT_WHITE | "80%" |

### QualityRow 子元素

| ID | Type | Parent | Anchor | Pos | Size | Style | Content |
|----|------|--------|--------|-----|------|-------|---------|
| Label | TMP | QualityRow | (0,0.5) | 0,-14,120,14 | 120×28 | F_BODY C_TEXT | "画质" |
| Radio_Low | Button | QualityRow | (0,0.5) | 192,-28,352,28 | 160×56 | C_BG_CARD 圆角8px | [低] F_BTN |
| Radio_Mid | Button | QualityRow | (0,0.5) | 368,-28,528,28 | 160×56 | 选中=C_Q_BLUE | [中] F_BTN |
| Radio_High | Button | QualityRow | (0,0.5) | 544,-28,704,28 | 160×56 | C_BG_CARD 圆角8px | [高] F_BTN |

---

## 交互逻辑

```
OnValueChanged(SfxSlider):  AudioManager.sfxVolume = value; Value.text = f"{value*100}%"
OnValueChanged(MusicSlider): AudioManager.musicVolume = value; Value.text = f"{value*100}%"
OnClick(Toggle_Sfx):  AudioManager.sfxMute = !mute
OnClick(Toggle_Music): AudioManager.musicMute = !mute
OnClick(Radio_*):
  选中高亮 C_Q_BLUE, 其他恢复 C_BG_CARD
  QualitySettings.SetQualityLevel(selected)

OnClose():
  PlayerPrefs.SetFloat("sfxVolume", sfxSlider.value)
  PlayerPrefs.SetFloat("musicVolume", musicSlider.value)
  PlayerPrefs.SetInt("quality", qualityLevel)
  PlayerPrefs.Save()
```

---

## 数据源

| 字段 | 来源 | 刷新频率 |
|------|------|---------|
| 音效/音乐音量 | PlayerPrefs | OnOpen |
| 画质等级 | PlayerPrefs | OnOpen |
| 版本号 | Application.version | 静态 |
