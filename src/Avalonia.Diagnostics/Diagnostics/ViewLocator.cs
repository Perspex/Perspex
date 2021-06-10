using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics.ViewModels;

namespace Avalonia.Diagnostics
{
    internal class ViewLocator : IDataTemplate
    {
        public IControl Build(object data)
        {
            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            else
            {
                return new TextBlock { Text = name };
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
