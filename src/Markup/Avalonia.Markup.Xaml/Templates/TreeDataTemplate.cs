using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TreeDataTemplate : ITreeDataTemplate
    {
        public Type DataType { get; set; }

        [Content]
        [TemplateContent]
        public object Content { get; set; }

        [AssignBinding]
        public BindingBase ItemsSource { get; set; }

        public bool Match(object data)
        {
            if (DataType == null)
            {
                return true;
            }
            else
            {
                return DataType.IsInstanceOfType(data);
            }
        }

        public InstancedBinding ItemsSelector(object item)
        {
            if (ItemsSource != null)
            {
                var obs = ItemsSource switch
                {
                    Binding reflection => ExpressionObserverBuilder.Build(item, reflection.Path),
                    CompiledBindingExtension compiled => new ExpressionObserver(item, compiled.Path.BuildExpression(false)),
                    _ => throw new InvalidOperationException("TreeDataTemplate currently only supports Binding and CompiledBindingExtension!")
                };

                return InstancedBinding.OneWay(obs, BindingPriority.Style);
            }

            return null;
        }

        public IControl Build(object data)
        {
            var visualTreeForItem = TemplateContent.Load(Content)?.Result;
            if (visualTreeForItem != null)
            {
                visualTreeForItem.DataContext = data;
            }

            return visualTreeForItem;
        }
    }
}
