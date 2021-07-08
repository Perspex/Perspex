using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Avalonia.UnitTests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Avalonia.UnitTests")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("88060192-33d5-4932-b0f9-8bd2763e857d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

#if SIGNED_BUILD
[assembly: InternalsVisibleTo("Avalonia.Base.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c1bba1142285fe0419326fb25866ba62c47e6c2b5c1ab0c95b46413fad375471232cb81706932e1cef38781b9ebd39d5100401bacb651c6c5bbf59e571e81b3bc08d2a622004e08b1a6ece82a7e0b9857525c86d2b95fab4bc3dce148558d7f3ae61aa3a234086902aeface87d9dfdd32b9d2fe3c6dd4055b5ab4b104998bd87")]
#else
[assembly: InternalsVisibleTo("Avalonia.Base.UnitTests")]
#endif
