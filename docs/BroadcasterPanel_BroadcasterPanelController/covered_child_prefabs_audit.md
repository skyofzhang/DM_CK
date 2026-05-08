# BroadcasterPanel_BroadcasterPanelController 已覆盖子 prefab 审计附录

> 日期：2026-05-08  
> 父 prefab：`Assets/Prefabs/UI/Panels/BroadcasterPanel_BroadcasterPanelController.prefab`  
> 目的：给“已跳过、不单独跑资料包”的子 prefab 提供可追溯审计，避免重复设计的同时保留节点/脚本/文案依据。

## 1. 覆盖原则

- 以下子 prefab 均直接嵌套在 `BroadcasterPanel_BroadcasterPanelController` 内，不另建 `docs/<ChildName>/`。
- 子 prefab 的坐标、显隐、视觉强弱必须服从父面板资料。
- 若后续某个子 prefab 从父面板拆出成为独立入口，再单独跑一套完整资料。

父面板绑定脚本：`FeatureLockOverlay`、`BroadcasterPanel`  
父面板直接子 prefab 数：2

## 2. 子 prefab 明细

| 子 prefab | 绑定脚本 | 节点/文案计数 | 节点摘录 | 文案摘录 | 覆盖说明 |
| --- | --- | --- | --- | --- | --- |
| `StreamerPromptUI_StreamerPromptCard` | `StreamerPromptUI` | 2 / 0 | `PromptText`、`CardRoot` | 运行时注入 | 主播话术卡：作为 BroadcasterPanel 的话术提示区，跟随主播控制台出现，不单独定义布局。 |
| `BroadcasterDecisionHUD_DecisionHUD` | `BroadcasterDecisionHUD` | 18 / 4 | `Card1`、`Icon`、`FirstTimeTip`、`Label`、`Card2`、`JumpBtn` | `前往`、`这里会告诉你现在该做什么` | 主播决策 HUD：与 BroadcasterPanel 的建造、远征、轮盘、城门升级入口联动。子面板资料留在父控制台下，避免和右侧工具按钮布局割裂。 |

## 3. 复查结论

本附录确认这些子 prefab 没有漏掉；它们是父面板的组成模块，而不是独立界面。当前保持“父资料覆盖 + 本附录追溯”的方式最稳，既避免重复，又能在调整 Unity prefab 时查到真实节点和脚本。