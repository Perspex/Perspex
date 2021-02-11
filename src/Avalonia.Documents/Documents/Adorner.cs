// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Info needed to draw decorations on objects
//
//              See spec at AdornerLayer Spec.htm
// 

using System;
using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace System.Windows.Documents
{
    /// <summary>
    /// An Adorner is a UIElement "attached" to another UIElement.  Adorners render
    /// on an AdornerLayer, which puts them higher in the Z-order than the element
    /// to which they are attached so they visually appear on top of that element.
    /// By default, the AdornerLayer positions an Adorner at the upper-left corner
    /// of the element it adorns.  However, the AdornerLayer passes the Adorner its
    /// proposed transform, and the Adorner can modify that proposed transform as it
    /// wishes.
    /// 
    /// Since Adorners are UIElements, they can respond to input events.
    /// </summary>
    public abstract class Adorner : Control
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        static Adorner()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Adorner>(false);
        }
 
        /// <summary>
        /// Constructor
        /// </summary>
        protected Adorner(Visual adornedElement)
        {
            if (adornedElement == null)
                throw new ArgumentNullException("adornedElement");

            _adornedElement = adornedElement;
            _isClipEnabled = false;

            // Bug 1383424: We need to make sure our FlowDirection is always that of our adorned element.
            // Need to allow derived class constructor to execute first
            Dispatcher.UIThread.Post(CreateFlowDirectionBinding);

        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Measure adorner.  Default behavior is to size to match the adorned element.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            var desiredSize = new Size(AdornedElement.Bounds.Width, AdornedElement.Bounds.Height);

            foreach (var ch in VisualChildren)
            {
                if (ch is ILayoutable controlChild)
                {
                    controlChild.Measure(desiredSize);
                }
            }

            return desiredSize;
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Adorners don't always want to be transformed in the same way as the elements they
        /// adorn.  Adorners which adorn points, such as resize handles, want to be translated
        /// and rotated but not scaled.  Adorners adorning an object, like a marquee, may want
        /// all transforms.  This method is called by AdornerLayer to allow the adorner to
        /// filter out the transforms it doesn't want and return a new transform with just the
        /// transforms it wants applied.  An adorner can also add an additional translation
        /// transform at this time, allowing it to be positioned somewhere other than the upper
        /// left corner of its adorned element.
        /// </summary>
        /// <param name="transform">The transform applied to the object the adorner adorns</param>
        /// <returns>Transform to apply to the adorner</returns>
        public virtual ITransform GetDesiredTransform(ITransform transform)
        {
            return transform;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        #region Public Properties

        /// <summary>
        /// Gets or sets the clip of this Visual.
        /// Needed by AdornerLayer
        /// </summary>
        internal Geometry AdornerClip
        {
            get
            {
                return Clip;
            }
            set
            {
                Clip = value;
            }
        }


        /// <summary>
        /// Gets or sets the transform of this Visual.
        /// Needed by AdornerLayer
        /// </summary>
        internal ITransform AdornerTransform
        {
            get
            {
                return RenderTransform;
            }
            set
            {
                RenderTransform = value;
            }
        }
        
        /// <summary>
        /// UIElement this Adorner adorns.
        /// </summary>
        public Visual AdornedElement
        {
            get { return _adornedElement; }
        }

        /// <summary>
        /// If set to true, the adorner will be clipped using the same clip geometry as the
        /// AdornedElement.  This is expensive, and therefore should not normally be used.
        /// Defaults to false.
        /// </summary>
        public bool IsClipEnabled
        {
            get
            {
                return _isClipEnabled;
            }

            set
            {
                _isClipEnabled = value;
                InvalidateArrange();
                AdornerLayer.GetAdornerLayer(_adornedElement).InvalidateArrange();
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Callback for binding the FlowDirection property.
        private void CreateFlowDirectionBinding()
        {
            // TODO: This is probably not the right way to mimic a binding from code
            AdornedElement.GetObservable(Inline.FlowDirectionProperty)
                .Subscribe(value => SetValue(Inline.FlowDirectionProperty, value));
        }

        /// <summary>
        /// Says if the Adorner needs update based on the 
        /// previously cached size if the AdornedElement.
        /// </summary>
        internal virtual bool NeedsUpdate(Size oldSize)
        {
            var adornedSize = AdornedElement.Bounds.Size;
            return !MathUtilities.AreClose(adornedSize.Width, oldSize.Width)
                   || !MathUtilities.AreClose(adornedSize.Height, oldSize.Height);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly Visual _adornedElement;
        private bool _isClipEnabled;

        #endregion Private Fields
    }
}

