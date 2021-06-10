// Ported from https://github.com/HandyOrg/HandyControl/blob/master/src/Shared/HandyControl_Shared/Controls/Panel/RelativePanel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an area within which you can position and align child objects in relation to each other or the parent panel.
    /// </summary>
    public partial class RelativePanel : Panel
    {
        private readonly Graph _childGraph;

        public RelativePanel() => _childGraph = new Graph();

        private Layoutable? GetDependencyElement(AvaloniaProperty property, AvaloniaObject child)
        {
            var dependency = child.GetValue(property);

            if (dependency is Layoutable layoutable)
            {
                if (Children.Contains((ILayoutable)layoutable))
                    return layoutable;

                throw new ArgumentException($"RelativePanel error: Element does not exist in the current context: {property.Name}");
            }

            return null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _childGraph.Clear();
            foreach (Layoutable child in Children)
            {
                if (child == null)
                    continue;
                var node = _childGraph.AddNode(child);

                node.AlignLeftWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignLeftWithProperty, child));
                node.AlignTopWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignTopWithProperty, child));
                node.AlignRightWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignRightWithProperty, child));
                node.AlignBottomWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignBottomWithProperty, child));

                node.LeftOfNode = _childGraph.AddLink(node, GetDependencyElement(LeftOfProperty, child));
                node.AboveNode = _childGraph.AddLink(node, GetDependencyElement(AboveProperty, child));
                node.RightOfNode = _childGraph.AddLink(node, GetDependencyElement(RightOfProperty, child));
                node.BelowNode = _childGraph.AddLink(node, GetDependencyElement(BelowProperty, child));

                node.AlignHorizontalCenterWith = _childGraph.AddLink(node, GetDependencyElement(AlignHorizontalCenterWithProperty, child));
                node.AlignVerticalCenterWith = _childGraph.AddLink(node, GetDependencyElement(AlignVerticalCenterWithProperty, child));

            }
            _childGraph.Measure(availableSize);

            _childGraph.Reset(false);
            var calcWidth = Width.IsNaN() && HorizontalAlignment != HorizontalAlignment.Stretch;
            var calcHeight = Height.IsNaN() && VerticalAlignment != VerticalAlignment.Stretch;

            var boundingSize = _childGraph.GetBoundingSize(calcWidth, calcHeight);
            _childGraph.Reset();
            _childGraph.Measure(boundingSize);
            return boundingSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _childGraph.GetNodes().Do(node => node.Arrange(arrangeSize));
            return arrangeSize;
        }

        private class GraphNode
        {
            public bool Measured { get; set; }

            public Layoutable Element { get; }

            private bool HorizontalOffsetFlag { get; set; }

            private bool VerticalOffsetFlag { get; set; }

            private Size BoundingSize { get; set; }

            public Size OriginDesiredSize { get; set; }

            public double Left { get; set; } = double.NaN;

            public double Top { get; set; } = double.NaN;

            public double Right { get; set; } = double.NaN;

            public double Bottom { get; set; } = double.NaN;

            public HashSet<GraphNode> OutgoingNodes { get; }

            public GraphNode? AlignLeftWithNode { get; set; }

            public GraphNode? AlignTopWithNode { get; set; }

            public GraphNode? AlignRightWithNode { get; set; }

            public GraphNode? AlignBottomWithNode { get; set; }

            public GraphNode? LeftOfNode { get; set; }

            public GraphNode? AboveNode { get; set; }

            public GraphNode? RightOfNode { get; set; }

            public GraphNode? BelowNode { get; set; }

            public GraphNode? AlignHorizontalCenterWith { get; set; }

            public GraphNode? AlignVerticalCenterWith { get; set; }

            public GraphNode(Layoutable element)
            {
                OutgoingNodes = new HashSet<GraphNode>();
                Element = element;
            }

            public void Arrange(Size arrangeSize) => Element.Arrange(new Rect(Left, Top, Math.Max(arrangeSize.Width - Left - Right, 0), Math.Max(arrangeSize.Height - Top - Bottom, 0)));

            public void Reset(bool clearPos)
            {
                if (clearPos)
                {
                    Left = double.NaN;
                    Top = double.NaN;
                    Right = double.NaN;
                    Bottom = double.NaN;
                }

                Measured = false;
            }

            public Size GetBoundingSize()
            {
                if (Left < 0 || Top < 0) return default;
                if (Measured)
                    return BoundingSize;

                if (!OutgoingNodes.Any())
                {
                    BoundingSize = Element.DesiredSize;
                    Measured = true;
                }
                else
                {
                    BoundingSize = GetBoundingSize(this, Element.DesiredSize, OutgoingNodes);
                    Measured = true;
                }

                return BoundingSize;
            }

            private static Size GetBoundingSize(GraphNode prevNode, Size prevSize, IEnumerable<GraphNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.Measured || !node.OutgoingNodes.Any())
                    {
                        if (prevNode.LeftOfNode != null && prevNode.LeftOfNode == node ||
                            prevNode.RightOfNode != null && prevNode.RightOfNode == node)
                        {
                            prevSize = prevSize.WithWidth(prevSize.Width + node.BoundingSize.Width);
                            if (GetAlignHorizontalCenterWithPanel(node.Element) || node.HorizontalOffsetFlag)
                            {
                                prevSize = prevSize.WithWidth(prevSize.Width + prevNode.OriginDesiredSize.Width);
                                prevNode.HorizontalOffsetFlag = true;
                            }
                            if (node.VerticalOffsetFlag)
                            {
                                prevNode.VerticalOffsetFlag = true;
                            }
                        }

                        if (prevNode.AboveNode != null && prevNode.AboveNode == node ||
                            prevNode.BelowNode != null && prevNode.BelowNode == node)
                        {
                            prevSize = prevSize.WithHeight(prevSize.Height + node.BoundingSize.Height);
                            if (GetAlignVerticalCenterWithPanel(node.Element) || node.VerticalOffsetFlag)
                            {
                                prevSize = prevSize.WithHeight(prevSize.Height + node.OriginDesiredSize.Height);
                                prevNode.VerticalOffsetFlag = true;
                            }
                            if (node.HorizontalOffsetFlag)
                            {
                                prevNode.HorizontalOffsetFlag = true;
                            }
                        }
                    }
                    else
                    {
                        return GetBoundingSize(node, prevSize, node.OutgoingNodes);
                    }
                }

                return prevSize;
            }
        }

        private class Graph
        {
            private readonly Dictionary<AvaloniaObject, GraphNode> _nodeDic;

            private Size AvailableSize { get; set; }

            public Graph() => _nodeDic = new Dictionary<AvaloniaObject, GraphNode>();

            public IEnumerable<GraphNode> GetNodes() => _nodeDic.Values;

            public void Clear()
            {
                AvailableSize = new Size();
                _nodeDic.Clear();
            }

            public void Reset(bool clearPos = true) => _nodeDic.Values.Do(node => node.Reset(clearPos));

            public GraphNode? AddLink(GraphNode from, Layoutable? to)
            {
                if (to == null)
                    return null;

                GraphNode nodeTo;
                if (_nodeDic.ContainsKey(to))
                {
                    nodeTo = _nodeDic[to];
                }
                else
                {
                    nodeTo = new GraphNode(to);
                    _nodeDic[to] = nodeTo;
                }

                from.OutgoingNodes.Add(nodeTo);
                return nodeTo;
            }

            public GraphNode AddNode(Layoutable value)
            {
                if (!_nodeDic.ContainsKey(value))
                {
                    var node = new GraphNode(value);
                    _nodeDic.Add(value, node);
                    return node;
                }

                return _nodeDic[value];
            }

            public void Measure(Size availableSize)
            {
                AvailableSize = availableSize;
                Measure(_nodeDic.Values, null);
            }

            private void Measure(IEnumerable<GraphNode> nodes, HashSet<AvaloniaObject>? set)
            {
                set ??= new HashSet<AvaloniaObject>();

                foreach (var node in nodes)
                {
                    if (!node.Measured && !node.OutgoingNodes.Any())
                    {
                        MeasureChild(node);
                        continue;
                    }
                    
                    if (node.OutgoingNodes.All(item => item.Measured))
                    {
                        MeasureChild(node);
                        continue;
                    }
                    
                    if (!set.Add(node.Element))
                        throw new Exception("RelativePanel error: Circular dependency detected. Layout could not complete.");
                    
                    Measure(node.OutgoingNodes, set);

                    if (!node.Measured)
                    {
                        MeasureChild(node);
                    }
                }
            }

            private void MeasureChild(GraphNode node)
            {
                var child = node.Element;
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                node.OriginDesiredSize = child.DesiredSize;

                var alignLeftWithPanel = GetAlignLeftWithPanel(child);
                var alignTopWithPanel = GetAlignTopWithPanel(child);
                var alignRightWithPanel = GetAlignRightWithPanel(child);
                var alignBottomWithPanel = GetAlignBottomWithPanel(child);

                if (alignLeftWithPanel)
                    node.Left = 0;
                if (alignTopWithPanel)
                    node.Top = 0;
                if (alignRightWithPanel)
                    node.Right = 0;
                if (alignBottomWithPanel)
                    node.Bottom = 0;

                if (node.AlignLeftWithNode != null)
                {
                    node.Left = node.Left.IsNaN() ? node.AlignLeftWithNode.Left : node.AlignLeftWithNode.Left * 0.5;
                }

                if (node.AlignTopWithNode != null)
                {
                    node.Top = node.Top.IsNaN() ? node.AlignTopWithNode.Top : node.AlignTopWithNode.Top * 0.5;
                }

                if (node.AlignRightWithNode != null)
                {
                    node.Right = node.Right.IsNaN()
                        ? node.AlignRightWithNode.Right
                        : node.AlignRightWithNode.Right * 0.5;
                }

                if (node.AlignBottomWithNode != null)
                {
                    node.Bottom = node.Bottom.IsNaN()
                        ? node.AlignBottomWithNode.Bottom
                        : node.AlignBottomWithNode.Bottom * 0.5;
                }

                var availableHeight = AvailableSize.Height - node.Top - node.Bottom;
                if (availableHeight.IsNaN())
                {
                    availableHeight = AvailableSize.Height;

                    if (!node.Top.IsNaN() && node.Bottom.IsNaN())
                    {
                        availableHeight -= node.Top;
                    }
                    else if (node.Top.IsNaN() && !node.Bottom.IsNaN())
                    {
                        availableHeight -= node.Bottom;
                    }
                }

                var availableWidth = AvailableSize.Width - node.Left - node.Right;
                if (availableWidth.IsNaN())
                {
                    availableWidth = AvailableSize.Width;

                    if (!node.Left.IsNaN() && node.Right.IsNaN())
                    {
                        availableWidth -= node.Left;
                    }
                    else if (node.Left.IsNaN() && !node.Right.IsNaN())
                    {
                        availableWidth -= node.Right;
                    }
                }

                child.Measure(new Size(Math.Max(availableWidth, 0), Math.Max(availableHeight, 0)));
                var childSize = child.DesiredSize;

                if (node.LeftOfNode != null && node.Left.IsNaN())
                {
                    node.Left = node.LeftOfNode.Left - childSize.Width;
                }

                if (node.AboveNode != null && node.Top.IsNaN())
                {
                    node.Top = node.AboveNode.Top - childSize.Height;
                }

                if (node.RightOfNode != null)
                {
                    if (node.Right.IsNaN())
                    {
                        node.Right = node.RightOfNode.Right - childSize.Width;
                    }

                    if (node.Left.IsNaN())
                    {
                        node.Left = AvailableSize.Width - node.RightOfNode.Right;
                    }
                }

                if (node.BelowNode != null)
                {
                    if (node.Bottom.IsNaN())
                    {
                        node.Bottom = node.BelowNode.Bottom - childSize.Height;
                    }

                    if (node.Top.IsNaN())
                    {
                        node.Top = AvailableSize.Height - node.BelowNode.Bottom;
                    }
                }

                if (node.AlignHorizontalCenterWith != null)
                {
                    var halfWidthLeft = (AvailableSize.Width + node.AlignHorizontalCenterWith.Left - node.AlignHorizontalCenterWith.Right - childSize.Width) * 0.5;
                    var halfWidthRight = (AvailableSize.Width - node.AlignHorizontalCenterWith.Left + node.AlignHorizontalCenterWith.Right - childSize.Width) * 0.5;

                    if (node.Left.IsNaN())
                        node.Left = halfWidthLeft;
                    else
                        node.Left = (node.Left + halfWidthLeft) * 0.5;

                    if (node.Right.IsNaN())
                        node.Right = halfWidthRight;
                    else
                        node.Right = (node.Right + halfWidthRight) * 0.5;
                }

                if (node.AlignVerticalCenterWith != null)
                {
                    var halfHeightTop = (AvailableSize.Height + node.AlignVerticalCenterWith.Top - node.AlignVerticalCenterWith.Bottom - childSize.Height) * 0.5;
                    var halfHeightBottom = (AvailableSize.Height - node.AlignVerticalCenterWith.Top + node.AlignVerticalCenterWith.Bottom - childSize.Height) * 0.5;

                    if (node.Top.IsNaN())
                        node.Top = halfHeightTop;
                    else
                        node.Top = (node.Top + halfHeightTop) * 0.5;

                    if (node.Bottom.IsNaN())
                        node.Bottom = halfHeightBottom;
                    else
                        node.Bottom = (node.Bottom + halfHeightBottom) * 0.5;
                }

                if (GetAlignHorizontalCenterWithPanel(child))
                {
                    var halfSubWidth = (AvailableSize.Width - childSize.Width) * 0.5;

                    if (node.Left.IsNaN())
                        node.Left = halfSubWidth;
                    else
                        node.Left = (node.Left + halfSubWidth) * 0.5;

                    if (node.Right.IsNaN())
                        node.Right = halfSubWidth;
                    else
                        node.Right = (node.Right + halfSubWidth) * 0.5;
                }

                if (GetAlignVerticalCenterWithPanel(child))
                {
                    var halfSubHeight = (AvailableSize.Height - childSize.Height) * 0.5;

                    if (node.Top.IsNaN())
                        node.Top = halfSubHeight;
                    else
                        node.Top = (node.Top + halfSubHeight) * 0.5;

                    if (node.Bottom.IsNaN())
                        node.Bottom = halfSubHeight;
                    else
                        node.Bottom = (node.Bottom + halfSubHeight) * 0.5;
                }

                if (node.Left.IsNaN())
                {
                    if (!node.Right.IsNaN())
                        node.Left = AvailableSize.Width - node.Right - childSize.Width;
                    else
                    {
                        node.Left = 0;
                        node.Right = AvailableSize.Width - childSize.Width;
                    }
                }
                else if (!node.Left.IsNaN() && node.Right.IsNaN())
                {
                    node.Right = AvailableSize.Width - node.Left - childSize.Width;
                }

                if (node.Top.IsNaN())
                {
                    if (!node.Bottom.IsNaN())
                        node.Top = AvailableSize.Height - node.Bottom - childSize.Height;
                    else
                    {
                        node.Top = 0;
                        node.Bottom = AvailableSize.Height - childSize.Height;
                    }
                }
                else if (!node.Top.IsNaN() && node.Bottom.IsNaN())
                {
                    node.Bottom = AvailableSize.Height - node.Top - childSize.Height;
                }

                node.Measured = true;
            }

            public Size GetBoundingSize(bool calcWidth, bool calcHeight)
            {
                var boundingSize = new Size();

                foreach (var node in _nodeDic.Values)
                {
                    var size = node.GetBoundingSize();
                    boundingSize = boundingSize.WithWidth(Math.Max(boundingSize.Width, size.Width));
                    boundingSize = boundingSize.WithHeight(Math.Max(boundingSize.Height, size.Height));
                }

                boundingSize = boundingSize.WithWidth(calcWidth ? boundingSize.Width : AvailableSize.Width);
                boundingSize = boundingSize.WithHeight(calcHeight ? boundingSize.Height : AvailableSize.Height);
                return boundingSize;
            }
        }
    }

    internal static partial class Extensions
    {
        /// <summary>
        ///     Returns a value that indicates whether the specified value is not a number ().
        /// </summary>
        /// <param name="d">A double-precision floating-point number.</param>
        /// <returns>true if  evaluates to ; otherwise, false.</returns>
        public static bool IsNaN(this double d)
        {
            return double.IsNaN(d);
        }

        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> predicate)
        {
            var enumerable = source as IList<TSource> ?? source.ToList();
            foreach (var item in enumerable)
            {
                predicate.Invoke(item);
            }

            return enumerable;
        }
    }
}
