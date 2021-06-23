using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    public class TransformGroup : Transform
    {
        /// <summary>
        /// Defines the <see cref="Children"/> property.
        /// </summary>
        public static readonly StyledProperty<Transforms> ChildrenProperty =
            AvaloniaProperty.Register<TransformGroup, Transforms>(nameof(Children));

        public TransformGroup()
        {
            Children = new Transforms();
            Children.ResetBehavior = ResetBehavior.Remove;
            Children.CollectionChanged += delegate
            {
                Children.ForEachItem(
                    (tr) => tr.Changed += ChildTransform_Changed,
                    (tr) => tr.Changed -= ChildTransform_Changed,
                    () => { });
            };
        }

        private void ChildTransform_Changed(object sender, System.EventArgs e)
        {
            this.RaiseChanged();
        }

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        [Content]
        public Transforms Children
        {
            get { return GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix" />.
        /// </summary>
        public override Matrix Value
        {
            get
            {
                Matrix result = Matrix.Identity;

                foreach (var t in Children)
                {
                    result *= t.Value;
                }

                return result;
            }
        }
    }

    public sealed class Transforms : AvaloniaList<Transform>
    {
    }
}
