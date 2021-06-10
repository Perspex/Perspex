using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics
{
    public static class VisualTreeDebug
    {
        public static string PrintVisualTree(IVisual visual)
        {
            StringBuilder result = new StringBuilder();
            PrintVisualTree(visual, result, 0);
            return result.ToString();
        }

        private static void PrintVisualTree(IVisual visual, StringBuilder builder, int indent)
        {
            Control? control = visual as Control;

            builder.Append(Indent(indent - 1));

            if (indent > 0)
            {
                builder.Append(" +- ");
            }

            builder.Append(visual.GetType().Name);

            if (control != null)
            {
                builder.Append(" ");
                builder.AppendLine(control.Classes.ToString());

                foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(control))
                {
                    var value = control.GetDiagnostic(property);

                    if (value.Priority != BindingPriority.Unset)
                    {
                        builder.Append(Indent(indent));
                        builder.Append(" |  ");
                        builder.Append(value.Property.Name);
                        builder.Append(" = ");
                        builder.Append(value.Value ?? "(null)");
                        builder.Append(" [");
                        builder.Append(value.Priority);
                        builder.AppendLine("]");
                    }
                }
            }
            else
            {
                builder.AppendLine();
            }

            foreach (var child in visual.VisualChildren)
            {
                PrintVisualTree(child, builder, indent + 1);
            }
        }

        private static string Indent(int indent)
        {
            return new string(' ' , Math.Max(indent, 0) * 4);
        }
    }
}
