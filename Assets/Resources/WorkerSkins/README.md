# WorkerSkins 目录

§30.7 礼物触发限时皮肤 + §30.11 阶段皮肤 Prefab 路径。

**命名规范**（硬约束，代码 Resources.Load 按此读）：
- 阶段皮肤：`Kuanggong_T{01..10}.prefab`（10 套，对应 §30.11 阶 1-10）
- 礼物限时皮肤：`Kuanggong_G{01..03}.prefab`
  - G01：T4 能量电池 → 炽热矿工（+2 炉温）
  - G02：T5 爱的爆炸 → 战魂矿工（+30% ATK）
  - G03：T6 神秘空投 → 空投精英（+20% 采矿）

**加载代码**：`WorkerController.SetGiftSkin(skinId)` → `Resources.Load<GameObject>($"WorkerSkins/Kuanggong_{skinId}")`
fallback：`Resources.Load<GameObject>("WorkerSkins/KuanggongWorker_01")`（未建 Prefab 时保底）

**规格**：scale=0.015 Variant；bones 对齐 kuanggong_01；Tex 1024；阴影关闭。

**当前状态**：美术未交付，SetGiftSkin 会 log warning + fallback。目录存在避免 Resources.Load 路径缺失报错。
