using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.HotReload
{
    internal class AvaloniaXamlAstLoader
    {
        private static SreTypeSystem _sreTypeSystem;
        private static ModuleBuilder _sreBuilder;
        private static IXamlType _sreContextType;
        private static XamlLanguageTypeMappings _sreMappings;
        private static XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> _sreEmitMappings;
        private static XamlXmlnsMappings _sreXmlns;
        private static AssemblyBuilder _sreAsm;

        public static IXamlTypeSystem TypeSystem => _sreTypeSystem;

        public static List<RecordingIlEmitter.RecordedInstruction> Load(
            string xaml,
            string filePath,
            Type objectType,
            Assembly localAssembly = null,
            object rootInstance = null,
            Uri uri = null,
            bool patchIl = true)
        {
            InitializeSre();
            
            var compilerSuite = CreateCompilerSuite(localAssembly, uri);
            var compiler = compilerSuite.Compiler;
            var configuration = compilerSuite.Configuration;
            var typeBuilder = compilerSuite.TypeBuilder;

            var compatibilityMappings = new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            };

            var parsed = XDocumentXamlParser.Parse(xaml, compatibilityMappings);
            var rootType = GetRootType(parsed, configuration, rootInstance);

            var xamlTypeBuilder = _sreTypeSystem.CreateTypeBuilder(typeBuilder);
            IFileSource fileSource = new FileSource(filePath, Encoding.UTF8.GetBytes(xaml));
            
            compiler.OverrideRootType(parsed, rootType);
            compiler.Transform(parsed);

            var instructions = compiler.Compile(
                parsed,
                xamlTypeBuilder,
                _sreContextType,
                AvaloniaXamlIlCompiler.PopulateName,
                AvaloniaXamlIlCompiler.BuildName,
                "__AvaloniaXamlIlNsInfo",
                uri?.ToString(),
                fileSource);

            if (patchIl)
            {
                var xamlMethod = xamlTypeBuilder
                    .CreateType().Methods
                    .Single(x => x.Name == AvaloniaXamlIlCompiler.PopulateName);

                var methodInfo = xamlMethod
                    ?.GetType()
                    .GetProperty("Method", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(xamlMethod) as MethodInfo;

                Action<object> action = x =>
                {
                    var provider = XamlIlRuntimeHelpers.CreateRootServiceProviderV2();
                    methodInfo.Invoke(null, new[] { provider, x });
                };

                Patch(objectType, action);
            }

            compilerSuite.TypeBuilder.CreateTypeInfo();
            compilerSuite.ClrPropertyBuilder.CreateTypeInfo();
            compilerSuite.IndexerClosureType.CreateTypeInfo();

            return instructions;
        }

        private static void InitializeSre()
        {
            _sreTypeSystem ??= new SreTypeSystem();

            if (_sreBuilder == null)
            {
                var name = new AssemblyName(Guid.NewGuid().ToString("N"));
                _sreAsm ??= AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);

                _sreBuilder = _sreAsm.DefineDynamicModule("XamlIlLoader.ildump");
            }

            if (_sreMappings == null)
            {
                (_sreMappings, _sreEmitMappings) = AvaloniaXamlIlLanguage.Configure(_sreTypeSystem);
            }

            _sreXmlns ??= XamlXmlnsMappings.Resolve(_sreTypeSystem, _sreMappings);

            _sreContextType ??= XamlILContextDefinition.GenerateContextClass(
                _sreTypeSystem.CreateTypeBuilder(_sreBuilder.DefineType("XamlIlContext", TypeAttributes.Public)),
                _sreTypeSystem,
                _sreMappings,
                _sreEmitMappings);
        }

        private static AstTransformationContext CreateTransformationContext(
            XamlDocument doc,
            TransformerConfiguration configuration,
            bool strict)
        {
            return new AstTransformationContext(configuration, doc.NamespaceAliases, strict);
        }
        
        private static void Patch(Type xamlType, Action<object> buildAction)
        {
            var overrideField = xamlType.GetField(
                "!XamlIlPopulateOverride",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (overrideField == null)
            {
                Logger
                    .TryGet(LogEventLevel.Warning, "HotReload")
                    ?.Log(null, "!XamlIlPopulateOverride field is not found. Ignoring patch request.");
                
                return;
            }

            overrideField.SetValue(null, buildAction);
        }

        private static CompilerSuite CreateCompilerSuite(
            Assembly localAssembly = null,
            Uri uri = null)
        {
            var assembly = localAssembly == null ? null : _sreTypeSystem.GetAssembly(localAssembly);
            var typeBuilder = _sreBuilder.DefineType(
                "Builder_" + Guid.NewGuid().ToString("N") + "_" + uri,
                TypeAttributes.Public);
            
            var clrPropertyBuilder = typeBuilder.DefineNestedType("ClrProperties_" + Guid.NewGuid().ToString("N"));
            var indexerClosureType = _sreBuilder.DefineType("IndexerClosure_" + Guid.NewGuid().ToString("N"));

            var configuration = new AvaloniaXamlIlCompilerConfiguration(
                _sreTypeSystem,
                assembly,
                _sreMappings,
                _sreXmlns,
                AvaloniaXamlIlLanguage.CustomValueConverter,
                new XamlIlClrPropertyInfoEmitter(_sreTypeSystem.CreateTypeBuilder(clrPropertyBuilder)),
                new XamlIlPropertyInfoAccessorFactoryEmitter(_sreTypeSystem.CreateTypeBuilder(indexerClosureType)));

            var compiler = new AvaloniaXamlIlCompiler(
                configuration,
                _sreEmitMappings,
                _sreContextType)
            {
                EnableIlVerification = true
            };
            
            return new CompilerSuite(compiler, configuration, typeBuilder, clrPropertyBuilder, indexerClosureType);
        }

        private static XamlAstClrTypeReference GetRootType(
            XamlDocument document,
            AvaloniaXamlIlCompilerConfiguration configuration,
            object rootInstance)
        {
            IXamlType overrideType = null;
            
            if (rootInstance != null)
            {
                overrideType = _sreTypeSystem.GetType(rootInstance.GetType());
            }
            
            var rootObject = (XamlAstObjectNode)document.Root;

            var classDirective = rootObject.Children
                .OfType<XamlAstXmlDirective>()
                .FirstOrDefault(x => x.Namespace == XamlNamespaces.Xaml2006 && x.Name == "Class");

            var rootType = classDirective != null
                ? new XamlAstClrTypeReference(
                    classDirective,
                    _sreTypeSystem.GetType(((XamlAstTextNode)classDirective.Values[0]).Text),
                    false)
                : TypeReferenceResolver.ResolveType(
                    CreateTransformationContext(document, configuration, true),
                    (XamlAstXmlTypeReference)rootObject.Type, true);

            if (overrideType != null)
            {
                if (!rootType.Type.IsAssignableFrom(overrideType))
                {
                    var message = $"Unable to substitute {rootType.Type.GetFqn()} with {overrideType.GetFqn()}";
                    throw new XamlX.XamlLoadException(message, rootObject);
                }

                rootType = new XamlAstClrTypeReference(rootObject, overrideType, false);
            }

            return rootType;
        }

        private class CompilerSuite
        {
            public AvaloniaXamlIlCompiler Compiler { get; }
            public AvaloniaXamlIlCompilerConfiguration Configuration { get; }
            public TypeBuilder TypeBuilder { get; }
            public TypeBuilder ClrPropertyBuilder { get; }
            public TypeBuilder IndexerClosureType { get; }

            public CompilerSuite(
                AvaloniaXamlIlCompiler compiler,
                AvaloniaXamlIlCompilerConfiguration configuration,
                TypeBuilder typeBuilder,
                TypeBuilder clrPropertyBuilder,
                TypeBuilder indexerClosureType)
            {
                Compiler = compiler;
                Configuration = configuration;
                TypeBuilder = typeBuilder;
                ClrPropertyBuilder = clrPropertyBuilder;
                IndexerClosureType = indexerClosureType;
            }
        }

        private class FileSource : IFileSource
        {
            public string FilePath { get; }
            public byte[] FileContents { get; }

            public FileSource(string filePath, byte[] fileContents)
            {
                FilePath = filePath;
                FileContents = fileContents;
            }
        }
    }
}
