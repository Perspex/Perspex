using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class DataTemplate : IRecyclingDataTemplate
    {
        public Type DataType { get; set; }

        [Content]
        [TemplateContent]
        public object Content { get; set; }

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

        public IControl Build(object data) => Build(data, null);

        public IControl Build(object data, IControl existing)
        {
            return existing ?? TemplateContent.Load(Content)?.Control;
        }
    }
}
