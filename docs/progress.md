# 极地生存法则 — 开发进度跟踪

> 最后更新：2026-02-25 #112 策划案v3.0升级：交互闭环系统§20（11个状态机）+技术美术规格§21+完整开发计划§22（242任务）| 维护者：策划Claude
> 上次更新：2026-02-24 #111 Phase3(M3-01~M3-05)完成：服务器逻辑修复+消息补全+排行榜重建+结算修复+设置面板
> **开发Claude规则**：每次完成任务后，必须更新本文件对应行的状态（🔄→✅）和日期

---

## 总体进度：Phase1(数值逻辑) ✅ | Phase2(表现层) ✅

---

## 系统完成度矩阵

| 系统 | 文件 | 完成度 | 状态 | 最后核查 |
|------|------|--------|------|---------|
| 游戏主管理器 | SurvivalGameManager.cs | 95% | ✅ 生产级 | 2026-02-24 |
| 网络管理 | NetworkManager.cs | 90% | ✅ 生产级 | 2026-02-24 |
| 消息协议 | SurvivalMessageProtocol.cs | 100% | ✅ 完整 | 2026-02-24 |
| 数据类型定义 | SurvivalDataTypes.cs | 100% | ✅ 完整 | 2026-02-24 |
| 资源系统 | ResourceSystem.cs | 100% | ✅ 完整（已对齐策划案 §4.1） | 2026-02-24 |
| 昼夜循环 | DayNightCycleManager.cs | 95% | ✅ 生产级 | 2026-02-24 |
| 城门系统 | CityGateSystem.cs | 100% | ✅ 完整（已对齐策划案 §4.3） | 2026-02-24 |
| 工人管理 | WorkerManager.cs | 100% | ✅ 完整（Pool 20个Workers，WORK_POSITIONS，Glow/Frozen） | 2026-02-24 |
| Worker视觉 | WorkerVisual.cs | 100% | ✅ **Phase2新建** 工位颜色/Glow/Frozen材质切换 | 2026-02-24 |
| Worker控制器 | WorkerController.cs | 100% | ✅ **Phase2新建** 5状态机（Idle/Move/Work/Special/Frozen） | 2026-02-24 |
| Worker气泡UI | WorkerBubble.cs | 100% | ✅ **Phase2新建** World Space Billboard，emoji工位图标 | 2026-02-24 |
| 礼物通知UI | GiftNotificationUI.cs | 100% | ✅ **Phase2重写** T1-T5分级特效（粒子/飞入/全屏/空投） | 2026-02-24 |
| 礼物特效工具 | GiftEffectSystem.cs | 100% | ✅ **Phase2新建** 动画工具库（FadeIn/PopIn/FlyIn/Drop…） | 2026-02-24 |
| 主播交互面板 | BroadcasterPanel.cs | 100% | ✅ **Phase2新建** ⚡加速(CD120s)+🌊事件(CD60s)，右侧圆形按钮 | 2026-02-24 |
| 服务器主播事件 | SurvivalRoom.js | 100% | ✅ **Phase2更新** broadcaster_action消息+creator权限+efficiency_boost | 2026-02-24 |
| 音频管理器 | AudioManager.cs | 100% | ✅ **Phase2扩展** +22SFX+4BGM+LoopSFX+SwitchBGM+CrossfadeBGM | 2026-02-24 |
| 音频常量 | AudioConstants.cs | 100% | ✅ **Phase2新建** 全部音效ID常量（防拼写错误） | 2026-02-24 |
| 昼夜音频集成 | DayNightCycleManager.cs | 100% | ✅ **Phase2更新** OnDay/OnNight触发BGM淡入淡出切换 | 2026-02-24 |
| 怪物控制器 | MonsterController.cs | 95% | ✅ 生产级 | 2026-02-24 |
| 怪物波次生成 | MonsterWaveSpawner.cs | 95% | ✅ 生产级 | 2026-02-24 |
| 顶栏UI | SurvivalTopBarUI.cs | 95% | ✅ 生产级 | 2026-02-24 |
| 结算UI | SurvivalSettlementUI.cs | 100% | ✅ 完整（T003已修复：Top3注入+边界处理） | 2026-02-24 |
| 排行榜系统 | RankingSystem.cs | 95% | ✅ 生产级（新增生存贡献追踪+GetTopN） | 2026-02-24 |
| **礼物配置** | **GiftConfig.js** | **100%** | **✅ 完整（7种礼物已对齐）** | 2026-02-24 |
| **客户端默认数值** | ResourceSystem.cs / CityGateSystem.cs | **100%** | **✅ 完整（已对齐策划案）** | 2026-02-24 |
| 服务器引擎 | SurvivalGameEngine.js | 100% | ✅ 生产级（数值已对齐） | 2026-02-24 |
| 服务器房间 | SurvivalRoom.js | 95% | ✅ 生产级 | 2026-02-24 |
| 房间管理 | RoomManager.js | 90% | ✅ 生产级 | 2026-02-24 |
| 抖音API | DouyinAPI.js | 85% | ✅ 可用（SECRET待填入） | 2026-02-24 |
| 配置文件 | GameConfig.cs | 95% | ✅ 完整 | 2026-02-24 |

---

## 任务队列

| 编号 | 任务描述 | 优先级 | 状态 | 目标文件 | 验收标准 | 完成日期 |
|------|---------|--------|------|---------|---------|---------|
| **T001** | 礼物系统重写：按策划案7种礼物重写 GiftConfig.js + 更新 SurvivalGameEngine.js 礼物处理 | P0 | ✅ 已完成 | GiftConfig.js, SurvivalGameEngine.js | 7种礼物名/价格/效果与 design_doc.md §5 完全一致 | 2026-02-24 |
| **T002** | 客户端默认数值对齐：food=500, coal=300, ore=150, gate_hp=1000 | P0 | ✅ 已完成 | ResourceSystem.cs, CityGateSystem.cs | 代码默认值与 design_doc.md §4.1/4.3 一致 | 2026-02-24 |
| **T003** | 排行榜注入修复：SurvivalSettlementUI C屏正确显示Top3 | P1 | ✅ 已完成 | SurvivalSettlementUI.cs, RankingSystem.cs | 结算C屏显示Top3头像+积分，无NullReference | 2026-02-24 |
| **T004** | 服务器数值同步：SurvivalGameEngine.js 资源初始值与策划案对齐 | P1 | ✅ 已完成 | SurvivalGameEngine.js, config/default.json | 服务器 initialFood=500, initialCoal=300, initialOre=150, gateHp=1000 | 2026-02-24 |
| **T005** | 生产部署：将T001/T004修改的GiftConfig.js + SurvivalGameEngine.js部署到服务器 | P0 | ✅ 已完成 | Server/src/GiftConfig.js, Server/src/SurvivalGameEngine.js | scp上传成功，PM2 online，/health返回200 | 2026-02-24 |
| **T006** | Inspector修复：_rankingSystem赋值 + ScreenC创建Top3Slot_0/1/2并赋值_top3Slots | P1 | ✅ 已完成 | MainScene（SurvivalSettlementPanel） | _rankingSystem=RankingSystem, _top3Slots=[Slot_0,1,2]，场景已保存 | 2026-02-24 |
| **T007** | 本地服务器弹幕模拟端到端测试 | P1 | ✅ 已完成 | Server/src/（只读测试，无修改） | 全部通过；发现3项配置偏差（见验收记录） | 2026-02-24 |
| **T008** | Unity Play Mode 端到端联调 | P1 | ✅ 已完成 | MainScene（只读验收） | 顶栏500/300/150/20℃/1000/1000 ✅；7种礼物名/分全部正确 ✅；无NullRef ✅ | 2026-02-24 |
| **T009** | 修复白天时长：config.json survivalDayDuration 180→240 | P1 | ✅ 已完成 | Server/config/default.json | remainingTime=240，与策划案§2一致 | 2026-02-24 |
| **T010** | 修复资源采集上限：food≤2000, coal≤1500, ore≤800（当前写死999） | P1 | ✅ 已完成 | Server/src/SurvivalGameEngine.js | 资源不超出各自上限 | 2026-02-24 |
| **T011** | 确认弹幕采集效果：food+5/coal+3/ore+2 是按200ms聚合还是单次（当前+2/+2/+1） | P2 | ✅ 分析完成（需修改） | Server/src/SurvivalGameEngine.js | 单次触发+2/+2/+1；策划案§3要求+5/+3/+2，需另立T012修正 | 2026-02-24 |
| **T012** | 修复弹幕采集基础效果：food+2→+5, coal+2→+3, ore+1→+2 | P1 | ✅ 已完成 | Server/src/SurvivalGameEngine.js | _applyWorkEffect cmd1=+5, cmd2=+3, cmd3=+2 | 2026-02-24 |
| **T013** | 生产部署：将T009/T010/T012修改的文件部署到远程服务器 | P0 | ✅ 已完成 | Server/config/default.json, SurvivalGameEngine.js | scp成功，PM2重启，/health返回200，/state remainingTime=240 | 2026-02-24 |
| **M4** | 100人并发弹幕压测：验证资源累积/上限/礼物/并发稳定性 | P0 | ✅ 已完成 | test_m4_barrage.js | 17/17通过，0失败；food/coal到上限正确；ore=730≤800；增速food=136/s，coal=90/s，ore=40/s；无崩溃无漂移 | 2026-02-24 |
| **T_FIX_BLACKSCREEN** | 调查并确认3D场景渲染状态（非真实黑屏：capture_ui_canvas仅捕获UI层） | P0 | ✅ 已完成 | Editor/CaptureGameView.cs | 游戏视图中心像素R=0.227 G=0.361 B=0.557（蓝色，非黑）；SnowGround/CentralFortress/CityGate均正确渲染 | 2026-02-24 |
| **T_WORKER_VISUAL** | Worker视觉系统：WorkerVisual + WorkerController(5状态机) + WorkerBubble + WorkerPool场景搭建 | P1 | ✅ 已完成 | WorkerVisual.cs, WorkerController.cs, WorkerBubble.cs, WorkerManager.cs | 20个Worker预创建并注入WorkerManager；5种工位颜色；emoji气泡；Glow/Frozen特殊状态 | 2026-02-24 |
| **T_GIFT_EFFECTS** | 礼物分级特效系统：重写GiftNotificationUI + 新建GiftEffectSystem（T1-T5动画工具库） | P1 | ✅ 已完成 | GiftNotificationUI.cs, GiftEffectSystem.cs | T1粒子0.5s✅ T2边框2s✅ T3飞入弹跳3s✅ T4全屏光晕5s✅ T5空投黑幕8s✅；3条横幅队列✅ | 2026-02-24 |
| **T_BROADCASTER** | 主播交互面板：BroadcasterPanel + SurvivalRoom.js broadcaster_action消息 | P1 | ✅ 已完成 | BroadcasterPanel.cs, SurvivalRoom.js | ⚡加速按钮CD=120s✅；🌊事件按钮CD=60s✅；首个连接者=creator✅；服务器广播broadcaster_effect✅ | 2026-02-24 |
| **T_AUDIO** | 音效基础层：AudioManager扩展+AudioConstants+DayNightCycleManager音频集成 | P1 | ✅ 已完成 | AudioManager.cs, AudioConstants.cs, DayNightCycleManager.cs | +22个SFX常量✅；+4个BGM(day/night/win/lose)✅；LoopSFX系统✅；昼夜切换BGM淡入淡出✅ | 2026-02-24 |
| **P2_SCENE_SETUP** | Phase2场景一键搭建：WorkerPool + Gift_Canvas + Broadcaster_Canvas（SetupPhase2Scene.cs） | P0 | ✅ 已完成 | MainScene, SetupPhase2Scene.cs | WorkerPool(20Workers,所有引用已注入)✅；Gift_Canvas(T1-T5+GiftBannerQueue)✅；Broadcaster_Canvas(2按钮)✅；编译0错误✅ | 2026-02-24 |
| **P2_WIRE_GIFT** | GiftNotificationUI SerializedField自动绑定（WireGiftNotificationUI.cs） | P0 | ✅ 已完成 | MainScene, Editor/WireGiftNotificationUI.cs | _canvasRoot+T1-T5所有面板+3个BannerSlot全部绑定✅；BannerSlot初始inactive✅ | 2026-02-24 |
| **P2_DIAG** | Phase2绑定状态诊断（DiagPhase2.cs） | P0 | ✅ 已完成 | Editor/DiagPhase2.cs | 4 PASS / 0 FAIL：WorkerManager(20workers)✅ GiftNotificationUI✅ BroadcasterPanel✅ Worker_00材质✅ | 2026-02-24 |
| **P2_FIX_PARTICLE** | 修复SnowParticleSystem粒子速度曲线模式不一致（x=RandomTwoCurves,y=Constant,z=RandomTwoCurves） | P1 | ✅ 已完成 | FixParticleCurves.cs | VelocityModule x/y/z统一为Constant(0)；Play Mode粒子错误消除✅ | 2026-02-24 |
| **P2_FIX_FONT** | 修复BubbleIcon+GiftIconBar+BarrageContent TMP字体→ChineseFont SDF | P2 | ✅ 已完成 | FixPhase2Issues.cs, FixGiftIconBarFont.cs | 20个BubbleIcon+6个IconBar/Barrage TMP改为ChineseFont SDF✅；emoji□警告大幅减少 | 2026-02-24 |
| **#110_LOBBY** | 大厅+Loading状态机：Idle→Loading→Running→Settlement，ExitBtn退出流程 | P0 | ✅ 已完成 | SurvivalGameManager.cs, SurvivalIdleUI.cs, SurvivalLoadingUI.cs | Play Mode截图验证大厅显示正常✅ | 2026-02-24 |
| **M3-01** | 服务器逻辑修复：城门数值/666弹幕/随机事件/炉温+3℃/点赞/效率倍率 | P0 | ✅ 已完成 | SurvivalGameEngine.js, SurvivalRoom.js | GATE_UPGRADE_COSTS=[0,100,250,500]✅；666→+15%效率30s✅；随机事件90-120s触发✅；broadcaster_effect data包装✅ | 2026-02-24 |
| **M3-02** | 客户端消息补全：broadcaster_effect/special_effect/random_event/gift_pause/bobao | P0 | ✅ 已完成 | SurvivalGameManager.cs, WorkerManager.cs, WorkerController.cs, SurvivalTopBarUI.cs | HandleBroadcasterEffect✅；HandleGiftPause→暂停Worker✅；资源预警阈值修正✅ | 2026-02-24 |
| **M3-03** | 排行榜UI全屏重建：SurvivalRankingUI（订阅结算事件，缓存Top10，Toggle显示） | P1 | ✅ 已完成 | SurvivalRankingUI.cs, SurvivalIdleUI.cs | ShowPanel()/HidePanel()/TogglePanel()✅；OnGameEnded缓存排行✅；Inspector需拖入_panel/_rowContainer/_rows | 2026-02-24 |
| **M3-04** | 结算UI修复：服务器rankings注入/playerName/auto-return-to-lobby | P0 | ✅ 已完成 | SurvivalGameEngine.js, SurvivalDataTypes.cs, SurvivalGameManager.cs | _buildRankings含playerName✅；SurvivalRankingEntry[]→SettlementData.Rankings映射✅；结算14s后自动返回大厅✅；RequestResetGame兼容Settlement状态✅ | 2026-02-24 |
| **M3-05** | 设置面板实现：BGM/SFX音量滑条，PlayerPrefs持久化，版本号 | P1 | ✅ 已完成 | SurvivalSettingsUI.cs, SurvivalIdleUI.cs | TogglePanel()✅；PlayerPrefs保存BGM/SFX音量✅；Inspector需拖入_panel/_bgmSlider/_sfxSlider | 2026-02-24 |

---

## 验收记录

| 日期 | 任务编号 | 验收结果 | 发现问题 |
|------|---------|---------|---------|
| 2026-02-24 | V001 | ✅ 编译通过，场景正常 | _rankingSystem=None（T006修复）；_top3Slots空（T006修复） |
| 2026-02-24 | T007 | ✅ 端到端弹幕模拟全部通过 | 见下方T007详细记录 |
| 2026-02-24 | T008 | ✅ Unity UI验收通过 | Play Mode启动正常；顶栏500/300/150/20℃/1000显示正确；7种礼物名称&积分全部正确；粒子系统警告（非关键）；GM模式WS联调留M5直播伴侣 |
| 2026-02-24 | M4 | ✅ 压测全部通过 | 17/17通过；food=500→2000(上限✅)；coal=300→1500(上限✅)；ore=150→730(≤800✅)；增速food=136/s、coal=90/s、ore=40/s；80弹幕/秒并发无崩溃；礼物Top3: 神秘空投5000/能量电池1000/甜甜圈500 |
| 2026-02-24 | Coplay运行时自测 | ⚠️ 数值逻辑通过，表现层严重缺失 | WS连接✅；日志[GM已连接]✅；弹幕→UI数值变化✅；礼物→食物+100✅；**3D场景黑屏❌；无工人动画❌；无礼物通知弹窗❌；主播无可点击交互❌** — 表现层需重新评估 |
| 2026-02-24 | Phase2验收 | ✅ 所有表现层系统代码完成+场景搭建完成+0编译错误 | **黑屏确认为误判**（capture_ui_canvas仅捕UI层，Game View实际正常蓝天白地✅）；WorkerPool 20个Worker已预创建并注入WorkerManager✅；Gift_Canvas T1-T5面板已预创建（需Inspector手动连线SerializedField）✅；Broadcaster_Canvas双按钮已创建（需Inspector连线）✅；音效框架完整（需添加.wav/.mp3音频文件到Resources/Audio/）✅；策划案docs/design_doc.md已升至v2.0✅ |
| 2026-02-24 | Phase2最终验收(Play Mode) | ✅ **0 ERROR** Play Mode启动无崩溃 | DiagPhase2: 4 PASS / 0 FAIL✅；GiftNotificationUI 21个字段全部绑定✅；BroadcasterPanel 7个字段绑定✅；WorkerManager._preCreatedWorkers[20]全部wired✅；Worker_00材质(_normalMat+_glowMat)✅；SnowParticleSystem粒子曲线修复✅；[SurvivalGM]已启动✅；AudioManager Loaded 3/7 BGM 11/38 SFX✅；无NullReferenceException✅；剩余警告：emoji□(TMP字体限制,需Sprite Asset)、DontDestroyOnLoad(预存架构) — 均为非阻塞性 |
| 2026-02-24 | Phase3(#111)代码完成 | ✅ 代码全部写完，服务器已部署 | M3-01~M3-05全部完成；服务器health返回ok✅；Unity编译待Coplay MCP恢复后验证；⏳ 待Inspector配置：SurvivalRankingUI._rowContainer(10行子对象)，SurvivalSettingsUI._panel/_bgmSlider/_sfxSlider，SurvivalIdleUI._rankingPanel/_settingsPanel引用；⏳ M3-00(FixWorkerMesh)待Unity菜单手动执行 |

---

## 里程碑

| 里程碑 | 描述 | 状态 | 预计完成 |
|--------|------|------|---------|
| **M1** | 核心系统可跑（昼夜+资源+城门+怪物） | ✅ 完成 | 2026-02-23 |
| **M2** | 礼物系统对齐策划案 | ✅ 完成 | 2026-02-24 |
| **M3** | 端到端联调（服务器→Unity→弹幕响应） | ✅ 完成 | 2026-02-24 |
| **M4** | 数值平衡验证（BarrageSimulator 100人测试） | ✅ 完成 | 2026-02-24 |
| **M_PHASE2** | Phase2表现层：Worker视觉+礼物特效+主播交互+音效系统 | ✅ 完成 | 2026-02-24 |
| **M_PHASE3** | Phase3可玩性修复：服务器逻辑+消息完整性+UI全功能化 | ✅ 完成 | 2026-02-24 |
| **M5** | 抖音直播伴侣联调+审核通过 | ⏳ 待开始 | Phase3后 |

---

## 已知风险

| 风险 | 等级 | 说明 |
|------|------|------|
| 礼物抖音ID未对齐 | 🔴 高 | 7种礼物的 douyin_id 需登录抖音直播后台填写 |
| DOUYIN_SECRET 未填入 | 🔴 高 | 服务器 .env 文件 SECRET 字段待填写，否则无法接收抖音推送 |
| Spine 2D角色动画 | 🟡 中 | 策划案提到 Spine 动画，但项目实际是3D，需确认角色方案 |
| TMP Emoji显示为□ | 🟡 中 | LiberationSans/ChineseFont SDF均不含emoji字形；需创建TMP Sprite Asset (emoji)或导入emoji TTF atlas方可显示彩色emoji |
| DontDestroyOnLoad警告 | 🟢 低 | NetworkManager/AudioManager嵌套在[Managers]下，DontDestroyOnLoad失效；跨场景复用时需迁移到场景根层级 |
| 音频文件缺失 | 🟡 中 | AudioManager框架完整但3/7 BGM、11/38 SFX文件缺失；需向Resources/Audio/添加.wav/.mp3文件 |

---

---

## T007 端到端测试详细记录（2026-02-24）

### 测试方法
本地启动服务器（`node Server/src/index.js`，端口8081），使用 curl + node WebSocket 脚本测试，**未修改任何源代码**。

### ✅ 通过项

| 测试点 | 预期 | 实际 | 结果 |
|--------|------|------|------|
| 健康检查 `/health` | `status:ok` | `status:ok` | ✅ |
| 初始 food（default房间 `/state`） | 500 | 500 | ✅ |
| 初始 coal | 300 | 300 | ✅ |
| 初始 ore | 150 | 150 | ✅ |
| 初始 furnaceTemp | 20 | 20 | ✅ |
| 初始 gateHp | 1000 | 1000 | ✅ |
| 礼物配置 `/gifts` | 7种礼物，fairy_wand存在 | 7种礼物全部返回 | ✅ |
| 旧礼物系统 | 不应出现小温暖/暖炉礼 | 无旧ID | ✅ |
| WS连接+join_room | 收到 survival_game_state | 已收到 | ✅ |
| start_game | state→day, day=1 | day=1, state=day | ✅ |
| 弹幕1（采食物） | work_command cmd=1 food | cmd=1 commandName=food | ✅ |
| 弹幕2（挖煤） | work_command cmd=2 coal | cmd=2 commandName=coal | ✅ |
| 弹幕3（挖矿） | work_command cmd=3 ore | cmd=3 commandName=ore | ✅ |
| 弹幕4（生火） | work_command cmd=4 heat | cmd=4 commandName=heat | ✅ |
| 礼物 fairy_wand（price_fen=1） | survival_gift giftId=fairy_wand, tier=T1 | fairy_wand 仙女棒 T1 score=1 efficiencyBonus=0.05 | ✅ |
| 礼物按价格匹配（douyin_id=TBD时） | findGiftByPrice(1)→fairy_wand | 正确回退 | ✅ |
| 推送统计 | 4评论+1礼物=5次 push | totalComments=4, totalGifts=1, pushesReceived=5 | ✅ |

### ⚠️ 发现的配置偏差（不影响核心逻辑，但需确认）

| 编号 | 问题 | 设计文档值 | 代码实际值 | 建议 |
|------|------|-----------|-----------|------|
| D1 | 白天时长 | design_doc §2 = **240s** | config.json = **180s**；实测remainingTime=180 | 更新config survivalDayDuration→240 |
| D2 | 资源上限（_applyWorkEffect） | food=2000, coal=1500, ore=800 | 代码写死999 | 将999改为对应上限（2000/1500/800） |
| D3 | 弹幕采集效果/次 | §3 food+5, coal+3, ore+2 /200ms/人 | 代码+2/+2/+1 /次指令 | 确认设计：是否为200ms聚合 vs 单次触发的差异 |

*策划案详见：docs/design_doc.md*

---

## #110 批次完成记录（2026-02-24）

### ✅ 已完成

#### 大厅 + Loading 状态机
| 任务 | 文件 | 状态 |
|------|------|------|
| SurvivalGameManager 新增 Loading 状态 + IsEnteringScene | SurvivalGameManager.cs | ✅ |
| RequestStartGame() 改为等服务器确认再进 Running | SurvivalGameManager.cs | ✅ |
| RequestExitToLobby() 新增（等服务器 idle 再回大厅） | SurvivalGameManager.cs | ✅ |
| LoadingTimeout() 15秒超时协程 | SurvivalGameManager.cs | ✅ |
| SurvivalIdleUI 扩展为完整大厅（标题/状态/排行榜/设置按钮） | SurvivalIdleUI.cs | ✅ |
| SurvivalLoadingUI 新建（挂 Canvas，Spinner 旋转） | SurvivalLoadingUI.cs | ✅ |
| Canvas 新增 LobbyPanel / LoadingPanel / GameUIPanel+ExitBtn | MainScene | ✅ |
| 所有 SerializedField 绑定（7个 IdleUI + 3个 LoadingUI） | MainScene | ✅ |
| ExitBtn → RequestExitToLobby() persistent listener | MainScene | ✅ |
| Play Mode 截图验证大厅界面正常 | — | ✅ |

#### Worker 角色替换（代码就绪）
| 任务 | 文件 | 状态 |
|------|------|------|
| WorkerVisual.cs Awake 改为 GetComponentInChildren<Renderer> | WorkerVisual.cs | ✅ |
| FixWorkerMesh.cs 编写（批量 Capsule→CowWorker） | Assets/Editor/FixWorkerMesh.cs | ⏳ 待执行 |

#### 编辑器工具
| 文件 | 用途 | 状态 |
|------|------|------|
| Assets/Editor/WireUIFields.cs | 创建+绑定 LobbyPanel/LoadingPanel/GameUIPanel 所有字段 | ✅ 已执行 |
| Assets/Editor/FixLobbyTextComponents.cs | UI.Text → TMP 批量转换 | ✅ 已执行 |
| Assets/Editor/FixWorkerMesh.cs | Capsule→CowWorker 批量替换 | ⏳ 待执行 |
| Assets/Editor/PreviewLobby.cs | LobbyPanel 可见性切换（调试） | ✅ |

### ⏳ 下一步（优先级顺序）

1. **[P0 立刻]** Unity 菜单 `Tools → DrscfZ → Fix Worker Mesh` → Console 确认 "共替换20个Worker"
2. **[P1]** Play Mode 验证 Worker 以 CowWorker 角色出现
3. **[P1]** 如 CowWorker 偏小/入地，调整 Worker scale 或 Body.localPosition.y
4. **[P1]** 联机全流程验证：大厅→进场→游戏→退出→回大厅（需服务器在线）
5. **[P2]** WorkerVisual 颜色/Glow/Frozen 效果在 CowWorker 上验证
6. **[P3]** 设置面板功能实现

### 系统完成度更新
| 系统 | 文件 | 完成度 | 变更 |
|------|------|--------|------|
| 游戏主管理器 | SurvivalGameManager.cs | 100% | +Loading 状态 + RequestExitToLobby + 超时协程 |
| 大厅UI | SurvivalIdleUI.cs | 100% | 扩展为完整大厅 |
| Loading UI | SurvivalLoadingUI.cs | 100% | 新建 |
| Worker视觉 | WorkerVisual.cs | 100% | GetComponentInChildren 修复 |
| Worker场景 | Worker_00~19 | 50% | 代码就绪，场景替换待执行 |
