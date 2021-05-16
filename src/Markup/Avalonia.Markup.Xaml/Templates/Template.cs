using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    public class Template : ITemplate<IControl>
    {
        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public IControl Build() => TemplateContent.Load(Content)?.Result;

        object ITemplate.Build() => Build();
    }
}
