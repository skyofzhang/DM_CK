# ChapterAnnouncementUI_ChapterAnnouncement UI 策划案

> Prefab：`Assets/Prefabs/UI/Panels/ChapterAnnouncementUI_ChapterAnnouncement.prefab`  
> 用途：章节幕名公告  
> 主效果图规格：1080 x 1920 竖屏  
> 日期：2026-05-07

## 1. 界面定位与目的

在章节切换时展示幕名和一句短说明，帮助观众理解当前进程。

该界面应被视为 章节幕名公告，不是营销页。设计第一屏必须直接表达真实可用状态，并保留足够游戏主体安全区或明确的弹窗遮罩关系。

## 2. 设计目标

- 幕名比普通 toast 更庄重
- 显示时间短
- 不与全屏结算混用

## 3. 当前 prefab / 模块核对

| 项目 | 内容 |
| --- | --- |
| Prefab 路径 | `Assets/Prefabs/UI/Panels/ChapterAnnouncementUI_ChapterAnnouncement.prefab` |
| 绑定脚本 | `ChapterAnnouncementUI` |
| 节点数量摘录 | 3 个 `m_Name` 记录 |
| 文案数量摘录 | 0 个 TMP/Text 文案记录 |
| 嵌套子 prefab | 无直接嵌套 Panels prefab |

关键节点摘录：

- NameText
- SubText
- BannerRoot

可见/默认文案摘录：

- 当前 prefab 文案多由运行时注入，设计稿应预留动态文本宽度。



## 4. 推荐布局与层级

BannerRoot 居中，NameText 为主，SubText 为副，短时淡入保持 2 秒内。

| 模块 | Prefab/节点 | x | y | w | h | 说明 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 游戏安全区 | `SceneSafeArea` | 60 | 250 | 960 | 1120 | 背景仍可见 |
| 幕名横幅 | `BannerRoot` | 110 | 640 | 860 | 250 | 章节切换 |
| 章节名 | `NameText` | 160 | 700 | 760 | 78 | 第 N 幕 |
| 副说明 | `SubText` | 210 | 800 | 660 | 54 | 短句说明 |

## 5. 模块设计说明

### 游戏安全区

- 对应节点：`SceneSafeArea`
- 建议位置：x=60 y=250 w=960 h=1120。
- 设计要点：背景仍可见

### 幕名横幅

- 对应节点：`BannerRoot`
- 建议位置：x=110 y=640 w=860 h=250。
- 设计要点：章节切换

### 章节名

- 对应节点：`NameText`
- 建议位置：x=160 y=700 w=760 h=78。
- 设计要点：第 N 幕

### 副说明

- 对应节点：`SubText`
- 建议位置：x=210 y=800 w=660 h=54。
- 设计要点：短句说明

## 6. 视觉风格

电影幕名感，深蓝半透明横幅配冰晶边线，文字居中。

建议保持项目统一语言：冰蓝代表常规 HUD，金色代表贡献/VIP/奖励，红橙代表危险或不可逆操作，绿色代表修复/完成。所有文字必须在 1080x1920 竖屏下可读，按钮文字不得溢出。

## 7. AI 效果图场景

进入新章节时，中屏出现章节名和一句说明，之后快速淡出回到战斗。

效果图应把施工原型里的坐标和灰盒理解为布局约束，而不是最终视觉元素。最终图可以加入材质、光效、图标、进度条和真实游戏背景，但不要出现 prefab 名、坐标、虚线框、施工标注等文字。

## 8. 实现注意事项

- 当前 prefab 没有额外特殊实现项；按以下通用规则落地。
- 所有临时层应通过 CanvasGroup 或子节点显隐控制，避免长期遮挡中央 gameplay 区。
- 动态文本需要预留 20%-30% 的宽度余量，中文、数字和昵称都要能截断或滚动。
- 若该界面作为其他主面板的子面板复用，应优先服从主面板坐标，不再另起一套视觉规范。

## 9. 验收标准

- 文件均位于 `docs/ChapterAnnouncementUI_ChapterAnnouncement/`。
- SVG 和 PNG 都是 1080x1920 竖屏参考。
- 效果图能清楚表达 章节幕名公告 的常用状态。
- 默认文案、节点名称和脚本关系能在文档中追溯到 prefab。
- 若存在嵌套子 prefab，README 和批量索引中明确避免重复生成。