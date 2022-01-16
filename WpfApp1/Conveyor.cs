﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    public class Conveyor
    {
        private bool _IsRunning;
        public bool IsRunning
        {
            get => _IsRunning;
            set => Func.Setter(ref _IsRunning, value, StartIfRunning);
        }

        private void StartIfRunning()
        {
            if (IsRunning)
            {
                (Dispatcher = new(new ParameterizedThreadStart(ItemDispatcherThreadAction))).Start(this);
            }
        }

        public LinkedList<ConveyorSegment> Segments = new();

        public static Conveyor Create(IEnumerable<Line> lines)
        {
            var conv = new Conveyor();
            double length = 0d;
            foreach (var line in lines)
            {
                var segment = new ConveyorSegment(length, line);
                conv.Segments.AddLast(segment);
                length += segment.Length;
            }

            return conv;
        }

        public static void AddToCanvas(Conveyor conveyor, Canvas canvas)
        {
            conveyor.Canvas = canvas;
            foreach (var sement in conveyor.Segments)
            {
                canvas.Children.Add(sement.Line);
            }
        }

        public Canvas Canvas;
        public double Speed = 60;

        internal void SpawnItem()
        {
            var item = new Item(this);
            Items.Enqueue(item);
        }

        private readonly ConcurrentQueue<Item> Items = new();
        private Thread Dispatcher = new(new ParameterizedThreadStart(ItemDispatcherThreadAction));

        private static void ItemDispatcherThreadAction(object? obj)
        {
            if (obj is Conveyor conveyor)
            {
                DateTime time = DateTime.Now;
                while (conveyor.IsRunning)
                {
                    var now = DateTime.Now;
                    var diff = (now - time).TotalMilliseconds;
                    time = now;
                    foreach (var item in conveyor.Items.ToList()) // TODO hier muss das tolist weg - anderen datentypen wählen.
                    {
                        if (!item.Done)
                        {
                            item.Age += diff;
                        }
                    }
                    Thread.Sleep(10);
                }
            }
        }

        internal Point GetItemLocation(Item item, out bool done, out LinkedListNode<ConveyorSegment> segment)
        {
            double length = item.Age / 1000 * Speed;
            done = false;
            segment = item.Segment ?? Segments.First;

            while (segment is not null && segment.Value.EndLength < length)
            {
                segment = segment!.Next;
            }

            if (segment is null)
            {
                done = true;
                segment = Segments.Last;
            }

            return segment.Value.GetPointAbsolute(length);
        }
    }

    public class Item
    {
        public Item(Conveyor conveyor)
        {
            Conveyor = conveyor;
            Shape = new Ellipse() { Width = 10, Height = 10, Fill = Brushes.Blue };
            Conveyor.Canvas.Children.Add(Shape);
            Age = 0;
        }
        public Shape Shape { get; }
        private double _Age;
        public double Age
        {
            get => _Age;
            set
            {
                _Age = value;
                Location = Conveyor.GetItemLocation(this, out var done, out var segment);
                Segment = segment;
                if (done)
                {
                    Done = true;
                }
            }
        }
        public bool Moving { get; set; }
        public bool Done { get; set; }
        public Conveyor Conveyor { get; set; }

        public LinkedListNode<ConveyorSegment> Segment { get; set; }

        private Point _Location;
        public Point Location
        {
            get => _Location;
            set
            {
                _Location = value;
                Shape.Dispatcher.BeginInvoke(() =>
                {
                    Canvas.SetLeft(Shape, value.X - Shape.Width / 2);
                    Canvas.SetTop(Shape, value.Y - Shape.Height / 2);
                });
            }
        }
    }

    public class ConveyorSegment
    {
        public ConveyorSegment(double beginLength, Line line)
        {
            BeginLength = beginLength;
            Line = line;
        }
        private Line? _Line;
        public Line? Line
        {
            get => _Line;
            set
            {
                _Line = value;
                _Line!.Tag = this;
                Length = _Line.Length();
                EndLength = BeginLength + Length;
                UnitVector = new((_Line.X2 - _Line.X1) / Length, (_Line.Y2 - _Line.Y1) / Length);
                StartPoint = new(_Line.X1, _Line.Y1);
                EndPoint = new(_Line.X2, _Line.Y2);
            }
        }

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public double EndLength { get; set; }
        public double BeginLength { get; set; }


        public double Length { get; private set; }
        public Point UnitVector { get; internal set; }

        internal Point GetPointAbsolute(double length, bool overshoot = false)
        {
            length -= BeginLength;
            if (length < Length || overshoot)
            {
                var mult = length;// TODO hier ggf. auf spline umstellen
                return new(UnitVector.X * mult + StartPoint.X, UnitVector.Y * mult + StartPoint.Y);
            }
            else
            {
                return EndPoint;
            }
        }
    }

    public static class Maths
    {
        public static double Distance(Point p1, Point p2)
        {
            p1.X = p2.X - p1.X;
            p1.X = p1.X * p1.X;
            p1.Y = p2.Y - p1.Y;
            p1.Y = p1.Y * p1.Y;
            return Math.Sqrt(p1.X + p1.Y);
        }

        public static double Length(this Line line) => Distance(new(line.X1, line.Y1), new(line.X2, line.Y2));
    }
}
