using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.HotReload.Actions;
using Avalonia.Markup.Xaml.HotReload.Blocks;
using XamlX.IL;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class IlDiffer
    {
        private readonly List<RecordingIlEmitter.RecordedInstruction> _oldInstructions;
        private readonly List<RecordingIlEmitter.RecordedInstruction> _newInstructions;

        public IlDiffer(
            List<RecordingIlEmitter.RecordedInstruction> oldInstructions,
            List<RecordingIlEmitter.RecordedInstruction> newInstructions)
        {
            _oldInstructions = oldInstructions;
            _newInstructions = newInstructions;
        }
        
        public static List<IHotReloadAction> Diff(
            List<RecordingIlEmitter.RecordedInstruction> oldInstructions,
            List<RecordingIlEmitter.RecordedInstruction> newInstructions)
        {
            var differ = new IlDiffer(oldInstructions, newInstructions);
            var diffToAction = new DiffToAction(AvaloniaXamlAstLoader.TypeSystem, newInstructions);

            var diff = differ.Diff();
            return diffToAction.ToActions(diff);
        }

        public Diff Diff()
        {
            var oldBlock = GetBlocks(_oldInstructions);
            var newBlock = GetBlocks(_newInstructions);

            var diff = new Diff();

            CompareBlocks(
                new List<ObjectBlock> { oldBlock },
                new List<ObjectBlock> { newBlock },
                new DiffScoreCache(),
                diff);

            return diff;
        }

        private void CompareBlocks(
            List<ObjectBlock> oldBlocks,
            List<ObjectBlock> newBlocks,
            DiffScoreCache cache,
            Diff diff)
        {
            var blockPairs = new List<BlockPair>();

            blockPairs.AddRange(GetBlockPairs(newBlocks, oldBlocks, cache, _newInstructions, _oldInstructions, false));
            blockPairs.AddRange(GetBlockPairs(oldBlocks, newBlocks, cache, _oldInstructions, _newInstructions));

            blockPairs.Sort((x, y) => y.Score.CompareTo(x.Score));

            foreach (var var in blockPairs)
            {
                var newBlock = var.Left;
                var oldBlock = var.Right;

                if (!newBlocks.Contains(newBlock) || !oldBlocks.Contains(oldBlock))
                {
                    continue;
                }

                newBlocks.Remove(newBlock);
                oldBlocks.Remove(oldBlock);

                Logger
                    .TryGet(LogEventLevel.Verbose, "HotReload")
                    ?.Log(this, "{New} - {Old}: {Score}", newBlock.Type, oldBlock.Type, var.Score);

                var newProperties = newBlock.Properties.ToList();
                var oldProperties = oldBlock.Properties.ToList();

                for (int i = newProperties.Count - 1; i >= 0; i--)
                {
                    var newProperty = newProperties[i];
                    var oldProperty = oldProperties.FirstOrDefault(x => x.Name == newProperty.Name);

                    if (oldProperty == null)
                    {
                        continue;
                    }

                    var score = PropertyScore(
                        oldProperty,
                        newProperty,
                        cache,
                        _oldInstructions,
                        _newInstructions);

                    // TODO: Hardcoded comparison. 1 for name score.
                    if (!oldProperty.IsList && score <= 1)
                    {
                        diff.PropertyMap.Add((oldProperty, newProperty));
                    }

                    newProperties.RemoveAt(i);
                    oldProperties.Remove(oldProperty);
                }

                foreach (var newProperty in newProperties)
                {
                    if (newProperty.IsList)
                    {
                        continue;
                    }

                    Logger
                        .TryGet(LogEventLevel.Verbose, "HotReload")
                        ?.Log(this, "Added Property: {Property}", newProperty.Name);
                    
                    diff.AddedProperties.Add(newProperty);
                }

                foreach (var oldProperty in oldProperties)
                {
                    if (oldProperty.IsList)
                    {
                        continue;
                    }

                    Logger
                        .TryGet(LogEventLevel.Verbose, "HotReload")
                        ?.Log(this, "Removed Property: {Property}", oldProperty.Name);
                    
                    diff.RemovedProperties.Add(oldProperty);
                }

                if (newBlock.Children.Any() || oldBlock.Children.Any())
                {
                    CompareBlocks(
                        oldBlock.Children.ToList(),
                        newBlock.Children.ToList(),
                        cache,
                        diff);
                }
            }

            foreach (var newBlock in newBlocks)
            {
                Logger
                    .TryGet(LogEventLevel.Verbose, "HotReload")
                    ?.Log(this, "Added Object: {Type}", newBlock.Type);
                
                diff.AddedBlocks.Add(newBlock);
            }

            foreach (var oldBlock in oldBlocks)
            {
                // Only remove when the parent property is a list. The other case will be handled
                // as removed property while handling properties.
                if (oldBlock.ParentProperty.IsList)
                {
                    Logger
                        .TryGet(LogEventLevel.Verbose, "HotReload")
                        ?.Log(this, "Removed Object: {Type}", oldBlock.Type);
                    
                    diff.RemovedBlocks.Add(oldBlock);
                }
            }
        }

        private List<BlockPair> GetBlockPairs(
            List<ObjectBlock> leftBlocks,
            List<ObjectBlock> rightBlocks,
            DiffScoreCache cache,
            List<RecordingIlEmitter.RecordedInstruction> firstInstructions,
            List<RecordingIlEmitter.RecordedInstruction> secondInstructions,
            bool matchOnLeft = true)
        {
            var blockPairs = new List<BlockPair>();

            foreach (var leftBlock in leftBlocks)
            {
                double maxScore = 0;
                ObjectBlock match = null;

                foreach (var rightBlock in rightBlocks)
                {
                    var score = BlockScore(
                        leftBlock,
                        rightBlock,
                        cache,
                        firstInstructions,
                        secondInstructions);

                    if (score > maxScore)
                    {
                        maxScore = score;
                        match = rightBlock;
                    }
                }

                if (match == null)
                {
                    continue;
                }

                var pair = matchOnLeft
                    ? new BlockPair(match, leftBlock, maxScore)
                    : new BlockPair(leftBlock, match, maxScore);

                blockPairs.Add(pair);
            }

            return blockPairs;
        }

        private double BlockScore(
            ObjectBlock first,
            ObjectBlock second,
            DiffScoreCache cache,
            List<RecordingIlEmitter.RecordedInstruction> firstInstructions,
            List<RecordingIlEmitter.RecordedInstruction> secondInstructions)
        {
            if (cache.TryGetScore(first, second, out double score))
            {
                return score;
            }

            if (first.Type != second.Type)
            {
                return double.MinValue;
            }

            var maxIndexScore = Math.Max(first.Parent?.Children.Count ?? 0, second.Parent?.Children.Count ?? 0);
            var indexDiff = Math.Abs(first.ParentIndex - second.ParentIndex);

            score += maxIndexScore - indexDiff;

            var secondProperties = second.Properties.ToList();

            foreach (var firstProperty in first.Properties)
            {
                var secondProperty = secondProperties.FirstOrDefault(x => x.Name == firstProperty.Name);

                if (secondProperty == null)
                {
                    continue;
                }

                var propertyScore = PropertyScore(
                    firstProperty,
                    secondProperty,
                    cache,
                    firstInstructions,
                    secondInstructions);
                
                secondProperties.Remove(secondProperty);

                score += propertyScore;
            }

            var secondChildren = second.Children.ToList();

            foreach (var firstChild in first.Children)
            {
                double matchScore = 0;
                ObjectBlock match = null;

                foreach (var secondChild in secondChildren)
                {
                    var childScore = BlockScore(
                        firstChild,
                        secondChild,
                        cache,
                        firstInstructions,
                        secondInstructions);

                    if (childScore > matchScore)
                    {
                        matchScore = childScore;
                        match = secondChild;
                    }
                }

                secondChildren.Remove(match);

                score += matchScore;
            }

            cache.Add(first, second, score);

            return score;
        }

        private double PropertyScore(
            PropertyBlock first,
            PropertyBlock second,
            DiffScoreCache cache,
            List<RecordingIlEmitter.RecordedInstruction> firstInstructions,
            List<RecordingIlEmitter.RecordedInstruction> secondInstructions)
        {
            if (cache.TryGetScore(first, second, out var score))
            {
                return score;
            }

            if (first.Type != second.Type || first.Name != second.Name)
            {
                cache.Add(first, second, 0);
                return 0;
            }

            // TODO: Hardcoded values.
            double nameScore = 1;
            double instructionScore = 0;

            if (first.Length == second.Length)
            {
                bool sameValue = true;

                for (int i = 0; i < first.Length; i++)
                {
                    var firstInstruction = firstInstructions[first.StartOffset + i];
                    var secondInstruction = secondInstructions[second.StartOffset + i];

                    bool opCodeEqual = firstInstruction.OpCode == secondInstruction.OpCode;
                    bool operandEqual = Equals(firstInstruction.Operand, secondInstruction.Operand);
                    bool opCodesLdlocOrStloc = firstInstruction.OpCode == OpCodes.Ldloc || firstInstruction.OpCode == OpCodes.Stloc;

                    // Object equality does not work for ldloc and stloc operands.
                    if (opCodeEqual && (operandEqual || opCodesLdlocOrStloc))
                    {
                        continue;
                    }

                    sameValue = false;
                    break;
                }

                if (sameValue)
                {
                    // TODO: Hardcoded values.
                    instructionScore = 10;
                }
            }

            score = nameScore + instructionScore;
            cache.Add(first, second, score);

            return score;
        }

        private ObjectBlock GetBlocks(IList<RecordingIlEmitter.RecordedInstruction> instructions)
        {
            ObjectBlock root = null;
            ObjectBlock currentBlock = null;
            PropertyBlock currentProperty = null;
            
            int newObjectInstructionsStartOffset = 0;
            int newObjectInstructionsEndOffset = 0;
            
            var setPropertyBlocks = new Stack<PropertyBlock>();
            var parentBlocks = new Stack<ObjectBlock>();
            var childIndexes = new Stack<int>();

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (instruction.IsStartObjectInitializationMarker())
                {
                    var previousInstruction = instructions[i - 1];
                    var opCode = previousInstruction.OpCode;
                    var operand = previousInstruction.Operand;

                    Debug.Assert(opCode == OpCodes.Ldstr);
                    Debug.Assert(operand is string);

                    currentBlock = new ObjectBlock((string)operand, currentProperty)
                    {
                        NewObjectStartOffset = newObjectInstructionsStartOffset,
                        NewObjectEndOffset = newObjectInstructionsEndOffset,
                        InitializationStartOffset = i + 1
                    };

                    root ??= currentBlock;

                    if (parentBlocks.Count > 0)
                    {
                        var parent = parentBlocks.Peek();
                        parent.AddChild(currentBlock);
                    }

                    parentBlocks.Push(currentBlock);
                    childIndexes.Push(0);
                }
                else if (instruction.IsEndObjectInitializationMarker())
                {
                    Debug.Assert(parentBlocks.Count > 0);
                    Debug.Assert(currentBlock != null);

                    currentBlock.InitializationEndOffset = i;
                    currentBlock = parentBlocks.Pop().Parent;

                    childIndexes.Pop();
                }
                else if (instruction.IsStartSetPropertyMarker())
                {
                    Debug.Assert(currentBlock != null);

                    var propertyInstruction = instructions[i - 1];
                    var typeInstruction = instructions[i - 2];

                    currentProperty = new PropertyBlock(
                        currentBlock,
                        (string)typeInstruction.Operand,
                        (string)propertyInstruction.Operand,
                        i + 1);

                    currentBlock.AddProperty(currentProperty);
                    setPropertyBlocks.Push(currentProperty);
                }
                else if (instruction.IsEndSetPropertyMarker())
                {
                    Debug.Assert(currentProperty != null);
                    currentProperty.EndOffset = i - 1;
                    setPropertyBlocks.Pop();

                    currentBlock = parentBlocks.Peek();
                    
                    if (setPropertyBlocks.Count > 0)
                    {
                        currentProperty = setPropertyBlocks.Peek();
                    }
                }
                else if (instruction.IsAddChildMarker())
                {
                    Debug.Assert(currentBlock != null);

                    var propertyInstruction = instructions[i - 1];
                    var typeInstruction = instructions[i - 2];

                    currentProperty = new PropertyBlock(
                        currentBlock,
                        (string)typeInstruction.Operand,
                        ((string)propertyInstruction.Operand).Replace("get_", ""),
                        i + 1,
                        true,
                        childIndexes.Peek());

                    currentBlock.AddProperty(currentProperty);
                    childIndexes.Push(childIndexes.Pop() + 1);
                }
                else if (instruction.IsStartNewObjectMarker())
                {
                    Debug.Assert(currentBlock != null);
                    newObjectInstructionsStartOffset = i + 1;
                }
                else if (instruction.IsEndNewObjectMarker())
                {
                    Debug.Assert(currentBlock != null);
                    newObjectInstructionsEndOffset = i;
                }
            }

            return root;
        }
    }
}
