using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Input.TextInput
{
    class TransformTrackingHelper : IDisposable
    {
        private IVisual? _visual;
        private bool _queuedForUpdate;
        private readonly EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChangedHandler;
        private readonly List<Visual> _propertyChangedSubscriptions = new List<Visual>();
        
        public TransformTrackingHelper()
        {
            _propertyChangedHandler = PropertyChangedHandler;
        }

        public void SetVisual(IVisual? visual)
        {
            Dispose();
            _visual = visual;
            if (visual != null)
            {
                visual.AttachedToVisualTree += OnAttachedToVisualTree;
                visual.DetachedFromVisualTree -= OnDetachedFromVisualTree;
                if (visual.IsAttachedToVisualTree)
                    SubscribeToParents();
                UpdateMatrix();
            }
        }
        
        public Matrix? Matrix { get; private set; }
        public event Action? MatrixChanged;
        
        public void Dispose()
        {
            if(_visual == null)
                return;
            UnsubscribeFromParents();
            _visual.AttachedToVisualTree -= OnAttachedToVisualTree;
            _visual.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            _visual = null;
        }

        private void SubscribeToParents()
        {
            var visual = _visual;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // false positive
            while (visual != null)
            {
                if (visual is Visual v)
                {
                    v.PropertyChanged += _propertyChangedHandler;
                    _propertyChangedSubscriptions.Add(v);
                }

                visual = visual.VisualParent;
            }
        }

        private void UnsubscribeFromParents()
        {
            foreach (var v in _propertyChangedSubscriptions)
                v.PropertyChanged -= _propertyChangedHandler;
            _propertyChangedSubscriptions.Clear();
        }

        void UpdateMatrix()
        {
            Matrix? matrix = null;
            if (_visual != null && _visual.VisualRoot != null)
                matrix = _visual.TransformToVisual(_visual.VisualRoot);
            if (Matrix != matrix)
            {
                Matrix = matrix;
                MatrixChanged?.Invoke();
            }
        }

        private void OnAttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
        {
            SubscribeToParents();
            UpdateMatrix();
        }

        private void EnqueueForUpdate()
        {
            if(_queuedForUpdate)
                return;
            _queuedForUpdate = true;
            Dispatcher.UIThread.Post(UpdateMatrix, DispatcherPriority.Render);
        }

        private void PropertyChangedHandler(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.IsEffectiveValueChange && e.Property == Visual.BoundsProperty)
                EnqueueForUpdate();
        }

        private void OnDetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
        {
            UnsubscribeFromParents();
            UpdateMatrix();
        }
    }
}
