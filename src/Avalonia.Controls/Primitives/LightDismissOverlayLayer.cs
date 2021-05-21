using System;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A layer that is used to dismiss a <see cref="Popup"/> when the user clicks outside.
    /// </summary>
    public class LightDismissOverlayLayer : Border, ICustomHitTest
    {
        public IInputElement? InputPassThroughElement { get; set; }

        static LightDismissOverlayLayer()
        {
            BackgroundProperty.OverrideDefaultValue<LightDismissOverlayLayer>(Brushes.Transparent);
        }

        /// <summary>
        /// Returns the light dismiss overlay for a specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The light dismiss overlay, or null if none found.</returns>
        public static LightDismissOverlayLayer? GetLightDismissOverlayLayer(IVisual visual)
        {
            visual = visual ?? throw new ArgumentNullException(nameof(visual));

            VisualLayerManager? manager;

            if (visual is TopLevel topLevel)
            {
                manager = topLevel.GetTemplateChildren()
                    .OfType<VisualLayerManager>()
                    .FirstOrDefault();
            }
            else
            {
                manager = visual.FindAncestorOfType<VisualLayerManager>();
            }

            return manager?.LightDismissOverlayLayer;
        }

        public bool HitTest(Point point)
        {
            var passThroughVisual = InputPassThroughElement?.GetClosestVisual();

            if (passThroughVisual != null)
            {
                var p = point.Transform(this.TransformToVisual(VisualRoot)!.Value);
                var hit = VisualRoot.GetVisualAt(p, x => x != this);

                if (hit != null)
                {
                    return !passThroughVisual.IsVisualAncestorOf(hit);
                }
            }

            return true;
        }
    }
}
