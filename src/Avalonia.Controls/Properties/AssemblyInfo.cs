using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;
#if SIGNED_BUILD
[assembly: InternalsVisibleTo("Avalonia.Controls.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
[assembly: InternalsVisibleTo("Avalonia.DesignerSupport, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
#else
[assembly: InternalsVisibleTo("Avalonia.Controls.UnitTests")]
[assembly: InternalsVisibleTo("Avalonia.DesignerSupport")]
#endif
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Automation")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Embedding")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Presenters")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Primitives")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Shapes")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Templates")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Notifications")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Chrome")]
