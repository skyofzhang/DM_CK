# 冬日生存法则 · HTML界面原型提示词（14个界面）
> 工具：Codx | 用途：先生成HTML原型确认布局，再对齐Unity实现
> 更新：2026-03-11

---

## 使用说明

1. 每个提示词**独立使用**，可直接粘贴给 Codx，无需附加任何额外说明
2. 每个提示词内已内嵌全局设计规范，确保风格统一
3. 建议按**优先顺序**生成：③游戏主HUD → ①空闲大厅 → 结算三屏 → 其余
4. 生成后在浏览器中以 **Ctrl+缩放50~60%** 查看完整竖屏效果

---

## ① 空闲大厅界面（IdleUI）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 空闲大厅（游戏未开始，等待主播开场）

【画布规格】
固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden，
font-family: 'PingFang SC','Microsoft YaHei',sans-serif

【全局设计语言】
冰雪要塞生存游戏，稍写实风格，冷色系配色。
主背景：深蓝渐变 #0D1F33→#061020。
面板：rgba(13,31,51,0.85)，边框1.5px实线#4A9ECC，内发光box-shadow:inset 0 0 20px rgba(74,158,204,0.2)。
主强调色：冰晶蓝#4A9ECC。文字白#fff，副文字#93C5FD。
按钮：background:linear-gradient(135deg,#1a4a7a,#2563a8)，边框2px #4A9ECC，发光。
雪花粒子：用JS随机生成30个白色圆形，绝对定位，CSS keyframes缓慢下落，循环，size 2~6px，opacity 0.3~0.7。

【布局区域与内容】

▌顶部标题区（top:80px，居中）
- 主标题："冬日生存法则"，font-size:88px，font-weight:900，
  color:透明，background:linear-gradient(180deg,#ffffff,#93C5FD)，
  -webkit-background-clip:text，text-shadow:0 0 40px rgba(74,158,204,0.8)
- 副标题："直播生存挑战"，font-size:36px，color:#7EC8E3，letter-spacing:6px，margin-top:16px
- 标题下方：宽600px水平装饰线，两端渐变透明，中段#4A9ECC，height:1px，margin-top:24px

▌左右装饰（标题两侧各一个雪花SVG，60px，color:#7EC8E3，opacity:0.6）

▌中部等待面板（top:500px，左右各margin:80px，height:480px）
- 磨砂面板：背景rgba(13,31,51,0.85)，border-radius:24px，边框如上
- 面板顶部图标行：居中放置3个小图标+文字（⛄人数图标"在线 1,024人"，❄活跃"活跃 388人"，🏰场次"今日场次 12"），水平排列均匀，每项icon 48px
- 面板中间主提示："等待主播开始游戏"，font-size:52px，color:#fff，居中，
  带呼吸动画(opacity 0.6↔1.0，2s ease-in-out infinite)
- 面板下方提示文字："发送弹幕或送礼物加入战队"，font-size:30px，color:#93C5FD

▌右侧贡献榜浮窗（right:0，top:520px，width:240px，height:520px）
- 面板：背景rgba(6,16,32,0.92)，左边框2px #4A9ECC，border-radius:16px 0 0 16px
- 标题："⚔ 贡献榜"，font-size:28px，color:#7EC8E3，padding:16px，border-bottom:1px solid rgba(74,158,204,0.3)
- 5行条目，每行高78px：
  第1行：金色#F59E0B渐变底条，左侧"👑"，中"凛冬战士"，右"8,888"加金色
  第2行：银色#94A3B8渐变底条，左侧"🥈"，中"极地猎人"，右"6,200"
  第3行：铜色#D97706渐变底条，左侧"🥉"，中"雪原守卫"，右"4,100"
  第4行：普通深蓝底，"4"，"行路者A"，"2,800"
  第5行：普通深蓝底，"5"，"行路者B"，"1,500"
- 底部小字："每3秒更新"，color:#93C5FD，opacity:0.5

▌底部按钮区（bottom:120px，居中）
- 主按钮"开始游戏"：width:520px，height:110px，border-radius:20px，按钮样式如上，
  font-size:52px，font-weight:900，color:#fff，文字发光 text-shadow:0 0 20px #4A9ECC，
  hover时亮度提升+scale:1.03，transition:0.2s
- 按钮下方小字"（主播专属）"，font-size:24px，color:#7EC8E3，opacity:0.6，margin-top:16px

【输出要求】
单文件HTML，所有CSS和JS内联，不使用任何外部依赖，画布1080×1920px居中于黑色页面。
```

---

## ② 加载界面（LoadingUI）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 加载界面（游戏进入时的过渡加载）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【全局样式】
font-family:'PingFang SC','Microsoft YaHei',sans-serif
主背景：深邃暗蓝径向渐变，radial-gradient(ellipse at 50% 40%, #0D2A44 0%, #061020 70%, #020810 100%)
雪粒：JS生成50个白点，随机大小1~4px，从顶部随机x位置加速向下飘落，循环，带水平微摆（用sin）
扫光效果：宽200px白色半透明斜条（opacity:0.06），从左到右扫过进度条，5s循环

【布局内容】

▌全屏中央区域（垂直居中整体，总高约900px）

主标题（距中心上移280px）：
- "冬日生存法则"，font-size:96px，font-weight:900
- 字体渐变：linear-gradient(180deg,#ffffff 0%,#7EC8E3 60%,#4A9ECC 100%)，-webkit-background-clip:text
- 发光：filter:drop-shadow(0 0 30px rgba(74,158,204,0.7))
- 下方副文：font-size:34px，color:#7EC8E3，letter-spacing:8px，"WINTER SURVIVAL"，margin-top:20px

装饰分割（标题下方30px）：
- 宽480px，1px高，两端透明→#4A9ECC→透明的水平线

加载提示文字（分割线下60px）：
- "凛冬将至，做好准备..."，font-size:36px，color:#93C5FD，居中
- 带文字末尾省略号闪烁动画（用JS每500ms切换"..."/".. "/"."）

进度条区域（提示文字下80px）：
- 容器：width:700px，居中，相对定位
- 轨道：height:22px，border-radius:11px，background:rgba(10,20,35,0.8)，
  border:1.5px solid rgba(74,158,204,0.4)，overflow:hidden
- 填充：height:100%，width:72%（示例），border-radius:11px，
  background:linear-gradient(90deg,#1a4a7a,#4A9ECC,#7EC8E3)，
  box-shadow:0 0 16px rgba(74,158,204,0.6)
- 扫光条：绝对定位在填充层上，跟随动画
- 进度数字：进度条右侧外12px，font-size:30px，color:#7EC8E3，"72%"

▌底部小字（bottom:80px，居中）
"© 冬日生存法则 · 抖音直播互动"，font-size:22px，color:#4A9ECC，opacity:0.35

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ③ 游戏主战斗HUD（GameHUD）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 游戏主战斗HUD（游戏运行中的主界面）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景模拟】
游戏场景区（顶部120px资源条下方，到底部180px通知区上方之间的区域）：
用深蓝绿渐变模拟游戏场景 linear-gradient(180deg,#0D2A20 0%,#0A1F30 40%,#061525 100%)，
叠加轻微网格线（background-image:linear-gradient(rgba(74,158,204,0.04) 1px,transparent 1px),linear-gradient(90deg,rgba(74,158,204,0.04) 1px,transparent 1px)，background-size:80px 80px）

【全局样式】font-family:'PingFang SC','Microsoft YaHei',sans-serif

【布局区域与内容】

▌顶部资源条（position:absolute，top:0，left:0，width:1080px，height:120px）
背景：linear-gradient(180deg,rgba(6,16,32,0.97) 0%,rgba(13,31,51,0.90) 100%)
下边缘：border-bottom:1px solid rgba(74,158,204,0.3)
内部水平flex，align-items:center，padding:0 20px，justify-content:space-between

5个资源格（各宽约180px，高100%，flex-column居中）：
资源格通用样式：垂直居中，包含 [图标emoji 44px] + [数值] + [资源名]

格1 食物：🍖图标，数值"1,250"，颜色#F5A623，标签"食物"白色小字
格2 煤炭：⛏图标，数值"340"，颜色#9CA3AF，标签"煤炭"
格3 矿石：💎图标，数值"180"，颜色#94A3B8，标签"矿石"
格4 温度：🌡图标，数值"-12°C"，颜色#EF4444（危险红，加闪烁动画），标签"温度"
格5 城门HP：视觉为HP条而非数值：
  标签"城门"在上，小字"850/1000"，
  下方绿色细HP条（width:120px，height:10px，filled:85%，background:#22C55E，border-radius:5px）

格之间用1px rgba(74,158,204,0.2)竖向分割线

▌右上角计时器（position:absolute，top:20px，right:24px）
已被资源条覆盖区域另做：right:0单独处理——
暂将计时器放在资源条最右格替换为：
  ⏱ 图标 + "08:42" 数值（font-size:40px，font-weight:900，color:#fff）
  + 下方小字"剩余时间"

▌右侧贡献榜浮窗（position:absolute，right:0，top:140px，width:230px，height:460px）
面板：background:rgba(6,16,32,0.88)，border-left:2px solid #4A9ECC，border-radius:16px 0 0 16px
标题栏："⚔ 贡献榜"，font-size:26px，color:#7EC8E3，padding:14px 16px，
        border-bottom:1px solid rgba(74,158,204,0.25)
5行排名（每行80px，flex，padding:0 12px）：
  1名金色皇冠+昵称+贡献分（同①的排行逻辑）
  2/3名银铜，4/5名蓝灰
最底小字"3s刷新"，font-size:20px，opacity:0.4

▌中央场景区域（top:120px ~ bottom:200px）
放置4个模拟Worker角色（用CSS画简单圆形+矩形的stick figure，冰蓝色轮廓）：
  每个Worker：40px圆头 + 60px矩形身体，color:#7EC8E3，opacity:0.7，绝对定位在场景中不同位置
  做简单上下浮动动画（translateY ±8px，2s infinite alternate）
中央稍右放一个城门图（用CSS画：80px宽，100px高，顶部弧形，border 3px #94A3B8，opacity:0.5）

▌底部通知区（position:absolute，bottom:0，left:0，width:1080px，height:200px）
背景：linear-gradient(0deg,rgba(6,16,32,0.97) 0%,rgba(13,31,51,0.85) 100%)
上边缘：border-top:1px solid rgba(74,158,204,0.25)
内部显示一条示例礼物通知横幅（居中，高130px）：
  圆角横幅（border-radius:14px，background:rgba(19,40,64,0.9)，border:1.5px solid #4A9ECC）
  内容："⚡ 极地猎人  送出  能力药丸 ×1  →  召唤守卫！+50食物"
  左侧绿色礼物图标区（60×60px绿色圆），文字颜色：用户名白色，礼物名#7EC8E3，效果#22C55E

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ④ 实时贡献榜（独立面板完整版）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 实时贡献榜（展示完整面板设计，以HUD为背景）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】深色HUD背景：linear-gradient(180deg,#06101e 0%,#0a1f30 100%)，模拟游戏进行中

【核心面板】（position:absolute，right:0，top:140px，width:260px）

面板容器：
background:rgba(4,12,24,0.94)
border-right:none，border:2px solid #4A9ECC，border-right:none
border-radius:20px 0 0 20px
padding-bottom:20px
box-shadow:-6px 0 30px rgba(74,158,204,0.15)

标题栏（height:60px，flex，align-items:center，padding:0 20px）：
"⚔ 贡献榜"，font-size:30px，font-weight:700，color:#7EC8E3
右侧小点动画（3px绿色圆，闪烁代表实时）

分割线：border-bottom:1px solid rgba(74,158,204,0.25)，margin:0 12px

5个排名条目（各高:82px，padding:0 12px，flex，align-items:center，gap:10px）：
条目1（background:linear-gradient(90deg,rgba(245,158,11,0.15),transparent)）：
  左：金色皇冠emoji 28px
  中：flex-column，上"凛冬战士"font-size:24px，下"贡献值 8,888"font-size:20px color:#F59E0B
  右：无（分数在中间下方）

条目2（silver渐变背景）：银色🥈+极地猎人+贡献值6,200（#94A3B8）
条目3（bronze渐变背景）：铜色🥉+雪原守卫+4,100（#D97706）
条目4（普通，background:rgba(13,31,51,0.4)）："4"+行路者A+2,800
条目5（普通）："5"+行路者B+1,500

分割线

底部（padding:12px 16px）：
小字"⏱ 实时更新中"，font-size:20px，color:#7EC8E3，opacity:0.5
绿色小圆点（6px，闪烁动画）

【额外展示】
面板左侧屏幕中央区域放文字说明：
"← 贡献榜浮窗（实际游戏中贴屏幕右边缘显示）"，
font-size:28px，color:#93C5FD，opacity:0.4，writing-mode正常横向

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ⑤ 结算A屏——游戏结果

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 结算A屏（游戏结束第一屏：展示胜利/失败结果）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】
深色半透明遮罩：background:rgba(2,8,20,0.92)，覆盖全屏
顶部和底部：径向渐变冷蓝光晕，radial-gradient在顶部中央（#0D3A5C，50%透明）
粒子效果：JS生成60个粒子，胜利版为金色(#F59E0B)和冰白色，缓慢向上飘散，opacity逐渐减小

【胜利版内容】（做胜利版，失败版只需换色调注释即可）

▌顶部装饰区（top:120px，居中）
左右各一把交叉的剑图标（用CSS或文字符号⚔，font-size:80px，color:#F59E0B，opacity:0.8）

▌主结果区（top:280px，居中）
巨大主标题"度过凛冬！"：
font-size:120px，font-weight:900
background:linear-gradient(180deg,#FFD700,#F59E0B,#D97706)，-webkit-background-clip:text
filter:drop-shadow(0 0 40px rgba(245,158,11,0.8))
进入动画：从scale(0.5)opacity(0)到scale(1)opacity(1)，0.6s ease-out（用CSS animation+keyframes）

英文副标题"WINTER SURVIVED"：
margin-top:20px，font-size:42px，color:#7EC8E3，letter-spacing:10px，opacity:0.8

存活天数（副标题下60px）：
"第 7 天"，font-size:88px，font-weight:900，color:#fff
上方小标签"存活天数"，font-size:28px，color:#93C5FD

▌中部数据面板（top:800px，left:right各100px，height:340px）
磨砂面板：background:rgba(13,31,51,0.85)，border-radius:24px，border:1.5px solid rgba(74,158,204,0.4)
内部三列均分：

列1（食物相关）：
  🍖 大图标(60px)居中
  数值"3,420"，font-size:56px，font-weight:900，color:#F5A623
  标签"总食物收集"，font-size:26px，color:#93C5FD

列2（城门HP）：
  🏰 大图标(60px)
  数值"720/1000"，font-size:46px，color:#22C55E
  标签"城门最终HP"

列3（贡献人数）：
  👥 大图标(60px)
  数值"128"，font-size:56px，color:#fff
  标签"参与勇士"

列之间用1px rgba(74,158,204,0.25)分割

▌参与者文字（数据面板下40px，居中）
"共 128 位勇士守护了要塞"，font-size:34px，color:#93C5FD

▌底部按钮区（bottom:160px，居中，flex-column，gap:24px）
主按钮"查看详情 →"（同①按钮样式，width:560px，height:110px，font-size:46px）
次按钮"返回大厅"（半透明深色，同宽但高度80px，border:1.5px solid #4A9ECC，font-size:36px，color:#7EC8E3）

▌分页指示（bottom:80px，居中）
三个小圆点：● ○ ○，当前第1个为实心亮色#4A9ECC，其余为空心#7EC8E350

【失败版说明（注释标出）】
主色调改暗红冷灰，主标题改"城门陷落"，粒子改为灰白碎冰，英文改"GATE HAS FALLEN"

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ⑥ 结算B屏——详细数据

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 结算B屏（游戏结束第二屏：本局详细数据统计）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】
全屏深色：linear-gradient(180deg,#040C18 0%,#061525 100%)
两侧边缘竖向冰蓝光带：box-shadow inset效果，模拟屏幕边缘冰霜

【布局内容】

▌标题区（top:100px，居中）
"本局数据"，font-size:72px，font-weight:900，color:#fff
下方冰蓝下划线（width:200px，height:3px，background:linear-gradient(90deg,transparent,#4A9ECC,transparent)，margin:20px auto 0）

▌主数据面板（top:260px，left:80px，right:80px，border-radius:28px）
背景：rgba(13,31,51,0.88)，border:1.5px solid rgba(74,158,204,0.35)
padding:50px 60px
内部2列×3行网格（grid-template-columns:1fr 1fr，gap:50px 40px）

6个数据格，每格包含（flex-column，左对齐）：

[左列]
格1 食物采集：
  左侧彩色图标圆（60px，background:#F5A62333，border:2px solid #F5A623，"🍖"居中）
  右侧：上方"食物采集量"font-size:28px color:#93C5FD，下方"3,420"font-size:64px font-weight:900 color:#F5A623

格2 煤炭采集：
  图标圆（灰，⛏），"煤炭采集量"，"1,280"，color:#9CA3AF

格3 矿石采集：
  图标圆（银，💎），"矿石采集量"，"560"，color:#94A3B8

[右列]
格4 抵御波次：
  图标圆（红，⚔），"抵御怪物波次"，"23波"，color:#EF4444

格5 城门最终HP：
  不是数字，而是HP条：上方标签+数值"720/1000"，下方绿色HP条（72%填充）

格6 存活天数：
  图标圆（金，🌟），"存活天数"，"第 7 天"，font-size:60px，color:#F59E0B

每个格子：padding:24px，background:rgba(6,16,32,0.5)，border-radius:16px，border:1px solid rgba(74,158,204,0.15)

▌面板下方引导文字（面板底部下40px，居中）
"💡 数据越高，贡献越大，积分越多"，font-size:28px，color:#93C5FD，opacity:0.7

▌底部按钮区（bottom:160px，居中，flex，gap:30px）
主按钮"查看英雄榜 →"（同前，width:480px，height:100px，font-size:42px）
次按钮"返回大厅"（width:300px，height:80px，透明系，font-size:34px）

▌分页指示（bottom:80px）○ ● ○

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ⑦ 结算C屏——英雄榜/Top3

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 结算C屏（第三屏：Top3英雄榜+MVP）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】
深暗蓝：#030A14全屏
顶部：radial-gradient圆形极光效果（椭圆，rgba(74,158,204,0.12)，centered在顶部）
金色粒子：JS生成40个#F59E0B小粒子，从中央向外扩散漂浮，opacity 0.3~0.7，缓慢移动

【内容布局】

▌顶部标题（top:80px，居中）
"英雄榜"，font-size:80px，font-weight:900
background:linear-gradient(135deg,#FFD700,#F59E0B)，-webkit-background-clip:text
两侧装饰：⚔ ⚔（emoji或SVG，color:#F59E0B，font-size:56px，opacity:0.8）

▌MVP专区（top:220px，居中，height:280px）
背景：宽760px圆角矩形，background:linear-gradient(135deg,rgba(245,158,11,0.2),rgba(217,119,6,0.1))，border:2px solid #F59E0B，border-radius:24px
顶部标签："👑 本局MVP"，font-size:30px，color:#F59E0B，文字居中在标签区
内部flex水平居中：
  圆形头像框（直径120px，border:3px solid #F59E0B，background:radial-gradient(#1a3a5c,#0d1f33)，
  内部放用户头像首字母"凛"，font-size:60px，color:#F59E0B）
  头像右侧：
    昵称"凛冬战士"，font-size:48px，font-weight:700，color:#fff
    贡献"8,888 贡献值"，font-size:32px，color:#F59E0B
    下方小标签"⭐ 本场最高贡献"，font-size:26px，color:#93C5FD
MVP区顶部和底部：各一条1px金色分割线（渐变，两端透明）

▌Top3领奖台（MVP下方80px，居中，宽920px，高380px）
三个台座，水平flex，justify-content:center，align-items:flex-end，gap:20px

台座通用样式：flex-column，align-items:center

第2名（左，flex-order:1）：
  圆形头像框（90px，border:3px solid #94A3B8，背景深蓝，首字母"极"）
  昵称"极地猎人"font-size:30px，color:#fff
  分数"6,200"，color:#94A3B8，font-size:26px
  台座：width:220px，height:200px，background:linear-gradient(180deg,#374151,#1F2937)，
  border-radius:12px 12px 0 0，border-top:3px solid #94A3B8
  台座上显示"🥈 2"，font-size:48px

第1名（中，flex-order:2，最高）：
  皇冠emoji（👑，font-size:56px）在头像上方
  圆形头像框（110px，border:3px solid #F59E0B，首字母"凛"）
  昵称font-size:34px，分数"8,888"，color:#F59E0B，font-size:28px
  台座：width:260px，height:300px，background:linear-gradient(180deg,#78350F,#451A03)，
  border-radius:12px 12px 0 0，border-top:3px solid #F59E0B
  台座上"🥇 1"，font-size:60px

第3名（右，flex-order:3）：
  圆形头像框（80px，border:3px solid #D97706，首字母"雪"）
  "雪原守卫"，"4,100"，color:#D97706
  台座：width:200px，height:160px，background:linear-gradient(180deg,#292524,#1C1917)，
  border-top:3px solid #D97706
  台座上"🥉 3"，font-size:40px

▌4~10名列表（领奖台下50px，left:right各80px）
半透明面板，内部两列×3行条目（共显示4~9名）：
  每条：排名数字+昵称+贡献值，font-size:26px，每行height:60px，带底部分割线
  4/5名一行，6/7名一行，8/9名一行（2列并排）

▌分页指示（bottom:80px）○ ○ ●

▌最底按钮（bottom:140px，居中）
"返回大厅"按钮（width:400px，height:90px，透明系）

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
```

---

## ⑧ 设置面板

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 设置面板（模态弹窗，游戏中调出）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】
游戏运行中的HUD背景（深蓝绿色模拟场景，同③），filter:blur(4px)作为衬底
全屏遮罩：rgba(2,8,20,0.75)覆盖在背景上

【设置面板（居中弹窗）】
position:absolute，top:50%，left:50%，transform:translate(-50%,-50%)
width:860px，height:620px（可根据内容自适应）
background:linear-gradient(160deg,rgba(13,31,51,0.97),rgba(6,16,32,0.97))
border:2px solid #4A9ECC
border-radius:28px
box-shadow:0 0 60px rgba(74,158,204,0.25)，inset 0 0 30px rgba(74,158,204,0.05)
padding:0

▌弹窗标题栏（height:90px，flex，align-items:center，justify-content:space-between，padding:0 40px）
左侧："⚙ 游戏设置"，font-size:46px，font-weight:700，color:#fff
右侧：关闭按钮（×，48px圆形按钮，background:rgba(74,158,204,0.15)，border:1.5px solid #4A9ECC，
      font-size:30px，color:#7EC8E3，hover:background变深+scale:1.1）
标题下方：border-bottom:1px solid rgba(74,158,204,0.3)

▌设置内容区（padding:50px 60px，flex-column，gap:50px）

设置项1 背景音乐：
行布局：flex，align-items:center，gap:24px
  左：🎵 图标圆（56px，background:rgba(74,158,204,0.15)，border-radius:50%，border:1.5px solid #4A9ECC，icon居中font-size:28px，color:#4A9ECC）
  中：flex-column（"背景音乐"font-size:36px color:#fff，"调节背景音乐音量"font-size:24px color:#93C5FD）
  右：自定义滑条（flex-1，max-width:320px）：
    input[type=range]宽100%，高16px
    用CSS美化：appearance:none
    track：background:linear-gradient(90deg,#4A9ECC 70%,rgba(74,158,204,0.2) 70%)，height:8px，border-radius:4px
    thumb：width:28px，height:28px，圆形，background:#4A9ECC，box-shadow
    当前值显示："70%"，font-size:28px，color:#4A9ECC，margin-left:16px
  JS监听input事件实时更新track渐变和百分比显示

设置项2 音效音量：（结构同上，图标改🔊，颜色同系）
  当前值85%

▌底部按钮区（padding-bottom:40px，居中）
"完成"按钮（width:400px，height:90px，主按钮样式，font-size:40px）

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
滑条交互必须工作（拖动滑块时百分比实时更新）。
```

---

## ⑨ 游戏公告弹窗（开始/结束/警报）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 全屏公告弹窗（展示游戏开始、怪物来袭、胜利/失败公告三种变体）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【说明】在一个画布内竖向排列三个缩小版（每个640px高）供对比预览，
用深色分隔条隔开，顶部说明文字标注变体名称

全屏遮罩基础：rgba(2,8,20,0.88)

【变体1：游戏开始（top:0，height:640px）】
背景：深蓝+顶部冰晶蓝光晕
雪粒从上落下（JS，15个，白色）
中央内容（垂直居中）：
  上方装饰线（两端冰晶，中间"❄ ❄"）
  主文字"凛冬开始！"：font-size:100px*（缩小版按0.55比例约55px），font-weight:900，
    background:linear-gradient(180deg,#ffffff,#7EC8E3)，-webkit-background-clip:text，
    filter:drop-shadow(0 0 30px rgba(74,158,204,0.8))
  英文副文"WINTER HAS BEGUN"：font-size:34px*0.55，color:#7EC8E3，letter-spacing:8px
  下方装饰线（镜像）
全屏入场动画：scale(0.8)→scale(1)，opacity(0)→opacity(1)，0.5s

【变体2：怪物来袭（top:680px，height:640px）】
背景：深暗红黑渐变（rgba(40,10,10,0.95)）
红色警报光晕（radial-gradient红光在四角边缘）
中央：
  ⚠ 警告图标（font-size:80px*0.55，color:#EF4444，上下浮动动画）
  "怪物来袭！"：font-size:100px*0.55，background红色渐变，-webkit-background-clip:text
  "MONSTERS INCOMING"：color:#EF4444，letter-spacing
  下方小字："第 3 波  ·  加速建造！"，color:#F87171

【变体3：胜利公告（top:1360px，height:640px）】
背景：深色+金色顶部光晕
金色粒子10个向上飘散
中央：
  "🎉 度过凛冬！"：金色渐变大字（比例同上）
  "WINTER SURVIVED"：color:#F59E0B
  下方："第 7 天 · 全员存活！"，color:#FCD34D

每个变体间有30px深色分隔，左侧竖向小标签说明变体类型

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
三个变体并排展示在同一画布内，各自标注名称。
```

---

## ⑩ VIP大额礼物公告

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · VIP大额礼物全屏公告（T5传说礼物触发）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】
深黑底：#010508
全屏光爆动画：用CSS keyframes实现从中央向外扩散的金色光圈（radial-gradient，由小变大，opacity 0→0.3→0，循环3s）
金色粒子（JS，50个，#F59E0B和#FCD34D，从中央向外随机方向飞散，带拖尾opacity渐变）
冰晶蓝粒子（JS，30个，#7EC8E3，从底部向上慢速飘升）

【内容布局（垂直居中整体，总高约1100px）】

▌顶部皇冠区（居中，margin-bottom:40px）
"👑"emoji，font-size:180px
filter:drop-shadow(0 0 60px rgba(245,158,11,0.9))
上下浮动动画（translateY ±15px，2s ease-in-out infinite）

▌用户信息（皇冠下方，居中）
"至尊贵族  ×××  送出了传说礼物"：
  "至尊贵族"：font-size:34px，color:#F59E0B，opacity:0.9
  "×××"（用户名）：font-size:48px，font-weight:900，color:#fff
  "送出了传说礼物"：font-size:34px，color:#F59E0B，opacity:0.9

▌礼物名称（用户信息下60px，居中）
"神秘空投"：font-size:120px，font-weight:900
background:linear-gradient(180deg,#FFD700 0%,#F59E0B 40%,#D97706 100%)，-webkit-background-clip:text
filter:drop-shadow(0 0 50px rgba(245,158,11,1.0))
下方光效：绝对定位宽400px高80px模糊椭圆（background:#F59E0B，opacity:0.3，filter:blur(40px)，top:100%）

▌分割线（金色渐变，margin:50px auto，width:600px）

▌礼物效果展示（居中，flex-wrap，gap:24px，justify-content:center）
四个效果标签（pill形状，border-radius:50px，padding:16px 36px，border:2px solid对应颜色，font-size:32px）：
"+500 🍖食物"（background:rgba(245,166,35,0.15)，border:#F5A623，color:#F5A623）
"+200 ⛏煤炭"（background:rgba(156,163,175,0.15)，border:#9CA3AF，color:#9CA3AF）
"+100 💎矿石"（background:rgba(148,163,184,0.15)，border:#94A3B8，color:#94A3B8）
"+300 🏰城门HP"（background:rgba(34,197,94,0.15)，border:#22C55E，color:#22C55E）

▌底部小字（效果下方80px，居中）
"感谢至尊贵族的慷慨赞助！"，font-size:36px，color:#FCD34D，opacity:0.9
"英雄榜贡献值 +5,000"，font-size:28px，color:#93C5FD，margin-top:16px

▌底部倒计时（bottom:100px，居中）
"公告将在 5 秒后消失"，font-size:26px，color:#7EC8E3，opacity:0.5
JS实现5→0倒数（每秒更新数字）

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
粒子动画和倒计时JS必须运行。
```

---

## ⑪ 玩家加入通知

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 玩家加入通知（展示通知胶囊在HUD中的效果，连续弹出多条）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】完整游戏HUD背景（同③，直接用深蓝绿渐变+网格模拟场景）
顶部120px资源条（简化版，深色条+资源标签占位）

【核心展示：通知胶囊队列（屏幕底部上方区域）】

底部通知区（position:absolute，bottom:0，left:0，width:1080px，height:260px）：
background:linear-gradient(0deg,rgba(6,16,32,0.96),rgba(6,16,32,0.6),transparent)

通知区内放置3条通知胶囊（模拟连续进入的效果），垂直从底部往上排列：

胶囊通用样式：
  width:680px，height:72px，居中水平（margin:0 auto）
  background:rgba(13,31,51,0.92)
  border:1.5px solid rgba(74,158,204,0.5)
  border-radius:36px
  display:flex，align-items:center，padding:0 24px，gap:16px

胶囊1（最新，opacity:1，bottom:20px）：
  左：⛄ emoji（font-size:32px）
  中：" 凛冬战士 加入了守城队伍"，font-size:28px，color:#fff，
    用户名部分加粗color:#7EC8E3
  右：极淡时间戳"刚刚"，color:#93C5FD，opacity:0.5，font-size:22px

胶囊2（稍旧，opacity:0.75，bottom:108px）：
  ⛄ + "极地猎人 加入了守城队伍" + "1s前"

胶囊3（更旧，opacity:0.45，bottom:196px）：
  ⛄ + "雪原守卫 加入了守城队伍" + "3s前"

JS动画（自动添加新条目）：
  每3秒自动在底部push一条新通知（循环用示例名字数组），
  旧条目向上移动并透明度降低，超过3条后最旧的消失（transition:all 0.4s ease）

屏幕中央放说明标注：
"← 底部：玩家加入通知（自动弹出，3条后最旧消失）"，
font-size:28px，color:#93C5FD，opacity:0.35，text-align:center

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
JS通知弹出动画必须运行。
```

---

## ⑫ 冻结状态横幅

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 冻结/暴风雪状态横幅（在HUD中间显示的全宽警告）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】完整游戏HUD背景（同③），叠加冰冻效果（全屏边缘结冰）

【冰冻边缘效果】
四条边用绝对定位半透明冰蓝渐变层模拟结冰：
  top边：position:absolute，top:0，left:0，width:100%，height:120px，
    background:linear-gradient(180deg,rgba(74,158,204,0.35) 0%,transparent 100%)
  bottom/left/right同理，形成四边冰晶蔓延感

全屏轻微冰蓝蒙版：rgba(100,200,255,0.06)覆盖全屏

【核心横幅（position:absolute，top:48%，left:0，width:1080px，height:100px）】

横幅主体：
background:linear-gradient(90deg,rgba(30,80,130,0.97),rgba(40,100,160,0.95),rgba(30,80,130,0.97))
border-top:2px solid rgba(164,228,255,0.6)
border-bottom:2px solid rgba(164,228,255,0.6)
display:flex，align-items:center，justify-content:center，gap:24px

横幅内容：
  ❄ 图标（font-size:48px，color:#E8F4FF，filter:drop-shadow(0 0 12px #7EC8E3)，
    左右摇摆动画：rotate(-15deg)↔rotate(15deg)，1s infinite alternate ease-in-out）
  主文字"⚠ 暴风雪来袭！工作效率 -50%"：
    "⚠ 暴风雪来袭！"：font-size:40px，font-weight:900，color:#fff，
      "⚠"部分color:#EF4444
    "工作效率 -50%"：font-size:36px，color:#FCA5A5，font-weight:700
  ❄ 图标（右侧对称）
  右端："持续 30s"小标签，font-size:26px，color:#93C5FD，opacity:0.8，margin-left:30px

横幅上下各一条细冰晶线（绝对定位，高2px，用linear-gradient从中央高亮向两端渐透明，color:#A8D8F0）

JS倒计时：横幅内"持续 30s"中的数字从30倒数到0，每秒-1

【冰粒子】
JS生成20个冰蓝色小粒子（#A8D8F0，size 2~5px），从屏幕各处缓慢横向飘移（模拟暴风雪水平风）

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
横幅JS倒计时、冰晶摇摆、水平粒子必须运行。
```

---

## ⑬ 礼物通知横幅（7种分级对照）

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · 7种礼物通知横幅对照（展示所有礼物级别的视觉差异）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】深蓝深色#040C18，标题区域

【顶部说明（top:40px，居中）】
"礼物通知横幅 · 7种设计"，font-size:40px，color:#7EC8E3，font-weight:700
"从上到下：T1普通 → T5传说"，font-size:26px，color:#93C5FD，opacity:0.7，margin-top:8px

【7条横幅（top:160px开始，每条间距16px，水平居中，宽980px）】

横幅通用结构（flex，height可变，border-radius:14px，overflow:hidden）：
  左区（width:130px，height:100%，flex居中）：礼物图标占位（彩色圆形emoji区域）
  中区（flex:1，padding:0 20px，flex-column，justify-content:center）：
    上行：用户名 + "送出了" + 礼物名
    下行：效果描述
  右区（width:140px，等级标签+数值）

━━━━━━━━━━━━━━━━━━━━

横幅1 T1仙女棒（普通级，height:110px）：
background:rgba(13,31,51,0.88)，border:1.5px solid rgba(74,158,204,0.5)
左区背景：rgba(74,158,204,0.1)，图标"✨"font-size:48px
上行："极地猎人  送出了  仙女棒 ×1"（用户名color:#7EC8E3，礼物名color:#4A9ECC）
下行："效率小幅提升"，font-size:26px，color:#93C5FD
右区："T1"标签（background:rgba(74,158,204,0.2)，color:#4A9ECC，border-radius:8px，padding:4px 12px），下方"0.1抖币"，font-size:22px，color:#7EC8E3，opacity:0.6

横幅2 T2能力药丸（稀有级，height:120px）：
background:rgba(13,31,45,0.9)，border:2px solid rgba(34,197,94,0.6)
box-shadow:0 0 16px rgba(34,197,94,0.15)
左区：图标"💊"，左区背景rgba(34,197,94,0.1)
上行：礼物名color:#22C55E
下行："召唤守卫！+50食物"，color:#4ADE80，font-size:26px
右区："T2"（background:rgba(34,197,94,0.2)，color:#22C55E），"10抖币"

横幅3 T2魔法镜（稀有-捣乱，height:120px）：
background:rgba(20,13,40,0.9)，border:2px solid rgba(139,92,246,0.6)
box-shadow:0 0 16px rgba(139,92,246,0.15)
左区：图标"🪞"，背景rgba(139,92,246,0.1)
上行：礼物名color:#A78BFA
下行："⚠ 捣乱效果：降低效率"，color:#C4B5FD，font-size:26px
右区："T2"（purple系），"10抖币"

横幅4 T3甜甜圈（精英级，height:130px）：
background:rgba(20,18,10,0.92)，border:2px solid rgba(245,158,11,0.65)
box-shadow:0 0 20px rgba(245,158,11,0.2)
左区：图标"🍩"，背景rgba(245,158,11,0.12)
下行："+100食物  +200城门HP"，color:#FCD34D
右区："T3"（amber系），"52抖币"

横幅5 T3超能喷射（精英级，height:130px）：
background:rgba(10,18,25,0.92)，border:2px solid rgba(56,189,248,0.7)
box-shadow:0 0 20px rgba(56,189,248,0.2)，有轻微扫光动画
左区：图标"⚡"，背景electric blue
下行："工作效率 ×2"，color:#38BDF8
右区："T3"（电蓝系），"52抖币"

横幅6 T4能量电池（史诗级，height:145px）：
background:linear-gradient(90deg,rgba(46,16,101,0.95),rgba(20,13,51,0.95))
border:2px solid rgba(168,85,247,0.8)
box-shadow:0 0 30px rgba(168,85,247,0.3)，内发光inset
左区：图标"🔋"，紫色背景
下行："+30°C 热量值"，color:#E879F9
右区："T4"（紫金系），"99抖币"，带小粒子装饰

横幅7 T5神秘空投（传说级，height:165px）：
background:linear-gradient(90deg,rgba(40,20,5,0.97),rgba(20,10,2,0.97))
border:2.5px solid #F59E0B
box-shadow:0 0 40px rgba(245,158,11,0.5)，外发光强烈
顶部和底部有金色粒子装饰（用CSS background-image dots动画或JS）
左区：图标"📦"，金色背景，border-right:2px solid rgba(245,158,11,0.3)
用户名和礼物名都更大（font-size加4px）
下行："+500食物  +200煤炭  +100矿石  +300HP"，color:#FCD34D，分4个小标签
右区："T5"（gold系，带发光），"520抖币"，下方"传说"徽章

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
T4/T5的粒子或动效需要运行。
```

---

## ⑭ GM调试控制栏

```
请生成一个完整的单文件 HTML+CSS+JS 游戏UI界面原型，不依赖任何外部库和CDN。

【界面】冬日生存法则 · GM调试控制栏（开发/主播调试面板）

【画布规格】固定宽1080px × 高1920px，居中显示在黑色body内，overflow:hidden

【背景】游戏HUD背景（深蓝绿，同③）

【调试面板（position:absolute，bottom:0，left:0，width:1080px，height:280px）】
背景：rgba(2,6,14,0.97)
border-top:2px solid rgba(255,140,0,0.6)（橙色表示调试模式，区别于正常UI的蓝色）
padding:20px 30px

▌顶部标识行（flex，align-items:center，gap:16px，margin-bottom:16px）
左侧："[DEV] 调试面板"，font-size:26px，color:#FB923C，font-weight:700，font-family:monospace
右侧状态指示：绿色圆点（8px，background:#22C55E，闪烁动画）+"服务器已连接"，font-size:22px，color:#4ADE80
最右：当前状态标签"状态：running"，background:rgba(34,197,94,0.15)，border:1px solid #22C55E，border-radius:6px，padding:4px 14px，font-size:22px，color:#22C55E

▌按钮组第一行（flex，gap:16px，flex-wrap:wrap）

按钮通用样式：height:60px，border-radius:10px，font-size:26px，font-weight:700，cursor:pointer，font-family:'PingFang SC','Microsoft YaHei',sans-serif，border:none，transition:0.15s

"▶ 开始游戏"（background:#166534，color:#4ADE80，hover:background:#15803D）width:200px
"⏸ 暂停游戏"（background:#713F12，color:#FCD34D）width:200px
"⏹ 结束游戏"（background:#7F1D1D，color:#FCA5A5）width:200px
"🔄 重置"（background:#1E3A5F，color:#7EC8E3）width:160px

▌按钮组第二行（flex，gap:16px，flex-wrap:wrap，margin-top:14px）
"🎁 模拟T1礼物"（background:#1E3A5F，color:#93C5FD，width:220px）
"🎁 模拟T3礼物"（background:#1E3A5F，color:#FCD34D，width:220px）
"🎁 模拟T5礼物"（background:#451A03，color:#F59E0B，width:220px）
"❄ 触发冻结"（background:#1E3A5F，color:#7EC8E3，width:200px）
"💥 召唤怪物"（background:#450A0A，color:#FCA5A5，width:200px）

▌底部状态行（flex，gap:24px，margin-top:14px）
"橘子位置：0.00"，"当前推力：0"，"在线人数：128"，"服务器延迟：23ms"
每项：font-size:22px，font-family:monospace，color:#93C5FD

JS：按钮hover时有简单高亮效果，click时在console.log输出模拟操作（不需要真实功能）

画布其余区域：游戏HUD场景模拟（同③）

【输出要求】单文件HTML，所有CSS和JS内联，无外部依赖，画布1080×1920居中黑色页面。
按钮hover效果必须工作。
```

---

## 附：生成顺序建议

| 优先 | 界面 | 原因 |
|------|------|------|
| 第1批 | ③主HUD + ①空闲大厅 | 最高频展示，基调确认 |
| 第2批 | ⑤⑥⑦结算三屏 | 玩家流失关键节点 |
| 第3批 | ⑬礼物横幅7种 + ⑩VIP公告 | 直播互动核心反馈 |
| 第4批 | ②加载 + ⑧设置 + ⑨公告 | 辅助功能界面 |
| 第5批 | ④⑪⑫⑭ | 浮窗/通知/调试 |
