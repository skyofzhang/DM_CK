# SurvivalSettlementUI_SurvivalSettlementPanel AI 效果图提示词

## 使用方式 / 注意事项

把 `ui_layout_prototype_SurvivalSettlementUI_SurvivalSettlementPanel.png` 作为布局参考图一起输入。参考图只表示模块位置、尺寸和层级；灰盒、虚线、坐标、节点名、prefab 名、施工说明都不要出现在最终效果图里。

## 1. 主提示词

生成一张 1080x1920 竖屏手机游戏 UI 效果图，主题是“极地生存法则”的直播互动生存游戏。当前界面是 `SurvivalSettlementUI_SurvivalSettlementPanel`，类型为 生存结算全屏面板。画面要直接呈现真实可用 UI，不要做宣传海报或落地页。整体风格为冷冽极地、深色半透明玻璃面板、冰蓝描边、清晰中文信息层级；金色用于贡献、奖励、VIP 或高价值状态，红橙用于危机/警告/危险操作，绿色用于修复或完成。

界面目的：游戏结束后按高光、统计、MVP/Top3 展示结算流程，并给主播重开/跳过能力。

场景表现：结算按三屏轮播：高光时刻、总统计与排行榜、MVP 表彰。

## 2. 反向提示词

不要出现英文占位词、Lorem ipsum、坐标数字、prefab 文件名、Unity 节点名、灰盒标签、虚线框、施工说明、设计注释、水印、无意义图标、过度装饰的卡片堆叠、模糊不可读文字、按钮文字溢出、遮挡核心战斗主体的大面积弹窗。不要把界面画成营销首页，不要使用纯渐变背景替代真实游戏/界面状态。

## 3. 布局硬约束

- 结算背景：接近 x=0 y=0 w=1080 h=1920，对应 SettlementBackdrop。
- 结果标题：接近 x=90 y=130 w=900 h=120，对应 ResultTitle。
- ScreenA 高光：接近 x=100 y=300 w=880 h=360，对应 ScreenA。
- ScreenB 统计：接近 x=100 y=700 w=880 h=450，对应 ScreenB。
- ScreenC MVP：接近 x=100 y=1190 w=880 h=300，对应 ScreenC。
- 操作与页点：接近 x=190 y=1550 w=700 h=120，对应 Buttons/PageDots。

## 4. 美术风格细化

胜负结算大屏，冰蓝背景配金色 MVP，高光卡片按内容分组。

UI 需要有真实游戏产品感：面板边缘克制、圆角不超过 8px，重要按钮和状态图标有清楚视觉反馈。背景可表现冰雪基地、夜间怪物攻城或大厅场景，但必须服从布局，不能抢走 UI 信息。

## 5. 可见文案建议

- 0
- 3
- 生存天数: 0
- '#2'
- 1
- —
- 2
- '#9'
- MVP
- 总采集: 0

## 6. 简短版提示词

1080x1920 竖屏手机游戏 UI，极地生存直播互动风格，界面为 SurvivalSettlementUI_SurvivalSettlementPanel（生存结算全屏面板）。按参考原型保持模块位置：结算背景在x0 y0 w1080 h1920；结果标题在x90 y130 w900 h120；ScreenA 高光在x100 y300 w880 h360；ScreenB 统计在x100 y700 w880 h450；ScreenC MVP在x100 y1190 w880 h300。深色半透明冰蓝 UI，金色奖励，红橙警告，中文可读，不要出现施工标注、prefab 名、坐标或灰盒标签。