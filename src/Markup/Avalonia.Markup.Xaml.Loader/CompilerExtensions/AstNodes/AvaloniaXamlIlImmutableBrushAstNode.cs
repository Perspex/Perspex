using Avalonia.Media;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlImmutableBrushAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly KnownColor _knownColor;
        private readonly IXamlMethod _toBrushMethod;

        public AvaloniaXamlIlImmutableBrushAstNode(IXamlLineInfo lineInfo, IXamlType type, KnownColor knownColor, IXamlMethod toBrushMethod) : base(lineInfo)
        {
            _knownColor = knownColor;
            _toBrushMethod = toBrushMethod;
            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_I4((int)_knownColor);
            codeGen.EmitCall(_toBrushMethod);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
