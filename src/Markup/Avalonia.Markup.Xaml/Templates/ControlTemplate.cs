using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public Type TargetType { get; set; }

        public TemplateResult<IControl> Build(ITemplatedControl control) => TemplateContent.Load(Content);
    }
}
