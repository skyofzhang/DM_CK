# UI Prefab Design 第十轮查漏补缺报告

> 日期：2026-05-08  
> 范围：30 个资料包、30 份切图回流规范、162 行回流表、真实 prefab 节点/脚本/嵌套子 prefab  
> 目标：确认第九轮新增的切图回流规范不仅有坐标，而且能反向命中真实 prefab 结构，避免后续切图回填时找不到节点、断脚本或把动态文字烘焙进图片。

## 1. 检查统计

| 检查项 | 数量 |
| --- | ---: |
| 资料包数量 | 30 |
| 回流表总行数 | 162 |
| 真实节点直接命中行 | 107 |
| 嵌套子 prefab 命中行 | 11 |
| 脚本名命中行 | 0 |
| 安全区/虚拟参考行 | 22 |
| 施工目标需创建/改名行 | 22 |
| 未解析行 | 0 |
| 动态 TMP 保留行 | 62 |
| Sprite/九宫格回流行 | 83 |
| 发现问题 | 0 |

## 2. 本轮检查内容

- 从每个 prefab 解析真实 `m_Name` 节点名、`m_SourcePrefab` 嵌套子 prefab、`m_Script` 绑定脚本。
- 从每份 `ui_asset_slicing_backflow_*.md` 解析回流表，逐行核对节点列是否能命中真实节点、子 prefab、脚本名，或明确属于安全区/虚拟参考、施工目标需创建/改名。
- 检查切图文件命名是否可批量落盘。
- 检查每行是否包含 `sizeDelta` 和 `anchoredPosition`。
- 检查动态文字行是否明确保留 TMP、不烘焙进切图。
- 检查每份规范是否提醒优先替换现有 `Image.sprite`，只有被标记为“施工目标需创建/改名”的行才允许新增容器，并保护脚本字段、按钮事件和显隐逻辑。

## 3. 节点命中抽样

| 资料包 | 回流行数 | 真实节点直接命中 | 抽样节点 |
| --- | ---: | ---: | --- |
| `AnnouncementUI_AnnouncementPanel` | 4 | 2 | 暗化游戏背景:`CanvasGroup`；主公告标题:`MainText`；副标题说明:`SubText` |
| `BossRushBanner` | 6 | 5 | Boss 横幅主体:`Content`；标题:`TitleText`；血量条:`BossHpBar` |
| `BroadcasterPanel_BroadcasterPanelController` | 6 | 3 | 主播控制台:`PanelRoot`；功能按钮网格:`Boost/Event/Roulette/Shop`；升级/建造/远征:`Building/Expedition/Gate` |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 4 | 3 | 幕名横幅:`BannerRoot`；章节名:`NameText`；副说明:`SubText` |
| `ConnectPanel` | 5 | 4 | 背景:`FullScreenBg`；标题:`TitleText`；连接动效:`Spinner/DotText` |
| `DayPreviewBanner` | 5 | 4 | 预告横幅:`BannerRoot`；标题:`HeadlineText`；倒计时:`CountdownText` |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 3 | 2 | 竞速条:`BannerRoot`；对比文案:`MessageText` |
| `FairyWandMaxedBanner` | 4 | 3 | 全屏闪光:`FlashRoot`；跑马灯轨道:`MarqueeRoot`；跑马灯文案:`MarqueeText` |
| `FeatureUnlockBanner` | 4 | 3 | 解锁横幅:`FeatureUnlockBanner`；标题:`TitleText`；说明:`DescText` |
| `FrozenStatusPanel` | 5 | 3 | 底部冻结横幅:`FrozenStatusPanel/BackgroundImage`；冻结主文案:`FrozenText`；倒计时:`CountdownText` |
| `GameControlUI_BottomBar` | 6 | 5 | 底部工具栏:`GameControlUI_BottomBar`；状态文本:`StatusText`；控制按钮组:`Start/Pause/End/Monster` |
| `GateUpgradeConfirmUI` | 7 | 6 | 暗化背景:`ModalBackdrop`；弹窗盒:`DialogBox`；标题:`Title` |

## 4. 发现问题

未发现 prefab 回流节点、脚本保护、动态文字或切图命名问题。

## 5. 结论

第十轮复核通过。第九轮新增的 30 份切图回流规范可以反向对上真实 prefab 结构：回流表行均能命中真实节点、嵌套子 prefab、脚本名、安全区参考，或被明确标记为施工目标需创建/改名；动态 TMP 文案明确不烘焙；切图文件命名、RectTransform 回填、九宫格/Sprite 回流和脚本事件保护要求完整。