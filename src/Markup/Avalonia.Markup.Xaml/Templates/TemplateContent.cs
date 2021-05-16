using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates
{
    
    public static class TemplateContent
    {
        public static TemplateResult<IControl> Load(object templateContent)
        {
            if (templateContent is Func<IServiceProvider, object> direct)
            {
                return (TemplateResult<IControl>)direct(null);
            }

            if (templateContent is null)
            {
                return null;
            }

            throw new ArgumentException(nameof(templateContent));
        }
    }
}
