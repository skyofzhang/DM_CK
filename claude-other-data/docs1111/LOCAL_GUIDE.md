# 《你修仙很牛吗》本地文件指南

> 方便你在本地浏览检查所有项目文件

## 目录结构总览

```
D:\claude\DM_polar\
├── CLAUDE.md                          ← 项目入口(Claude新会话必读)
├── docs\
│   ├── LOCAL_GUIDE.md                 ← 本文件(文件指南)
│   └── modules\
│       ├── INDEX.md                   ← 模块索引(必读,很短)
│       ├── M01_visual_attraction.md   ← 视觉吸引力
│       ├── M02_interaction_entry.md   ← 互动入口(弹幕指令)
│       ├── M03_core_gameplay.md       ← 核心玩法(战斗/关卡)
│       ├── M04_growth_payment.md      ← 成长付费(14条养成线)
│       ├── M05_gift_economy.md        ← 礼物经济(7档/VIP/商城)
│       ├── M06_social_competition.md  ← 社交竞争(PVP/Boss/排行)
│       ├── M07_retention_loop.md      ← 留存循环(每日时间表)
│       ├── M08_tech_foundation.md     ← 技术底座(服务器/协议)
│       ├── M09_ui_screens.md          ← 界面交互(15+面板清单)
│       └── M10_art_resources.md       ← 美术资源(Spine/音效)
├── nxxhnm\                            ← Unity工程目录
│   ├── Assets\                        ← 游戏资源(待导入)
│   ├── Packages\
│   ├── ProjectSettings\
│   └── ...
├── server\                            ← 服务器代码(待创建)
├── tools\                             ← 开发工具(待创建)
└── 项目参考资料\                       ← 九劫问长生提取资料
    ├── jjwcs资源素材\                  ← 提取的Unity资源
    │   ├── AnimationClip\             ← 7个动画片段
    │   ├── AudioClip\                 ← 56个音效文件
    │   ├── Font\                      ← 字体文件
    │   ├── Material\                  ← 材质文件
    │   ├── Mesh\                      ← 3D模型
    │   ├── Shader\                    ← 着色器
    │   ├── Sprite\                    ← 1989张精灵图(UI/图标)
    │   ├── TextAsset\                 ← ⭐ 核心数据
    │   │   ├── *.json                 ← 59张游戏配表
    │   │   ├── *_atlas.txt            ← Spine图集
    │   │   └── *.txt(非atlas)         ← Spine骨骼数据
    │   ├── Texture2D\                 ← 1743张纹理(背景/地图)
    │   └── ...其他类型
    ├── 我以往的类似项目的策划表\        ← 30张XLS策划表
    │   ├── Buff数据表.xls
    │   ├── VIP贡献等级表.xls
    │   ├── 技能表.xls
    │   ├── 怪物表.xls
    │   ├── 装备表.xls
    │   ├── 关卡表.xls
    │   ├── 礼物表(抖音).xls
    │   └── ...等30个文件
    ├── 【九劫问长生】开播文档.docx     ← 原始游戏设计文档
    └── docx_images\                   ← 开播文档提取的图片
```

## 核心文件说明

### 策划文档 (docs/modules/)

| 文件 | 内容 | 你关注什么 |
|------|------|-----------|
| INDEX.md | 10个模块索引 | 快速查找模块 |
| M01 视觉 | Spine角色规格/色彩规范/特效 | 画面效果好不好看 |
| M02 互动 | 5条弹幕指令/加入流程/人数限制 | 玩家怎么进游戏 |
| M03 核心 | 500关/3波制/Boss/伤害公式 | 游戏怎么玩 |
| M04 成长 | 14条养成线/数值表/元素系统 | 玩家怎么变强 |
| M05 礼物 | 7档礼物/VIP/商城/夺宝 | 怎么赚钱 |
| M06 竞争 | 通天塔/15v15/世界Boss/排行 | 社交玩法 |
| M07 留存 | 每日时间表/副本/福利 | 玩家为什么回来 |
| M08 技术 | 服务器架构/协议/部署 | 技术实现方案 |
| M09 界面 | 15+面板详细布局 | UI长什么样 |
| M10 美术 | 资源清单/导入规范 | 素材怎么用 |

### 参考资料

| 资料 | 数量 | 说明 |
|------|------|------|
| JSON配表 | 59张 | 九劫完整数值体系(直接复用) |
| XLS策划表 | 30张 | 数值设计参考 |
| Spine角色 | 100+ | 可直接导入Unity |
| 精灵图 | 1989张 | UI/图标/元素图 |
| 纹理图 | 1743张 | 背景/地图 |
| 音效 | 56个 | 战斗/礼物/UI音效 |

### 关键JSON配表速查

| 配表 | 用途 | 关注 |
|------|------|------|
| RoleLvTable.json | 角色等级+属性 | 500+级数据 |
| EquipmentTable.json | 装备属性 | 8品质×9部位 |
| GemTable.json | 宝石系统 | 6种×10级 |
| PetTable.json | 宠物数据 | 10+只 |
| BarrierTable.json | 关卡数据 | 500+关 |
| MonsterTable.json | 怪物属性 | 78种 |
| SkillTable.json | 技能数据 | 200+技能 |
| GiftEffectTable.json | 礼物效果 | 30条映射 |
| PlatformGiftTable.json | 平台礼物 | 42条×6平台 |
| VipTable.json | VIP等级 | 15级 |
| ShopTable.json | 商城数据 | 积分兑换 |
| EndlessTowerTable.json | 通天塔 | 600层 |
| LuckyDrawTable.json | 夺宝奖池 | 36种奖品 |

## 新命名方案

本项目使用全新名称（非九劫原名）：

| 系统 | 新名称 |
|------|--------|
| 宝石 | 赤炎石/破锋石/玄铁石/裂天石/幻影石/噬血石 |
| 宠物 | 青鸾/玄龟/赤蛟/金翅/墨麒麟/白泽/烛龙 |
| 世界Boss | 焰灵妖君/渊海魔尊/九幽邪帝 |
| PVP | 通天塔 |
| 副本 | 灵兽秘境/仙灵幻域/羽翼试炼/神兵遗冢 |
| 保底称号 | 万法归宗 |
| 修仙时装 | 清风剑仙/月华仙子/星河道人 |

## 服务器信息

| 项目 | 值 |
|------|-----|
| IP | 123.206.122.216 |
| SSH | root / Ouxuanze@qq#$@ |
| 端口 | 8088 |
| 代码 | /opt/nxxhnm/src/ |
| PM2 | nxxhnm-server |
