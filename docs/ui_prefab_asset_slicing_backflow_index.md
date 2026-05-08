# UI Prefab 切图回流总索引

> 日期：2026-05-08  
> 目的：让根据 UI 设计资料生成的效果图/切图，能够反向回填到 Unity prefab，而不是只停留在视觉参考图。  
> 总原则：效果图负责定风格，切图回流表负责落到 Sprite、TMP、Image、RectTransform 和九宫格。

## 1. 全局回流规则

- 不把整屏效果图直接塞进 prefab；按模块拆成透明 PNG Sprite。
- 动态文字、昵称、数值、倒计时、排名、状态文案继续用 TMP 和原脚本字段驱动。
- 面板底板、按钮底板、横幅、toast、卡片优先九宫格；图标、徽章、装饰角可用普通 Sprite。
- 所有切图必须去掉灰盒、坐标、节点名、prefab 名、施工标注。
- 回填时优先替换现有节点上的 Image.sprite / RawImage.texture，避免新增平行节点破坏脚本引用。
- 坐标换算统一使用 1080x1920：文档 x/y 是左上角；Unity anchoredPosition 用中心点换算。

## 2. 资料包切图回流表

| 资料包 | 坐标/切图片段数 | 回流规范 |
| --- | ---: | --- |
| `AnnouncementUI_AnnouncementPanel` | 4 | `docs/AnnouncementUI_AnnouncementPanel/ui_asset_slicing_backflow_AnnouncementUI_AnnouncementPanel.md` |
| `BossRushBanner` | 6 | `docs/BossRushBanner/ui_asset_slicing_backflow_BossRushBanner.md` |
| `BroadcasterPanel_BroadcasterPanelController` | 6 | `docs/BroadcasterPanel_BroadcasterPanelController/ui_asset_slicing_backflow_BroadcasterPanel_BroadcasterPanelController.md` |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 4 | `docs/ChapterAnnouncementUI_ChapterAnnouncement/ui_asset_slicing_backflow_ChapterAnnouncementUI_ChapterAnnouncement.md` |
| `ConnectPanel` | 5 | `docs/ConnectPanel/ui_asset_slicing_backflow_ConnectPanel.md` |
| `DayPreviewBanner` | 5 | `docs/DayPreviewBanner/ui_asset_slicing_backflow_DayPreviewBanner.md` |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 3 | `docs/EfficiencyRaceUI_EfficiencyRaceBanner/ui_asset_slicing_backflow_EfficiencyRaceUI_EfficiencyRaceBanner.md` |
| `FairyWandMaxedBanner` | 4 | `docs/FairyWandMaxedBanner/ui_asset_slicing_backflow_FairyWandMaxedBanner.md` |
| `FeatureUnlockBanner` | 4 | `docs/FeatureUnlockBanner/ui_asset_slicing_backflow_FeatureUnlockBanner.md` |
| `FrozenStatusPanel` | 5 | `docs/FrozenStatusPanel/ui_asset_slicing_backflow_FrozenStatusPanel.md` |
| `GameControlUI_BottomBar` | 6 | `docs/GameControlUI_BottomBar/ui_asset_slicing_backflow_GameControlUI_BottomBar.md` |
| `GateUpgradeConfirmUI` | 7 | `docs/GateUpgradeConfirmUI/ui_asset_slicing_backflow_GateUpgradeConfirmUI.md` |
| `GiftImpactUI_GiftImpactBanner` | 3 | `docs/GiftImpactUI_GiftImpactBanner/ui_asset_slicing_backflow_GiftImpactUI_GiftImpactBanner.md` |
| `GloryMomentUI_GloryMomentBanner` | 5 | `docs/GloryMomentUI_GloryMomentBanner/ui_asset_slicing_backflow_GloryMomentUI_GloryMomentBanner.md` |
| `LobbyPanel` | 6 | `docs/LobbyPanel/ui_asset_slicing_backflow_LobbyPanel.md` |
| `NightModifierUI_NightModifierBanner` | 4 | `docs/NightModifierUI_NightModifierBanner/ui_asset_slicing_backflow_NightModifierUI_NightModifierBanner.md` |
| `NightReportUI_NightReportPanel` | 5 | `docs/NightReportUI_NightReportPanel/ui_asset_slicing_backflow_NightReportUI_NightReportPanel.md` |
| `OreRepairFloatingText_OreRepairFloatRoot` | 4 | `docs/OreRepairFloatingText_OreRepairFloatRoot/ui_asset_slicing_backflow_OreRepairFloatingText_OreRepairFloatRoot.md` |
| `PauseOverlayUI_PauseOverlayPanel` | 4 | `docs/PauseOverlayUI_PauseOverlayPanel/ui_asset_slicing_backflow_PauseOverlayUI_PauseOverlayPanel.md` |
| `PeaceNightOverlay` | 5 | `docs/PeaceNightOverlay/ui_asset_slicing_backflow_PeaceNightOverlay.md` |
| `PreGameBannerUI_PreGameBanner` | 6 | `docs/PreGameBannerUI_PreGameBanner/ui_asset_slicing_backflow_PreGameBannerUI_PreGameBanner.md` |
| `ReconnectDialog` | 5 | `docs/ReconnectDialog/ui_asset_slicing_backflow_ReconnectDialog.md` |
| `ShopConfirmDialogUI_ShopConfirmPanel` | 7 | `docs/ShopConfirmDialogUI_ShopConfirmPanel/ui_asset_slicing_backflow_ShopConfirmDialogUI_ShopConfirmPanel.md` |
| `ShopUI_ShopPanel` | 7 | `docs/ShopUI_ShopPanel/ui_asset_slicing_backflow_ShopUI_ShopPanel.md` |
| `StatusLineBannerUI_StatusLineBanner` | 3 | `docs/StatusLineBannerUI_StatusLineBanner/ui_asset_slicing_backflow_StatusLineBannerUI_StatusLineBanner.md` |
| `SurvivalLiveRankingUI_GameUIPanel` | 15 | `docs/SurvivalLiveRankingUI_GameUIPanel/ui_asset_slicing_backflow_SurvivalLiveRankingUI_GameUIPanel.md` |
| `SurvivalRankingPanel` | 7 | `docs/SurvivalRankingPanel/ui_asset_slicing_backflow_SurvivalRankingPanel.md` |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | 7 | `docs/SurvivalSettingsUI_SurvivalSettingsPanel/ui_asset_slicing_backflow_SurvivalSettingsUI_SurvivalSettingsPanel.md` |
| `SurvivalSettlementUI_SurvivalSettlementPanel` | 6 | `docs/SurvivalSettlementUI_SurvivalSettlementPanel/ui_asset_slicing_backflow_SurvivalSettlementUI_SurvivalSettlementPanel.md` |
| `TensionOverlayUI_TensionOverlay` | 4 | `docs/TensionOverlayUI_TensionOverlay/ui_asset_slicing_backflow_TensionOverlayUI_TensionOverlay.md` |

## 3. 建议目录

切图进入 Unity 前，建议按资料包分目录放置：

```text
Assets/Art/UI/Generated/<PrefabName>/
```

如果后续需要正式入库，可再合并进项目现有 UI 图集流程；合并前仍保留本索引里的文件名映射，方便从 prefab 反查来源。

## 4. 第十轮节点命中补充

第十轮已对 30 份切图回流规范追加“节点命中与回填策略”：

- `现有节点命中`：优先替换该节点上的 `Image.sprite` / `RawImage.texture`，保留子级 TMP、Button、脚本引用和显隐逻辑。
- `嵌套子 prefab 命中`：在父 prefab 中定位子 prefab 实例，优先回填该实例根节点或底板，不为子 prefab 重复建立资料包。
- `安全区/表现参考`：不作为必须回填的 Sprite 节点，只用于限定 gameplay 安全区、世界锚点、动效路径或留白。
- `施工目标需创建/改名`：只有这类行允许新增 Image 容器；新增时必须放在最近的稳定父节点下，并不得删除或重建原有 TMP、Button、脚本字段、按钮事件。

第十轮复查报告：`docs/ui_prefab_design_tenth_pass_audit.md`

## 5. 第十一轮机器可读 Manifest

第十一轮新增机器可读切图清单：

- `docs/ui_prefab_asset_slice_manifest.json`
- `docs/ui_prefab_asset_slice_manifest.csv`
- `docs/ui_prefab_asset_slice_manifest.md`
- `docs/ui_prefab_design_eleventh_pass_audit.md`

这份 manifest 把每个切图片段的 prefab、节点、坐标、目标路径、九宫格、动态 TMP 保留、节点命中状态和 RectTransform 回填值统一整理出来，便于后续自动化切图、导入 Unity 和回填 prefab。
