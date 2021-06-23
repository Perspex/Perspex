using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a cross-fade animation between two <see cref="IVisual"/>s.
    /// </summary>
    public class CrossFade : IPageTransition
    {
        private readonly Animation _fadeOutAnimation;
        private readonly Animation _fadeInAnimation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossFade"/> class.
        /// </summary>
        public CrossFade()
            : this(TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossFade"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public CrossFade(TimeSpan duration)
        {
            _fadeOutAnimation = new Animation
            {
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = 0d
                            }
                        },
                        Cue = new Cue(1d)
                    }

                }
            };
            _fadeInAnimation = new Animation
            {
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = 1d
                            }
                        },
                        Cue = new Cue(1d)
                    }

                }
            };
            _fadeOutAnimation.Duration = _fadeInAnimation.Duration = duration;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration
        {
            get => _fadeOutAnimation.Duration;
            set => _fadeOutAnimation.Duration = _fadeInAnimation.Duration = value;
        }

        /// <summary>
        /// Gets or sets element entrance easing.
        /// </summary>
        public Easing FadeInEasing
        {
            get => _fadeInAnimation.Easing;
            set => _fadeInAnimation.Easing = value;
        }

        /// <summary>
        /// Gets or sets element exit easing.
        /// </summary>
        public Easing FadeOutEasing
        {
            get => _fadeOutAnimation.Easing;
            set => _fadeOutAnimation.Easing = value;
        }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        public async Task Start(Visual from, Visual to)
        {
            var tasks = new List<Task>();

            if (to != null)
            {
                to.Opacity = 0;
            }

            if (from != null)
            {
                tasks.Add(_fadeOutAnimation.RunAsync(from));
            }

            if (to != null)
            {
                to.IsVisible = true;
                tasks.Add(_fadeInAnimation.RunAsync(to));

            }

            await Task.WhenAll(tasks);

            if (from != null)
            {
                from.IsVisible = false;
            }

            if (to != null)
            {
                to.Opacity = 1;
            }
        }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// Unused for cross-fades.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        Task IPageTransition.Start(Visual from, Visual to, bool forward)
        {
            return Start(from, to);
        }
    }
}
