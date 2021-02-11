// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Run-length table of document status for use the by the Speller.
//

using Avalonia.Media.TextFormatting;

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Collections;
    using System.Diagnostics;
    using System.Windows.Controls;

    // Run-length table of document status for use the by the Speller.
    //
    // The speller tracks all document content as either
    // 1. Clean.  Analyzed with no errors.
    // 2. Dirty.  Unanalyzed.
    // 3. Error.  A misspelled word.
    //
    // This class maintains the state, and keeps the SpellerHighlightLayer
    // up-to-date about changes so that error squiggles update appropriately.
    //
    // Use the debug-only Dump() method to view _runList state.
    internal class SpellerStatusTable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Constructor.
        internal SpellerStatusTable(ITextPointer textContainerStart, SpellerHighlightLayer highlightLayer)
        {
            _highlightLayer = highlightLayer;

            _runList = new ArrayList(1);

            _runList.Add(new Run(textContainerStart, RunType.Dirty));
        }
 
        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called by the Speller whenever document state changes.
        // Returns true if any new dirty runs are created.
        internal void OnTextChange(TextContainerChangeEventArgs e)
        {
            if (e.TextChange == TextChangeType.ContentAdded)
            {
                // Content was added.  Update the run list.
                OnContentAdded(e);
            }
            else if (e.TextChange == TextChangeType.ContentRemoved)
            {
                // Content was deleted.  Update the run list.
                OnContentRemoved(e.ITextPosition);
            }
            else
            {
                // Language or SpellingReform property changed.
                ITextPointer end = e.ITextPosition.CreatePointer(e.Count);
                end.Freeze();
                MarkDirtyRange(e.ITextPosition, end);
            }

            DebugAssertRunList();
        }

        // Returns the first dirty run following a specified position in the document.
        // start/end will be left null if no dirty ranges are found.
        internal void GetFirstDirtyRange(ITextPointer searchStart, out ITextPointer start, out ITextPointer end)
        {
            int index;
            Run run;

            start = null;
            end = null;

            // If this is ever slow enough to matter, we could cache the value.
            for (index = FindIndex(searchStart.CreateStaticPointer(), LogicalDirection.Forward); index >= 0 && index < _runList.Count; index++)
            {
                run = GetRun(index);

                if (run.RunType == RunType.Dirty)
                {
                    // We might get a hit in the first run, in which case start <= searchStart.
                    // Always return searchStart as a minimum.
                    start = TextPointerBase.Max(searchStart, run.Position);
                    end = GetRunEndPositionDynamic(index);
                    break;
                }
            }
        }

        // Mark a run of text clean.
        internal void MarkCleanRange(ITextPointer start, ITextPointer end)
        {
            MarkRange(start, end, RunType.Clean);
            DebugAssertRunList();
        }

        // Mark a run of text dirty.
        internal void MarkDirtyRange(ITextPointer start, ITextPointer end)
        {
            MarkRange(start, end, RunType.Dirty);
            DebugAssertRunList();
        }

        // Tags a run of text as an error.
        // NB: we expect that the new error range has already been marked clean.
        internal void MarkErrorRange(ITextPointer start, ITextPointer end)
        {
            int runIndex;
            Run run;

            runIndex = FindIndex(start.CreateStaticPointer(), LogicalDirection.Forward);
            run = GetRun(runIndex);

            // There should be a clean run here, that covers all the error.
            // We always start analyzing text by cleaning the entire range.
            Invariant.Assert(run.RunType == RunType.Clean);
            Invariant.Assert(run.Position.CompareTo(start) <= 0);
            Invariant.Assert(GetRunEndPosition(runIndex).CompareTo(end) >= 0);

            if (run.Position.CompareTo(start) == 0)
            {
                // The run starts exactly at this error.
                // Convert it to an error and add a second clean run for the remainder.
                run.RunType = RunType.Error;
            }
            else
            {
                // The run starts before this error.
                // Insert a new error run, and an additional run for the remainder.
                _runList.Insert(runIndex + 1, new Run(start, RunType.Error));
                runIndex++;
            }

            // Handle any remainder since we split the original clean run.
            if (GetRunEndPosition(runIndex).CompareTo(end) > 0)
            {
                _runList.Insert(runIndex + 1, new Run(end, RunType.Clean));
            }

            // Tell the HighlightLayer about this change.
            _highlightLayer.FireChangedEvent(start, end);

            DebugAssertRunList();
        }

        // Returns true if the specified character is part of a run of the specified type.
        internal bool IsRunType(StaticTextPointer textPosition, LogicalDirection direction, RunType runType)
        {
            int index = FindIndex(textPosition, direction);

            if (index < 0)
            {
                return false;
            }

            return GetRun(index).RunType == runType;
        }

        // Returns the position of the next error start or end in an
        // indicated direction, or null if there is no such position.
        // Called by the SpellerHighlightLayer.
        internal StaticTextPointer GetNextErrorTransition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            StaticTextPointer transitionPosition;
            int index;
            int i;

            transitionPosition = StaticTextPointer.Null;

            index = FindIndex(textPosition, direction);

            if (index == -1)
            {
                // textPosition is at the document edge.
                // leave transitionPosition null.
            }
            else if (direction == LogicalDirection.Forward)
            {
                if (IsErrorRun(index))
                {
                    transitionPosition = GetRunEndPosition(index);
                }
                else
                {
                    for (i = index+1; i < _runList.Count; i++)
                    {
                        if (IsErrorRun(i))
                        {
                            transitionPosition = GetRun(i).Position.CreateStaticPointer();
                            break;
                        }
                    }
                }
            }
            else // direction == LogicalDirection.Backward
            {
                if (IsErrorRun(index))
                {
                    transitionPosition = GetRun(index).Position.CreateStaticPointer();
                }
                else
                {
                    for (i = index - 1; i > 0; i--)
                    {
                        if (IsErrorRun(i))
                        {
                            transitionPosition = GetRunEndPosition(i);
                            break;
                        }
                    }
                }
            }

            // If we ever had two consecuative errors (with touching borders)
            // we could return a transitionPosition == textPosition, which is illegal.
            // We rely on the fact that consecutive errors are always separated
            // by a word break to avoid this.
            // This sounds like a bogus thing to rely on for languages with no white space.
            Invariant.Assert(transitionPosition.IsNull || textPosition.CompareTo(transitionPosition) != 0);

            return transitionPosition;
        }

        // Returns the position and suggested replacement list of an error covering
        // the specified content.
        // If no error exists, return false.
        internal bool GetError(StaticTextPointer textPosition, LogicalDirection direction,
            out ITextPointer start, out ITextPointer end)
        {
            int index;

            start = null;
            end = null;

            index = GetErrorIndex(textPosition, direction);

            if (index >= 0)
            {
                start = GetRun(index).Position;
                end = GetRunEndPositionDynamic(index);
            }

            return (start != null);
        }

        // Returns the type and end of the Run intersecting position.
        internal bool GetRun(StaticTextPointer position, LogicalDirection direction, out RunType runType, out StaticTextPointer end)
        {
            int index = FindIndex(position, direction);

            runType = RunType.Clean;
            end = StaticTextPointer.Null;

            if (index < 0)
            {
                return false;
            }

            Run run = GetRun(index);

            runType = run.RunType;
            end = (direction == LogicalDirection.Forward) ? GetRunEndPosition(index) : run.Position.CreateStaticPointer();

            return true;
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Types
        //
        //------------------------------------------------------

        #region Internal Types

        // Tags used to identify run types.
        internal enum RunType { Clean, Dirty, Error };

        #endregion Internal Types

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Returns the index in _runList of an error covering the specified
        // content, or -1 if no such error exists.
        private int GetErrorIndex(StaticTextPointer textPosition, LogicalDirection direction)
        {
            int index;
            Run run;

            index = FindIndex(textPosition, direction);

            if (index >= 0)
            {
                run = GetRun(index);

                if (run.RunType == RunType.Clean || run.RunType == RunType.Dirty)
                {
                    index = -1;
                }
            }

            return index;
        }

        // Finds the index of a run containing the specified content.
        // Returns -1 if there is no run in the indicated direction -- when
        // position is at the document edge pointing to nothing.
        private int FindIndex(StaticTextPointer position, LogicalDirection direction)
        {
            Run run;
            int index;
            int minIndex;
            int maxIndex;

            index = -1;
            minIndex = 0;
            maxIndex = _runList.Count;

            while (minIndex < maxIndex)
            {
                index = (minIndex + maxIndex) / 2;

                run = GetRun(index);

                if (direction == LogicalDirection.Forward && position.CompareTo(run.Position) < 0 ||
                    direction == LogicalDirection.Backward && position.CompareTo(run.Position) <= 0)
                {
                    // Search to the left.
                    maxIndex = index;
                }
                else if (direction == LogicalDirection.Forward && position.CompareTo(GetRunEndPosition(index)) >= 0 ||
                         direction == LogicalDirection.Backward && position.CompareTo(GetRunEndPosition(index)) > 0)
                {
                    // Search to the right.
                    minIndex = index + 1;
                }
                else
                {
                    // Got a match.
                    break;
                }
            }

            if (minIndex >= maxIndex)
            {
                // We walked off the document edge searching.
                // position is at document start or end, and direction
                // points off into space, so there's no associated run.
                index = -1;
            }

            return index;
        }

        // Marks a text run as clean or dirty.
        private void MarkRange(ITextPointer start, ITextPointer end, RunType runType)
        {
            if (start.CompareTo(end) == 0)
            {
                return;
            }

            int startIndex;
            int endIndex;

            Invariant.Assert(runType == RunType.Clean || runType == RunType.Dirty);

            startIndex = FindIndex(start.CreateStaticPointer(), LogicalDirection.Forward);
            endIndex = FindIndex(end.CreateStaticPointer(), LogicalDirection.Backward);

            // We don't expect start/end to ever point off the edge of the document.
            Invariant.Assert(startIndex >= 0);
            Invariant.Assert(endIndex >= 0);

            // Remove wholly covered runs.
            if (startIndex + 1 < endIndex)
            {
                // Tell the HighlightLayer about any error runs that are going away.
                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    NotifyHighlightLayerBeforeRunChange(i);
                }

                _runList.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                endIndex = startIndex + 1;
            }

            // Merge the bordering edge runs.

            if (startIndex == endIndex)
            {
                // We're contained in a single run.
                AddRun(startIndex, start, end, runType);
            }
            else
            {
                // We cover two runs.
                Invariant.Assert(startIndex == endIndex - 1);

                // Handle the first run.
                AddRun(startIndex, start, end, runType);

                // Recalc endIndex, since it may have changed in the merge.
                endIndex = FindIndex(end.CreateStaticPointer(), LogicalDirection.Backward);
                Invariant.Assert(endIndex >= 0);
                // Handle the second run.
                AddRun(endIndex, start, end, runType);
            }
        }

        // Adds a new run into an old one, merging the two.
        private void AddRun(int index, ITextPointer start, ITextPointer end, RunType runType)
        {
            Run run;
            Run newRun;
            RunType oppositeRunType;

            // We don't expect runType.Error, just clean or dirty.
            Invariant.Assert(runType == RunType.Clean || runType == RunType.Dirty);
            // We don't expect empty runs here.
            Invariant.Assert(start.CompareTo(end) < 0);

            oppositeRunType = (runType == RunType.Clean) ? RunType.Dirty : RunType.Clean;
            run = GetRun(index);

            if (run.RunType == runType)
            {
                // Existing run value matches new one.
                TryToMergeRunWithNeighbors(index);
            }
            else if (run.RunType == oppositeRunType)
            {
                // We're merging a new clean run with an old dirty one, or vice versa.

                // Split the run, insert a new run in the middle.
                if (run.Position.CompareTo(start) >= 0)
                {
                    if (GetRunEndPosition(index).CompareTo(end) <= 0)
                    {
                        // We entirely cover this run, just flip the RunType.
                        run.RunType = runType;

                        TryToMergeRunWithNeighbors(index);
                    }
                    else
                    {
                        // We cover the left half.
                        if (index > 0 && GetRun(index - 1).RunType == runType)
                        {
                            // Previous run matches the new value, merge with it.
                            run.Position = end;
                        }
                        else
                        {
                            run.RunType = runType;
                            newRun = new Run(end, oppositeRunType);
                            _runList.Insert(index + 1, newRun);
                        }
                    }
                }
                else if (GetRunEndPosition(index).CompareTo(end) <= 0)
                {
                    // We cover the right half.
                    if (index < _runList.Count - 1 && GetRun(index + 1).RunType == runType)
                    {
                        // Following run matches the new value, merge with it.
                        GetRun(index + 1).Position = start;
                    }
                    else
                    {
                        // Insert new run.
                        newRun = new Run(start, runType);
                        _runList.Insert(index + 1, newRun);
                    }
                }
                else
                {
                    // We're in the middle of the run.
                    // Split the run, adding a new run and a new second
                    // half of the original run.
                    newRun = new Run(start, runType);
                    _runList.Insert(index + 1, newRun);
                    newRun = new Run(end, oppositeRunType);
                    _runList.Insert(index + 2, newRun);
                }
            }
            else
            {
                ITextPointer errorStart;
                ITextPointer errorEnd;

                // We hit an error run, the whole thing becomes dirty/clean.
                run.RunType = runType;

                errorStart = run.Position;
                errorEnd = GetRunEndPositionDynamic(index);

                // This call might remove run...
                TryToMergeRunWithNeighbors(index);

                // Tell the HighlightLayer about this change.
                _highlightLayer.FireChangedEvent(errorStart, errorEnd);
            }
        }

        // Attemps to merge a run with its two bordering neighbors.
        private void TryToMergeRunWithNeighbors(int index)
        {
            Run run;

            run = GetRun(index);

            if (index > 0 && GetRun(index - 1).RunType == run.RunType)
            {
                // Previous run matches the new value, merge with it.
                _runList.RemoveAt(index);
                index--;
            }
            if (index < _runList.Count - 1 && GetRun(index + 1).RunType == run.RunType)
            {
                // Following run matches the new value, merge with it.
                _runList.RemoveAt(index + 1);
            }
        }

        // Called when content is added to the document.
        // Updates the run list with a new dirty region.
        private void OnContentAdded(TextContainerChangeEventArgs e)
        {
            ITextPointer start;
            ITextPointer end;

            // Expand the affected region by one char in either direction
            // to make sure we examine surrounding text that might be affected
            // by the addition of new whitespace.
            if (e.ITextPosition.Offset > 0)
            {
                start = e.ITextPosition.CreatePointer(-1);
            }
            else
            {
                start = e.ITextPosition;
            }
            start.Freeze();

            if (e.ITextPosition.Offset + e.Count < e.ITextPosition.TextContainer.SymbolCount - 1)
            {
                end = e.ITextPosition.CreatePointer(e.Count + 1);
            }
            else
            {
                end = e.ITextPosition.CreatePointer(e.Count);
            }
            end.Freeze();

            // Mark the new text dirty.
            MarkRange(start, end, RunType.Dirty);
        }

        // Called when content is removed from the document.
        // Update the run list and notifies the highlight layer.
        private void OnContentRemoved(ITextPointer position)
        {
            int index;
            int i;
            Run run;

            // Get the first bordering run.
            index = FindIndex(position.CreateStaticPointer(), LogicalDirection.Backward);
            if (index == -1)
            {
                // position is at beginning of document.
                // Look at the first run.
                index = 0;
            }

            // First run gets reset to dirty.
            run = GetRun(index);

            if (run.RunType != RunType.Dirty)
            {
                NotifyHighlightLayerBeforeRunChange(index);

                run.RunType = RunType.Dirty; //  Should just be one char, unless its an error.

                if (index > 0 && GetRun(index - 1).RunType == RunType.Dirty)
                {
                    // Previous run matches the new value, merge with it.
                    _runList.RemoveAt(index);
                    index--;
                }
            }

            // Start looking at the following runs.
            index += 1;

            // Middle runs (collapsed to zero width) are removed.
            for (i = index; i < _runList.Count; i++)
            {
                ITextPointer runPosition = GetRun(i).Position;
                // Stop if we find a non-bordering Run that is not empty.
                if (runPosition.CompareTo(position) > 0 && runPosition.CompareTo(GetRunEndPosition(i)) != 0)
                    break;
            }

            // Note we don't worry about announcing anything to the HighlightLayer
            // here because these are zero-width runs.
            _runList.RemoveRange(index, i - index);

            // Reset last run to dirty.
            // Since we know the first run at index is already dirty,
            // just remove it.
            if (index < _runList.Count)
            {
                NotifyHighlightLayerBeforeRunChange(index); 
                _runList.RemoveAt(index); // just one char, unless it's an error run.
                
                // Finally, merge the following run with the run at index
                // if it happens to also be dirty.
                if (index < _runList.Count && GetRun(index).RunType == RunType.Dirty)
                {
                    _runList.RemoveAt(index);
                }
            }
        }

        // Notifies the highlight layer about a changing run.
        private void NotifyHighlightLayerBeforeRunChange(int index)
        {
            ITextPointer errorStart;
            ITextPointer errorEnd;

            // The highlight layer only cares about error runs.
            if (IsErrorRun(index))
            {
                errorStart = GetRun(index).Position;
                errorEnd = GetRunEndPositionDynamic(index);

                if (errorStart.CompareTo(errorEnd) != 0) // errorStart == errorEnd if content was deleted.
                {
                    _highlightLayer.FireChangedEvent(errorStart, errorEnd);
                }
            }
        }

        // Validates the state of _runList.
        // Invariant.Strict only.
        private void DebugAssertRunList()
        {
            int i;
            Run run;
            RunType previousRunType;

            Invariant.Assert(_runList.Count >= 1, "Run list should never be empty!");

            if (Invariant.Strict)
            {
                previousRunType = RunType.Clean;

                for (i = 0; i < _runList.Count; i++)
                {
                    run = GetRun(i);

                    if (_runList.Count == 1)
                    {
                        Invariant.Assert(run.Position.CompareTo(run.Position.TextContainer.Start) == 0);
                    }
                    else
                    {
                        // We can legally have a zero-width run, in the case of a TextElement extract.
                        // In that case, we'll two separate notifications, one for each edge.  After we
                        // handle the first edge we might have a zero-width run still waiting to be
                        // handled in the following notification.  So here we can only look for out-of-order
                        // runs.
                        Invariant.Assert(run.Position.CompareTo(GetRunEndPosition(i)) <= 0, "Found negative width run!");
                    }
                    Invariant.Assert(i == 0 || GetRunEndPosition(i - 1).CompareTo(run.Position) <= 0, "Found overlapping runs!");

                    if (!IsErrorRun(i))
                    {
                        Invariant.Assert(i == 0 || previousRunType != run.RunType, "Found consecutive dirty/dirt or clean/clean runs!");
                    }

                    previousRunType = run.RunType;
                }
            }
        }

#if DEBUG
        // Diagnostic tool: dumps _runList to the current debugger.
        private void Dump()
        {
            int i;
            Run run;
            string runType;

            for (i = 0; i < _runList.Count; i++)
            {
                run = GetRun(i);

                if (run.RunType == RunType.Clean)
                {
                    runType = "clean";
                }
                else if (run.RunType == RunType.Dirty)
                {
                    runType = "dirty";
                }
                else
                {
                    runType = "error";
                }

                Debug.WriteLine(i + ": " + run.Position.TextContainer.Start.GetOffsetToPosition(run.Position) +
                                " " + runType);
            }
        }
#endif // DEBUG

        // Typesafe run accessor.
        private Run GetRun(int index)
        {
            return (Run)_runList[index];
        }

        // Returns the end position of run.
        private ITextPointer GetRunEndPositionDynamic(int index)
        {
            return GetRunEndPosition(index).CreateDynamicTextPointer(LogicalDirection.Forward);
        }

        // Returns the end position of run.
        private StaticTextPointer GetRunEndPosition(int index)
        {
            StaticTextPointer position;

            if (index + 1 < _runList.Count)
            {
                position = GetRun(index + 1).Position.CreateStaticPointer();
            }
            else
            {
                Run run = GetRun(index);
                ITextContainer textContainer = run.Position.TextContainer;
                position = textContainer.CreateStaticPointerAtOffset(textContainer.SymbolCount);
            }

            return position;
        }

        // Returns true if the specified run index matches an error (and not
        // a clean or dirty run).
        private bool IsErrorRun(int index)
        {
            Run run;

            run = GetRun(index);

            return run.RunType != RunType.Clean && run.RunType != RunType.Dirty;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // An entry in the run list.
        private class Run
        {
            internal Run(ITextPointer position, RunType runType)
            {
                _position = position.GetFrozenPointer(LogicalDirection.Backward);
                _runType = runType;
            }

            internal ITextPointer Position
            { 
                get { return _position; }
                set { _position = value; }
            }

            internal RunType RunType
            {
                get { return _runType; }
                set { _runType = value; }
            }

            private ITextPointer _position;

            private RunType _runType;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // HighlighLayer associated with this table.
        private readonly SpellerHighlightLayer _highlightLayer;

        // Run length array of document status.
        private readonly ArrayList _runList;

        #endregion Private Fields
    }
}

