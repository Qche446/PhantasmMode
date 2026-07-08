using FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins;
using Microsoft.Xna.Framework;
using Terraria;

namespace FargosPhantasmMode.Common
{
    public class RotatedRectangle
    {
        public Vector2 Center { get; set; }
        public Vector2 Size { get; set; }
        public float Rotation { get; set; }
        public RotatedRectangle() { }
        public RotatedRectangle(Vector2 center, Vector2 size, float rotation)
        {
            Center = center;
            Size = size;
            Rotation = rotation;
        }
        public RotatedRectangle(Rectangle rect, float rotation = 0)
        {
            Center = rect.TopLeft() + rect.Size() / 2f;
            Size = rect.Size();
            Rotation = rotation;
        }
        public Vector2[] GetCorners()
        {
            Vector2 halfSize = Size / 2f;
            Vector2[] corners = new Vector2[]
            {
            new Vector2(-halfSize.X, -halfSize.Y),
            new Vector2( halfSize.X, -halfSize.Y),
            new Vector2( halfSize.X,  halfSize.Y),
            new Vector2(-halfSize.X,  halfSize.Y)
            };
            float cos = (float)System.Math.Cos(Rotation);
            float sin = (float)System.Math.Sin(Rotation);

            for (int i = 0; i < corners.Length; i++)
            {
                // 旋转局部坐标
                float rotatedX = corners[i].X * cos - corners[i].Y * sin;
                float rotatedY = corners[i].X * sin + corners[i].Y * cos;
                // 平移到世界位置
                corners[i] = new Vector2(rotatedX + Center.X, rotatedY + Center.Y);
            }
            return corners;
        }
        // 获取外接轴对齐矩形（用于粗筛）
        public Rectangle GetBoundingRectangle()
        {
            Vector2[] corners = GetCorners();
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var c in corners)
            {
                if (c.X < minX) minX = c.X;
                if (c.X > maxX) maxX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.Y > maxY) maxY = c.Y;
            }
            return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }
    }
    public static class CollisionDetector
    {
        // 检测两个旋转矩形是否相交
        public static bool Intersects(RotatedRectangle a, RotatedRectangle b)
        {
            // 获取顶点
            Vector2[] cornersA = a.GetCorners();
            Vector2[] cornersB = b.GetCorners();

            // 获取需要检测的轴（矩形的边法线方向）
            // 注意：矩形有4条边，但只有2个唯一方向（对边平行），所以只需取2条边
            Vector2[] axes = new Vector2[4];
            axes[0] = GetEdgeNormal(cornersA[0], cornersA[1]);
            axes[1] = GetEdgeNormal(cornersA[1], cornersA[2]);
            axes[2] = GetEdgeNormal(cornersB[0], cornersB[1]);
            axes[3] = GetEdgeNormal(cornersB[1], cornersB[2]);

            // 遍历所有轴
            foreach (Vector2 axis in axes)
            {
                // 如果在任何一条轴上投影不重叠，则无碰撞
                if (!ProjectionsOverlap(cornersA, cornersB, axis))
                {
                    return false; // 提前退出，节约性能
                }
            }

            return true; // 所有轴都重叠，发生碰撞
        }
        // ---------- 重载1：RotatedRectangle vs Rectangle ----------
        public static bool Intersects(RotatedRectangle a, Rectangle b)
        {
            // 将 Rectangle 转换为 RotatedRectangle（旋转为0）
            RotatedRectangle rb = new RotatedRectangle(b);
            return Intersects(a, rb);
        }

        // ---------- 重载2：Rectangle vs RotatedRectangle（参数顺序交换） ----------
        public static bool Intersects(Rectangle a, RotatedRectangle b)
        {
            return Intersects(b, a); // 直接复用，交换参数
        }

        // ---------- 重载3：两个普通 Rectangle（利用 XNA 原生方法，更高效） ----------
        public static bool Intersects(Rectangle a, Rectangle b)
        {
            return a.Intersects(b); // XNA 自带的高效 AABB 碰撞
        }

        // 计算边向量并返回其法线（单位向量）
        private static Vector2 GetEdgeNormal(Vector2 p1, Vector2 p2)
        {
            Vector2 edge = p2 - p1;
            // 边法线：垂直于边向量，即 (edge.Y, -edge.X) 或 (-edge.Y, edge.X)
            Vector2 normal = new Vector2(edge.Y, -edge.X);
            normal.Normalize(); // 归一化
            return normal;
        }

        // 判断两个矩形在给定轴上的投影是否重叠
        private static bool ProjectionsOverlap(Vector2[] cornersA, Vector2[] cornersB, Vector2 axis)
        {
            // 计算矩形A在轴上的投影区间 (minA, maxA)
            float minA = float.MaxValue, maxA = float.MinValue;
            foreach (Vector2 corner in cornersA)
            {
                float projection = Vector2.Dot(corner, axis);
                if (projection < minA) minA = projection;
                if (projection > maxA) maxA = projection;
            }

            // 计算矩形B在轴上的投影区间 (minB, maxB)
            float minB = float.MaxValue, maxB = float.MinValue;
            foreach (Vector2 corner in cornersB)
            {
                float projection = Vector2.Dot(corner, axis);
                if (projection < minB) minB = projection;
                if (projection > maxB) maxB = projection;
            }

            // 判断区间是否重叠：如果 A 的最大值小于 B 的最小值，或者 B 的最大值小于 A 的最小值，则不重叠
            return !(maxA < minB || maxB < minA);
        }
    }
}
