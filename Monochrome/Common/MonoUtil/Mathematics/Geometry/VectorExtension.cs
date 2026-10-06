using Monochrome.Common.MonoUtil.Mathematics.Geometry;

namespace Monochrome.Common.MonoUtil
{
    public static partial class MonoUtil
    {
        #region 矢量扩展
        /// <summary>
        /// 矢量线性插值
        /// </summary>
        public static Vector2 VecLerp(Vector2 start, Vector2 end, float progress) => new(MathHelper.Lerp(start.X, end.X, progress), MathHelper.Lerp(start.Y, end.Y, progress));
        /// <summary>
        /// 矢量贝塞尔曲线插值；次数为填入矢量的数量+1
        /// </summary>
        public static Vector2 BezierLerp(float amout, params Vector2[] veclist)
        {
            int n = veclist.Length - 1;
            Vector2 result = Vector2.Zero;
            for (int i = 0; i <= n; i++)
            {
                result += (float)(Combinatorial(n, i) * Math.Pow(1 - amout, n - i) * Math.Pow(amout, i)) * veclist[i];
            }
            return result;
        }
        /*
        extension(Vector2 vec)
        {
            /// <summary>
            /// 单位X矢量的简写
            /// </summary>
            public static Vector2 UX => Vector2.UnitX;
            /// <summary>
            /// 单位Y矢量的简写
            /// </summary>
            public static Vector2 UY => Vector2.UnitY;
        }
        */
        /// <summary>
        /// 计算两二维矢量夹角
        /// </summary>
        public static float AngleDifference(this Vector2 vec, Vector2 end) => MathHelper.WrapAngle(end.ToRotation() - vec.ToRotation());
        /// <summary>
        /// 获取以axis为对称轴方向的对称矢量（只翻转角度，大小不变）
        /// </summary>
        /// <param name="vec">本矢量</param>
        /// <param name="axis">对称轴向量</param>
        /// <returns></returns>
        public static Vector2 Axisymmetry(this Vector2 vec, Vector2 axis) => vec.RotatedBy(2 * vec.AngleDifference(axis));
        /// <summary>
        /// 获取vec以某条线为对称轴的对称点，矢量大小可能发生生变化
        /// </summary>
        /// <param name="vec">本点</param>
        /// <param name="line1">线上点1</param>
        /// <param name="line2">线上点2</param>
        /// <returns></returns>
        public static Vector2 Axisymmetry(this Vector2 vec, Vector2 line1, Vector2 line2) => (vec - line2).Axisymmetry(line1 - line2) + line2;
        /// <summary>
        /// 将矢量的长度约束在（min，max）范围内
        /// </summary>
        public static Vector2 ClampLength(this Vector2 vec, float min, float max) => vec.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(vec.Length(), min, max);
        /*
        /// <summary>
        /// 若vec1的两个分量同时大于vec2的两个分量，则true，否者false
        /// </summary>
        public static bool operator >(Vector2 vec1, Vector2 vec2) => vec1.X > vec2.X && vec1.Y > vec2.Y;
        /// <summary>
        /// 若vec1的两个分量同时小于vec2的两个分量，则true，否者false
        /// </summary>
        public static bool operator <(Vector2 vec1, Vector2 vec2) => vec1.X < vec2.X && vec1.Y < vec2.Y;
        /// <summary>
        /// 让矢量能乘double
        /// </summary>
        public static Vector2 operator *(double muty, Vector2 vector)
        {
            float x = (float)(vector.X * muty);
            float y = (float)(vector.Y * muty);
            return new Vector2(x, y);
        }
        /// <summary>
        /// 让矢量能乘double
        /// </summary>
        public static Vector2 operator *(Vector2 vector, double muty)
        {
            float x = (float)(vector.X * muty);
            float y = (float)(vector.Y * muty);
            return new Vector2(x, y);
        }
        */
        /// <summary>
        /// 安全归一化：零矢量（或长度小于容差）时返回 fallback，避免 NaN 沿计算链扩散
        /// </summary>
        public static Vector2 SafeUnit(this Vector2 vec, Vector2 fallback = default) => MonoUtil.SafeNormalize(vec, fallback);
        /// <summary>
        /// 逆时针 90° 的垂直矢量
        /// </summary>
        public static Vector2 Perp(this Vector2 vec) => MonoUtil.Perpendicular(vec);
        /// <summary>
        /// 本点与目标点构成的线段
        /// </summary>
        /// <param name="vec">起点</param>
        /// <param name="end">终点</param>
        public static MonoSegment ToSegment(this Vector2 vec, Vector2 end) => new(vec, end);
        /// <summary>
        /// 以本点为圆心、目标点为圆周上一点构造圆
        /// </summary>
        /// <param name="vec">圆心</param>
        /// <param name="circumferencePoint">圆周上的点</param>
        public static MonoCircle ToCircleThrough(this Vector2 vec, Vector2 circumferencePoint) => new(vec, Vector2.Distance(vec, circumferencePoint));
        /// <summary>
        /// 本点与目标点的中点
        /// </summary>
        /// <param name="vec">本点</param>
        /// <param name="other">另一个点</param>
        public static Vector2 Midpoint(this Vector2 vec, Vector2 other) => (vec + other) * 0.5f;
        /// <summary>
        /// 本点到目标点的距离的平方（省一次开方）
        /// </summary>
        /// <param name="vec">本点</param>
        /// <param name="other">另一个点</param>
        public static float DistanceSquaredTo(this Vector2 vec, Vector2 other) => Vector2.DistanceSquared(vec, other);
        /// <summary>
        /// 是否在给定形状的轴对齐包围盒内
        /// </summary>
        /// <param name="vec">本点</param>
        /// <param name="box">包围盒</param>
        /// <param name="tolerance">容差</param>
        public static bool Inside(this Vector2 vec, MonoAABB box, float tolerance = 0f) => box.Contains(vec, tolerance);
        #endregion
    }
}

