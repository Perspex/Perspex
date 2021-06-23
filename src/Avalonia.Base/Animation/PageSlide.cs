using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transitions between two pages by sliding them horizontally.
    /// </summary>
    public class PageSlide : IPageTransition
    {
        /// <summary>
        /// The axis on which the PageSlide should occur
        /// </summary>
        public enum SlideAxis
        {
            Horizontal,
            Vertical
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        public PageSlide()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        /// <param name="orientation">The axis on which the animation should occur</param>
        public PageSlide(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal)
        {
            Duration = duration;
            Orientation = orientation;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public SlideAxis Orientation { get; set; }
        
        /// <summary>
        /// Gets or sets element entrance easing.
        /// </summary>
        public Easing SlideInEasing { get; set; } = new LinearEasing();
        
        /// <summary>
        /// Gets or sets element exit easing.
        /// </summary>
        public Easing SlideOutEasing { get; set; } = new LinearEasing();

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
        /// If true, the new page is slid in from the right, or if false from the left.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        public async Task Start(Visual from, Visual to, bool forward)
        {
            var tasks = new List<Task>();
            var parent = GetVisualParent(from, to);
            var distance = Orientation == SlideAxis.Horizontal ? parent.Bounds.Width : parent.Bounds.Height;
            var translateProperty = Orientation == SlideAxis.Horizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;

            if (from != null)
            {
                var animation = new Animation
                {
                    Easing = SlideOutEasing,
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = translateProperty, Value = 0d } },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = translateProperty,
                                    Value = forward ? -distance : distance
                                }
                            },
                            Cue = new Cue(1d)
                        }
                    },
                    Duration = Duration
                };
                tasks.Add(animation.RunAsync(from));
            }

            if (to != null)
            {
                to.IsVisible = true;
                var animation = new Animation
                {
                    Easing = SlideInEasing,
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = translateProperty,
                                    Value = forward ? distance : -distance
                                }
                            },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = translateProperty, Value = 0d } },
                            Cue = new Cue(1d)
                        }
                    },
                    Duration = Duration
                };
                tasks.Add(animation.RunAsync(to));
            }

            await Task.WhenAll(tasks);

            if (from != null)
            {
                from.IsVisible = false;
            }
        }

        /// <summary>
        /// Gets the common visual parent of the two control.
        /// </summary>
        /// <param name="from">The from control.</param>
        /// <param name="to">The to control.</param>
        /// <returns>The common parent.</returns>
        /// <exception cref="ArgumentException">
        /// The two controls do not share a common parent.
        /// </exception>
        /// <remarks>
        /// Any one of the parameters may be null, but not both.
        /// </remarks>
        private static IVisual GetVisualParent(IVisual from, IVisual to)
        {
            var p1 = (from ?? to).VisualParent;
            var p2 = (to ?? from).VisualParent;

            if (p1 != null && p2 != null && p1 != p2)
            {
                throw new ArgumentException("Controls for PageSlide must have same parent.");
            }

            return p1;
        }
    }
}
