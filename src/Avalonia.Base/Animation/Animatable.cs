using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for all animatable objects.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Clock"/> property.
        /// </summary>
        public static readonly StyledProperty<IClock> ClockProperty =
            AvaloniaProperty.Register<Animatable, IClock>(nameof(Clock), inherits: true);

        /// <summary>
        /// Defines the <see cref="Transitions"/> property.
        /// </summary>
        public static readonly StyledProperty<Transitions?> TransitionsProperty =
            AvaloniaProperty.Register<Animatable, Transitions?>(nameof(Transitions));

        private bool _transitionsEnabled = true;
        private Dictionary<ITransition, TransitionState>? _transitionState;

        /// <summary>
        /// Gets or sets the clock which controls the animations on the control.
        /// </summary>
        public IClock Clock
        {
            get => GetValue(ClockProperty);
            set => SetValue(ClockProperty, value);
        }

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions? Transitions
        {
            get => GetValue(TransitionsProperty);
            set => SetValue(TransitionsProperty, value);
        }

        /// <summary>
        /// Enables transitions for the control.
        /// </summary>
        /// <remarks>
        /// This method should not be called from user code, it will be called automatically by the framework
        /// when a control is added to the visual tree.
        /// </remarks>
        protected void EnableTransitions()
        {
            if (!_transitionsEnabled)
            {
                _transitionsEnabled = true;

                if (Transitions is object)
                {
                    AddTransitions(Transitions);
                }
            }
        }

        /// <summary>
        /// Disables transitions for the control.
        /// </summary>
        /// <remarks>
        /// This method should not be called from user code, it will be called automatically by the framework
        /// when a control is added to the visual tree.
        /// </remarks>
        protected void DisableTransitions()
        {
            if (_transitionsEnabled)
            {
                _transitionsEnabled = false;

                if (Transitions is object)
                {
                    RemoveTransitions(Transitions);
                }
            }
        }

        protected sealed override void OnPropertyChangedCore<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == TransitionsProperty && change.IsEffectiveValueChange)
            {
                var oldTransitions = change.OldValue.GetValueOrDefault<Transitions>();
                var newTransitions = change.NewValue.GetValueOrDefault<Transitions>();

                // When transitions are replaced, we add the new transitions before removing the old
                // transitions, so that when the old transition being disposed causes the value to
                // change, there is a corresponding entry in `_transitionStates`. This means that we
                // need to account for any transitions present in both the old and new transitions
                // collections.
                if (newTransitions is object)
                {
                    var toAdd = (IList)newTransitions;

                    if (newTransitions.Count > 0 && oldTransitions?.Count > 0)
                    {
                        toAdd = newTransitions.Except(oldTransitions).ToList();
                    }

                    newTransitions.CollectionChanged += TransitionsCollectionChanged;
                    AddTransitions(toAdd);
                }

                if (oldTransitions is object)
                {
                    var toRemove = (IList)oldTransitions;

                    if (oldTransitions.Count > 0 && newTransitions?.Count > 0)
                    {
                        toRemove = oldTransitions.Except(newTransitions).ToList();
                    }

                    oldTransitions.CollectionChanged -= TransitionsCollectionChanged;
                    RemoveTransitions(toRemove);
                }
            }
            else if (_transitionsEnabled &&
                     Transitions is object &&
                     _transitionState is object &&
                     !change.Property.IsDirect &&
                     change.Priority > BindingPriority.Animation)
            {
                for (var i = Transitions.Count -1; i >= 0; --i)
                {
                    var transition = Transitions[i];

                    if (transition.Property == change.Property &&
                        _transitionState.TryGetValue(transition, out var state))
                    {
                        var oldValue = state.BaseValue;
                        var newValue = GetAnimationBaseValue(transition.Property);

                        if (!Equals(oldValue, newValue))
                        {
                            state.BaseValue = newValue;

                            // We need to transition from the current animated value if present,
                            // instead of the old base value.
                            var animatedValue = GetValue(transition.Property);

                            if (!Equals(newValue, animatedValue))
                            {
                                oldValue = animatedValue;
                            }

                            state.Instance?.Dispose();
                            state.Instance = transition.Apply(
                                this,
                                Clock ?? AvaloniaLocator.Current.GetService<IGlobalClock>(),
                                oldValue,
                                newValue);
                            return;
                        }
                    }
                }
            }

            base.OnPropertyChangedCore(change);
        }

        private void TransitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_transitionsEnabled)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddTransitions(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveTransitions(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveTransitions(e.OldItems);
                    AddTransitions(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Transitions collection cannot be reset.");
            }
        }

        private void AddTransitions(IList items)
        {
            if (!_transitionsEnabled)
            {
                return;
            }

            _transitionState ??= new Dictionary<ITransition, TransitionState>();

            for (var i = 0; i < items.Count; ++i)
            {
                var t = (ITransition)items[i];

                _transitionState.Add(t, new TransitionState
                {
                    BaseValue = GetAnimationBaseValue(t.Property),
                });
            }
        }

        private void RemoveTransitions(IList items)
        {
            if (_transitionState is null)
            {
                return;
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var t = (ITransition)items[i];

                if (_transitionState.TryGetValue(t, out var state))
                {
                    state.Instance?.Dispose();
                    _transitionState.Remove(t);
                }
            }
        }

        private object GetAnimationBaseValue(AvaloniaProperty property)
        {
            var value = this.GetBaseValue(property, BindingPriority.LocalValue);

            if (value == AvaloniaProperty.UnsetValue)
            {
                value = GetValue(property);
            }

            return value;
        }

        private class TransitionState
        {
            public IDisposable? Instance { get; set; }
            public object? BaseValue { get; set; }
        }
    }
}
