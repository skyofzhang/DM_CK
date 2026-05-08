# UI Prefab Asset Slice Manifest

> 日期：2026-05-08  
> 用途：给后续 UI 切图、Unity 导入和 prefab 回填提供机器可读清单。  
> 数据来源：30 份 `ui_asset_slicing_backflow_<PrefabName>.md`。

## 1. 文件

- JSON：`docs/ui_prefab_asset_slice_manifest.json`
- CSV：`docs/ui_prefab_asset_slice_manifest.csv`
- 本说明：`docs/ui_prefab_asset_slice_manifest.md`
- 第十一轮审计：`docs/ui_prefab_design_eleventh_pass_audit.md`

## 2. 汇总

| 项 | 数量 |
| --- | ---: |
| 资料包 | 30 |
| manifest entries | 162 |
| 需要实际切图 | 144 |
| 安全区/表现参考 | 18 |
| 九宫格/Sliced 建议 | 53 |
| TMP/动态文案保留 | 62 |
| 允许创建/改名容器 | 22 |

## 3. 命中状态

| 命中状态 | 数量 |
| --- | ---: |
| 安全区/表现参考 | 22 |
| 嵌套子 prefab 命中 | 11 |
| 施工目标需创建/改名 | 22 |
| 现有节点命中 | 107 |

## 4. 每个资料包

| 资料包 | 条目 | 动态文案 | 需创建/改名 | 不切图参考 |
| --- | ---: | ---: | ---: | ---: |
| `AnnouncementUI_AnnouncementPanel` | 4 | 2 | 1 | 1 |
| `BossRushBanner` | 6 | 3 | 0 | 1 |
| `BroadcasterPanel_BroadcasterPanelController` | 6 | 1 | 0 | 1 |
| `ChapterAnnouncementUI_ChapterAnnouncement` | 4 | 2 | 0 | 1 |
| `ConnectPanel` | 5 | 3 | 1 | 0 |
| `DayPreviewBanner` | 5 | 3 | 0 | 1 |
| `EfficiencyRaceUI_EfficiencyRaceBanner` | 3 | 1 | 0 | 1 |
| `FairyWandMaxedBanner` | 4 | 1 | 0 | 1 |
| `FeatureUnlockBanner` | 4 | 2 | 0 | 1 |
| `FrozenStatusPanel` | 5 | 3 | 1 | 1 |
| `GameControlUI_BottomBar` | 6 | 1 | 0 | 1 |
| `GateUpgradeConfirmUI` | 7 | 2 | 1 | 0 |
| `GiftImpactUI_GiftImpactBanner` | 3 | 1 | 0 | 1 |
| `GloryMomentUI_GloryMomentBanner` | 5 | 2 | 0 | 1 |
| `LobbyPanel` | 6 | 2 | 1 | 1 |
| `NightModifierUI_NightModifierBanner` | 4 | 2 | 0 | 1 |
| `NightReportUI_NightReportPanel` | 5 | 3 | 2 | 0 |
| `OreRepairFloatingText_OreRepairFloatRoot` | 4 | 0 | 1 | 0 |
| `PauseOverlayUI_PauseOverlayPanel` | 4 | 2 | 4 | 0 |
| `PeaceNightOverlay` | 5 | 3 | 0 | 1 |
| `PreGameBannerUI_PreGameBanner` | 6 | 3 | 0 | 1 |
| `ReconnectDialog` | 5 | 2 | 1 | 0 |
| `ShopConfirmDialogUI_ShopConfirmPanel` | 7 | 4 | 1 | 0 |
| `ShopUI_ShopPanel` | 7 | 3 | 1 | 0 |
| `StatusLineBannerUI_StatusLineBanner` | 3 | 2 | 1 | 1 |
| `SurvivalLiveRankingUI_GameUIPanel` | 15 | 5 | 0 | 1 |
| `SurvivalRankingPanel` | 7 | 1 | 1 | 0 |
| `SurvivalSettingsUI_SurvivalSettingsPanel` | 7 | 2 | 1 | 0 |
| `SurvivalSettlementUI_SurvivalSettlementPanel` | 6 | 1 | 1 | 0 |
| `TensionOverlayUI_TensionOverlay` | 4 | 0 | 3 | 0 |

## 5. 使用方式

1. 先按 `sourcePrototypePng` 和 `sourcePrompt` 生成 UI 效果图。
2. 按 JSON/CSV 中 `sliceFile` 拆透明 PNG；`noSlice=true` 的行只做参考，不生成 Sprite。
3. 将切图放到 `unityFolder` / `targetAssetPath`。
4. 按 `nineSlice`、`dynamicTextPreserved`、`allowCreateOrRename` 和 `hitStatus` 决定导入和回填策略。
5. 回填 prefab 时按 `rectTransform`、`sizeDelta`、`anchoredPosition` 校验位置，保持 TMP、Button、脚本字段和显隐逻辑不断链。
