# M10: 美术资源

## 赚钱逻辑
美术是视觉吸引力的基础。好看的角色/特效=观众停留=潜在付费。资源丰富度决定养成深度感。

## 依赖模块
无（底层模块）

## 资源总览

| 类型 | 数量 | 位置 | 说明 |
|------|------|------|------|
| Spine角色 | 100+ | TextAsset/ | atlas+skel配对 |
| Sprite精灵 | 1989 | Sprite/ | UI/图标/元素 |
| Texture2D纹理 | 1743 | Texture2D/ | 背景/地图/大图 |
| AudioClip音效 | 56 | AudioClip/ | 战斗/礼物/UI |
| 图标资源 | 500+ | 各目录 | 装备/道具/技能图标 |

所有资源位于: `D:\claude\DM_polar\项目参考资料\jjwcs资源素材\`

## Spine角色资源

### 文件结构
每个Spine角色由2个文件组成:
- `{name}_atlas.txt` — 图集描述文件
- `{name}.txt` — 骨骼数据文件(skel)

### 角色分类

| 分类 | ID范围 | 数量 | 用途 |
|------|--------|------|------|
| 英雄(时装) | hero_301~hero_714 | 35+ | 玩家角色皮肤 |
| 小怪 | monster_40001~monster_40040 | 40 | PVE战斗敌人 |
| 关卡Boss | boss_41001~boss_42025 | 50+ | 关卡Boss |
| 通天塔Boss | boss_42101~boss_42105 | 5 | PVP Boss |
| 世界Boss | boss_42201~boss_42206 | 6 | 全服Boss |
| 礼物HUD | 各种 | 10+ | 礼物展示横幅 |

### ID→资源映射
通过 ArtResourcesAvatarTable.json:
```
ArtResAvartaId → ArtResourcesAvatarTable.Id → ResPath(Spine路径)
```
- AvatarTable.ArtResAvartaId → 英雄Spine
- MonsterTable.ArtResAvartaId → 怪物Spine
- 缩放: ArtResAvartaFactor (0.5~1.5)

## 图标资源

通过 ArtResourcesIconResTable.json:
```
IconResId → ArtResourcesIconResTable.Id → ResPath(图标路径)
```
- 装备图标: EquipmentTable.IconResId
- 道具图标: ItemTable.ArtResourcesIconResId
- 宠物图标: PetTable中引用
- 宝石图标: GemTable中引用

## 音效资源

通过 AudiosTable.json:
```
AudioId → AudiosTable.Id → ResPath(音频路径)
```

| 类型 | 数量 | 示例 |
|------|------|------|
| 战斗音效 | 20+ | 攻击/受击/死亡/技能 |
| 礼物音效 | 7+ | 每档礼物不同音效 |
| UI音效 | 10+ | 按钮/面板/升级 |
| BGM | 5+ | 主界面/战斗/Boss/PVP |

## 地图/背景资源

通过 MapTable.json (24张地图):

| MapId | 场景 | 说明 |
|-------|------|------|
| 1001 | 主城/大厅 | 待机场景 |
| 1002~1018 | 主线关卡 | 5大区域×不同风格 |
| 1008 | 世界Boss场景 | 特殊Boss背景 |
| 1009-1010 | PVP场景 | 准备室+战场 |
| 1011-1012 | 神兵遗冢 | 副本背景 |
| 1013-1014 | 通天塔 | PVP塔背景 |
| 其他 | 各副本 | 灵兽/仙灵/圣翼/坐骑 |

## 技能特效

通过 SkillEffectsTable 三阶段系统:

| 阶段 | 表名 | 说明 |
|------|------|------|
| Pre(前摇) | SkillEffectsPreTable | 蓄力动画 |
| Fly(飞行) | SkillEffectsFlyTable | 弹道/飞行物 |
| Hit(命中) | SkillEffectsHitTable | 爆炸/伤害效果 |

每个技能的 SkillEffectId → 对应三个阶段的Prefab路径

## UI精灵资源清单 (来自jjwcs Texture2D)

### 资源源目录
`D:\claude\DM_polar\项目参考资料\jjwcs资源素材\Texture2D\`
共计 1743 张 Texture2D，其中UI相关约 400+ 张。

### UI精灵分类及导入目标

#### 1. 面板背景 → `Assets/Textures/UI/Panels/`
| 用途 | 来源文件模式 | 尺寸要求 | 9-Slice |
|------|-------------|---------|---------|
| 主面板背景 | bg_panel_*, ui_panel_bg_* | 1080×1600+ | ✓ 四边60px |
| 弹窗背景 | bg_popup_*, ui_dialog_* | 720×400+ | ✓ 四边40px |
| 标题栏背景 | bg_title_*, ui_titlebar_* | 1080×115 | ✓ 水平拉伸 |
| 页签背景 | bg_tab_*, ui_tab_* | 200×60 | ✓ 水平拉伸 |
| 列表项背景 | bg_item_*, ui_listitem_* | 1000×120 | ✓ 水平拉伸 |

#### 2. 按钮 → `Assets/Textures/UI/Buttons/`
| 用途 | 来源文件模式 | 尺寸 | 状态 |
|------|-------------|------|------|
| 主要按钮(金) | btn_gold_*, btn_main_* | 280×80 | Normal/Pressed/Disabled |
| 次要按钮(蓝) | btn_blue_*, btn_sub_* | 200×60 | Normal/Pressed/Disabled |
| 关闭按钮 | btn_close_*, ui_close_* | 60×60 | Normal/Pressed |
| 返回按钮 | btn_back_*, ui_back_* | 80×60 | Normal/Pressed |
| 功能图标按钮 | btn_func_*, icon_btn_* | 120×120 | 正方形带框 |
| Tab按钮 | btn_tab_*, tab_* | 160×60 | Active/Inactive |

#### 3. 图标 → `Assets/Textures/UI/Icons/`
| 用途 | 来源文件模式 | 尺寸 | 数量 |
|------|-------------|------|------|
| 属性图标(HP/ATK/DEF等) | icon_attr_*, stat_* | 48×48 | 11种 |
| 货币图标(金币/钻石/积分) | icon_currency_*, gold_* | 48×48 | 5种 |
| 功能入口图标 | icon_func_*, menu_* | 96×96 | 12种(大厅按钮) |
| 品质框 | frame_quality_*, border_* | 120×120 | 8种(白~暗金) |
| VIP等级图标 | icon_vip_*, vip_* | 80×40 | 15级 |
| 宝石图标 | icon_gem_*, gem_* | 80×80 | 6种 |
| 宠物图标 | icon_pet_*, pet_* | 120×120 | 7种 |
| 元素图标(冰/毒/雷/火) | icon_element_* | 48×48 | 4种 |

#### 4. 进度条/滑条 → `Assets/Textures/UI/Bars/`
| 用途 | 来源文件模式 | 说明 |
|------|-------------|------|
| HP血条(绿) | bar_hp_*, hp_fill_* | 前景+背景，9-Slice水平 |
| 经验条(蓝) | bar_exp_*, exp_fill_* | 前景+背景 |
| Boss血条(红) | bar_boss_*, boss_hp_* | 多段制，每段独立 |
| 技能CD条 | bar_cd_*, skill_cd_* | 圆形遮罩 |
| 进度条(通用) | bar_progress_* | 前景+背景+边框 |

#### 5. 边框/装饰 → `Assets/Textures/UI/Frames/`
| 用途 | 来源文件模式 | 说明 |
|------|-------------|------|
| 头像框 | frame_avatar_*, avatar_border_* | 8种品质颜色 |
| 装备槽框 | frame_equip_*, slot_* | 空槽/已装备两态 |
| 卡片边框 | frame_card_* | 用于宠物/时装卡片 |
| 分割线 | line_*, divider_* | 水平/垂直分割 |
| 装饰角 | corner_*, deco_* | 面板四角装饰 |

#### 6. 特效/动画帧 → `Assets/Textures/UI/Effects/`
| 用途 | 来源文件模式 | 说明 |
|------|-------------|------|
| 品质光效 | glow_*, quality_fx_* | 橙/红/金品质发光 |
| 升级特效 | fx_levelup_*, upgrade_* | 序列帧动画 |
| 点击反馈 | fx_click_*, ripple_* | 按钮点击水波纹 |
| 红点提示 | dot_red_*, notification_* | 新内容提示红点 |
| 礼物动画帧 | fx_gift_*, gift_anim_* | 礼物飘入动画序列 |

### UI精灵导入设置
| 设置项 | UI精灵 | 图标 | 背景 |
|--------|--------|------|------|
| Sprite Mode | Single | Single | Single |
| Pixels Per Unit | 100 | 100 | 100 |
| Max Size | 512 | 256 | 1024 |
| Compression | ASTC 6×6 | ASTC 4×4 | ASTC 6×6 |
| Filter Mode | Bilinear | Point | Bilinear |
| Generate Mip Maps | ✗ | ✗ | ✗ |
| 9-Slice (Border) | 按需设置 | 无 | 无 |

### Sprite Atlas 打包规则
| Atlas名 | 包含内容 | 预估尺寸 |
|---------|---------|---------|
| UI_Common | 按钮+框+进度条+分割线 | 2048×2048 |
| UI_Icons | 所有图标(属性/货币/VIP等) | 1024×1024 |
| UI_Panels | 面板背景(9-Slice) | 2048×2048 |
| UI_Effects | 特效帧+光效 | 1024×1024 |
| UI_Quality | 品质框8种+品质光效 | 512×512 |

## 资源导入Unity规范

### Spine导入
1. 将atlas.txt + skel.txt + 对应png放入 Assets/Spine/ 目录
2. Unity Spine Runtime自动识别并生成SkeletonDataAsset
3. 设置: Scale=0.01(Spine默认像素单位), Premultiply Alpha=true

### Sprite导入
1. 放入 Assets/Sprites/ 按功能分子目录
2. 设置: Sprite Mode=Single, Pixels Per Unit=100
3. 用Sprite Atlas打包减少draw call

### AudioClip导入
1. 放入 Assets/Audio/ 按类型分子目录
2. BGM: Load Type=Streaming, 降低内存
3. SFX: Load Type=Decompress On Load, 减少延迟

### 背景/纹理导入
1. 放入 Assets/Textures/Maps/
2. Max Size=2048(地图背景), 1024(UI元素)
3. Compression=ASTC 6x6(移动端)

## 资源命名规范

| 资源类型 | 命名格式 | 示例 |
|---------|---------|------|
| 英雄Spine | hero_{id} | hero_301 |
| 怪物Spine | monster_{id} | monster_40001 |
| Boss Spine | boss_{id} | boss_41001 |
| 装备图标 | equip_{part}_{quality} | equip_weapon_gold |
| 道具图标 | item_{id} | item_20001 |
| 技能图标 | skill_{id} | skill_2001 |
| BGM | bgm_{scene} | bgm_battle |
| SFX | sfx_{action} | sfx_attack |

## 验收清单
- [ ] 至少5个Spine角色成功导入Unity并播放idle动画
- [ ] 图标资源在UI中正确显示(无缺失/拉伸)
- [ ] BGM可循环播放,SFX可触发播放
- [ ] 地图背景加载无黑屏
- [ ] 技能特效三阶段正确播放
- [ ] Sprite Atlas打包后draw call在合理范围
- [ ] 内存占用在500MB以内(20个角色场景)
