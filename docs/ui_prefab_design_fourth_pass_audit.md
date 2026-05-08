# UI Prefab Design 第四轮查漏补缺报告

> 日期：2026-05-08  
> 范围：`Assets/Prefabs/UI/Panels/*.prefab` 与 30 个 UI 设计资料包  
> 目标：检查资料能否真正支撑 Unity 落地，补齐脚本绑定、PNG 渲染、AI 提示词结构和旧文件归档证据。

## 1. 本轮新增 / 修正

- 新增 `docs/ui_prefab_runtime_binding_audit.md`：30 个资料包的脚本字段、公开 API、事件入口、显隐控制、数据刷新审计。
- 重新用 Chrome headless 从 SVG 渲染全部 30 张 PNG，替代前轮坐标绘图生成的 PNG，确保 SVG/PNG 真正一致。
- 修正 `docs/SurvivalLiveRankingUI_GameUIPanel/ai_prompt.md`，补上标准 `## 使用方式 / 注意事项` 结构。
- 检查 `docs/` 根目录旧 Markdown，未发现需要移动到界面文件夹的同名旧版资料。

## 2. 本轮发现的真实问题

| 问题 | 处理 |
| --- | --- |
| `SurvivalLiveRankingUI_GameUIPanel/ai_prompt.md` 内容完整但标题结构不符合技能模板 | 已补标准结构，不改核心提示词 |
| 前轮 PNG 不是 Chrome 从 SVG 渲染 | 已用 Chrome headless 重新渲染 30 张 PNG |
| 资料包缺少运行时绑定总览 | 已新增 `ui_prefab_runtime_binding_audit.md` |

## 3. 复查结论

- 没有发现新增漏跑 prefab。
- 没有发现需要移动归档的旧界面资料散落在 `docs/` 根目录。
- AI 提示词结构、画布尺寸、施工标注反向约束已复查。
- Chrome 渲染后的 30 张 PNG 已重新进入最终复验。