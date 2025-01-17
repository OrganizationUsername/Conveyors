﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfLib;

namespace ConveyorLib;

class ItemTextAdorner : TextAdorner<Item>
{
    public ItemTextAdorner(UIElement adornedElement) : base(adornedElement) { }
}

public class Item : ISelectObject, IRefreshable, ITextAdornable
{
    public string DebugText => $"Item {Number}";

    public string Text => $"Item {Number}";

    [ThreadStatic]
    private static int Num = 0;

    public int LaneNumber { get; }
    public Item(Conveyor conveyor, int lane, ConveyorShapeProvider shapeProvider)
    {
        LaneNumber = lane;
        Conveyor = conveyor;
        Shape = shapeProvider.CreateConveyorItemEllipse();
        Shape.Tag = this;
        Conveyor.Canvas.Children.Add(Shape);
        var layer = AdornerLayer.GetAdornerLayer(Shape);
        layer.Add(new ItemTextAdorner(Shape));
        AddAge(0);
        Number = Num++;
    }

    public int Number { get; }
    public Shape Shape { get; }

    private double _Age;
    public double Age => _Age;

    public void AddAge(double offset)
    {
        var oldlocation = Location;
        _Age += offset;
        var newLocation = Conveyor.GetItemLocation(this, out var done, out var lane, out var staleAge);
        _Age -= staleAge;
        StaleAge += staleAge;
        if (oldlocation == newLocation)
        {
            Location = oldlocation;
        }
        else
        {
            Lane = lane;
            Location = newLocation;
        }

        if (done)
        {
            Done = true;
        }
    }

    //public double Age
    //{
    //    get => _Age;
    //    set
    //    {
    //        _Age = value;
    //        Location = Conveyor.GetItemLocation(this, out var done, out var segment);
    //        Segment = segment;
    //        if (done)
    //        {
    //            Done = true;
    //        }
    //    }
    //}

    public double StaleAge = 0;

    public bool Moving { get; set; }
    public bool Done { get; set; }
    public Conveyor Conveyor { get; set; }

    public LinkedListNode<ILanePart> Lane { get; set; }

    private Point _Location;
    public Point Location
    {
        get => _Location;
        set => Func.Setter(ref _Location, value, newValue => Shape.Dispatcher.BeginInvoke(SetLocation, newValue));
    }

    private Point[] SelectionBoundsPoints = new Point[1];
    public Point[] GetSelectionBoundsPoints() => SelectionBoundsPoints;

    public ISelectObject? SelectionParent => null;

    public string AdornmentText => Number.ToString();

    private void SetLocation(Point point)
    {
        Shape.SetCenterLocation(point);
        ((ISelectObject)this).SetSelectionPoints(point);
    }
}
