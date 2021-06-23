using System;
using Avalonia.Styling.Activators;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Describes how a <see cref="SelectorMatch"/> matches a control and its type.
    /// </summary>
    public enum SelectorMatchResult
    {
        /// <summary>
        /// The selector never matches this type.
        /// </summary>
        NeverThisType,

        /// <summary>
        /// The selector never matches this instance, but can match this type.
        /// </summary>
        NeverThisInstance,

        /// <summary>
        /// The selector matches this instance based on the <see cref="SelectorMatch.Activator"/>.
        /// </summary>
        Sometimes,

        /// <summary>
        /// The selector always matches this instance, but doesn't always match this type.
        /// </summary>
        AlwaysThisInstance,

        /// <summary>
        /// The selector always matches this type.
        /// </summary>
        AlwaysThisType,
    }

    /// <summary>
    /// Holds the result of a <see cref="Selector"/> match.
    /// </summary>
    /// <remarks>
    /// A selector match describes whether and how a <see cref="Selector"/> matches a control, and
    /// in addition whether the selector can ever match a control of the same type.
    /// </remarks>
    public readonly struct SelectorMatch
    {
        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.NeverThisType"/>.
        /// </summary>
        public static readonly SelectorMatch NeverThisType = new SelectorMatch(SelectorMatchResult.NeverThisType);

        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.NeverThisInstance"/>.
        /// </summary>
        public static readonly SelectorMatch NeverThisInstance = new SelectorMatch(SelectorMatchResult.NeverThisInstance);

        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.AlwaysThisType"/>.
        /// </summary>
        public static readonly SelectorMatch AlwaysThisType = new SelectorMatch(SelectorMatchResult.AlwaysThisType);

        /// <summary>
        /// Gets a selector match with the result of <see cref="SelectorMatchResult.AlwaysThisInstance"/>.
        /// </summary>
        public static readonly SelectorMatch AlwaysThisInstance = new SelectorMatch(SelectorMatchResult.AlwaysThisInstance);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorMatch"/> class with a 
        /// <see cref="SelectorMatchResult.Sometimes"/> result.
        /// </summary>
        /// <param name="match">The match activator.</param>
        public SelectorMatch(IStyleActivator match)
        {
            match = match ?? throw new ArgumentNullException(nameof(match));

            Result = SelectorMatchResult.Sometimes;
            Activator = match;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorMatch"/> class with the specified result.
        /// </summary>
        /// <param name="result">The match result.</param>
        public SelectorMatch(SelectorMatchResult result)
        {
            Result = result;
            Activator = null;
        }

        /// <summary>
        /// Gets a value indicating whether the match was positive.
        /// </summary>
        public bool IsMatch => Result >= SelectorMatchResult.Sometimes;

        /// <summary>
        /// Gets the result of the match.
        /// </summary>
        public SelectorMatchResult Result { get; }

        /// <summary>
        /// Gets an activator which tracks the selector match, in the case of selectors that can
        /// change over time.
        /// </summary>
        public IStyleActivator? Activator { get; }

        /// <summary>
        /// Logical ANDs this <see cref="SelectorMatch"/> with another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SelectorMatch And(in SelectorMatch other)
        {
            var result = (SelectorMatchResult)Math.Min((int)Result, (int)other.Result);

            if (result == SelectorMatchResult.Sometimes)
            {
                var activators = new AndActivatorBuilder();
                activators.Add(Activator);
                activators.Add(other.Activator);
                return new SelectorMatch(activators.Get());
            }
            else
            {
                return new SelectorMatch(result);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => Result.ToString();
    }
}
