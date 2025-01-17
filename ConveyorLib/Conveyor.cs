﻿using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;

namespace ConveyorLib;

public class Conveyor : ISelectObject, IRefreshable, IAppObject<ConveyorAppApplication>
{
    private static int NextConveyorNumber = 0;

    private bool _IsRunning;
    public bool IsRunning
    {
        get => _IsRunning;
        set => Func.Setter(ref _IsRunning, value, StartIfRunning);
    }

    public int LanesCount { get; init; } = 1;
    public IEnumerable<int> LaneIndexes;

    private void StartIfRunning()
    {
        if (IsRunning)
        {
            Dispatcher = new(new ParameterizedThreadStart(ItemDispatcherThreadAction));
            Dispatcher.SetApartmentState(ApartmentState.STA);
            Dispatcher.Start(this);
        }
    }

    public LinkedList<ConveyorSegment> Segments = new();
    public LinkedList<ConveyorSegmentLane>[] SegmentLanes;
    public LinkedList<ConveyorPoint> Points { get; } = new();
    public LinkedList<IPathPart> PointsAndSegments = new();
    public LinkedList<ConveyorPointLane>[] PointLanes;
    public LinkedList<ILanePart>[] PointAndSegmentLanes;

    public static Conveyor Create(IEnumerable<Point> points, bool isRunning, int lanesCount = 1)
    {
        var conv = new Conveyor(lanesCount);
        double length = 0d;
        bool first = true;
        {
            Point oldPoint = default;
            ConveyorPoint? convPoint = null;
            foreach (var point in points)
            {
                if (first)
                {
                    oldPoint = point;
                    first = false;
                    convPoint = AddPoint(point);
                    convPoint.IsFirst = true;
                    continue;
                }
                var segment = new ConveyorSegment(conv, length, (oldPoint, point));
                oldPoint = point;
                segment.Node = conv.Segments.AddLast(segment);
                segment.ElementsNode = conv.PointsAndSegments.AddLast(segment);

                length += segment.DefinitionLength;

                convPoint = AddPoint(point);
            }
            if (convPoint is not null)
            {
                convPoint.IsLast = true;
            }
        }
        foreach (var segment in conv.Segments)
        {
            segment.CreateLanes();
        }

        //foreach (var point in conv.Points)
        //{
        //    point.PreparePoint();
        //}

        foreach (var point in conv.Points)
        {
            point.CreateLanes();
        }

        foreach (var part in conv.PointsAndSegments)
        {
            part.RegisterLanes();
        }

        foreach (var point in conv.Points)
        {
            point.PrepareLanes();
        }

        //conv.PointsAndSegments.First().RebuildLanes();

        foreach (var part in conv.PointsAndSegments)
        {
            part.UpdateLengths();
        }

        conv.IsRunning = isRunning;

        return conv;

        ConveyorPoint AddPoint(Point point)
        {
            ConveyorPoint cPoint = new(conv);
            cPoint.Node = conv.Points.AddLast(cPoint);
            cPoint.ElementsNode = conv.PointsAndSegments.AddLast(cPoint);
            cPoint.Location = point;
            return cPoint;
        }
    }

    public static void AddToCanvas(Conveyor conveyor, ConveyorCanvasInfo canvasInfo)
    {
        conveyor.Canvas = canvasInfo.Canvas;
        foreach (var segment in conveyor.Segments)
        {
            segment.AddToCanvas(canvasInfo);
        }

        foreach (var point in conveyor.Points)
        {
            point.AddToCanvas(canvasInfo);
        }
    }

    public Canvas? Canvas;
    public double Speed = 20;

    public void SpawnItems(ConveyorShapeProvider shapeProvider, bool? firstOnly = null)
    {
        foreach (var i in LaneIndexes)
        {
            //if ((firstOnly ?? false) && i == 0) continue;
            var item = new Item(this, i, shapeProvider);
            Items[i].Enqueue(item);
            if (firstOnly ?? false) break;
        }
    }

    public Item? GetNextItem(Item currentItem)
    {
        var queue = Items[currentItem.LaneNumber];
        var itemList = queue.Reverse().ToList(); // TODO OPTIMIZE!!!   !!!!
        var idx = itemList.IndexOf(currentItem);
        if (idx < itemList.Count - 1)
        {
            return itemList[idx + 1];
        }
        return null;
    }

    private readonly ConcurrentQueue<Item>[] Items;

    public int Number { get; }

    public string Text => nameof(Conveyor);

    public Point[] GetSelectionBoundsPoints() => Points.SelectMany(x => x.GetSelectionBoundsPoints())
        .Concat(Segments.SelectMany(x => x.GetSelectionBoundsPoints()))
        .Concat(SegmentLanes.SelectMany(x => x.SelectMany(y => y.GetSelectionBoundsPoints()))) // TODO use outer lanes only
        .Concat(PointLanes.SelectMany(x => x.SelectMany(y => y.GetSelectionBoundsPoints()))).ToArray();


    public ISelectObject? SelectionParent => null;

    private Thread? Dispatcher;

    public Conveyor(int lanesCount)
    {
        LanesCount = lanesCount;
        LaneIndexes = Enumerable.Range(0, lanesCount).ToArray();

        SegmentLanes = new LinkedList<ConveyorSegmentLane>[lanesCount];
        PointLanes = new LinkedList<ConveyorPointLane>[lanesCount];
        PointAndSegmentLanes = new LinkedList<ILanePart>[lanesCount];

        Items = new ConcurrentQueue<Item>[lanesCount];

        Number = NextConveyorNumber++;

        foreach (int i in LaneIndexes)
        {
            SegmentLanes[i] = new();
            PointLanes[i] = new();
            PointAndSegmentLanes[i] = new();
            Items[i] = new();
        }
    }

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
                foreach (int i in conveyor.LaneIndexes)
                {
                    foreach (var item in conveyor.Items[i].ToList()) // TODO hier muss das tolist weg - anderen datentypen wählen.
                    {
                        if (!item.Done)
                        {
                            item.AddAge(diff);
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }
    }

    // TODO add an out parameter for stalling
    internal Point GetItemLocation(Item item, out bool done, out LinkedListNode<ILanePart> laneNode, out double staleAge)
    {
        var actualAge = item.Age - item.StaleAge;
        var nextItem = GetNextItem(item);
        if (nextItem is not null)
        {
            var nextAge = nextItem.Age - nextItem.StaleAge;
            if (actualAge + Speed * 2.8 > nextAge) // this needs to be something depending on the speed and size of the items.
            {
                // collision -> avoid
                laneNode = nextItem.Lane;
                var targetAge = nextAge - Speed * 2.8;
                var targetlength = targetAge / 1000 * Speed;
                staleAge = actualAge - targetAge;
                done = false;
                return laneNode.Value.GetPointAbsolute(targetlength);
            }
        }

        double length = actualAge / 1000 * Speed;
        done = false;
        laneNode = item.Lane ?? Segments.First.Value.Lanes[item.LaneNumber].ElementsNode;

        while (laneNode is not null && laneNode.Value.EndLength < length)
        {
            laneNode = laneNode!.Next;
        }

        if (laneNode is null)
        {
            done = true;
            laneNode = Segments.Last.Value.Lanes[item.LaneNumber].ElementsNode;
        }
        if (length < 0)
        {
            length = 0;
        }
        staleAge = 0;
        return laneNode.Value.GetPointAbsolute(length);
    }
}