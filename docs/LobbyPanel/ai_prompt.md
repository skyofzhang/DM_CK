# LobbyPanel AI 效果图提示词

## 使用方式 / 注意事项

把 `ui_layout_prototype_LobbyPanel.png` 作为布局参考图一起输入。参考图只表示模块位置、尺寸和层级；灰盒、虚线、坐标、节点名、prefab 名、施工说明都不要出现在最终效果图里。

## 1. 主提示词

生成一张 1080x1920 竖屏手机游戏 UI 效果图，主题是“极地生存法则”的直播互动生存游戏。当前界面是 `LobbyPanel`，类型为 大厅主面板。画面要直接呈现真实可用 UI，不要做宣传海报或落地页。整体风格为冷冽极地、深色半透明玻璃面板、冰蓝描边、清晰中文信息层级；金色用于贡献、奖励、VIP 或高价值状态，红橙用于危机/警告/危险操作，绿色用于修复或完成。

界面目的：连接成功且 SurvivalGameManager 处于 Idle 时展示首次入口；提供开始游戏、排行榜、设置入口。

运行时关系：Assets/Scripts/UI/SurvivalIdleUI.cs：RefreshVisibility 只在 connected + Idle 时 ShowPanel；OnStartClicked 调 SurvivalGameManager.RequestStartGame()；OnRankingClicked / OnSettingsClicked 分别 Toggle SurvivalRankingUI、SurvivalSettingsUI

场景表现：连接成功后、Idle 状态下显示大厅；点击开始后状态文案变为“进入战场...”，随后请求 start_game；排行榜和设置打开各自面板。

## 2. 反向提示词

不要出现英文占位词、Lorem ipsum、坐标数字、prefab 文件名、Unity 节点名、灰盒标签、虚线框、施工说明、设计注释、水印、无意义图标、过度装饰的卡片堆叠、模糊不可读文字、按钮文字溢出、遮挡核心战斗主体的大面积弹窗。不要把界面画成营销首页，不要使用纯渐变背景替代真实游戏/界面状态。

## 3. 布局硬约束

- 背景：接近 x=0 y=0 w=1080 h=1920，对应 LobbyBg。
- 标题区：接近 x=120 y=300 w=840 h=180，对应 TitleImage/TitleText。
- 状态区：接近 x=180 y=620 w=720 h=120，对应 ServerStatus/StatusText。
- 开始按钮：接近 x=240 y=860 w=600 h=110，对应 StartBtn。
- 次级按钮：接近 x=240 y=1005 w=600 h=96，对应 RankingBtn/SettingsBtn。
- 底部留白：接近 x=80 y=1460 w=920 h=240，对应 SafeFooter。

## 4. 美术风格细化

极地生存主菜单，标题清楚但不做营销页；开始游戏是第一操作，排行榜/设置保持次级。

UI 需要有真实游戏产品感：面板边缘克制、圆角不超过 8px，重要按钮和状态图标有清楚视觉反馈。背景可表现冰雪基地、夜间怪物攻城或大厅场景，但必须服从布局，不能抢走 UI 信息。

## 5. 可见文案建议

- 已连接 √
- 排行榜
- 开始游戏
- 等待主播开始游戏...
- 设置
- 冬日生存法则

## 6. 简短版提示词

1080x1920 竖屏手机游戏 UI，极地生存直播互动风格，界面为 LobbyPanel（大厅主面板）。按参考原型保持模块位置：背景在x0 y0 w1080 h1920；标题区在x120 y300 w840 h180；状态区在x180 y620 w720 h120；开始按钮在x240 y860 w600 h110；次级按钮在x240 y1005 w600 h96。深色半透明冰蓝 UI，金色奖励，红橙警告，中文可读，不要出现施工标注、prefab 名、坐标或灰盒标签。