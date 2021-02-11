// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Text highlights associated with a TextContainer.
//

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media.TextFormatting;
using MS.Internal;

namespace System.Windows.Documents
{
    /// <summary>
    /// Text highlights associated with a TextContainer.
    /// </summary>
    internal class Highlights
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Constructor.
        internal Highlights(ITextContainer textContainer)
        {
            _textContainer = textContainer;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns the value of a property stored on scoping highlight, if any.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        /// <param name="highlightLayerOwnerType">
        /// Type of the matching highlight layer owner.
        /// </param>
        /// <returns>
        /// The highlight value if set on any scoping highlight.  If no property
        /// value is set, returns UnsetValue.
        /// </returns>
        internal virtual object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction, Type highlightLayerOwnerType)
        {
            int layerIndex;
            object value;
            HighlightLayer layer;

            value = AvaloniaProperty.UnsetValue;

            // Take the value on the closest layer.  "Closest" == added first.
            for (layerIndex = 0; layerIndex < this.LayerCount; layerIndex++)
            {
                layer = GetLayer(layerIndex);

                if (layer.OwnerType == highlightLayerOwnerType)
                {
                    value = layer.GetHighlightValue(textPosition, direction);
                    if (value != AvaloniaProperty.UnsetValue)
                        break;
                }
            }

            return value;
        }

        /// <summary>
        /// Returns true iff the indicated content has scoping highlights.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        internal virtual bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction)
        {
            int i;

            for (i = 0; i < this.LayerCount; i++)
            {
                if (GetLayer(i).IsContentHighlighted(textPosition, direction))
                {
                    break;
                }
            }

            return (i < this.LayerCount);
        }

        /// <summary>
        /// Returns the position of the next highlight start or end in an
        /// indicated direction, or null if there is no such position.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        internal virtual StaticTextPointer GetNextHighlightChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            StaticTextPointer changePosition;
            StaticTextPointer closestChangePosition;
            int i;

            closestChangePosition = StaticTextPointer.Null;

            // Calculate the min of the layers' transitions.
            for (i = 0; i < this.LayerCount; i++)
            {
                changePosition = GetLayer(i).GetNextChangePosition(textPosition, direction);

                if (!changePosition.IsNull)
                {
                    if (closestChangePosition.IsNull)
                    {
                        closestChangePosition = changePosition;
                    }
                    else if (direction == LogicalDirection.Forward)
                    {
                        closestChangePosition = StaticTextPointer.Min(closestChangePosition, changePosition);
                    }
                    else
                    {
                        closestChangePosition = StaticTextPointer.Max(closestChangePosition, changePosition);
                    }
                }
            }

            return closestChangePosition;
        }

        /// <summary>
        /// Returns the closest neighboring TextPointer in an indicated
        /// direction where a property value calculated from an embedded
        /// object, scoping text element, or scoping highlight could
        /// change.
        /// </summary>
        /// <param name="textPosition">
        /// Position to query.
        /// </param>
        /// <param name="direction">
        /// Direction of content to query.
        /// </param>
        /// <returns>
        /// If the following symbol is TextPointerContext.EmbeddedElement,
        /// TextPointerContext.ElementBegin, or TextPointerContext.ElementEnd, returns
        /// a TextPointer exactly one symbol distant.
        ///
        /// If the following symbol is TextPointerContext.Text, the distance
        /// of the returned TextPointer is the minimum of the value returned
        /// by textPosition.GetTextLength and the distance to any highlight
        /// start or end edge.
        ///
        /// If the following symbol is TextPointerContext.None, returns null.
        /// </returns>
        internal virtual StaticTextPointer GetNextPropertyChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            StaticTextPointer changePosition;
            StaticTextPointer characterRunEndPosition;

            switch (textPosition.GetPointerContext(direction))
            {
                case TextPointerContext.None:
                    changePosition = StaticTextPointer.Null;
                    break;

                case TextPointerContext.Text:
                    changePosition = GetNextHighlightChangePosition(textPosition, direction);
                    characterRunEndPosition = textPosition.GetNextContextPosition(LogicalDirection.Forward);

                    if (changePosition.IsNull ||
                        characterRunEndPosition.CompareTo(changePosition) < 0)
                    {
                        changePosition = characterRunEndPosition;
                    }

                    break;

                case TextPointerContext.EmbeddedElement:
                case TextPointerContext.ElementStart:
                case TextPointerContext.ElementEnd:
                default:
                    changePosition = textPosition.CreatePointer(+1);
                    break;
            }

            return changePosition;
        }

        /// <summary>
        /// Adds a HighlightLayer to this collection.
        /// </summary>
        /// <param name="highlightLayer">
        /// HighlightLayer to add.
        /// </param>
        internal void AddLayer(HighlightLayer highlightLayer)
        {
            // Delay alloc the layers store.
            if (_layers == null)
            {
                _layers = new ArrayList(1);
            }

            Invariant.Assert(!_layers.Contains(highlightLayer));

            _layers.Add(highlightLayer);

            highlightLayer.Changed += new HighlightChangedEventHandler(OnLayerChanged);

            // Raise initial change event to cover existing content.
            RaiseChangedEventForLayerContent(highlightLayer);
        }

        /// <summary>
        /// Removes a HighlightLayer to this collection.
        /// </summary>
        /// <param name="highlightLayer">
        /// HighlightLayer to add.
        /// </param>
        internal void RemoveLayer(HighlightLayer highlightLayer)
        {
            Invariant.Assert(_layers != null && _layers.Contains(highlightLayer));

            // Raise final change event to cover existing content.
            RaiseChangedEventForLayerContent(highlightLayer);

            highlightLayer.Changed -= new HighlightChangedEventHandler(OnLayerChanged);

            _layers.Remove(highlightLayer);
        }


        /// <summary>
        /// Retrieve a HighlightLayer from this collection
        /// </summary>
        /// <param name="highlightLayerType">
        /// </param>
        /// <returns>
        /// The first added HighlightLayer owned by the type. 
        /// null if no such HighlightLayer can be found. 
        /// </returns>
        internal HighlightLayer GetLayer(Type highlightLayerType)
        {
            int i;
 
            for (i = 0; i < this.LayerCount; i++)
            {
                if (highlightLayerType == GetLayer(i).OwnerType)
                {
                    return GetLayer(i);
                }
            }
            return null;
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        protected ITextContainer TextContainer
        {
            get
            {
                return _textContainer;
            }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Event raised when a highlight is inserted, removed, moved, or
        /// has a local property value change.
        /// </summary>
        internal event HighlightChangedEventHandler Changed;

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Returns a contained HighlightLayer.
        private HighlightLayer GetLayer(int index)
        {
            return (HighlightLayer)_layers[index];
        }

        // Callback for changes on a single layer.
        private void OnLayerChanged(object sender, HighlightChangedEventArgs args)
        {
            // Forward on the event to any listeners.
            if (this.Changed != null)
            {
                Changed(this, args);
            }
        }

        // Raises initial or final change event for added/removed layers.
        private void RaiseChangedEventForLayerContent(HighlightLayer highlightLayer)
        {
            List<TextSegment> ranges;
            StaticTextPointer highlightTransitionPosition;
            StaticTextPointer highlightRangeStart;

            if (this.Changed != null)
            {
                // Build a list of all highlights in this layer -- they're all
                // going to "change" as the layer is added/removed.

                ranges = new List<TextSegment>();

                highlightTransitionPosition = _textContainer.CreateStaticPointerAtOffset(0);

                while (true)
                {
                    // Move to the next highlight start.
                    if (!highlightLayer.IsContentHighlighted(highlightTransitionPosition, LogicalDirection.Forward))
                    {
                        highlightTransitionPosition = highlightLayer.GetNextChangePosition(highlightTransitionPosition, LogicalDirection.Forward);

                        // No more highlights?
                        if (highlightTransitionPosition.IsNull)
                            break;
                    }

                    highlightRangeStart = highlightTransitionPosition;
                    highlightTransitionPosition = highlightLayer.GetNextChangePosition(highlightTransitionPosition, LogicalDirection.Forward);
                    Invariant.Assert(!highlightTransitionPosition.IsNull, "Highlight start not followed by highlight end!");

                    ranges.Add(new TextSegment(highlightRangeStart.CreateDynamicTextPointer(LogicalDirection.Forward), highlightTransitionPosition.CreateDynamicTextPointer(LogicalDirection.Forward)));
                }

                if (ranges.Count > 0)
                {
                    Changed(this, new LayerHighlightChangedEventArgs(new ReadOnlyCollection<TextSegment>(ranges), highlightLayer.OwnerType));
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        // Count of contained HighlightLayers.
        private int LayerCount
        {
            get
            {
                return (_layers == null) ? 0 : _layers.Count;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // EventArgs for the Changed event.
        private class LayerHighlightChangedEventArgs : HighlightChangedEventArgs
        {
            // Constructor.
            internal LayerHighlightChangedEventArgs(ReadOnlyCollection<TextSegment> ranges, Type ownerType)
            {
                _ranges = ranges;
                _ownerType = ownerType;
            }

            // List of changed ranges.
            internal override IList Ranges
            {
                get
                {
                    return _ranges;
                }
            }

            // Type identifying the owner of the changed layer.
            internal override Type OwnerType
            {
                get
                {
                    return _ownerType;
                }
            }

            // List of changed ranges.
            private readonly ReadOnlyCollection<TextSegment> _ranges;

            // Type identifying the owner of the changed layer.
            private readonly Type _ownerType;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Associated TextContainer.
        private readonly ITextContainer _textContainer;

        // List of contained HighlightLayers.
        private ArrayList _layers;

        #endregion Private Fields
    }
}
