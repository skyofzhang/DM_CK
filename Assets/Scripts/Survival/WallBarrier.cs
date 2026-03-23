using UnityEngine;

namespace DrscfZ.Survival
{
    /// <summary>
    /// 城墙屏障系统：阻止 Worker/Monster 穿越城墙，只能通过城门缺口通行。
    /// 使用简单的 Z 轴防线检测，不依赖 Unity 物理系统。
    ///
    /// 城墙布局（俯视图）：
    ///   左墙 ████████ [城门缺口] ████████ 右墙
    ///                    ↑ gateMinX ~ gateMaxX
    ///   Z = wallZ（防线位置）
    /// </summary>
    public static class WallBarrier
    {
        // ===== 城墙参数（根据场景 chengqiang 模型位置配置）=====

        /// <summary>城墙防线的 Z 坐标（从场景中 chengqiang-chengmen 的 bounds 推算）</summary>
        private static float _wallZ = -7.5f;

        /// <summary>城门缺口 X 范围（允许通行的区间）</summary>
        private static float _gateMinX = -2.5f;
        private static float _gateMaxX = 4.5f;

        /// <summary>墙壁厚度（防止高速移动穿透）</summary>
        private static float _wallThickness = 1.5f;

        /// <summary>是否已初始化（从场景读取实际参数）</summary>
        private static bool _initialized = false;

        /// <summary>
        /// 从场景中的城墙模型自动推算参数（可选，在游戏开始时调用）
        /// </summary>
        public static void Initialize(float wallZ, float gateMinX, float gateMaxX, float thickness = 1.5f)
        {
            _wallZ = wallZ;
            _gateMinX = gateMinX;
            _gateMaxX = gateMaxX;
            _wallThickness = thickness;
            _initialized = true;
            Debug.Log($"[WallBarrier] 初始化: wallZ={wallZ}, gate X=[{gateMinX}, {gateMaxX}], thickness={thickness}");
        }

        /// <summary>
        /// 检查从 currentPos 移动到 targetPos 是否会穿墙。
        /// 如果会穿墙，返回修正后的位置（贴墙停下）；否则返回 targetPos。
        /// </summary>
        public static Vector3 ClampMovement(Vector3 currentPos, Vector3 targetPos)
        {
            // 判断是否跨越了墙壁防线
            float wallZMin = _wallZ - _wallThickness * 0.5f;
            float wallZMax = _wallZ + _wallThickness * 0.5f;

            // 情况1：两点都在墙壁同侧 → 不受影响
            bool currentInside = currentPos.z < wallZMin;  // "inside" = 城门内侧（Z较小）
            bool targetInside  = targetPos.z < wallZMin;
            bool currentOutside = currentPos.z > wallZMax;  // "outside" = 城门外侧（Z较大）
            bool targetOutside  = targetPos.z > wallZMax;

            // 不穿越墙壁区域 → 放行
            if ((currentInside && targetInside) || (currentOutside && targetOutside))
                return targetPos;

            // 已经在墙壁厚度内 → 检查是否在城门缺口内
            // 情况2：在城门缺口范围内 → 允许通行
            float checkX = targetPos.x;
            if (checkX >= _gateMinX && checkX <= _gateMaxX)
                return targetPos;

            // 情况3：不在城门缺口内 → 阻止穿越，贴墙停下
            Vector3 clamped = targetPos;
            if (currentInside || (!currentOutside && targetPos.z > currentPos.z))
            {
                // 从内侧往外走 → 贴在墙内侧
                clamped.z = Mathf.Min(clamped.z, wallZMin);
            }
            else
            {
                // 从外侧往内走 → 贴在墙外侧
                clamped.z = Mathf.Max(clamped.z, wallZMax);
            }

            return clamped;
        }

        /// <summary>
        /// 快速检查某个位置是否在城门缺口内
        /// </summary>
        public static bool IsInGateOpening(Vector3 pos)
        {
            return pos.x >= _gateMinX && pos.x <= _gateMaxX;
        }

        /// <summary>
        /// 获取城门中心位置（用于路径规划绕行点）
        /// </summary>
        public static Vector3 GetGateCenter()
        {
            return new Vector3((_gateMinX + _gateMaxX) * 0.5f, 0f, _wallZ);
        }

        /// <summary>
        /// 获取绕行路径点：如果目标在墙另一侧且不在城门口，
        /// 先走到城门口，再穿过去。返回中间路径点，null 表示不需要绕行。
        /// </summary>
        public static Vector3? GetDetourPoint(Vector3 currentPos, Vector3 targetPos)
        {
            float wallZMin = _wallZ - _wallThickness * 0.5f;
            float wallZMax = _wallZ + _wallThickness * 0.5f;

            bool currentInside = currentPos.z < wallZMin;
            bool targetOutside = targetPos.z > wallZMax;
            bool currentOutside = currentPos.z > wallZMax;
            bool targetInside = targetPos.z < wallZMin;

            // 需要穿越墙壁
            bool needsCross = (currentInside && targetOutside) || (currentOutside && targetInside);
            if (!needsCross) return null;

            // 已经在城门缺口范围 → 不需要绕行
            if (currentPos.x >= _gateMinX && currentPos.x <= _gateMaxX) return null;

            // 返回城门口中心作为中间点
            return GetGateCenter();
        }
    }
}
