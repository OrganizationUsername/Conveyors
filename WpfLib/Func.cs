using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace WpfLib
{
    public static class Func
    {
        public static void ApplyMouseBehaviour(this Shape shape, Action<Shape> behaviour, MouseAction mouseAction = MouseAction.LeftClick) => shape.InputBindings.Add(new MouseBinding(new MyCommand<Shape>(behaviour, shape), new(mouseAction)));

        public static void SetLocation(this Shape shape, Point location)
        {
            Canvas.SetLeft(shape, location.X);
            Canvas.SetTop(shape, location.Y);
        }

        public static void SetLocation(this Line line, TwoPoints points)
        {
            if (points.P1.X != line.X1)
            {
                line.X1 = points.P1.X;
            }
            if (points.P1.Y != line.Y1)
            {
                line.Y1 = points.P1.Y;
            }
            if (points.P2.X != line.X2)
            {
                line.X2 = points.P2.X;
            }
            if (points.P2.Y != line.Y2)
            {
                line.Y2 = points.P2.Y;
            }

        }

        public static void SetCenterLocation(this Shape shape, Point location) => shape.SetLocation(location.Subtract((shape.Width / 2, shape.Height / 2)));

        public static Vector Vector(this TwoPoints startEnd) => (startEnd.P2.X - startEnd.P1.X, startEnd.P2.Y - startEnd.P1.Y);

        public static double Length(this Vector vect) => Math.Sqrt(vect.X * vect.X + vect.Y * vect.Y);

        public static double Length(this TwoPoints startEnd) => Length(startEnd.Vector());
        public static Vector Normalize(this Vector vect) => vect.Divide(vect.Length());
        public static Vector Normalize(this Vector vect, double length) => vect.Divide(length);

        public static Vector Multiply(this Vector vect, double factor) => (vect.X * factor, vect.Y * factor);
        public static Vector Divide(this Vector vect, double factor) => (vect.X / factor, vect.Y / factor);
        public static Point Subtract(this Point point, Vector vect) => (point.X - vect.X, point.Y - vect.Y);
        public static Point Add(this Point point, Vector vect) => (point.X + vect.X, point.Y + vect.Y);
        public static TwoPoints Add(this TwoPoints twoPoints, Vector vect) => (twoPoints.P1.Add(vect), twoPoints.P2.Add(vect));

        /// <summary>
        /// returns whether the elements in an array have the same distance
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static bool IsEvenlyDistributed(int[] arr)
        {
            if (arr.Length < 2) return false;
            // 1. step
            var step = arr[1] - arr[0];
            // check whether the step is the same for every consecutive pair in the array
            for(int i = 1; i < arr.Length; i++)
            {
                if (arr[i - 1] + step != arr[i]) return false;
            }
            return true;
        }
    }
}
