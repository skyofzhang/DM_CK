# 模块索引 — 你修仙很牛吗 (nxxhnm)

> 按赚钱逻辑排列：吸引观众→留住观众→转化付费→持续付费

| # | 模块 | 核心问题 | 文件 | 依赖 |
|---|------|----------|------|------|
| M01 | 视觉吸引 | 观众滑到直播间，3秒内要被画面抓住 | M01_visual_attraction.md | M10 |
| M02 | 互动入口 | 观众看了想玩，怎么加入？门槛要低 | M02_interaction_entry.md | M08 |
| M03 | 核心玩法 | 加入后做什么？自动战斗闯关，要有爽感 | M03_core_gameplay.md | M01,M02 |
| M04 | 成长付费 | 想变强怎么办？14条养成线驱动消费 | M04_growth_payment.md | M03 |
| M05 | 礼物经济 | 怎么花钱？7档礼物→游戏效果→VIP→商城 | M05_gift_economy.md | M04 |
| M06 | 社交竞争 | 为什么和别人比？PVP+排行+世界Boss | M06_social_competition.md | M03,M05 |
| M07 | 留存循环 | 明天还来吗？每日时间表+收集+福利 | M07_retention_loop.md | M04,M06 |
| M08 | 技术底座 | 怎么让这一切跑起来？服务器+协议+平台 | M08_tech_foundation.md | — |
| M09 | 界面交互 | 玩家看到什么？所有面板+按钮+HUD | **ui/** 目录(已重构) | 全部 |
| M10 | 美术资源 | 用什么素材？Spine角色+场景+音效+图标 | M10_art_resources.md | — |

## 快速定位表

| 你要做什么 | 读哪些模块 |
|-----------|-----------|
| 改战斗/关卡 | M03 |
| 改养成数值 | M04 |
| 改礼物效果 | M05 |
| 改PVP/排行 | M06 |
| 改每日活动 | M07 |
| 改服务器/协议 | M08 |
| 改UI布局 | ui/ 目录(ui_theme + ui_architecture + views/) |
| 改美术/动画 | M01 + M10 |
| 接抖音平台 | M08 |
| 完整理解游戏 | M03 → M04 → M05（按顺序）|

## 模块状态

| 模块 | 状态 | 说明 |
|------|------|------|
| M01-M08, M10 | 🔄 生成中 | Phase 0 策划案生成 |
| M09 | ✅ 已重构 | 拆分为 ui/ 多文件结构，详见 `docs/modules/ui/` |

## 实现备注区（开发过程中策划调整记录）

> 开发过程中发现策划需要调整，在这里记录:
> - [日期] 模块ID: 调整内容 + 原因

## 参考资料位置

| 资料 | 路径 |
|------|------|
| jjwcs提取资源 | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\` |
| 策划表(XLS) | `D:\claude\DM_polar\项目参考资料\我以往的类似项目的策划表\` |
| 开播文档 | `D:\claude\DM_polar\项目参考资料\【九劫问长生】开播文档.docx` |
| JSON配表(59张) | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\TextAsset\` |
| Spine角色 | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\TextAsset\` (atlas+skel) |
| 精灵图片 | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\Sprite\` |
| 纹理图片 | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\Texture2D\` |
| 音效文件 | `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\AudioClip\` |
| 抖音对接指南 | `D:\claude\DM_kpbl\docs\douyin_integration_guide.md`（1289行，完全复用）|
