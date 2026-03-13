# 任务：T_BROADCASTER — 主播交互面板

| 属性 | 值 |
|------|-----|
| 任务ID | T_BROADCASTER |
| 负责方 | 子Claude（使用 Coplay MCP工具）|
| 优先级 | P2 |
| 前置依赖 | T_FIX_BLACKSCREEN 完成 |
| 预计时间 | 2-3小时 |
| 状态 | ✅ 已完成 |

---

## 目标

实现主播专用控制面板（2个按钮），让主播成为游戏的参与者而非旁观者。

---

## 详细UI规格

参考：`docs/modules/panels/panel_broadcaster.md`

---

## 工程文件范围

| 文件 | 操作 |
|------|------|
| `Assets/Scripts/UI/BroadcasterPanel.cs` | **新建** |
| `Server/src/SurvivalRoom.js` | **修改**：添加 broadcaster_action 消息处理 |

---

## BroadcasterPanel.cs 实现规范

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrscfZ.Core;

public class BroadcasterPanel : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] private Button _boostBtn;
    [SerializeField] private Button _eventBtn;
    [SerializeField] private Image _boostBtnBg;
    [SerializeField] private Image _eventBtnBg;

    [Header("Cooldown")]
    [SerializeField] private TMP_Text _boostCdText;
    [SerializeField] private TMP_Text _eventCdText;

    private float _boostCd = 0f;
    private float _eventCd = 0f;
    private const float BOOST_CD = 120f;
    private const float EVENT_CD = 60f;

    // ⚠️ 规则7: 脚本挂在 always-active 的Canvas上
    private bool _isRoomCreator = false;

    void Start() {
        _boostBtn.onClick.AddListener(OnBoostClicked);
        _eventBtn.onClick.AddListener(OnEventClicked);
        gameObject.SetActive(false); // 默认隐藏

        // 监听服务器返回 join_room 确认
        NetworkManager.Instance.OnMessageReceived += OnMessage;
    }

    void OnMessage(string type, object data) {
        if (type == "join_room_confirm") {
            var d = (dynamic)data;
            _isRoomCreator = d.isRoomCreator == true;
            gameObject.SetActive(_isRoomCreator);
        }
    }

    void Update() {
        // CD倒计时
        if (_boostCd > 0) {
            _boostCd -= Time.deltaTime;
            _boostCdText.text = $"CD {Mathf.CeilToInt(_boostCd)}s";
            if (_boostCd <= 0) ResetBoostBtn();
        }
        if (_eventCd > 0) {
            _eventCd -= Time.deltaTime;
            _eventCdText.text = $"CD {Mathf.CeilToInt(_eventCd)}s";
            if (_eventCd <= 0) ResetEventBtn();
        }
    }

    void OnBoostClicked() {
        if (_boostCd > 0) return;
        // 发送服务器消息
        NetworkManager.Instance.SendMessage("broadcaster_action", new {
            action = "efficiency_boost",
            duration = 30000
        });
        // 开始CD
        _boostCd = BOOST_CD;
        _boostBtn.interactable = false;
        _boostCdText.gameObject.SetActive(true);
    }

    void OnEventClicked() {
        if (_eventCd > 0) return;
        NetworkManager.Instance.SendMessage("broadcaster_action", new {
            action = "trigger_event"
        });
        _eventCd = EVENT_CD;
        _eventBtn.interactable = false;
        _eventCdText.gameObject.SetActive(true);
    }

    void ResetBoostBtn() {
        _boostBtn.interactable = true;
        _boostCdText.gameObject.SetActive(false);
    }

    void ResetEventBtn() {
        _eventBtn.interactable = true;
        _eventCdText.gameObject.SetActive(false);
    }
}
```

---

## 服务器修改：SurvivalRoom.js

在 `handleMessage` 方法中添加：

```javascript
case 'broadcaster_action':
    if (!this._isRoomCreator(client)) {
        // 非主播发送，忽略
        break;
    }
    if (data.action === 'efficiency_boost') {
        this._applyEfficiencyBoost(2.0, 30000); // 效率×2，30秒
        this._broadcastAll({
            type: 'broadcaster_effect',
            action: 'efficiency_boost',
            multiplier: 2.0,
            duration: 30000,
            triggeredBy: '主播'
        });
        this._broadcastBobao('⚡ 主播激活紧急加速！全体效率翻倍30秒！');
    } else if (data.action === 'trigger_event') {
        const eventId = this._randomEventId();
        this._triggerEvent(eventId);
        this._broadcastBobao(`🌊 主播触发了 ${this._getEventName(eventId)}！`);
    }
    break;
```

**辅助方法**：
```javascript
_isRoomCreator(client) {
    return client.id === this.roomCreatorId;
}

_applyEfficiencyBoost(multiplier, durationMs) {
    this.broadcasterEfficiencyMultiplier = multiplier;
    setTimeout(() => {
        this.broadcasterEfficiencyMultiplier = 1.0;
    }, durationMs);
}
```

---

## 面板布局（Coplay操作步骤）

1. `create_ui_element("panel", "BroadcasterPanel", parent="Broadcaster_Canvas")`
2. 设置位置：右下角，X=860, Y=1380，尺寸200×280
3. 添加背景Image（圆角深蓝色）
4. `create_ui_element("button", "BoostButton")` × 2
5. 设置圆形按钮样式
6. 挂载 `BroadcasterPanel.cs`

---

## 验收标准

- [ ] 主播端（GM模式）：右下角显示两个圆形按钮（⚡和🌊）
- [ ] 观众端：面板不可见
- [ ] 点击⚡按钮 → 服务器日志显示"efficiency_boost激活"→ 全局效率×2
- [ ] CD期间按钮灰色 + 倒计时数字
- [ ] CD结束 → 按钮恢复可用
- [ ] 点击🌊按钮 → 触发一个随机事件 + bobao播报

## 完成后操作

更新 `docs/progress.md` → T_BROADCASTER: ✅

---

*创建：2026-02-24 | 负责：子Claude*
