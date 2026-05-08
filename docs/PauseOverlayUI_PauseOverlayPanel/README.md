# PauseOverlayUI_PauseOverlayPanel

这个文件夹集中管理 `PauseOverlayUI_PauseOverlayPanel` 的 UI 策划、施工原型和 AI 效果图提示词。

| 文件 | 用途 |
| --- | --- |
| `ui_plan_PauseOverlayUI_PauseOverlayPanel.md` | 详细 UI 策划案 |
| `ui_layout_prototype_PauseOverlayUI_PauseOverlayPanel.md` | 施工布局说明和坐标表 |
| `ui_layout_prototype_PauseOverlayUI_PauseOverlayPanel.svg` | 1080x1920 可编辑施工原型图 |
| `ui_layout_prototype_PauseOverlayUI_PauseOverlayPanel.png` | 1080x1920 出图参考图 |
| `ai_prompt.md` | AI 效果图提示词 |
| `README.md` | 资料包索引和 prefab 路径 |
| `ui_asset_slicing_backflow_PauseOverlayUI_PauseOverlayPanel.md` | UI 切图命名、Unity 导入设置和 prefab 回流规范 |

Prefab：`Assets/Prefabs/UI/Panels/PauseOverlayUI_PauseOverlayPanel.prefab`

运行时控制补充：

- Assets/Scripts/UI/PauseOverlayUI.cs：BuildOverlayHierarchy 动态创建 UI
- HandleGamePaused 显示 _overlayRoot，HandleGameResumed 隐藏
- 脚本自身 GO 保持 active，只有子节点 SetActive(false)
