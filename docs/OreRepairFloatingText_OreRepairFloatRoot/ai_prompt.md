# OreRepairFloatingText_OreRepairFloatRoot AI 效果图提示词

## 使用方式 / 注意事项

把 `ui_layout_prototype_OreRepairFloatingText_OreRepairFloatRoot.png` 作为布局参考图一起输入。参考图只表示模块位置、尺寸和层级；灰盒、虚线、坐标、节点名、prefab 名、施工说明都不要出现在最终效果图里。

## 1. 主提示词

生成一张 1080x1920 竖屏手机游戏 UI 效果图，主题是“极地生存法则”的直播互动生存游戏。当前界面是 `OreRepairFloatingText_OreRepairFloatRoot`，类型为 矿石修复世界飘字。画面要直接呈现真实可用 UI，不要做宣传海报或落地页。整体风格为冷冽极地、深色半透明玻璃面板、冰蓝描边、清晰中文信息层级；金色用于贡献、奖励、VIP 或高价值状态，红橙用于危机/警告/危险操作，绿色用于修复或完成。

界面目的：检测 resource_update 中 gateHp 增加且 ore 减少的自动修复行为，并在城门上方显示“矿石修复城门 +NHP”。

运行时关系：Assets/Scripts/UI/OreRepairFloatingText.cs：监听 SurvivalGameManager.OnResourceUpdate；gateHpDelta > 0 且 oreDelta < 0 时显示飘字；复用 DamageNumber.Show，颜色为 new Color(1f, 0.85f, 0.3f)

场景表现：城门上方出现“矿石修复城门 +7HP”之类暖黄飘字，随后上浮消失。

## 2. 反向提示词

不要出现英文占位词、Lorem ipsum、坐标数字、prefab 文件名、Unity 节点名、灰盒标签、虚线框、施工说明、设计注释、水印、无意义图标、过度装饰的卡片堆叠、模糊不可读文字、按钮文字溢出、遮挡核心战斗主体的大面积弹窗。不要把界面画成营销首页，不要使用纯渐变背景替代真实游戏/界面状态。

## 3. 布局硬约束

- 城门世界投影区：接近 x=360 y=730 w=360 h=120，对应 CityGateWorldAnchor。
- 飘字内容：接近 x=300 y=650 w=480 h=76，对应 DamageNumber.Show。
- 上浮淡出轨迹：接近 x=390 y=520 w=300 h=240，对应 TweenPath。
- 无常驻 UI 区：接近 x=160 y=980 w=760 h=160，对应 NoPersistentPanel。

## 4. 美术风格细化

暖黄色资源反馈飘字，1.2 秒上浮淡出，和红色伤害数字、绿色治疗数字区分。

UI 需要有真实游戏产品感：面板边缘克制、圆角不超过 8px，重要按钮和状态图标有清楚视觉反馈。背景可表现冰雪基地、夜间怪物攻城或大厅场景，但必须服从布局，不能抢走 UI 信息。

## 5. 可见文案建议

- 使用运行时动态文案，但必须预留中文、数字和昵称宽度。

## 6. 简短版提示词

1080x1920 竖屏手机游戏 UI，极地生存直播互动风格，界面为 OreRepairFloatingText_OreRepairFloatRoot（矿石修复世界飘字）。按参考原型保持模块位置：城门世界投影区在x360 y730 w360 h120；飘字内容在x300 y650 w480 h76；上浮淡出轨迹在x390 y520 w300 h240；无常驻 UI 区在x160 y980 w760 h160。深色半透明冰蓝 UI，金色奖励，红橙警告，中文可读，不要出现施工标注、prefab 名、坐标或灰盒标签。