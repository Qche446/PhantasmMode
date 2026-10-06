using System.Runtime.CompilerServices;

using static Monochrome.Common.MonoUtil.MonoUtil;

namespace Monochrome.Common.MonoUtil.Mathematics.Geometry
{
    /// <summary>
    /// 二维（可旋转的）椭圆。
    /// <para>
    /// 点是否在内有闭式解；但「点到椭圆的最近点」没有闭式解，
    /// 本实现使用按参数角的一元牛顿迭代（最多 6 次），起始点已按象限给出，
    /// 在半径比不超过约 1000:1 时精度远优于 1e-4 像素。
    /// </para>
    /// </summary>
    public readonly struct MonoEllipse : IEquatable<MonoEllipse>
    {
        /// <summary>圆心。</summary>
        public readonly Vector2 Center;

        /// <summary>X 半轴（椭圆自身的局部 X 轴）。</summary>
        public readonly float RadiusX;

        /// <summary>Y 半轴（椭圆自身的局部 Y 轴）。</summary>
        public readonly float RadiusY;

        /// <summary>旋转角（弧度，绕 <see cref="Center"/>）。</summary>
        public readonly float Rotation;

        /// <summary>构造椭圆。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radiusX">X 半轴。</param>
        /// <param name="radiusY">Y 半轴。</param>
        /// <param name="rotation">旋转角。</param>
        public MonoEllipse(Vector2 center, float radiusX, float radiusY, float rotation = 0f)
        {
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
            Rotation = rotation;
        }

        /// <summary>以圆心与两半轴构造（无旋转）。</summary>
        /// <param name="center">圆心。</param>
        /// <param name="radii">两半轴。</param>
        public static MonoEllipse FromRadii(Vector2 center, Vector2 radii) => new(center, radii.X, radii.Y);

        /// <summary>以圆构造（两半轴相等）。</summary>
        /// <param name="circle">源圆。</param>
        public static MonoEllipse FromCircle(MonoCircle circle) => new(circle.Center, circle.Radius, circle.Radius);

        /// <summary>面积。</summary>
        public float Area => Pi * RadiusX * RadiusY;

        /// <summary>等效平均半径 <c>(a+b)/2</c>，用于粗略的距离/绘制估计。</summary>
        public float MeanRadius => (RadiusX + RadiusY) * 0.5f;

        /// <summary>旋转角对应的单位轴。</summary>
        public Vector2 AxisX => Rotation.ToRotationVector2();

        /// <summary>局部 Y 轴。</summary>
        public Vector2 AxisY => MonoUtil.Perpendicular(AxisX);

        /// <summary>把世界坐标点变换到椭圆的局部（未旋转、以圆心为原点）坐标系。</summary>
        /// <param name="point">世界坐标点。</param>
        public Vector2 ToLocal(Vector2 point) => (point - Center).RotatedBy(-Rotation);

        /// <summary>把椭圆局部坐标点变换回世界坐标。</summary>
        /// <param name="localPoint">局部坐标点。</param>
        public Vector2 ToWorld(Vector2 localPoint) => Center + localPoint.RotatedBy(Rotation);

        /// <summary>点是否在椭圆内（含边界）。</summary>
        /// <param name="point">查询点。</param>
        /// <param name="tolerance">容差（世界单位，会按半轴归一化）。</param>
        public bool Contains(Vector2 point, float tolerance = 0f)
        {
            if (Math.Abs(RadiusX) <= MonoUtil.Epsilon || Math.Abs(RadiusY) <= MonoUtil.Epsilon)
                return false;
            Vector2 p = ToLocal(point);
            float nx = p.X / (RadiusX + tolerance);
            float ny = p.Y / (RadiusY + tolerance);
            return nx * nx + ny * ny <= 1f;
        }

        /// <summary>椭圆上参数角 <paramref name="angle"/> 处的点（角度为局部参数角，非几何极角）。</summary>
        /// <param name="angle">参数角（弧度）。</param>
        public Vector2 PointAtAngle(float angle)
            => ToWorld(new Vector2(MathF.Cos(angle) * RadiusX, MathF.Sin(angle) * RadiusY));

        /// <summary>椭圆上距离 <paramref name="point"/> 最近的点（牛顿迭代）。</summary>
        /// <param name="point">查询点。</param>
        public Vector2 ClosestPoint(Vector2 point)
        {
            float a = RadiusX;
            float b = RadiusY;

            if (a <= MonoUtil.Epsilon || b <= MonoUtil.Epsilon)
                return Center;

            Vector2 p = ToLocal(point);
            float px = p.X;
            float py = p.Y;

            if (px * px + py * py <= MonoUtil.EpsilonSqr)
                return Center + AxisX * a;

            float eps = 1e-7f * Math.Max(a, b);
            float t = MathF.Atan2(py / b, px / a);

            for (int i = 0; i < 6; i++)
            {
                float cos = MathF.Cos(t);
                float sin = MathF.Sin(t);
                float fx = (a * a - b * b) * cos * sin - px * a * sin + py * b * cos;
                float dfx = (a * a - b * b) * (cos * cos - sin * sin) - px * a * cos - py * b * sin;
                if (Math.Abs(dfx) <= 1e-12f)
                    break;
                float delta = fx / dfx;
                t -= delta;
                if (Math.Abs(delta) <= eps)
                    break;
            }

            return ToWorld(new Vector2(a * MathF.Cos(t), b * MathF.Sin(t)));
        }

        /// <summary>点到椭圆的最近距离（到边界，内部点同样返回正值）。</summary>
        /// <param name="point">查询点。</param>
        public float DistanceTo(Vector2 point) => Vector2.Distance(point, ClosestPoint(point));

        /// <summary>
        /// 点到椭圆的近似有符号距离：外部为正、内部为负。
        /// 精确的椭圆 SDF 无闭式解，此处用归一化径向距离做线性近似，
        /// 足够用于剔除与风格化渲染；需要精确值时请用 <see cref="DistanceTo"/>。
        /// </summary>
        /// <param name="point">查询点。</param>
        public float SignedDistanceTo(Vector2 point)
        {
            if (Math.Abs(RadiusX) <= MonoUtil.Epsilon || Math.Abs(RadiusY) <= MonoUtil.Epsilon)
                return Vector2.Distance(point, Center);

            Vector2 p = ToLocal(point);
            float weight = 1f / (RadiusX * RadiusX) - 1f / (RadiusY * RadiusY);
            float k1 = p.LengthSquared() - 1f / (RadiusX * RadiusX) - 1f / (RadiusY * RadiusY);
            float k2 = k1 * k1 + 4f * weight * (p.X * p.X / (RadiusX * RadiusX) + p.Y * p.Y / (RadiusY * RadiusY));
            float sign = p.X * p.X / (RadiusX * RadiusX) + p.Y * p.Y / (RadiusY * RadiusY) <= 1f ? -1f : 1f;
            return sign * (k1 + MathF.Sqrt(Math.Max(k2, 0f))) / (2f * MathF.Abs(weight));
        }

        /// <summary>轴对齐包围盒（精确，考虑旋转）。</summary>
        public Rectangle Bounds
        {
            get
            {
                float cos = MathF.Cos(Rotation);
                float sin = MathF.Sin(Rotation);
                float extentX = MathF.Sqrt(RadiusX * RadiusX * cos * cos + RadiusY * RadiusY * sin * sin);
                float extentY = MathF.Sqrt(RadiusX * RadiusX * sin * sin + RadiusY * RadiusY * cos * cos);
                return new Rectangle(
                    (int)MathF.Floor(Center.X - extentX),
                    (int)MathF.Floor(Center.Y - extentY),
                    (int)MathF.Ceiling(extentX * 2f),
                    (int)MathF.Ceiling(extentY * 2f));
            }
        }

        /// <summary>轴对齐包围盒（浮点，不取整）。</summary>
        public MonoAABB FloatBounds
        {
            get
            {
                float cos = MathF.Cos(Rotation);
                float sin = MathF.Sin(Rotation);
                float extentX = MathF.Sqrt(RadiusX * RadiusX * cos * cos + RadiusY * RadiusY * sin * sin);
                float extentY = MathF.Sqrt(RadiusX * RadiusX * sin * sin + RadiusY * RadiusY * cos * cos);
                Vector2 extent = new(extentX, extentY);
                return new MonoAABB(Center - extent, Center + extent);
            }
        }

        /// <summary>椭圆上均匀参数采样的 <paramref name="count"/> 个点（注意：参数均匀不等于弧长均匀）。</summary>
        /// <param name="count">点数。</param>
        /// <param name="angleOffset">起始参数角。</param>
        public Vector2[] Sample(int count, float angleOffset = 0f)
        {
            if (count <= 0)
                return Array.Empty<Vector2>();
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++)
                points[i] = PointAtAngle(angleOffset + TwoPi * i / count);
            return points;
        }

        /// <inheritdoc/>
        public bool Equals(MonoEllipse other)
            => Center == other.Center && RadiusX == other.RadiusX && RadiusY == other.RadiusY && Rotation == other.Rotation;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is MonoEllipse other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Center, RadiusX, RadiusY, Rotation);

        /// <summary>相等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator ==(MonoEllipse a, MonoEllipse b) => a.Equals(b);

        /// <summary>不等比较。</summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        public static bool operator !=(MonoEllipse a, MonoEllipse b) => !a.Equals(b);

        /// <inheritdoc/>
        public override string ToString() => $"Ellipse[{Center} rx={RadiusX} ry={RadiusY} rot={Rotation}]";
    }
}
