# M08: 技术底座

## 赚钱逻辑
技术不直接赚钱，但技术崩了就一分钱赚不到。稳定性=收入保障。

## 依赖模块
无（底层模块）

## 技术栈

| 层级 | 技术 | 说明 |
|------|------|------|
| 游戏引擎 | Unity 2022.3.47f1c1 | 2D项目, Spine角色 |
| 服务端 | Node.js + Express + ws | WebSocket实时通信 |
| 平台对接 | LiveOpenSDK (HTTPWithSDK) | 抖音主平台 |
| 部署 | PM2 + Nginx | 进程管理+反向代理 |
| 数据存储 | JSON文件(初期) → MongoDB(后期) | 玩家数据持久化 |

## 服务端架构（复用DM_kpbl）

### 核心模块
```
server/
├── app.js                 # 主入口, Express+WebSocket
├── DouyinAPI.js          # 抖音平台对接(推送/签名/ACK)
├── PlatformAPI.js        # 多平台适配层(新增)
├── Room.js               # 房间逻辑(游戏状态机)
├── RoomManager.js        # 房间管理(创建/销毁/查找)
├── PlayerManager.js      # 玩家数据管理
├── BattleEngine.js       # 战斗计算引擎(新增)
├── GrowthManager.js      # 成长系统管理(新增)
├── ConfigLoader.js       # 配表加载(59张JSON)
├── DataStore.js          # 数据持久化
└── utils/
    ├── signature.js       # MD5→Base64签名
    └── bigint.js          # roomId大数处理
```

### 游戏状态机
```
idle → waiting_players → battle_start → wave_1 → wave_2 → wave_3(boss)
  → battle_end → reward_settlement → next_level / game_over

PVP分支:
  pvp_matching → pvp_countdown → pvp_battle → pvp_settlement

世界Boss分支:
  boss_waiting → boss_battle(5min) → boss_settlement
```

### Tick循环
- 频率: 200ms/tick (5 ticks/秒)
- 每tick: 战斗计算 → 状态更新 → 广播客户端
- 战斗计算: 自动攻击/技能CD/伤害/死亡/复活
- 状态广播: 仅差量(delta)，非全量

## 抖音平台对接

### 推荐模式: HTTPWithSDK（双推）
- HTTP POST回调接收弹幕/礼物/关注
- LiveOpenSDK WebSocket接收实时数据
- 两路数据去重(msg_id)
- 参考: `D:\claude\DM_kpbl\docs\douyin_integration_guide.md`（1289行完整指南）

### 关键踩坑记录
1. **roomId大数精度**: JavaScript Number最大2^53, 抖音roomId超此范围 → 用String处理
2. **sec_gift_id匹配**: 礼物匹配用sec_gift_id(非gift_value), 因为不同平台同币值礼物不同
3. **签名**: MD5→Base64编码, 注意字段排序
4. **header路由fallback**: 抖音可能在header中传room_id而非body
5. **必须返回err_no:0**: 即使处理失败也要返回成功，否则平台会熔断
6. **ACK**: 收到推送先200响应，再异步处理业务逻辑

## 多平台支持

PlatformAPI.js 抽象层:
```
PlatformAPI
├── DouyinAdapter    (PlatformId=1)
├── WeixinAdapter    (PlatformId=2, 视频号)
├── KuaishouAdapter  (PlatformId=3)
├── BilibiliAdapter  (PlatformId=5)
└── YYAdapter        (PlatformId=9)
```

每个Adapter实现:
- parseGiftEvent(raw) → { userId, giftId(CGiftId), count, platform }
- parseBarrageEvent(raw) → { userId, content, platform }
- sendAck(response) → 平台特定格式

## 房间生命周期

```
创建房间 → 主播开播触发
  ↓
等待玩家 → 弹幕"加入" (≤20人)
  ↓
游戏进行 → 自动战斗/关卡推进
  ↓
活动触发 → 世界Boss/PVP (按时间表)
  ↓
主播下播 → 保存所有玩家数据 → 销毁房间
```

限制:
- 最大玩家: 20人/房间
- 观众上限: 30人(ViewerLimit)
- AFK超时: 5分钟自动踢出
- 房间存活: 主播下播后保留5分钟

## 数据持久化

### 玩家数据结构
```json
{
  "userId": "openId_xxx",
  "platform": 1,
  "role": { "level": 150, "exp": 234567, "realm": "金丹" },
  "equipment": [{ "slot": 1, "qualityId": 805, "starLv": 15, "enchantLv": 5 }, ...],
  "gems": [{ "type": 1, "level": 5 }, ...],
  "pets": [{ "id": 501, "level": 30, "strengthenLv": 3 }],
  "wing": { "level": 5 },
  "steed": { "level": 4 },
  "avatars": [611, 612, 615],
  "currentAvatar": 615,
  "vipLevel": 3,
  "vipExp": 1280,
  "contribution": 45000,
  "pvpScore": 2500,
  "towerFloor": 120,
  "shopDaily": { "date": "2026-02-24", "items": { "20001": 3 } },
  "luckyDrawCount": 150,
  "luckyValue": 5,
  "dailyReward": { "date": "2026-02-24", "claimed": true },
  "createdAt": "2026-02-01T10:00:00Z",
  "lastLogin": "2026-02-24T15:30:00Z"
}
```

### 存储策略
- 初期: JSON文件 → /opt/nxxhnm/data/players/{platform}_{userId}.json
- 后期: MongoDB (当玩家超过1000时迁移)
- 自动保存: 每5分钟 + 关键事件(升级/获得装备)
- 备份: 每日凌晨自动备份到 /opt/nxxhnm/backup/

## WebSocket协议

### 客户端→服务端
| 类型 | 说明 |
|------|------|
| join | 弹幕"加入"触发 |
| query_role | 弹幕"查角色" |
| query_bag | 弹幕"查背包" |
| query_pet | 弹幕"查战宠" |
| open_box | 弹幕"开箱子" |
| gift_event | 平台礼物回调转发 |

### 服务端→客户端
| 类型 | 频率 | 说明 |
|------|------|------|
| battle_state | 200ms | 战斗实时状态(HP/位置/技能) |
| player_joined | 事件触发 | 新玩家加入 |
| player_left | 事件触发 | 玩家离开 |
| level_clear | 事件触发 | 关卡通关+奖励 |
| level_up | 事件触发 | 等级提升+境界突破 |
| gift_effect | 事件触发 | 礼物效果展示 |
| pvp_match | 事件触发 | PVP匹配结果 |
| boss_spawn | 事件触发 | 世界Boss出现 |
| daily_schedule | 定时 | 每日活动提醒 |

## 部署

- **服务器**: 123.206.122.216
- **SSH**: root / Ouxuanze@qq#$@
- **端口**: 8088
- **部署流程**:
  1. `scp -r server/ root@123.206.122.216:/opt/nxxhnm/src/`
  2. `ssh root@123.206.122.216 "cd /opt/nxxhnm && pm2 restart nxxhnm-server"`
- **Nginx**: 反向代理 + HTTPS + WSS
- **PM2**: `pm2 start src/app.js --name nxxhnm-server`

## 验收清单
- [ ] 服务器启动无报错
- [ ] WebSocket连接成功
- [ ] 弹幕消息正确解析
- [ ] 礼物映射(SGiftId→CGiftId)正确
- [ ] 200ms tick稳定（无明显延迟）
- [ ] 玩家数据保存/加载正确
- [ ] 房间创建/销毁流程正常
- [ ] PM2守护进程自动重启
