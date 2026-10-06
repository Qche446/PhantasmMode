using System.Runtime.CompilerServices;

using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维轴对齐包围盒（浮点精度，区别于整数精度的 <see cref="Rectangle"/>）。
    /// </summary>
    public readonly struct MonoAABB : IEquatable<MonoAABB>
    {
        /// <summary>最小角。</summary>
        public readonly Vector2 Min;

        /// <summary>最大角。</summary>
        public readonly Vector2 Max;

        /// <summary>以最小角与最大角构造。</summary>
        /// <param name="min">最小角。</param>
        /// <param name="max">最大角。</param>
        public MonoAABB(Vector2 min, Vector2 max)
        {
            Min = Vector2.Min(min, max);
            Max = Vector2.Max(min, max);
        }

        /// <summary>以中心与半尺寸构造。</summary>
        /// <param name="center">中心。</param>
        /// <param name="halfExtents">半尺寸。</param>
        public static MonoAABB FromCenter(Vector2 center, Vector2 halfExtents) => new(center - halfExtents, center + halfExtents);

        /// <summary>以左上角与尺寸构造。</summary>
        /// <param name="topLeft">左上角。</param>
        /// <param name="size">尺寸。</param>
        public static MonoAABB FromSize(Vector2 topLeft, Vector2 size) => new(topLeft, topLeft + size);

        /// <summary>由一组点求包围盒。</summary>
        /// <param name="points">点集。</param>
        public static MonoAABB FromPoints(ReadOnlySpan<Vector2> points)
        {
            if (points.Length == 0)
                return new MonoAABB(Vector2.Zero, Vector2.Zero);

            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }
            return new MonoAABB(min, max);
        }

        /// <summary>由整数矩形构造。</summary>
        /// <param name="rect">整数矩形。</param>
        public static MonoAABB FromRectangle(Rectangle rect) => new(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Bottom));

        /// <summary>中心。</summary>
        public Vector2 Center => (Min + Max) * 0.5f;

        /// <summary>尺寸。</summary>
        public Vector2 Size => Max - Min;

        /// <summary>半尺寸。</summary>
        public Vector2 HalfExtents => Size * 0.5f;

        /// <summary>面积，恒非负。</summary>
        public float Area => Size.X * Size.Y;

        /// <summary>外接圆半径（中心到角）。</summary>
        public float BoundingRadius => Size.Length() * 0.5f;

        /// <summary>四个角点（顺序：左上、右上、右下、左下）。</summary>
        public Vector2[] Corners => new[]
        {
            new Vector2(Min.X, Min.Y),
            new Vector2(Max.X, Min.Y),
            new Vector2(Max.X, Max.Y),
            new Vector2(Min.X, Max.Y),
        };

        /// <summary>点是否在盒内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector2 point, float tolerance = 0f)
            => point.X >= Min.X - tolerance && point.X <= Max.X + tolerance
            && point.Y >= Min.Y - tolerance && point.Y <= Max.Y + tolerance;

        /// <summary>另一个包围盒是否完全被包含。</summary>
        /// <param name="other">另一个包围盒。</param>
        public bool Contains(MonoAABB other) => Contains(other.Min) && Contains(other.Max);

        /// <summary>两个包围盒是否重叠（接触也算）。</summary>
        /// <param name="other">另一个包围盒。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(MonoAABB other)
            => Min.X <= other.Max.X && Max.X >= other.Min.X && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;

        /// <summary>盒内距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 ClosestPoint(Vector2 point) => Vector2.Clamp(point, Min, Max);

        /// <summary>点到盒表面的距离；点在盒内时返回 0。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>点到盒的 SDF 语义有符号距离：内部为负（到最近边的距离，负值），外部为正。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point)
        {
            Vector2 center = Center;
            Vector2 d = Abs(point - center) - HalfExtents;
            float outside = Vector2.Max(d, Vector2.Zero).Length();
            float inside = Math.Min(Math.Max(d.X, d.Y), 0f);
            return outside + inside;
        }

        /// <summary>扩大（<paramref name="amount"/> 为负则收缩）。</summary>
        /// <param name="amount">变化量。</param>
        public MonoAABB Inflated(float amount) => new(Min - new Vector2(amount), Max + new Vector2(amount));

        /// <summary>逐轴扩大。</summary>
        /// <param name="amount">逐轴变化量。</param>
        public MonoAABB Inflated(Vector2 amount) => new(Min - amount, Max + amount);

        /// <summary>平移。</summary>
        /// <param name="offset">位移。</param>
        public MonoAABB Moved(Vector2 offset) => new(Min + offset, Max + offset);

        /// <summary>取两盒的并集。</summary>
        /// <param name="other">另一个包围盒。</param>
        public MonoAABB Union(MonoAABB other) => new(Vector2.Min(Min, other.Min), Vector2.Max(Max, other.Max));

        /// <summary>取两盒的交集；无交集时得到退化的空盒。</summary>
        /// <param name="other">另一个包围盒。</param>
        public MonoAABB Intersection(MonoAABB other) => new(Vector2.Max(Min, other.Min), Vector2.Min(Max, other.Max));

        /// <summary>以 <paramref name="pivot"/> 为中心按比例缩放。</summary>
        /// <param name="scale">缩放系数。</param>
        /// <param name="pivot">缩放中心。</param>
        public MonoAABB Scaled(Vector2 scale, Vector2 pivot)
        {
            Vector2 a = (Min - pivot) * scale + pivot;
            Vector2 b = (Max - pivot) * scale + pivot;
            return new MonoAABB(a, b);
        }

        /// <summary>转换为向外取整的整数矩形。</summary>
        public Rectangle ToRectangle()
            => new((int)MathF.Floor(Min.X), (int)MathF.Floor(Min.Y), (int)MathF.Ceiling(Size.X), (int)MathF.Ceiling(Size.Y));

        /// <summary>点是否在圆内部与盒重叠的判定（等价于 AABB vs 圆）。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        public bool IntersectsCircle(Vector2 center, float radius)
            => Vector2.DistanceSquared(center, ClosestPoint(center)) <= radius * radius;

        /// <inheritdoc/>
        public bool Equals(MonoAABB other) => Min == other.Min && Max == other.Max;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoAABB other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Min, Max);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoAABB a, MonoAABB b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoAABB a, MonoAABB b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"AABB[{Min} .. {Max}]";
    }

    /// <summary>
    /// 二维有向盒（OBB，可旋转的矩形）。大量用于旋转的攻击判定与自定义碰撞体。
    /// </summary>
    public readonly struct MonoOBB : IEquatable<MonoOBB>
    {
        /// <summary>中心。</summary>
        public readonly Vector2 Center;

        /// <summary>沿局部 X 轴的半尺寸。</summary>
        public readonly Vector2 HalfExtents;

        /// <summary>旋转角（弧度）。</summary>
        public readonly float Rotation;

        /// <summary>构造有向盒。</summary>
        /// <param name="center">中心。</param>
        /// <param name="halfExtents">半尺寸。</param>
        /// <param name="rotation">旋转角。</param>
        public MonoOBB(Vector2 center, Vector2 halfExtents, float rotation = 0f)
        {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = rotation;
        }

        /// <summary>局部 X 轴单位矢量。</summary>
        public Vector2 AxisX => Rotation.ToRotationVector2();

        /// <summary>局部 Y 轴单位矢量。</summary>
        public Vector2 AxisY => MonoUtil.Perpendicular(AxisX);

        /// <summary>面积。</summary>
        public float Area => HalfExtents.X * HalfExtents.Y * 4f;

        /// <summary>把世界坐标点变换到 OBB 的局部（以中心为原点、未旋转）坐标系。</summary>
        /// <param name="point">世界坐标点。</param>
        public Vector2 ToLocal(Vector2 point) => (point - Center).RotatedBy(-Rotation);

        /// <summary>把 OBB 局部坐标点变换回世界坐标。</summary>
        /// <param name="localPoint">局部坐标点。</param>
        public Vector2 ToWorld(Vector2 localPoint) => Center + localPoint.RotatedBy(Rotation);

        /// <summary>四个角点。</summary>
        public Vector2[] Corners =>
        [
            ToWorld(new Vector2(-HalfExtents.X, -HalfExtents.Y)),
            ToWorld(new Vector2(HalfExtents.X, -HalfExtents.Y)),
            ToWorld(new Vector2(HalfExtents.X, HalfExtents.Y)),
            ToWorld(new Vector2(-HalfExtents.X, HalfExtents.Y)),
        ];

        /// <summary>轴对齐包围盒（精确，考虑旋转）。</summary>
        public MonoAABB FloatBounds
        {
            get
            {
                Vector2 ex = Abs(AxisX) * HalfExtents.X;
                Vector2 ey = Abs(AxisY) * HalfExtents.Y;
                Vector2 extent = ex + ey;
                return new MonoAABB(Center - extent, Center + extent);
            }
        }

        /// <summary>整数精度的轴对齐包围盒。</summary>
        public Rectangle Bounds => FloatBounds.ToRectangle();

        /// <summary>点是否在盒内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(Vector2 point, float tolerance = 0f)
        {
            Vector2 local = ToLocal(point);
            return Math.Abs(local.X) <= HalfExtents.X + tolerance && Math.Abs(local.Y) <= HalfExtents.Y + tolerance;
        }

        /// <summary>盒上距离 <paramref name="point"/> 最近的点。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
        {
            Vector2 local = ToLocal(point);
            Vector2 clamped = Vector2.Clamp(local, -HalfExtents, HalfExtents);
            return ToWorld(clamped);
        }

        /// <summary>点到盒表面的距离；点在盒内时返回 0。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>SDF 语义有符号距离：内部为负。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point)
        {
            Vector2 d = Abs(ToLocal(point)) - HalfExtents;
            float outside = Vector2.Max(d, Vector2.Zero).Length();
            float inside = Math.Min(Math.Max(d.X, d.Y), 0f);
            return outside + inside;
        }

        /// <summary>转为四点的凸多边形（绘制与 SAT 求交用）。</summary>
        public MonoPolygon ToPolygon() => new(Corners, false);

        /// <summary>平移。</summary>
        /// <param name="offset">位移。</param>
        public MonoOBB Moved(Vector2 offset) => new(Center + offset, HalfExtents, Rotation);

        /// <inheritdoc/>
        public bool Equals(MonoOBB other)
            => Center == other.Center && HalfExtents == other.HalfExtents && Rotation == other.Rotation;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoOBB other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Center, HalfExtents, Rotation);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoOBB a, MonoOBB b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoOBB a, MonoOBB b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"OBB[{Center} half={HalfExtents} rot={Rotation}]";
    }

    /// <summary>
    /// 二维圆角矩形。同时是绘制 9-slice 面板、UI 按钮的几何基础。
    /// </summary>
    public readonly struct MonoRoundedRect : IEquatable<MonoRoundedRect>
    {
        /// <summary>中心。</summary>
        public readonly Vector2 Center;

        /// <summary>外沿半尺寸（已包含圆角；芯部半尺寸为 <c>HalfExtents - CornerRadius</c>）。</summary>
        public readonly Vector2 HalfExtents;

        /// <summary>圆角半径。</summary>
        public readonly float CornerRadius;

        /// <summary>旋转角（弧度）。</summary>
        public readonly float Rotation;

        /// <summary>构造圆角矩形。</summary>
        /// <param name="center">中心。</param>
        /// <param name="halfExtents">外沿半尺寸（含圆角）。</param>
        /// <param name="cornerRadius">圆角半径。</param>
        /// <param name="rotation">旋转角。</param>
        public MonoRoundedRect(Vector2 center, Vector2 halfExtents, float cornerRadius, float rotation = 0f)
        {
            Center = center;
            HalfExtents = halfExtents;
            CornerRadius = cornerRadius;
            Rotation = rotation;
        }

        /// <summary>以位置与尺寸构造（<paramref name="position"/> 为左上角，<paramref name="size"/> 为外沿尺寸）。</summary>
        /// <param name="position">左上角。</param>
        /// <param name="size">外沿尺寸（含圆角）。</param>
        /// <param name="cornerRadius">圆角半径。</param>
        /// <param name="rotation">旋转角。</param>
        public static MonoRoundedRect FromSize(Vector2 position, Vector2 size, float cornerRadius, float rotation = 0f)
            => new(position + size * 0.5f, size * 0.5f, cornerRadius, rotation);

        /// <summary>以中心与芯部半尺寸构造（不含圆角外扩，整体外沿为 <c>coreHalfExtents + CornerRadius</c>）。</summary>
        /// <param name="center">中心。</param>
        /// <param name="coreHalfExtents">芯部半尺寸（不含圆角）。</param>
        /// <param name="cornerRadius">圆角半径。</param>
        /// <param name="rotation">旋转角。</param>
        public static MonoRoundedRect FromCoreHalfExtents(Vector2 center, Vector2 coreHalfExtents, float cornerRadius, float rotation = 0f)
            => new(center, coreHalfExtents + new Vector2(cornerRadius), cornerRadius, rotation);

        /// <summary>把世界坐标点变换到局部坐标系。</summary>
        /// <param name="point">世界坐标点。</param>
        public Vector2 ToLocal(Vector2 point) => (point - Center).RotatedBy(-Rotation);

        /// <summary>把局部坐标点变换回世界坐标。</summary>
        /// <param name="localPoint">局部坐标点。</param>
        public Vector2 ToWorld(Vector2 localPoint) => Center + localPoint.RotatedBy(Rotation);

        /// <summary>实际生效的圆角半径（被外沿半尺寸限制）。</summary>
        public float EffectiveRadius => MathHelper.Clamp(CornerRadius, 0f, Math.Min(HalfExtents.X, HalfExtents.Y));

        /// <summary>芯部半尺寸（外沿半尺寸向内收一个圆角）。</summary>
        private Vector2 CoreHalfExtents
        {
            get
            {
                float radius = EffectiveRadius;
                return Vector2.Max(HalfExtents - new Vector2(radius), Vector2.Zero);
            }
        }

        /// <summary>SDF 语义有符号距离：内部为负。</summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point)
        {
            float radius = EffectiveRadius;
            Vector2 q = Abs(ToLocal(point)) - CoreHalfExtents;
            float outside = Vector2.Max(q, Vector2.Zero).Length();
            float inside = Math.Min(Math.Max(q.X, q.Y), 0f);
            return outside + inside - radius;
        }

        /// <summary>点是否在圆角矩形内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差。</param>
        public bool Contains(Vector2 point, float tolerance = 0f) => SignedDistanceTo(point) <= tolerance;

        /// <summary>形状表面或内部距离 <paramref name="point"/> 最近的点（内部点返回其自身，外部点落到形状外沿）。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
        {
            float radius = EffectiveRadius;
            Vector2 local = ToLocal(point);
            Vector2 q = Abs(local) - CoreHalfExtents;
            float outside = Vector2.Max(q, Vector2.Zero).Length();
            float inside = Math.Min(Math.Max(q.X, q.Y), 0f);
            float distance = outside + inside - radius;
            if (distance <= 0f)
                return point;

            // 外部点沿 SDF 梯度方向（即由最近芯部点指向查询点的方向）回落到外沿。
            Vector2 core = Vector2.Clamp(local, -CoreHalfExtents, CoreHalfExtents);
            Vector2 direction = MonoUtil.SafeNormalize(local - core, Vector2.UnitX);
            return ToWorld(core + direction * radius);
        }

        /// <summary>点到形状边界的距离；点在内部时返回 0。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point)
        {
            float signed = SignedDistanceTo(point);
            return signed <= 0f ? Vector2.Distance(point, ClosestPoint(point)) : signed;
        }

        /// <summary>轴对齐包围盒（浮点）。外沿半尺寸已含圆角，无需再加。</summary>
        public MonoAABB FloatBounds
        {
            get
            {
                Vector2 ex = Abs(Rotation.ToRotationVector2()) * HalfExtents.X;
                Vector2 ey = Abs(MonoUtil.Perpendicular(Rotation.ToRotationVector2())) * HalfExtents.Y;
                Vector2 extent = ex + ey;
                return new MonoAABB(Center - extent, Center + extent);
            }
        }

        /// <summary>整数精度的轴对齐包围盒。</summary>
        public Rectangle Bounds => FloatBounds.ToRectangle();

        /// <inheritdoc/>
        public bool Equals(MonoRoundedRect other)
            => Center == other.Center && HalfExtents == other.HalfExtents && CornerRadius == other.CornerRadius && Rotation == other.Rotation;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoRoundedRect other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Center, HalfExtents, CornerRadius, Rotation);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoRoundedRect a, MonoRoundedRect b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoRoundedRect a, MonoRoundedRect b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"RoundedRect[{Center} half={HalfExtents} r={CornerRadius} rot={Rotation}]";
    }
}
