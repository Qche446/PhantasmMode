using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Animations;
using Terraria.Utilities;

namespace FargosPhantasmMode.Common
{
    public static class PhanUtil
    {
        public static Vector2 VecLerp(Vector2 start, Vector2 end, float progress) => new Vector2(MathHelper.Lerp(start.X, end.X, progress), MathHelper.Lerp(start.Y, end.Y, progress));
        public static float AngleDifference(this Vector2 start, Vector2 end) => MathHelper.WrapAngle(end.ToRotation() - start.ToRotation());
        public static Vector2 Axisymmetry(this Vector2 oldvel, Vector2 axis) => oldvel.RotatedBy(2 * oldvel.AngleDifference(axis));
        public static Vector2 Axisymmetry(this Vector2 oldp, Vector2 line1, Vector2 line2) => (oldp - line2).Axisymmetry(line1 - line2) + line2;
        /// <summary>
        /// 生成连接两点的圆弧曲线采样点（包含起点和终点，各点间等角距）
        /// </summary>
        /// <returns>长度为 pointCount 的圆弧点数组（首元素为 start，末元素为 end）</returns>
        public static Vector2[] GetArcPoints(Vector2 start, Vector2 end, float arcAngle, int pointCount)
        {
            if (pointCount < 2)
                throw new ArgumentException("pointCount 必须 ≥ 2");
            Vector2[] points = new Vector2[pointCount];
            points[0] = start;
            if (pointCount == 2)
            {
                points[1] = end;
                return points;
            }
            // 弦向量与长度
            Vector2 v = end - start;
            float d = v.Length();
            // 退化情况：弦长极短或圆心角接近 0 → 直线插值
            if (d < 1e-6f || Math.Abs(arcAngle) < 1e-6f)
            {
                for (int i = 0; i < pointCount; i++)
                    points[i] = Vector2.Lerp(start, end, (float)i / (pointCount - 1));
                return points;
            }
            float angle = Math.Abs(arcAngle);          // 圆心角绝对值
            float sign = Math.Sign(arcAngle);         // 弧方向符号
            float sinHalf = (float)Math.Sin(angle * 0.5f);
            float cosHalf = (float)Math.Cos(angle * 0.5f);
            float radius = d / (2f * sinHalf);         // 圆弧半径
            Vector2 mid = (start + end) * 0.5f;        // 弦中点
            Vector2 n = new Vector2(-v.Y, v.X);        // 弦逆时针90°方向
            n.Normalize();
            // 圆心
            Vector2 center = mid + n * (sign * radius * cosHalf);
            // 起点角度（相对于圆心）
            float startAngle = (start - center).ToRotation();
            float angleStep = sign * angle / (pointCount - 1);
            for (int i = 1; i < pointCount - 1; i++)
            {
                float a = startAngle + angleStep * i;
                points[i] = center + new Vector2(radius * (float)Math.Cos(a),
                                                  radius * (float)Math.Sin(a));
            }
            points[pointCount - 1] = end;   // 强制终点精确
            return points;
        }
        /// <summary>
        /// 生成一段圆弧，用于模拟圆弧形追踪
        /// </summary>
        /// <returns>长度为 pointCount的圆弧点数组（首元素为 start，末元素为 end）</returns>
        public static Vector2[] GetArcPoints(Vector2 start, Vector2 startvel, Vector2 target, float acc, int pointCount)
        {
            if (pointCount < 2)
                throw new ArgumentException("pointCount 必须 ≥ 2");
            float r = startvel.LengthSquared() / acc;
            int flag = Math.Sign(startvel.AngleDifference(target - start));
            Vector2 Center = r * startvel.SafeNormalize(Vector2.UnitX).RotatedBy(flag * MathHelper.PiOver2) + start;
            Vector2 end = start.Axisymmetry(Center, target);
            Vector2 endvel = startvel.Axisymmetry(end, start);
            float arcAngle = startvel.AngleDifference(endvel);
            Vector2[] points = new Vector2[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = (start - Center).RotatedBy(i * arcAngle / ((float)pointCount - 1f)) + Center;
            }
            /*
            for (int i = pointCount; i < pointCount + 10; i++)
            {
                points[i] = end + 0.4f * (i - pointCount) * endvel;
            }
            */
            //Vector2[] points = GetArcPoints(start, end, arcAngle, pointCount);
            //points[0] = start;
            //points[pointCount - 1] = end;
            return points;
        }
        /// <summary>
        /// 生成两点之间的采样点（直线），间隔相同，pointCount >= 2
        /// </summary>
        /// <returns>长度为pointCount的直线点数组</returns>
        public static Vector2[] GetStraightLinePoint(Vector2 start, Vector2 end, int pointCount)
        {
            if (pointCount < 2)
                throw new ArgumentException("pointCount 必须 ≥ 2");
            Vector2[] points = new Vector2[pointCount];
            Vector2 r = end - start;
            Vector2 offset = r / (pointCount - 1);
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = start + i * offset;
            }
            points[0] = start;
            points[pointCount - 1] = end;
            return points;
        }
        public static List<Vector2> GetLightningLinePoint(Vector2 start, Vector2 end, int pointCount, float Amplitude, Random random)
        {
            if (pointCount < 2)
                throw new ArgumentException("pointCount 必须 ≥ 2");
            List<Vector2> points = [];
            Vector2 r = end - start;
            Vector2 nr = r.SafeNormalize(Vector2.Zero);
            Vector2 nt = new Vector2(-nr.Y, nr.X);
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / ((float)pointCount - 1f);
                Vector2 basePos = VecLerp(start, end, t);
                float offset = (float)(random.NextDouble() * 2 - 1) * Amplitude * (1 - Math.Abs(t - 0.5f) * 2);
                points.Add(basePos + nt * offset);
            }
            return points;
        }
    }
}
