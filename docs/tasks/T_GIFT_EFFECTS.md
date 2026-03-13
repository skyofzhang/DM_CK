# 任务：T_GIFT_EFFECTS — 礼物分级特效系统

| 属性 | 值 |
|------|-----|
| 任务ID | T_GIFT_EFFECTS |
| 负责方 | 子Claude（使用 Coplay MCP工具）|
| 优先级 | P1 |
| 前置依赖 | T_FIX_BLACKSCREEN 完成 |
| 预计时间 | 4-6小时 |
| 状态 | ✅ 已完成 |

---

## 目标

重写礼物通知系统，实现T1-T5分级视觉效果。当前 GiftNotificationUI.cs 已标注"已弃用"，需完全重写。

---

## 详细视觉规格

参考：
- `docs/modules/panels/panel_gift_notify.md`（像素级面板规格）
- `docs/modules/animation_spec.md` §3（礼物特效时间轴）
- `docs/modules/ui_theme.md`（颜色/动画变量）

---

## 工程文件范围

| 文件 | 操作 |
|------|------|
| `Assets/Scripts/UI/GiftNotificationUI.cs` | **重写**（原版已弃用）|
| `Assets/Scripts/UI/GiftEffectSystem.cs` | **新建**（特效时序控制）|
| Scene中 Gift_Canvas | **需要预创建所有面板** |

---

## Gift_Canvas预创建结构

```
Gift_Canvas (Sort Order=100)
├── T1_StarParticle       (ParticleSystem预制)
├── T2_BorderEffect       (Panel, SetActive=false)
│   ├── TopLeft_PS        (ParticleSystem)
│   ├── TopRight_PS       (ParticleSystem)
│   ├── BotLeft_PS        (ParticleSystem)
│   ├── BotRight_PS       (ParticleSystem)
│   └── CenterRing_Image  (Image)
├── T3_GiftBounce         (Panel, SetActive=false)
│   ├── GiftIcon_Image    (Image)
│   └── Explode_PS        (ParticleSystem)
├── T4_FullscreenGlow     (Panel, SetActive=false)
│   ├── OrangeOverlay     (Image, full screen)
│   ├── BatteryIcon       (Image)
│   └── ChargingSlider    (Slider)
├── T5_EpicAirdrop        (Panel, SetActive=false)
│   ├── BlackOverlay      (Image, full screen)
│   ├── AirdropBox        (Image)
│   ├── Fireworks_PS      (ParticleSystem)
│   ├── ResourceIcons     (4×Image, food/coal/ore/shield)
│   └── PlayerNameText    (TMP)
└── GiftBannerQueue       (3个BannerSlot，预创建)
    ├── BannerSlot_0
    ├── BannerSlot_1
    └── BannerSlot_2
```

---

## GiftNotificationUI.cs 重写规范

```csharp
// 主入口：收到服务器 gift 消息时调用
public void ShowGiftEffect(string giftId, string nickname, int tier)

// 根据tier调用对应效果
switch (tier) {
    case 1: StartCoroutine(PlayT1Effect(nickname)); break;
    case 2: StartCoroutine(PlayT2Effect(giftId, nickname)); break;
    case 3: StartCoroutine(PlayT3Effect(giftId, nickname)); break;
    case 4: StartCoroutine(PlayT4Effect(nickname)); break;
    case 5: StartCoroutine(PlayT5Effect(nickname)); break;
}
```

### T1效果实现要点
```csharp
private IEnumerator PlayT1Effect(string nickname) {
    // 1. 在弹幕滚动条中发送者名字位置播放ParticleSystem
    _t1Particle.transform.position = GetBarrageNamePosition(nickname);
    _t1Particle.Play();
    // 2. 播放音效
    AudioManager.Instance.PlaySFX("sfx_gift_t1_ding");
    yield return new WaitForSeconds(0.5f);
    // 3. 效果结束
}
```

### T5效果实现要点（最复杂）
```csharp
private IEnumerator PlayT5Effect(string nickname) {
    // 0. 通知服务器GIFT_PAUSE（由服务器发送，客户端接收后暂停UI更新）
    // 1. 遮罩淡入
    yield return FadeIn(_blackOverlay, 0.3f, 0.85f);
    // 2. 空投箱下落
    yield return DropAirdropBox();
    // 3. 着地震动
    yield return ShakeScreen(0.2f, 5f);
    // 4. 烟花爆炸
    _fireworks.Play();
    yield return ScatterResourceIcons();
    // 5. 玩家名大字
    _playerNameText.text = $"{nickname} 拯救了村庄！";
    yield return PopIn(_playerNameText.transform, 0.5f);
    // 6. 守护者排名该玩家闪烁（通知RankingPanel）
    RankingPanel.Instance?.StartGoldFlash(nickname, 10f);
    // 7. 等待特效完成（游戏逻辑3s后已恢复）
    yield return new WaitForSeconds(5f);
    // 8. 大字淡出
    yield return FadeOut(_playerNameText, 1f);
    // 9. 遮罩淡出
    yield return FadeOut(_blackOverlay, 0.5f);
    // 10. 关闭面板
    _t5Panel.SetActive(false);
}
```

---

## 服务器消息监听

```csharp
// 在 NetworkManager 的消息处理中添加：
case "live_gift":
    var giftData = ParseGiftMessage(data);
    GiftNotificationUI.Instance.ShowGiftEffect(
        giftData.giftId, giftData.nickname, giftData.tier);
    break;

case "gift_pause_start":
    // 暂停UI自动更新逻辑
    break;

case "gift_pause_end":
    // 恢复UI更新
    break;
```

---

## 参考代码

路径：`D:/claude-dm/cehua-doc/xgrj/极寒之夜资源素材/cursor_exoprt/Assets/Scripts/`
重点查找：礼物特效相关类（GiftEffect, GiftUI, 礼物弹幕相关）

---

## 验收标准

- [ ] 推送T1礼物 → 弹幕滚动条附近出现星形粒子，0.5s消散
- [ ] 推送T2礼物 → 屏幕四角粒子环聚拢，2s消散
- [ ] 推送T3礼物 → 礼物大图标飞入中央，抖动，爆炸，3s完成
- [ ] 推送T4礼物 → 全屏橙色光晕，进度条充能，5s完成
- [ ] 推送T5礼物 → 黑色遮罩，空投箱落下，烟花，玩家名大字，8s完成
- [ ] 礼物横幅队列正常工作（最多3条同时显示）
- [ ] 音效正确播放（各tier不同音效）

## 完成后操作

更新 `docs/progress.md` → T_GIFT_EFFECTS: ✅

---

*创建：2026-02-24 | 负责：子Claude*
