using System;

using Avalonia.Utilities;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Builds a control for a piece of data of specified type.
    /// </summary>
    /// <typeparam name="T">The type of the template's data.</typeparam>
    public class FuncDataTemplate<T> : FuncDataTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="supportsRecycling">Whether the control can be recycled.</param>
        public FuncDataTemplate(Func<T, INameScope, IControl> build, bool supportsRecycling = false)
            : base(o => TypeUtilities.CanCast<T>(o), CastBuild(build), supportsRecycling)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="supportsRecycling">Whether the control can be recycled.</param>
        public FuncDataTemplate(
            Func<T, bool> match,
            Func<T, INameScope, IControl> build,
            bool supportsRecycling = false)
            : base(CastMatch(match), CastBuild(build), supportsRecycling)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="supportsRecycling">Whether the control can be recycled.</param>
        public FuncDataTemplate(
            Func<T, bool> match,
            Func<T, IControl> build,
            bool supportsRecycling = false)
            : this(match, (a, _) => build(a), supportsRecycling)
        {
        }

        /// <summary>
        /// Casts a strongly typed match function to a weakly typed one.
        /// </summary>
        /// <param name="f">The strongly typed function.</param>
        /// <returns>The weakly typed function.</returns>
        private static Func<object, bool> CastMatch(Func<T, bool> f)
        {
            return o => TypeUtilities.CanCast<T>(o) && f((T)o);
        }

        /// <summary>
        /// Casts a strongly typed build function to a weakly typed one.
        /// </summary>
        /// <typeparam name="TResult">The strong data type.</typeparam>
        /// <param name="f">The strongly typed function.</param>
        /// <returns>The weakly typed function.</returns>
        private static Func<object, INameScope, TResult> CastBuild<TResult>(Func<T, INameScope, TResult> f)
        {
            return (o, s) => f((T)o, s);
        }
    }
}
