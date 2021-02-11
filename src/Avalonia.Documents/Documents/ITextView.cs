// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An interface representing the presentation of an ITextContainer.
//

using System.ComponentModel;            // AsyncCompletedEventArgs
using System.Collections.ObjectModel;   // ReadOnlyCollection           
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting; // GlyphRun

namespace System.Windows.Documents
{
    /// <summary>
    /// The TextView class exposes presentation information for 
    /// a TextContainer. Its methods reveal document structure, including 
    /// line layout, hit testing, and character bounding boxes.
    ///
    /// Layouts that support TextView must implement the IServiceProvider 
    /// interface, and support GetService(typeof(TextView)) method calls.
    /// </summary>
    internal interface ITextView
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns an ITextPointer that matches the supplied Point 
        /// in this TextView.
        /// </summary>
        /// <param name="point">
        /// Point in pixel coordinates to test.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method should return the closest position as 
        /// calculated by the control's heuristics. 
        /// If false, this method should return null position, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the point.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <remarks>
        /// If there is no position that matches the supplied point and 
        /// snapToText is True, this method returns an ITextPointer 
        /// that is closest to the Point. 
        /// However, If snapToText is False, the method returns NULL if the 
        /// supplied point does not fall within the bounding box of 
        /// a character.
        /// </remarks>
        ITextPointer GetTextPositionFromPoint(Point point, bool snapToText);

        /// <summary>
        /// Retrieves the height and offset of the object or character 
        /// represented by the given TextPointer.
        /// </summary>
        /// <param name="position">
        /// Position of an object/character.
        /// </param>
        /// <returns>
        /// The height and offset of the object or character 
        /// represented by the given TextPointer.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        /// <remarks>
        /// The Width of the returned rectangle is always 0.
        /// 
        /// If the content at the specified position is empty, then this 
        /// method will return the expected height of a character placed 
        /// at the specified position.
        /// </remarks>
        Rect GetRectangleFromTextPosition(ITextPointer position);

        /// <summary>
        /// Retrieves the height and offset of the object or character 
        /// represented by the given TextPointer.
        /// </summary>
        /// <param name="position">
        /// Position of an object/character.
        /// </param>
        /// <param name="transform">
        /// Transform to be applied to returned Rect
        /// </param>
        /// <returns>
        /// The height and offset of the object or character 
        /// represented by the given TextPointer.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        /// <remarks>
        /// The Width of the returned rectangle is always 0.
        /// If the content at the specified position is empty, then this 
        /// method will return the expected height of a character placed 
        /// at the specified position.
        /// This rectangle returned is completely untransformed to any ancestors.
        /// The transform parameter contains the aggregate of transforms that must be 
        /// applied to the rectangle.
        /// </remarks>
        Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform);

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <returns>Geometry object containing tight bound.</returns>
        Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition);

        /// <summary>
        /// Returns an ITextPointer matching the given position 
        /// advanced by the given number of lines.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="suggestedX">
        /// The suggestedX parameter is the suggested X offset, in pixels, of 
        /// the TextPointer on the destination line; the function returns the 
        /// position whose offset is closest to suggestedX.
        /// </param>
        /// <param name="count">
        /// Number of lines to advance. Negative means move backwards.
        /// </param>
        /// <param name="newSuggestedX">
        /// newSuggestedX is the offset at the position moved (useful when moving 
        /// between columns or pages).
        /// </param>
        /// <param name="linesMoved">
        /// linesMoved indicates the number of lines moved, which may be less 
        /// than count if there is no more content.
        /// </param>
        /// <returns>
        /// ITextPointer matching the given position advanced by the 
        /// given number of lines.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        /// <remarks>
        /// The count parameter may be negative, which means move backwards. 
        /// 
        /// If count is larger than the number of available lines in that 
        /// direction, then the returned position will be on the last line 
        /// (or first line if count is negative).
        /// 
        /// If suggestedX is Double.NaN, then it will be ignored.
        /// </remarks>
        ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved);

        /// <summary>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="suggestedOffset"></param>
        /// <param name="count"></param>
        /// <param name="newSuggestedOffset"></param>
        /// <param name="pagesMoved"></param>
        /// <returns></returns>
        ITextPointer GetPositionAtNextPage(ITextPointer position, Point suggestedOffset, int count, out Point newSuggestedOffset, out int pagesMoved);

        /// <summary>
        /// Determines if the given position is at the edge of a caret unit 
        /// in the specified direction.
        /// </summary>
        /// <param name="position">
        /// Position to test.
        /// </param>
        /// <returns>
        /// Returns true if the specified position precedes or follows 
        /// the first or last code point of a caret unit, respectively.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <remarks>
        /// "Caret Unit" is a group of one or more Unicode code points that 
        /// map to a single rendered glyph.
        /// </remarks>
        bool IsAtCaretUnitBoundary(ITextPointer position);

        /// <summary>
        /// Finds the next position at the edge of a caret unit in 
        /// specified direction.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <param name="direction">
        /// If Forward, this method returns the "caret unit" position following 
        /// the initial position.
        /// If Backward, this method returns the caret unit" position preceding 
        /// the initial position.
        /// </param>
        /// <returns>
        /// The next caret unit break position in specified direction.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        /// <remarks>
        /// "Caret Unit" is a group of one or more Unicode code points that 
        /// map to a single rendered glyph.
        /// 
        /// If the given position is located between two caret units, this 
        /// method returns a new position located at the opposite edge of 
        /// the caret unit in the indicated direction.
        /// If position is located within a group of Unicode code points that 
        /// map to a single caret unit, this method returns a new position at 
        /// the edge of the caret unit indicated by direction.
        /// If position is located at the beginning or end of content -- there 
        /// is no content in the indicated direction -- then this method returns 
        /// position at the same location as the given position.
        /// </remarks>
        ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction);

        /// <summary>
        /// Returns the position at the edge of a caret unit after backspacing.
        /// </summary>
        /// <param name="position">
        /// Initial text position of an object/character.
        /// </param>
        /// <returns>
        /// The the position at the edge of a caret unit after backspacing.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position);

        /// <summary>
        /// Returns a TextRange that spans the line on which the given 
        /// position is located.
        /// </summary>
        /// <param name="position">
        /// Any oriented text position on the line.
        /// </param>
        /// <returns>
        /// TextRange that spans the line on which the given position is located.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        TextSegment GetLineRange(ITextPointer position);

        /// <summary>
        /// Provides a collection of glyph properties corresponding to runs 
        /// of Unicode code points.
        /// </summary>
        /// <param name="start">
        /// A position preceding the first code point to examine.
        /// </param>
        /// <param name="end">
        /// A position following the last code point to examine.
        /// </param>
        /// <returns>
        /// A collection of glyph property runs.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <remarks>
        /// A "glyph" in this context is the lowest level rendered representation 
        /// of text.  Each entry in the output array describes a constant run 
        /// of properties on the glyphs corresponding to a range of Unicode 
        /// code points. With this array, it's possible to enumerate the glyph 
        /// properties for each code point in the specified text run.
        /// </remarks>
        ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end);

        /// <summary>
        /// Returns whether the position is contained in this view.
        /// </summary>
        /// <param name="position">
        /// A position to test.
        /// </param>
        /// <returns>
        /// True if TextView contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// Throws InvalidOperationException if IsValid is false.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if incoming position is not 
        /// part of this TextView (should call Contains first to check).
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Throws ArgumentNullException if position is invalid.
        /// </exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException">
        /// Throws InvalidEnumArgumentException if an invalid enum value for 
        /// direction is passed.
        /// </exception>
        /// <remarks>
        /// This method is used for multi-view (paginated) scenarios, 
        /// when a position is not guaranteed to be in the current view.
        /// </remarks>
        bool Contains(ITextPointer position);

        /// <summary>
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        void BringPositionIntoViewAsync(ITextPointer position, object userState);

        /// <summary>
        /// </summary>
        /// <param name="point">Point in pixel coordinates.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        void BringPointIntoViewAsync(Point point, object userState);

        /// <summary>
        /// </summary>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedX">
        /// The suggestedX parameter is the suggested X offset, in pixels, of 
        /// the TextPointer on the destination line.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        void BringLineIntoViewAsync(ITextPointer position, double suggestedX, int count, object userState);

        /// <summary>
        /// </summary>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedOffset">
        /// The suggestedX parameter is the suggested X offset, in pixels, of 
        /// the TextPointer on the destination line.
        /// </param>
        /// <param name="count">Number of pages to advance. Negative means move backwards.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        void BringPageIntoViewAsync(ITextPointer position, Point suggestedOffset, int count, object userState);
        
        /// <summary>
        /// Cancels all asynchronous calls made with the given userState. 
        /// If userState is NULL, all asynchronous calls are cancelled.
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        void CancelAsync(object userState);

        /// <summary>
        /// Ensures the TextView is in a clean layout state and that it is 
        /// possible to retrieve layout data.
        /// </summary>
        /// <remarks>
        /// This method may be expensive, because it may lead to a full 
        /// layout update.
        /// </remarks>
        bool Validate();

        /// <summary>
        /// Ensures this ITextView has a clean layout at the specified Point.
        /// </summary>
        /// <param name="point">
        /// Location to validate.
        /// </param>
        /// <returns>
        /// True if the Point is validated, false otherwise.
        /// </returns>
        /// <remarks>
        /// Use this method before calling GetTextPositionFromPoint.
        /// </remarks>
        bool Validate(Point point);

        /// <summary>
        /// Ensures this ITextView has a clean layout at the specified ITextPointer.
        /// </summary>
        /// <param name="position">
        /// Position to validate.
        /// </param>
        /// <returns>
        /// True if the position is validated, false otherwise.
        /// </returns>
        /// <remarks>
        /// Use this method before calling any of the methods on this class that
        /// take a ITextPointer parameter.
        /// </remarks>
        bool Validate(ITextPointer position);

        /// <summary>
        /// Called by the TextEditor after receiving user input.
        /// Implementors of this method should balance for minimum latency
        /// for the next few seconds.
        /// </summary>
        /// <remarks>
        /// For example, during the next few seconds it would be
        /// appropriate to examine much smaller chunks of text during background
        /// layout calculations, so that the latency of a keystroke repsonse is
        /// minimal.
        /// </remarks>
        void ThrottleBackgroundTasksForUserInput();

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// A UIElement owning this text view. All coordinates are calculated relative to this element.
        /// </summary>
        Visual RenderScope { get; }

        /// <summary>
        /// The container for the text being displayed in this view. 
        /// TextPositions refer to positions within this TextContainer.
        /// </summary>
        ITextContainer TextContainer { get; }

        /// <summary>
        /// Whether the TextView object is in a valid layout state.
        /// </summary>
        /// <remarks>
        /// If False, Validate must be called before calling any other 
        /// method on TextView.
        /// </remarks>
        bool IsValid { get; }

        /// <summary>
        /// Whether the TextView renders its own selection
        /// </summary>
        bool RendersOwnSelection { get; }

        /// <summary>
        /// Collection of TextSegments representing content of the TextView.
        /// </summary>
        ReadOnlyCollection<TextSegment> TextSegments { get; }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// Fired when a BringPositionIntoViewAsync call has completed.
        /// </summary>
        event BringPositionIntoViewCompletedEventHandler BringPositionIntoViewCompleted;

        /// <summary>
        /// Fired when a BringPointIntoViewAsync call has completed.
        /// </summary>
        event BringPointIntoViewCompletedEventHandler BringPointIntoViewCompleted;

        /// <summary>
        /// Fired when a BringLineIntoViewAsync call has completed.
        /// </summary>
        event BringLineIntoViewCompletedEventHandler BringLineIntoViewCompleted;

        /// <summary>
        /// Fired when a BringPageIntoViewAsync call has completed.
        /// </summary>
        event BringPageIntoViewCompletedEventHandler BringPageIntoViewCompleted;

        /// <summary>
        /// Fired when TextView is updated and becomes valid.
        /// </summary>
        event EventHandler Updated;

        #endregion Internal Events
    }

    /// <summary>
    /// BringPositionIntoViewCompleted event handler.
    /// </summary>
    internal delegate void BringPositionIntoViewCompletedEventHandler(object sender, BringPositionIntoViewCompletedEventArgs e);

    /// <summary>
    /// BringPointIntoViewCompleted event handler.
    /// </summary>
    internal delegate void BringPointIntoViewCompletedEventHandler(object sender, BringPointIntoViewCompletedEventArgs e);

    /// <summary>
    /// BringLineIntoViewCompleted event handler.
    /// </summary>
    internal delegate void BringLineIntoViewCompletedEventHandler(object sender, BringLineIntoViewCompletedEventArgs e);

    /// <summary>
    /// BringLineIntoViewCompleted event handler.
    /// </summary>
    internal delegate void BringPageIntoViewCompletedEventHandler(object sender, BringPageIntoViewCompletedEventArgs e);

    /// <summary>
    /// Event arguments for the BringPositionIntoViewCompleted event.
    /// </summary>
    internal class BringPositionIntoViewCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <param name="succeeded">Whether operation was successful.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public BringPositionIntoViewCompletedEventArgs(ITextPointer position, bool succeeded, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            //_position = position;
            //_succeeded = succeeded;
        }

        // Position of an object/character.
        //private readonly ITextPointer _position;

        // Whether operation was successful.
        //private readonly bool _succeeded;
    }

    /// <summary>
    /// Event arguments for the BringPointIntoViewCompleted event.
    /// </summary>
    internal class BringPointIntoViewCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="point">Point in pixel coordinates.</param>
        /// <param name="position">A text position and its orientation matching or closest to the point.</param>
        /// <param name="succeeded">Whether operation was successful.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public BringPointIntoViewCompletedEventArgs(Point point, ITextPointer position, bool succeeded, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _point = point;
            _position = position;
            //_succeeded = succeeded;
        }

        /// <summary>
        /// Point in pixel coordinates.
        /// </summary>
        public Point Point
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _point;
            }
        }

        /// <summary>
        /// A text position and its orientation matching or closest to the point.
        /// </summary>
        public ITextPointer Position
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _position;
            }
        }

        /// <summary>
        /// Point in pixel coordinates.
        /// </summary>
        private readonly Point _point;

        /// <summary>
        /// A text position and its orientation matching or closest to the point.
        /// </summary>
        private readonly ITextPointer _position;

        // Whether operation was successful.
        //private readonly bool _succeeded;
    }

    /// <summary>
    /// Event arguments for the BringLineIntoViewCompleted event.
    /// </summary>
    internal class BringLineIntoViewCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedX">
        /// The suggestedX parameter is the suggested X offset, in pixels, of 
        /// the TextPointer on the destination line.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <param name="newPosition">ITextPointer matching the given position advanced by the given number of line.</param>
        /// <param name="newSuggestedX">The offset at the position moved (useful when moving between columns or pages).</param>
        /// <param name="linesMoved">Indicates the number of lines moved, which may be less than count if there is no more content.</param>
        /// <param name="succeeded">Whether operation was successful.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public BringLineIntoViewCompletedEventArgs(ITextPointer position, double suggestedX, int count, ITextPointer newPosition, double newSuggestedX, int linesMoved, bool succeeded, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _position = position;
            //_suggestedX = suggestedX;
            _count = count;
            _newPosition = newPosition;
            _newSuggestedX = newSuggestedX;
            //_linesMoved = linesMoved;
            //_succeeded = succeeded;
        }

        /// <summary>
        /// Initial text position of an object/character.
        /// </summary>
        public ITextPointer Position
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _position;
            }
        }

        /// <summary>
        /// Number of lines to advance. Negative means move backwards.
        /// </summary>
        public int Count
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _count;
            }
        }

        /// <summary>
        /// ITextPointer matching the given position advanced by the given number of line.
        /// </summary>
        public ITextPointer NewPosition
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _newPosition;
            }
        }

        /// <summary>
        /// The offset at the position moved (useful when moving between columns or pages).
        /// </summary>
        public double NewSuggestedX
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _newSuggestedX;
            }
        }

        /// <summary>
        /// Initial text position of an object/character.
        /// </summary>
        private readonly ITextPointer _position;

        // The suggestedX parameter is the suggested X offset, in pixels, of 
        // the TextPointer on the destination line.
        //private readonly double _suggestedX;

        /// <summary>
        /// Number of lines to advance. Negative means move backwards.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// ITextPointer matching the given position advanced by the given number of line.
        /// </summary>
        private readonly ITextPointer _newPosition;

        /// <summary>
        /// The offset at the position moved (useful when moving between columns or pages).
        /// </summary>
        private readonly double _newSuggestedX;

        // Indicates the number of lines moved, which may be less than count if there is no more content.
        //private readonly int _linesMoved;

        // Whether operation was successful.
        //private readonly bool _succeeded;
    }

    /// <summary>
    /// Event arguments for the BringPageIntoViewCompleted event.
    /// </summary>
    internal class BringPageIntoViewCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedOffset">
        /// The suggestedX parameter is the suggested X offset, in pixels, of 
        /// the TextPointer on the destination line.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <param name="newPosition">ITextPointer matching the given position advanced by the given number of line.</param>
        /// <param name="newSuggestedOffset">The offset at the position moved (useful when moving between columns or pages).</param>
        /// <param name="pagesMoved">Indicates the number of pages moved, which may be less than count if there is no more content.</param>
        /// <param name="succeeded">Whether operation was successful.</param>
        /// <param name="error">Error occurred during an asynchronous operation.</param>
        /// <param name="cancelled">Whether an asynchronous operation has been cancelled.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public BringPageIntoViewCompletedEventArgs(ITextPointer position, Point suggestedOffset, int count, ITextPointer newPosition, Point newSuggestedOffset, int pagesMoved, bool succeeded, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _position = position;
            //_suggestedX = suggestedX;
            _count = count;
            _newPosition = newPosition;
            _newSuggestedOffset = newSuggestedOffset;
            //_linesMoved = linesMoved;
            //_succeeded = succeeded;
        }

        /// <summary>
        /// Initial text position of an object/character.
        /// </summary>
        public ITextPointer Position
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _position;
            }
        }

        /// <summary>
        /// Number of lines to advance. Negative means move backwards.
        /// </summary>
        public int Count
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _count;
            }
        }

        /// <summary>
        /// ITextPointer matching the given position advanced by the given number of line.
        /// </summary>
        public ITextPointer NewPosition
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _newPosition;
            }
        }

        /// <summary>
        /// The offset at the position moved (useful when moving between columns or pages).
        /// </summary>
        public Point NewSuggestedOffset
        {
            get
            {
                // Raise an exception if the operation failed or was cancelled.
                this.RaiseExceptionIfNecessary();
                return _newSuggestedOffset;
            }
        }

        /// <summary>
        /// Initial text position of an object/character.
        /// </summary>
        private readonly ITextPointer _position;

        // The suggestedX parameter is the suggested X offset, in pixels, of 
        // the TextPointer on the destination line.
        //private readonly double _suggestedX;

        /// <summary>
        /// Number of lines to advance. Negative means move backwards.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// ITextPointer matching the given position advanced by the given number of line.
        /// </summary>
        private readonly ITextPointer _newPosition;

        /// <summary>
        /// The offset at the position moved (useful when moving between columns or pages).
        /// </summary>
        private readonly Point _newSuggestedOffset;

        // Indicates the number of lines moved, which may be less than count if there is no more content.
        //private readonly int _linesMoved;

        // Whether operation was successful.
        //private readonly bool _succeeded;
    }
}
