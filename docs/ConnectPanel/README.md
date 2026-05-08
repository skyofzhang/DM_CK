# ConnectPanel

这个文件夹集中管理 `ConnectPanel` 的 UI 策划、施工原型和 AI 效果图提示词。

| 文件 | 用途 |
| --- | --- |
| `ui_plan_ConnectPanel.md` | 详细 UI 策划案 |
| `ui_layout_prototype_ConnectPanel.md` | 施工布局说明和坐标表 |
| `ui_layout_prototype_ConnectPanel.svg` | 1080x1920 可编辑施工原型图 |
| `ui_layout_prototype_ConnectPanel.png` | 1080x1920 出图参考图 |
| `ai_prompt.md` | AI 效果图提示词 |
| `README.md` | 资料包索引和 prefab 路径 |
| `ui_asset_slicing_backflow_ConnectPanel.md` | UI 切图命名、Unity 导入设置和 prefab 回流规范 |

Prefab：`Assets/Prefabs/UI/Panels/ConnectPanel.prefab`

运行时控制补充：

- Assets/Scripts/UI/SurvivalConnectUI.cs：Start 显示连接面板并调用 DoConnect()
- OnConnected 显示“已连接！正在加载游戏状态...”并 1.5 秒后隐藏
- OnDisconnected / OnConnectFailed 显示失败原因并打开 RetryButton
