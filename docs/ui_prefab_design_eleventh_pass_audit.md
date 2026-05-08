# UI Prefab Design 第十一轮查漏补缺报告

> 日期：2026-05-08  
> 范围：第九/十轮切图回流规范生成的 162 条切图片段 manifest  
> 目标：把“切图反向回 prefab”的资料从 Markdown 进一步整理成机器可读 manifest，并校验它能支撑后续自动化切图、Unity 导入和 prefab 回填。

## 1. 新增产物

- `docs/ui_prefab_asset_slice_manifest.json`
- `docs/ui_prefab_asset_slice_manifest.csv`
- `docs/ui_prefab_asset_slice_manifest.md`

## 2. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| 资料包数量 | 30 |
| manifest 条目 | 162 |
| 需要实际切图 | 144 |
| 安全区/表现参考 | 18 |
| 九宫格/Sliced 建议 | 53 |
| TMP/动态文案保留 | 62 |
| 允许创建/改名容器 | 22 |
| 唯一目标路径 | 144 |
| 发现问题 | 0 |

## 3. 命中状态分布

| 命中状态 | 数量 |
| --- | ---: |
| 安全区/表现参考 | 22 |
| 嵌套子 prefab 命中 | 11 |
| 施工目标需创建/改名 | 22 |
| 现有节点命中 | 107 |

## 4. 校验内容

- JSON 是否可解析，画布是否为 1080x1920。
- 每条记录是否能追到 prefab、spec、prototype PNG 和 AI prompt。
- 每条记录坐标是否在 1080x1920 内。
- 切图文件名和 Unity 目标路径是否规范、唯一、无非法字符。
- `noSlice=true` 是否只用于安全区/表现参考。
- 动态 TMP 保留条目是否避免直接烘焙文字。
- `allowCreateOrRename` 是否只用于“施工目标需创建/改名”。
- CSV 行数是否与 JSON entries 完全一致。
- 回流总索引是否链接第十一轮 manifest。

## 5. 发现问题

未发现 manifest、路径、命名、动态文案、九宫格或回流状态问题。

## 6. 结论

第十一轮复核通过。现在切图回流资料同时具备人读 Markdown 与机器读 JSON/CSV：162 条回流记录可追到 prefab、spec、原型 PNG、AI prompt、目标 Unity 路径、节点命中状态、RectTransform、九宫格和 TMP 动态文字策略；后续可据此自动化生成切图任务和 Unity 导入/回填清单。