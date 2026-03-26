# 助手A任务指令 — UI面板重写 (需设计的7个面板)

> 你是"你修仙很牛吗"项目的UI开发助手A。
> 你有 Coplay MCP 权限，可以在 Unity 中验证布局。

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
5. 每个文件必须有头部声明: Requires/Canvas/Priority/Entry/Exit/Animation/ShowCondition

## 标准文件结构

```
# [面板中文名] — view_xxx
> Requires: ui_theme.md
> Canvas: L_XXX (sort order)
> Priority: P?
> CachePolicy: Always/Normal/Disposable
> Entry: 从哪来
> Exit: 去哪
> Animation: A_PANEL_IN / A_PANEL_OUT
> ShowCondition: 显示条件

## 元素表
(8列表格: ID/Type/Parent/Anchor/Pos/Size/Style/Content)

## 交互逻辑
(伪代码)

## 状态变化
(条件→表现 表格)

## Logic Rules (如有复杂逻辑)

## 数据源
(字段/来源/刷新频率 表格)
```

## 你的7个任务 (按顺序执行)

### 任务1: view_battle_hud.md (P0+)
- 源: 旧M09 第198-248行
- 补充: Boss多段血条伪代码(每段100万HP, 奇偶段颜色切换)
- 补充: 飘字对象池管理伪代码
- 补充: 数据源表

### 任务2: view_gem.md (P1)
- 源: 旧M09 第347-395行
- **重点补充**: 轨道环形精确坐标! 旧版只写了"左上/右上"太模糊
- 设计方案: 6颗宝石以角色剪影为中心, 半径280px环形排列
- 从12点方向顺时针: 赤炎(0°)→破锋(60°)→玄铁(120°)→噬血(180°)→幻影(240°)→裂天(300°)
- 计算每颗宝石的精确Pos (基于GemOrbitArea中心点)
- 颜色用 C_GEM_* 变量

### 任务3: view_costume.md (P2)
- 源: 旧M09 第438-461行 (极简描述,需完整重写)
- **重点补充**: 5列7行网格的完整规格
- cell=196×196, 水平间距4px, 纵向ScrollRect滚动
- 选中态: C_Q_GOLD边框 + 品质发光
- 未拥有: 全灰Alpha=0.4 + 锁图标覆盖
- 底部[穿戴]+[升星]按钮 + 属性对比区

### 任务4: view_shop.md (P1)
- 源: 旧M09 第463-481行 (仅描述文字,需完整重写为元素表)
- 设计: 顶部积分余额 + 3Tab + 商品卡片2列GridLayout(cell=516×160) + 倒计时
- 包含价格配表引用

### 任务5: view_ranking.md (P2)
- 源: 旧M09 第484-502行 (仅描述文字)
- 设计: 4Tab + 前3名大卡(金银铜) + 4-100名列表 + 底部自身排名栏
- 前3名高度: 第1名240px, 第2/3名200px

### 任务6: view_pvp.md (P2)
- 源: 旧M09 第505-520行 (仅描述文字)
- 设计: 上半通天塔区(塔插画+层数+时间+按钮) + 下半实时PVP区(段位徽章+积分+按钮)
- 10级段位: 青铜→白银→黄金→钻石→铂金→翡翠→紫晶→星耀→王者→传奇王者

### 任务7: view_battle_result.md (P1)
- 源: 旧M09 第622-634行 (简述)
- 设计: stagger进入动画序列(标题0s→MVP 0.3s→奖励0.6s→排名0.9s→按钮1.2s)
- 胜利=C_TITLE / 失败=C_TEXT_DIM

## 输出位置
所有文件写入: `D:\claude\DM_polar\docs\modules\ui\views\`

## 完成后
每个文件写完后自检:
1. 是否有硬编码颜色值? → 替换为C_变量
2. 头部6项声明是否完整?
3. 交互逻辑是否全部伪代码?
4. 单文件是否≤100行?
