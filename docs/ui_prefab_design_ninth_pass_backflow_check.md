# UI Prefab Design 第九轮切图回流校验报告

> 日期：2026-05-08  
> 范围：30 个资料包、30 份切图回流规范、162 行 layout 坐标  
> 目标：确认后续由资料生成 UI 切图后，能按节点、坐标、Sprite 设置、TMP 策略和 RectTransform 反向回到 prefab。

## 1. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| 资料包数量 | 30 |
| layout 坐标行总数 | 162 |
| 切图回流表行总数 | 162 |
| 发现问题 | 0 |

## 2. 大型资料包抽样

| 资料包 | layout 坐标行 | 回流表行 |
| --- | ---: | ---: |
| `SurvivalLiveRankingUI_GameUIPanel` | 15 | 15 |
| `GateUpgradeConfirmUI` | 7 | 7 |
| `ShopConfirmDialogUI_ShopConfirmPanel` | 7 | 7 |
| `ShopUI_ShopPanel` | 7 | 7 |
| `SurvivalRankingPanel` | 7 | 7 |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | 7 | 7 |
| `BossRushBanner` | 6 | 6 |
| `BroadcasterPanel_BroadcasterPanelController` | 6 | 6 |

## 3. 校验内容

- 每个资料包是否存在 `ui_asset_slicing_backflow_<PrefabName>.md`。
- README 是否列出切图回流规范。
- 回流规范是否引用正确 prefab 路径和 1080 x 1920 坐标基准。
- 回流规范是否包含 Unity Sprite 导入设置、Alpha、Mip Maps、九宫格 Border。
- 回流规范是否明确动态 TMP 文案不要烘焙进切图。
- 回流规范是否包含 RectTransform、sizeDelta、anchoredPosition 回填说明。
- 回流规范表格行数是否与 layout 坐标行数一致。
- 切图回流总索引和 batch index 是否能追到第九轮产物。

## 4. 发现问题

未发现切图回流链路问题。

## 5. 结论

第九轮切图回流校验通过。30 份回流规范与 162 行 layout 坐标一一对应，README 和总索引均已接入；后续生成 UI 切图时，可以按这些规范导入 Unity 并反向回填到对应 prefab 节点。