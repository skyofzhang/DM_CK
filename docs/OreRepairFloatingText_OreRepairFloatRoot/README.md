# OreRepairFloatingText_OreRepairFloatRoot

这个文件夹集中管理 `OreRepairFloatingText_OreRepairFloatRoot` 的 UI 策划、施工原型和 AI 效果图提示词。

| 文件 | 用途 |
| --- | --- |
| `ui_plan_OreRepairFloatingText_OreRepairFloatRoot.md` | 详细 UI 策划案 |
| `ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.md` | 施工布局说明和坐标表 |
| `ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.svg` | 1080x1920 可编辑施工原型图 |
| `ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.png` | 1080x1920 出图参考图 |
| `ai_prompt.md` | AI 效果图提示词 |
| `README.md` | 资料包索引和 prefab 路径 |
| `ui_asset_slicing_backflow_OreRepairFloatingText_OreRepairFloatRoot.md` | UI 切图命名、Unity 导入设置和 prefab 回流规范 |

Prefab：`Assets/Prefabs/UI/Panels/OreRepairFloatingText_OreRepairFloatRoot.prefab`

运行时控制补充：

- Assets/Scripts/UI/OreRepairFloatingText.cs：监听 SurvivalGameManager.OnResourceUpdate
- gateHpDelta > 0 且 oreDelta < 0 时显示飘字
- 复用 DamageNumber.Show，颜色为 new Color(1f, 0.85f, 0.3f)
