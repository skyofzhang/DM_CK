# SurvivalLiveRankingUI_GameUIPanel UI 切图回流规范

> 对应 prefab：`Assets/Prefabs/UI/Panels/SurvivalLiveRankingUI_GameUIPanel.prefab`  
> 参考原型：`ui_layout_prototype_SurvivalLiveRankingUI_GameUIPanel.png` / `ui_layout_prototype_SurvivalLiveRankingUI_GameUIPanel.svg`  
> 坐标基准：1080 x 1920，文档坐标为左上角 x/y，Unity 回流时按 RectTransform 中心点换算。  
> 建议导入目录：`Assets/Art/UI/Generated/SurvivalLiveRankingUI_GameUIPanel/`

## 1. 回流原则

1. 不建议把 AI 效果图整屏一张图塞回 prefab；必须按下面表格切成面板底板、按钮底板、图标、装饰条等可复用 Sprite。
2. 动态内容继续由 prefab 中的 TMP、Button、Slider、Image 组件和脚本字段驱动；昵称、数值、状态、倒计时、排名、按钮文字不要烘焙进切图。
3. 切图必须去掉原型里的灰盒、虚线、坐标、节点名、prefab 名、施工说明；最终 Sprite 只保留干净美术。
4. 大面板、横幅、按钮底板优先做九宫格，避免回到 prefab 后拉伸变形；小图标和徽章按原尺寸透明 PNG 回填。
5. RectTransform 回流以本文件表格为准；如果 Unity 里已有同名节点，优先替换该节点上的 Image.sprite，不新增平行重复节点。

## 2. Unity 导入设置

| 项 | 建议 |
| --- | --- |
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single；图集批处理时再改 Multiple |
| Mesh Type | Full Rect |
| Pixels Per Unit | 100，除非项目 UI 图集已有统一 PPU |
| sRGB | 开启 |
| Alpha Is Transparency | 开启 |
| Mip Maps | 关闭 |
| Compression | None 或 High Quality，文字底板和半透明边缘优先 None |
| Filter Mode | Bilinear |
| Wrap Mode | Clamp |
| 九宫格 Border | 小按钮 12-18px；普通面板 18-28px；大弹窗/横幅 24-40px，按圆角与描边厚度微调 |

## 3. 切图回流表

| 模块 | Prefab/节点 | 原型 x,y,w,h | 建议切图文件 | 回流方式 | RectTransform 回填 | 文案处理 |
| --- | --- | --- | --- | --- | --- | --- |
| 返回按钮 | `ExitBtn` | 30, 56, 118, 70 | `ui_SurvivalLiveRankingUI_GameUIPanel__01_ExitBtn.png` | 切透明 PNG，按 Image.sprite 回填 | TopLeft，pivot=(0.5,0.5)，sizeDelta=(118,70)，anchoredPosition=(-451,869) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 设置按钮 | `BtnSettings` | 932, 56, 118, 70 | `ui_SurvivalLiveRankingUI_GameUIPanel__02_BtnSettings.png` | 切透明 PNG，按 Image.sprite 回填 | TopRight，pivot=(0.5,0.5)，sizeDelta=(118,70)，anchoredPosition=(451,869) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 顶部主状态栏 | `SurvivalTopBarUI_TopBar` | 164, 56, 752, 100 | `ui_SurvivalLiveRankingUI_GameUIPanel__03_SurvivalTopBarUI_TopBar_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | TopCenter，pivot=(0.5,0.5)，sizeDelta=(752,100)，anchoredPosition=(0,854) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 主跑马灯 | `HorizontalMarqueeUI_MarqueeZone` / `SupporterMarquee` | 40, 176, 1000, 64 | `ui_SurvivalLiveRankingUI_GameUIPanel__04_HorizontalMarqueeUI_MarqueeZone.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | TopCenter，pivot=(0.5,0.5)，sizeDelta=(1000,64)，anchoredPosition=(0,752) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 游戏主体安全区 | 场景画面预留 | 40, 260, 1000, 1040 | `-` | 不单独切图；作为游戏画面/安全区参考 | Center，pivot=(0.5,0.5)，sizeDelta=(1000,1040)，anchoredPosition=(0,180) | 无文案切图 |
| 事件提示 | `EventTriggeredUI_EventTriggeredToast` | 242, 288, 596, 76 | `ui_SurvivalLiveRankingUI_GameUIPanel__06_EventTriggeredUI_EventTriggeredToast.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | TopCenter，pivot=(0.5,0.5)，sizeDelta=(596,76)，anchoredPosition=(0,634) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 资源贡献榜 | `ResourceRankPanel` | 56, 288, 174, 350 | `ui_SurvivalLiveRankingUI_GameUIPanel__07_ResourceRankPanel_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | TopLeft，pivot=(0.5,0.5)，sizeDelta=(174,350)，anchoredPosition=(-397,497) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 守护者榜 | `LiveRankingPanel` | 850, 288, 174, 350 | `ui_SurvivalLiveRankingUI_GameUIPanel__08_LiveRankingPanel_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | TopRight，pivot=(0.5,0.5)，sizeDelta=(174,350)，anchoredPosition=(397,497) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 礼物动画 | `GiftAnimationUI_GiftAnimation` | 260, 1184, 560, 78 | `ui_SurvivalLiveRankingUI_GameUIPanel__09_GiftAnimationUI_GiftAnimation.png` | 切透明 PNG，按 Image.sprite 回填 | Center，pivot=(0.5,0.5)，sizeDelta=(560,78)，anchoredPosition=(0,-263) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 弹幕动态 | `BarragePanel` | 40, 1324, 1000, 132 | `ui_SurvivalLiveRankingUI_GameUIPanel__10_BarragePanel.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | BottomCenter，pivot=(0.5,0.5)，sizeDelta=(1000,132)，anchoredPosition=(0,-430) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 建筑状态 | `BuildingStatusPanelUI_BuildingStatusPanel` | 40, 1480, 312, 124 | `ui_SurvivalLiveRankingUI_GameUIPanel__11_BuildingStatusPanelUI_BuildingStatusPanel_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | BottomLeft，pivot=(0.5,0.5)，sizeDelta=(312,124)，anchoredPosition=(-344,-582) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 个人贡献 | `PersonalContribUI_PersonalContribBar` | 40, 1624, 312, 92 | `ui_SurvivalLiveRankingUI_GameUIPanel__12_PersonalContribUI_PersonalContribBar.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | BottomLeft，pivot=(0.5,0.5)，sizeDelta=(312,92)，anchoredPosition=(-344,-710) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 礼物贴纸说明 | `StickerPanelUI` / `GiftRecommendationUI_GiftIconBar` / `GiftInfoPanel` | 384, 1480, 656, 236 | `ui_SurvivalLiveRankingUI_GameUIPanel__13_StickerPanelUI_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | BottomCenter，pivot=(0.5,0.5)，sizeDelta=(656,236)，anchoredPosition=(172,-638) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| VIP/加入提示 | `VIPAnnouncementUI_VIPAnnouncement` / `SupporterJoinedToastUI` | 40, 1736, 486, 92 | `ui_SurvivalLiveRankingUI_GameUIPanel__14_VIPAnnouncementUI_VIPAnnouncement.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | BottomLeft，pivot=(0.5,0.5)，sizeDelta=(486,92)，anchoredPosition=(-257,-822) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 建造投票 | `BuildVoteUI_BuildVotePanel` | 554, 1736, 486, 92 | `ui_SurvivalLiveRankingUI_GameUIPanel__15_BuildVoteUI_BuildVotePanel.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | BottomRight，pivot=(0.5,0.5)，sizeDelta=(486,92)，anchoredPosition=(257,-822) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |

## 4. 回流步骤

1. 按 `ai_prompt.md` 和原型 PNG 生成效果图后，先拆出不含动态文字的透明 PNG 切片。
2. 将切片放入 `Assets/Art/UI/Generated/SurvivalLiveRankingUI_GameUIPanel/`，按本文件的建议文件名命名。
3. 在 Unity Import Settings 中套用上面的 Sprite 设置；需要拉伸的底板进入 Sprite Editor 设置 Border。
4. 打开 `Assets/Prefabs/UI/Panels/SurvivalLiveRankingUI_GameUIPanel.prefab`，按“Prefab/节点”列定位节点，优先替换现有 Image.sprite / RawImage.texture。
5. 按“RectTransform 回填”列核对 sizeDelta 与 anchoredPosition；运行时文本仍绑定原 TMP 节点。
6. 回放对应界面状态，检查 1080x1920、窄屏等比缩放、动态文字溢出、九宫格边角和按钮点击区域。

## 5. 验收清单

- [ ] 切图没有灰盒、虚线、坐标、prefab 名、节点名、施工标注。
- [ ] 动态 TMP 文案、数字、昵称、倒计时没有被烘焙进背景图。
- [ ] 每个 Sprite 都能追到本文件表格中的 prefab 节点或明确标注为安全区参考。
- [ ] 大面板/横幅/按钮底板已设置九宫格 Border，拉伸后圆角和描边不变形。
- [ ] 回填后 RectTransform 与 layout 坐标一致，画布仍按 1080x1920 竖屏设计。
- [ ] prefab 原有脚本字段、按钮事件、显隐逻辑没有因为换图被断开。

## 6. 第十轮节点命中与回填策略

> 本节由第十轮反向审计补充：用于说明“Prefab/节点”列在真实 prefab 中的命中方式。  
> 回流优先级：先替换现有节点上的 Image.sprite / RawImage.texture；其次定位嵌套子 prefab；再次按脚本字段找 Inspector 绑定对象；只有“施工目标需创建/改名”的行才允许新增 Image 容器。  
> 新增容器时必须保留原 prefab 的 TMP、Button、脚本字段、按钮事件和显隐逻辑，不要为了贴图方便删除或重建绑定节点。

| 模块 | Prefab/节点 | 命中状态 | 命中目标 | 回填策略 |
| --- | --- | --- | --- | --- |
| 返回按钮 | `ExitBtn` | 现有节点命中 | `ExitBtn` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 设置按钮 | `BtnSettings` | 现有节点命中 | `BtnSettings` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 顶部主状态栏 | `SurvivalTopBarUI_TopBar` | 嵌套子 prefab 命中 | `SurvivalTopBarUI_TopBar` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 主跑马灯 | `HorizontalMarqueeUI_MarqueeZone` / `SupporterMarquee` | 嵌套子 prefab 命中 | `HorizontalMarqueeUI_MarqueeZone`、`SupporterMarqueeUI_SupporterMarquee` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 游戏主体安全区 | 场景画面预留 | 安全区/表现参考 | - | 不作为必须回填的 Sprite 节点；用于限定画面留白、世界锚点、动效路径或 gameplay 安全区。 |
| 事件提示 | `EventTriggeredUI_EventTriggeredToast` | 嵌套子 prefab 命中 | `EventTriggeredUI_EventTriggeredToast` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 资源贡献榜 | `ResourceRankPanel` | 现有节点命中 | `ResourceRankPanel` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 守护者榜 | `LiveRankingPanel` | 现有节点命中 | `LiveRankingPanel` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 礼物动画 | `GiftAnimationUI_GiftAnimation` | 嵌套子 prefab 命中 | `GiftAnimationUI_GiftAnimation` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 弹幕动态 | `BarragePanel` | 现有节点命中 | `BarragePanel` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 建筑状态 | `BuildingStatusPanelUI_BuildingStatusPanel` | 嵌套子 prefab 命中 | `BuildingStatusPanelUI_BuildingStatusPanel` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 个人贡献 | `PersonalContribUI_PersonalContribBar` | 嵌套子 prefab 命中 | `PersonalContribUI_PersonalContribBar` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 礼物贴纸说明 | `StickerPanelUI` / `GiftRecommendationUI_GiftIconBar` / `GiftInfoPanel` | 嵌套子 prefab 命中 | `GiftRecommendationUI_GiftIconBar` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| VIP/加入提示 | `VIPAnnouncementUI_VIPAnnouncement` / `SupporterJoinedToastUI` | 嵌套子 prefab 命中 | `SupporterJoinedToastUI_SupporterJoinedToast`、`VIPAnnouncementUI_VIPAnnouncement` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
| 建造投票 | `BuildVoteUI_BuildVotePanel` | 嵌套子 prefab 命中 | `BuildVoteUI_BuildVotePanel` | 在父 prefab 中定位该子 prefab 实例；优先进入子 prefab 根节点或其 Image 底板回填，不为该子 prefab 另建资料包。 |
