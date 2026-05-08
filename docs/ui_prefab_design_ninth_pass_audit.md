# UI Prefab Design 第九轮查漏补缺报告

> 日期：2026-05-08  
> 范围：30 个独立设计资料包、162 行 layout 坐标、30 份新增切图回流规范  
> 目标：补齐“由设计资料生成 UI 切图后，能够反向回到 prefab”的落地链路。

## 1. 本轮新增产物

- 每个资料包新增 `ui_asset_slicing_backflow_<PrefabName>.md`。
- 新增总索引：`docs/ui_prefab_asset_slicing_backflow_index.md`。
- 新增回流校验报告：`docs/ui_prefab_design_ninth_pass_backflow_check.md`。
- 每个资料包 README 已加入对应切图回流规范入口。

## 2. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| 资料包数量 | 30 |
| 切图回流规范数量 | 30 |
| layout 坐标/切图片段总数 | 162 |
| README 更新/规范写入次数 | 60 |

## 3. 回流链路确认

每份回流规范都包含：

- 对应 prefab 路径。
- 1080x1920 左上角坐标到 Unity RectTransform 的换算说明。
- Unity Sprite 导入设置。
- 九宫格 Border 建议。
- 切图文件命名建议。
- 每个模块的 prefab/节点、原型坐标、回流方式、RectTransform 回填值和文案处理策略。
- “动态 TMP 文案不烘焙进切图”的验收要求。

## 4. 结论

第九轮补齐了 UI 切图反向回 prefab 的资料链路。现在从 `ui_layout_prototype_*.png` / `ai_prompt.md` 生成效果图后，可以按 `ui_asset_slicing_backflow_*.md` 拆成 Sprite，并依据节点名、坐标、RectTransform、导入设置和文案策略回填到对应 prefab。随后回流校验确认 30 份规范与 162 行 layout 坐标一一对应。
