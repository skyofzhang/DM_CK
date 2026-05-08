# FrozenStatusPanel

这个文件夹集中管理 `FrozenStatusPanel` 的 UI 策划、施工原型和 AI 效果图提示词。

| 文件 | 用途 |
| --- | --- |
| `ui_plan_FrozenStatusPanel.md` | 详细 UI 策划案 |
| `ui_layout_prototype_FrozenStatusPanel.md` | 施工布局说明和坐标表 |
| `ui_layout_prototype_FrozenStatusPanel.svg` | 1080x1920 可编辑施工原型图 |
| `ui_layout_prototype_FrozenStatusPanel.png` | 1080x1920 出图参考图 |
| `ai_prompt.md` | AI 效果图提示词 |
| `README.md` | 资料包索引和 prefab 路径 |
| `ui_asset_slicing_backflow_FrozenStatusPanel.md` | UI 切图命名、Unity 导入设置和 prefab 回流规范 |

Prefab：`Assets/Prefabs/UI/Panels/FrozenStatusPanel.prefab`

运行时控制补充：

- Assets/Scripts/UI/FrozenStatusUI.cs：ShowFrozen(duration) 静态入口
- ShowInternal 设置 _panel active、主文字和倒计时协程
- CountdownCoroutine 结束后显示“解冻完成！”并淡出隐藏
