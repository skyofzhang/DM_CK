# PeaceNightOverlay UI 切图回流规范

> 对应 prefab：`Assets/Prefabs/UI/Panels/PeaceNightOverlay.prefab`  
> 参考原型：`ui_layout_prototype_PeaceNightOverlay.png` / `ui_layout_prototype_PeaceNightOverlay.svg`  
> 坐标基准：1080 x 1920，文档坐标为左上角 x/y，Unity 回流时按 RectTransform 中心点换算。  
> 建议导入目录：`Assets/Art/UI/Generated/PeaceNightOverlay/`

## 1. 回流原则

1. 不建议把 AI 效果图整屏一张图塞回 prefab；必须按下面表格切成面板底板、按钮底板、图标、装饰条等可复用 Sprite。
2. 动态内容继续由 prefab 中的 TMP、Button、Slider、Image 组件和脚本字段驱动；昵称、数值、状态、倒计时、排名、按钮文字不要烘焙进切图。
3. 切图必须去掉原型里的灰盒、虚线、坐标、节点名、prefab 名、施工说明；最终 Sprite 只保留干净美术。
4. 大面板、横幅、按钮底板优先做九宫格，避免回到 prefab 后拉伸变形；小图标和徽章按原尺寸透明 PNG 回填。
5. RectTransform 回流以本文件表格为准；如果 Unity 里已有同名节点，优先替换该节点上的 Image.sprite，不新增平行重复节点。

## 2. Unity 导入设置

| 项 | 建议 |
| --- | --- |
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single；图集批处理时再改 Multiple |
| Mesh Type | Full Rect |
| Pixels Per Unit | 100，除非项目 UI 图集已有统一 PPU |
| sRGB | 开启 |
| Alpha Is Transparency | 开启 |
| Mip Maps | 关闭 |
| Compression | None 或 High Quality，文字底板和半透明边缘优先 None |
| Filter Mode | Bilinear |
| Wrap Mode | Clamp |
| 九宫格 Border | 小按钮 12-18px；普通面板 18-28px；大弹窗/横幅 24-40px，按圆角与描边厚度微调 |

## 3. 切图回流表

| 模块 | Prefab/节点 | 原型 x,y,w,h | 建议切图文件 | 回流方式 | RectTransform 回填 | 文案处理 |
| --- | --- | --- | --- | --- | --- | --- |
| 和平夜氛围 | `OverlayImage` | 0, 0, 1080, 1920 | `ui_PeaceNightOverlay__01_OverlayImage.png` | 切透明 PNG，建议九宫格 Sprite，Image Type=Sliced | Center，pivot=(0.5,0.5)，sizeDelta=(1080,1920)，anchoredPosition=(0,0) | 静态装饰可烘焙；动态数字/昵称/状态不要烘焙 |
| 游戏安全区 | `SceneSafeArea` | 70, 260, 940, 1150 | `-` | 不单独切图；作为游戏画面/安全区参考 | Center，pivot=(0.5,0.5)，sizeDelta=(940,1150)，anchoredPosition=(0,125) | 无文案切图 |
| 提示容器 | `Content` | 150, 1320, 780, 190 | `ui_PeaceNightOverlay__03_Content_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | BottomCenter，pivot=(0.5,0.5)，sizeDelta=(780,190)，anchoredPosition=(0,-455) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 提示文案 | `HintText` | 200, 1360, 680, 58 | `ui_PeaceNightOverlay__04_HintText_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | BottomCenter，pivot=(0.5,0.5)，sizeDelta=(680,58)，anchoredPosition=(0,-429) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |
| 倒计时 | `CountdownText` | 320, 1440, 440, 54 | `ui_PeaceNightOverlay__05_CountdownText_bg.png` | 保留 TMP/按钮文字动态层；只切底板、图标或装饰 | BottomCenter，pivot=(0.5,0.5)，sizeDelta=(440,54)，anchoredPosition=(0,-507) | 文字不烘焙进切图，继续由 TMP/脚本字段驱动 |

## 4. 回流步骤

1. 按 `ai_prompt.md` 和原型 PNG 生成效果图后，先拆出不含动态文字的透明 PNG 切片。
2. 将切片放入 `Assets/Art/UI/Generated/PeaceNightOverlay/`，按本文件的建议文件名命名。
3. 在 Unity Import Settings 中套用上面的 Sprite 设置；需要拉伸的底板进入 Sprite Editor 设置 Border。
4. 打开 `Assets/Prefabs/UI/Panels/PeaceNightOverlay.prefab`，按“Prefab/节点”列定位节点，优先替换现有 Image.sprite / RawImage.texture。
5. 按“RectTransform 回填”列核对 sizeDelta 与 anchoredPosition；运行时文本仍绑定原 TMP 节点。
6. 回放对应界面状态，检查 1080x1920、窄屏等比缩放、动态文字溢出、九宫格边角和按钮点击区域。

## 5. 验收清单

- [ ] 切图没有灰盒、虚线、坐标、prefab 名、节点名、施工标注。
- [ ] 动态 TMP 文案、数字、昵称、倒计时没有被烘焙进背景图。
- [ ] 每个 Sprite 都能追到本文件表格中的 prefab 节点或明确标注为安全区参考。
- [ ] 大面板/横幅/按钮底板已设置九宫格 Border，拉伸后圆角和描边不变形。
- [ ] 回填后 RectTransform 与 layout 坐标一致，画布仍按 1080x1920 竖屏设计。
- [ ] prefab 原有脚本字段、按钮事件、显隐逻辑没有因为换图被断开。

## 6. 第十轮节点命中与回填策略

> 本节由第十轮反向审计补充：用于说明“Prefab/节点”列在真实 prefab 中的命中方式。  
> 回流优先级：先替换现有节点上的 Image.sprite / RawImage.texture；其次定位嵌套子 prefab；再次按脚本字段找 Inspector 绑定对象；只有“施工目标需创建/改名”的行才允许新增 Image 容器。  
> 新增容器时必须保留原 prefab 的 TMP、Button、脚本字段、按钮事件和显隐逻辑，不要为了贴图方便删除或重建绑定节点。

| 模块 | Prefab/节点 | 命中状态 | 命中目标 | 回填策略 |
| --- | --- | --- | --- | --- |
| 和平夜氛围 | `OverlayImage` | 现有节点命中 | `OverlayImage` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 游戏安全区 | `SceneSafeArea` | 安全区/表现参考 | - | 不作为必须回填的 Sprite 节点；用于限定画面留白、世界锚点、动效路径或 gameplay 安全区。 |
| 提示容器 | `Content` | 现有节点命中 | `Content` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 提示文案 | `HintText` | 现有节点命中 | `HintText` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
| 倒计时 | `CountdownText` | 现有节点命中 | `CountdownText` | 在对应现有节点上替换 Image.sprite / RawImage.texture；保留其子级 TMP、Button、脚本引用和显隐逻辑。 |
