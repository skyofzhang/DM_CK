# UI Prefab Design 第八轮查漏补缺报告

> 日期：2026-05-08  
> 范围：30 个独立设计资料包、24 个父面板覆盖子 prefab、54 个 Panels prefab 的矩阵/运行时追溯  
> 目标：从“可施工质量”角度复核资料是否真的能指导 Unity prefab 落地，不只检查文件是否存在。

## 1. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| 资料包数量 | 30 |
| 跳过子 prefab 数量 | 24 |
| 坐标表总行数 | 162 |
| SVG text 标注总数 | 714 |
| SVG rect 模块总数 | 304 |
| PNG 总字节数 | 2509443 |
| 发现问题 | 0 |

## 2. 本轮重点

- 逐包检查 plan/layout/prompt/README/SVG 是否存在乱码。
- 逐包检查资料是否提到真实 prefab 节点、可见文案或绑定脚本。
- 逐包解析 layout 坐标表，检查坐标是否在 1080x1920 内、宽高是否有效、节点列是否可追溯。
- 逐包检查 UI plan 是否有足够章节和落地/施工/验收说明。
- 逐包检查 AI prompt 是否包含中文可读性要求，以及灰盒、虚线、坐标、prefab、施工标注等排除项。
- 逐包检查 SVG 是否有足够文本标注和模块矩形，PNG 是否为 1080x1920 且不是空图。
- 复核全量矩阵、运行时绑定审计和总索引仍覆盖 30 个资料包与 24 个跳过子 prefab。

## 3. 坐标/原型密度抽样

| 资料包 | 坐标行 | SVG 文本 | SVG 矩形 |
| --- | ---: | ---: | ---: |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 3 | 14 | 7 |
| `GiftImpactUI_GiftImpactBanner` | 3 | 14 | 7 |
| `StatusLineBannerUI_StatusLineBanner` | 3 | 14 | 7 |
| `AnnouncementUI_AnnouncementPanel` | 4 | 17 | 8 |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 4 | 17 | 8 |
| `FairyWandMaxedBanner` | 4 | 17 | 8 |
| `NightModifierUI_NightModifierBanner` | 4 | 17 | 8 |
| `OreRepairFloatingText_OreRepairFloatRoot` | 4 | 17 | 8 |

> 上表列出坐标行较少的资料包，主要是 toast、横幅、浮字、overlay 等小型界面；它们通过本轮阈值校验。

## 4. 发现问题

未发现可施工质量问题。

## 5. 结论

第八轮复核通过。当前资料不仅文件齐全，而且能追到真实 prefab 节点/脚本/文案；坐标表均在 1080x1920 内；SVG/PNG 不是空产物；AI prompt 均包含施工标注排除和中文可读性要求；总索引、全量矩阵、运行时审计仍然覆盖完整。