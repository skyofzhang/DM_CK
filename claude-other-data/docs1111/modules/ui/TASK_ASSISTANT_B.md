# 助手B任务指令 — UI面板重写 (格式转换为主的14个面板)

> 你是"你修仙很牛吗"项目的UI开发助手B。
> 你的任务以格式转换为主，将旧M09中已有的元素数据转为新格式。

## 必读文件 (开始前加载)

1. `D:\claude\DM_polar\docs\modules\ui\ui_theme.md` — 全局变量定义
2. `D:\claude\DM_polar\docs\modules\ui\ui_architecture.md` — UIManager架构
3. `D:\claude\DM_polar\docs\modules\ui\views\view_main_lobby.md` — **标准模板示例** (严格参照格式)
4. `D:\claude\DM_polar\docs\modules\M09_ui_screens.md` — 旧版数据源 (提取坐标/尺寸/颜色)

## 格式规则 (Manus 5条)

1. 所有颜色用 C_ 变量，禁止硬编码 #xxx
2. 通用组件用 @Component 引用 (@TitleBar/@CloseButton/@BackButton/@RedDot/@QualityBorder)
3. Style列只写与默认值不同的差异
4. 交互逻辑用伪代码: `OnClick(X): if cond → action`
5. 每个文件必须有头部声明: Requires/Canvas/Priority/CachePolicy/Entry/Exit/Animation/ShowCondition

## 颜色映射速查 (旧→新)

| 旧硬编码 | 新变量 |
|---------|--------|
| #FFD700 | C_TITLE 或 C_VIP 或 C_Q_GOLD (按上下文) |
| #CC44FF | C_REALM 或 C_Q_PURPLE |
| #FF8800 | C_POWER 或 C_Q_ORANGE |
| #4488FF | C_Q_BLUE 或 C_DEF |
| #FF4444 / #FF2200 | C_NEGATIVE |
| #00FF88 | C_POSITIVE |
| #FFFFFF | C_TEXT_WHITE |
| #C8C8C8 | C_TEXT |
| #888888 | C_TEXT_DIM |
| #0A0A28 | C_BG_DEEP |
| #1A1A4A | C_BG_CARD |
| #0E0E2A | C_BG_BAR |
| #00000080 | C_BG_OVERLAY_50 |

## 你的14个任务 (按优先级排序)

### P0 组 — 简单面板 (5个, 各约25-35行)

#### 任务1: view_barrage_bar.md
- 源: 旧M09 第590-605行
- 要点: 常驻, Raycast穿透, 滚动文字
- CachePolicy: Always, Canvas: L_BARRAGE

#### 任务2: view_loading.md
- 源: 旧M09 第608-619行
- 要点: 全屏Logo+进度条+状态文字+Tips循环
- CachePolicy: Always, Canvas: L_OVERLAY

#### 任务3: view_confirm_dialog.md
- 源: 旧M09 第637-647行
- 要点: 半透明遮罩+卡片720×400+[确认]/[取消]
- 补充API: ConfirmDialog.Show(title, content, onConfirm, onCancel)
- CachePolicy: Normal, Canvas: L_POPUP

#### 任务4: view_toast.md
- 源: 旧M09 第650-663行
- 要点: 最多3条, 新的在上旧的下移, Raycast穿透
- 补充API: ToastManager.Show(text, type)
- 补充5种type: SUCCESS(绿)/FAIL(红)/REWARD(金)/INFO(蓝)/WARN(橙)
- CachePolicy: Always, Canvas: L_TOAST

#### 任务5: view_disconnect.md
- 源: 旧M09 第684-697行
- 要点: 全屏遮罩85%+自动重试(5s间隔,最多5次)+手动重试按钮
- 补充重连状态机伪代码
- CachePolicy: Normal, Canvas: L_OVERLAY

### P1 组 — 有完整元素表的面板 (5个, 各约50-70行)

#### 任务6: view_character.md
- 源: 旧M09 第251-298行
- 要点: 左侧Spine+右侧8属性+底部3Tab+突破按钮
- 格式转换为主, 颜色全部换变量

#### 任务7: view_equipment.md
- 源: 旧M09 第301-344行
- 要点: 模型区+3×3槽位+详情区+4操作按钮
- 补充: 子面板EquipSelectList的入口声明
- 补充: 强化/附魔/吞噬的伪代码逻辑

#### 任务8: view_pet.md
- 源: 旧M09 第398-435行
- 要点: Spine展示+属性+6格装备+横向卡片列表+4按钮
- 补充: 7种宠物品质色映射

#### 任务9: view_lottery.md
- 源: 旧M09 第523-547行
- 要点: 大转盘36格+指针+保底进度+幸运值+按钮
- 补充: 旋转动画序列伪代码(加速→匀速→减速→停止)

#### 任务10: view_gift_effect.md
- 源: 旧M09 第667-681行
- 要点: 4档特效(1币/10-52币/99-199币/520币)+Raycast穿透
- 补充: 520档时其他UI Alpha降至0.3的伪代码

### P2 组 (1个)

#### 任务11: view_inventory.md
- 源: 旧M09 第550-559行 (简述)
- 设计: 4Tab + 5列道具网格(cell=176×200) + 点击→道具详情浮层(L_POPUP)
- 详情浮层包含: 名称+描述+来源+用途+[使用]按钮

### P3 组 (2个)

#### 任务12: view_collection.md
- 源: 旧M09 第562-573行 (简述)
- 设计: 总进度条 + 4Tab副本分类 + Boss网格(4列,cell=244×280) + 已收集/未收集态

#### 任务13: view_settings.md
- 源: 旧M09 第576-587行 (简述)
- 设计: 较小面板900×960居中 + 音效/音乐Slider + 画质Radio + 版本号
- Slider规格: 高36px, 滑块圆形48×48

### 额外任务

#### 任务14: 更新 INDEX.md
- 文件: `D:\claude\DM_polar\docs\modules\INDEX.md`
- 更新M09条目指向新的 ui/ 目录
- 添加说明: "M09已重构为多文件结构,详见 ui/ 目录"

## 输出位置
所有view文件写入: `D:\claude\DM_polar\docs\modules\ui\views\`

## 完成后自检清单
每个文件写完自检:
1. ✅ 无硬编码颜色值(#xxx)? → 全部替换为C_变量
2. ✅ 头部声明完整(Requires/Canvas/Priority/CachePolicy/Entry/Exit/Animation/ShowCondition)?
3. ✅ 通用组件用@引用?
4. ✅ 交互逻辑全部伪代码?
5. ✅ 单文件≤100行?
6. ✅ Style列只写差异?
