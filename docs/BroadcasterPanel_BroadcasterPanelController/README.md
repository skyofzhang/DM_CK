# BroadcasterPanel_BroadcasterPanelController

这个文件夹集中管理 `BroadcasterPanel_BroadcasterPanelController` 的 UI 策划、施工原型和 AI 效果图提示词。

| 文件 | 用途 |
| --- | --- |
| `ui_plan_BroadcasterPanel_BroadcasterPanelController.md` | 详细 UI 策划案 |
| `ui_layout_prototype_BroadcasterPanel_BroadcasterPanelController.md` | 施工布局说明和坐标表 |
| `ui_layout_prototype_BroadcasterPanel_BroadcasterPanelController.svg` | 1080x1920 可编辑施工原型图 |
| `ui_layout_prototype_BroadcasterPanel_BroadcasterPanelController.png` | 1080x1920 出图参考图 |
| `ai_prompt.md` | AI 效果图提示词 |
| `README.md` | 资料包索引和 prefab 路径 |
| `ui_asset_slicing_backflow_BroadcasterPanel_BroadcasterPanelController.md` | UI 切图命名、Unity 导入设置和 prefab 回流规范 |

Prefab：`Assets/Prefabs/UI/Panels/BroadcasterPanel_BroadcasterPanelController.prefab`

本主面板已覆盖直接嵌套子 prefab：`StreamerPromptUI_StreamerPromptCard`、`BroadcasterDecisionHUD_DecisionHUD`，本轮不为这些子 prefab 另建资料，避免重复。

## 第三轮补充

- `covered_child_prefabs_audit.md`：主播控制台直接嵌套子 prefab 的逐项审计附录，包含脚本、节点、文案和覆盖说明。
