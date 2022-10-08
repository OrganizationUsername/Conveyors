using System.Numerics;
using CoreLib.Maths;
using PointDef;
using PointDef.twopoints;

namespace ConveyorTests
{

    public class UnitTest1
    {
        [Theory]
        [InlineData(1, 0, 0)]
        [InlineData(1, 1, 45)]
        [InlineData(0, 1, 90)]
        [InlineData(-1, 0, 180)]
        [InlineData(0, -1, 270)]
        public void AngleOnACircleTest(double x, double y, double angleDeg)
        {
            var calcAngle = Maths.PosAngleBetween((1, 0), (x, y));
            Assert.Equal(angleDeg, calcAngle.Degrees);
        }

        [Theory]
        [InlineData(1, 0, 0, 1, 1,1, 90)]
        [InlineData(1, 0, 0, 1, -1, -1, 270)]
        [InlineData(1, 0, 1, 1, 1, 2, 45)]
        [InlineData(1, 1, 1, 1, 1, 1, 0)]
        [InlineData(1, 1, -1, 1, 0, 1, 90)]
        [InlineData(1, 1, -1, 0, 0, 1, 135)]
        [InlineData(1, 1, -1, -1, 0, 1, 180)]
        [InlineData(1, 1, 1, -1, 0, 1, 270)]
        public void AngleBetweenVectorTest(double x1, double y1, double x2, double y2, double xBetween, double yBetween, double angleDeg)
        {
            var calcAngleBetween = Maths.PosAngleBetween((x1, y1), (x2, y2), (xBetween, yBetween));
            Assert.Equal(angleDeg, calcAngleBetween.Degrees);
        }

        [Theory]
        [InlineData(1, 0, 0, 1, 90)]
        [InlineData(1, 0, 1, 1, 45)]
        [InlineData(1, 1, 1, 1, 0)]
        [InlineData(1, 1, -1, 1, 90)]
        [InlineData(1, 1, -1, 0, 135)]
        [InlineData(1, 1, -1, -1, 180)]
        [InlineData(1, 1, 1, -1, 270)]
        public void AngleBetweenTest(double x1, double y1, double x2, double y2, double angleDeg)
        {
            var angle = Maths.PosAngleBetween(x1, y1, x2, y2);
            Assert.Equal(angleDeg, angle.Degrees);
        }

        [Theory()]
        [InlineData(10, 10, 20, 20, 30, 30, 180)]
        [InlineData(10, 10, 20, 20, 30, 20, 135)]
        
        //[InlineData(10, 10, 20, 20, 20, 30, 225)]
        [InlineData(10, 10, 20, 20, 20, 30, -135)]
        public void Test1(double ax, double ay, double bx, double by, double cx, double cy, double angleRad)
        {
            V2d a = new(ax, ay);
            V2d b = new(bx, by);
            V2d c = new(cx, cy);

            var ab = Maths.Vector((b, a)).Normalize();
            var bc = Maths.Vector((b, c)).Normalize();

            var angle = calculateAngleSODeg(ab.X, ab.Y, bc.X, bc.Y);
            Assert.Equal(angleRad, angle);
        }

        [Fact]
        public void Test2()
        {
            Assert.Equal(2, 2);
        }

        [Theory]
        [InlineData(15, 0, 0, 15, 0)]
        [InlineData(15, 0, 90, 0, 15)]
        [InlineData(15, 0, 180, -15, 0)]
        [InlineData(15, 0, 270, 0, -15)]
        [InlineData(15, 0, 360, 15, 0)]
        public void AngleRTest(double x, double y, double angle, double targetX, double targetY)
        {
            var point = new Vector(x, y);
            var turnedPoint = point.RotateAroundOrigin(angle.Degrees());

            Assert.Equal((targetX, targetY), (turnedPoint.X, turnedPoint.Y));
        }


        [Theory]
        [InlineData(1, 2, Maths.Quadrants.One, 0)]
        [InlineData(-2, 1, Maths.Quadrants.Two, 90)]
        [InlineData(-1, -2, Maths.Quadrants.Three, 180)]
        [InlineData(2, -1, Maths.Quadrants.Four, 270)]
        public void QuadrantTest(double x, double y, Maths.Quadrants quadrant, double correctionAngleDeg)
        {
            Point p = (x, y);
            var res = Maths.BringInFirstQuadrant(p);

            Assert.Equal((quadrant, correctionAngleDeg, 1, 2), (p.Quadrant, res.CorrectionAngle.Degrees, res.FirstQuadrantPoint.X, res.FirstQuadrantPoint.Y));

        }

        /// <summary>
        /// https://stackoverflow.com/questions/13458992/angle-between-two-vectors-2d
        /// </summary>
        public double calculateAngleSODeg(double ax, double ay, double bx, double by)
{
            double sin = ax * by - bx * ay;
            double cos = ax * bx + ay * by;
            return Maths.RadToDeg(Math.Atan2(sin, cos));
        }
    }
}