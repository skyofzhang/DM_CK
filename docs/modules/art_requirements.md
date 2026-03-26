# 极地生存法则 — 美术资源需求清单 v1.0

> Phase 2 表现层所需美术资源全清单
> 来源优先级：①可用的现有资源 ②Coplay AI生成 ③Unity Asset Store免费 ④纯代码实现（ProceduralArt）

---

## 1. 3D场景资源

| 资源名 | 类型 | 规格 | 来源 | 状态 |
|--------|------|------|------|------|
| 雪地地面 | 3D Mesh + Material | 平坦雪地，URP Lit材质，白色+蓝色调 | ProceduralArt/简单Plane | ⏳ |
| 村庄中心建筑 | 3D Prefab | 低多边形(Low-poly)风格，有屋顶积雪效果 | Asset Store: "Low Poly Winter" | ⏳ |
| 鱼塘区域 | 3D Mesh | 蓝色圆形冰面+鱼跃出水面装饰 | ProceduralArt | ⏳ |
| 煤矿区域 | 3D Prefab | 矿山入口+矿石堆 | Low-poly资源包 | ⏳ |
| 矿山区域 | 3D Prefab | 岩石山丘+矿石晶体 | Low-poly资源包 | ⏳ |
| 炉灶区域 | 3D Prefab | 石砌炉灶，有发光火焰效果 | ProceduralArt + Particle | ⏳ |
| 城门 | 3D Prefab | 木制城门+两侧木墙 | Low-poly资源包 | ⏳ |
| 怪物（简单版） | 3D Prefab | 低多边形红色生物，Capsule改造 | ProceduralArt（Phase 2临时） | ⏳ |
| 天空盒（白天） | Skybox Material | 冬日晴空，蓝白渐变 | Unity内置 Procedural Skybox | ⏳ |
| 天空盒（夜晚） | Skybox Material | 深蓝夜空+满月 | Unity内置 + 自定义 | ⏳ |

**Phase 2简化策略**：
- 所有3D模型使用Low-poly风格（少于500面）
- Worker使用彩色Capsule（不同工位颜色不同）
- 重点是场景能正常渲染，不求精细

---

## 2. UI图标资源

| 图标 | 尺寸 | 颜色 | 用途 | 来源 |
|------|------|------|------|------|
| icon_food（鱼） | 48×48px | 橙色 | 顶部HUD食物 | Coplay生成 或 emoji PNG |
| icon_coal（煤炭） | 48×48px | 深灰 | 顶部HUD煤炭 | 同上 |
| icon_ore（矿石） | 48×48px | 青蓝 | 顶部HUD矿石 | 同上 |
| icon_thermometer（温度计） | 32×64px | 白色 | 顶部HUD炉温 | 同上 |
| icon_gate（城门HP） | 32×32px | 绿→红 | 城门HP条标签 | 同上 |
| icon_sun（白天） | 32×32px | 金黄 | 倒计时白天图标 | 同上 |
| icon_moon（夜晚） | 32×32px | 冷蓝 | 倒计时夜晚图标 | 同上 |
| icon_fairy_wand（仙女棒） | 64×64 | 金色 | T1礼物 | 同上 |
| icon_ability_pill（药丸） | 128×128 | 蓝色 | T2礼物 | 同上 |
| icon_magic_mirror（镜子） | 128×128 | 冰蓝 | T2礼物 | 同上 |
| icon_donut（甜甜圈） | 240×240 | 粉橙 | T3礼物大图标 | 同上 |
| icon_super_jet（火箭） | 240×240 | 红橙 | T3礼物大图标 | 同上 |
| icon_energy_battery（电池） | 320×320 | 黄色 | T4礼物大图标 | 同上 |
| icon_mystery_airdrop（空投箱） | 400×400 | 深色+金色标识 | T5礼物大图标 | 同上 |
| icon_broadcaster_boost（闪电） | 80×80 | 黄色 | 主播⚡按钮 | 同上 |
| icon_broadcaster_event（波浪） | 80×80 | 绿色 | 主播🌊按钮 | 同上 |

**生成建议（给Coplay generate_or_edit_images）**：
- 风格：扁平化简约图标，冬日主题配色，透明背景PNG
- 示例prompt："flat vector icon, fish with snowflake, blue and orange colors, transparent background, 48x48px style"

---

## 3. 特效资源

| 特效 | 类型 | 说明 |
|------|------|------|
| 火焰（炉灶） | ParticleSystem | 暖橙火焰，循环；已有基础，需优化颜色 |
| 雪花（背景） | ParticleSystem | 轻微飘雪，Coverage_Canvas或世界空间 |
| 金色粒子（T5） | ParticleSystem | 金色sparkle，烟花效果 |
| 冰晶（冻结） | ParticleSystem | 冰蓝色粒子环绕Worker |
| Worker光晕（666） | Material + Shader | 自发光金色材质，HDR |

**所有粒子效果均使用 Unity URP兼容的材质**（不使用Legacy粒子着色器）

---

## 4. 字体资源

| 字体 | 用途 | 来源 |
|------|------|------|
| 中文粗体（Primary） | HUD数值、bobao、标题 | Unity中文字体包 / 思源黑体Bold |
| Mono数字（Numbers） | 倒计时、积分数值 | Roboto Mono Bold（免费） |

**TextMeshPro字体资源生成**：
- 需要创建TMP Font Asset（包含常用汉字+数字+标点）
- 字符集：常用汉字3000字 + ASCII

---

## 5. 生成优先级

| 优先级 | 资源 | 理由 |
|--------|------|------|
| P0（必须）| 顶部HUD图标×7 | 游戏运行必须可见 |
| P0（必须）| 礼物图标×7（各等级代表） | 礼物系统演示必须 |
| P1（重要）| 场景基础Skybox + 雪地地面 | 解决黑屏问题 |
| P1（重要）| Worker气泡emoji图标 | Worker工作类型可见 |
| P2（美化）| 低多边形村庄建筑 | 提升场景观感 |
| P3（可选）| 完整怪物模型 | Phase 3再优化 |

---

*文档维护：策划Claude | 更新：2026-02-24*
