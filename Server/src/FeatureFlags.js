/**
 * FeatureFlags - 功能开关集中管理
 *
 * §36.5.1 每日闯关上限（daily fortress day cap）
 *
 * 设计动机：
 * - 提供**灰度开关**以支持线上渐进式上线与快速回滚
 * - 同时被 SurvivalGameEngine 与（未来可能的）RoomManager / 测试用例共用
 *
 * 使用方式：
 *   const FeatureFlags = require('./FeatureFlags');
 *   if (FeatureFlags.ENABLE_DAILY_CAP) { ... }
 *
 * ⚠️ 所有 flag 默认"正常业务值"（启用时取 true，禁用时取 false）
 *    回滚操作请在此文件修改后重启 pm2 进程（drscfz-server）
 */

module.exports = {
  /**
   * §36.5.1 每日闯关上限
   *   true  — 启用 150/dayKey 上限 + cap_blocked / cap_reset 协议推送
   *   false — 全量透传 _onRoomSuccess，不检查 cap；协议字段仍保留默认值
   */
  ENABLE_DAILY_CAP: true,
};
