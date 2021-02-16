using Avalonia.Media;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlVisualElementAstNode<T> : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly IXamlConstructor _constructor;
        private readonly T _element;

        public AvaloniaXamlIlVisualElementAstNode(IXamlLineInfo lineInfo, IXamlType type, IXamlConstructor constructor,
            T element)
            : base(lineInfo)
        {
            _constructor = constructor;
            _element = element;

            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (_element is RelativePoint relativePoint)
            {
                codeGen.Ldc_R8(relativePoint.Point.X);
                codeGen.Ldc_R8(relativePoint.Point.Y);
                codeGen.Ldc_I4((int)relativePoint.Unit);
            }
            else if (_element is SolidColorBrush solidColorBrush)
            {
                codeGen.Ldc_I4((int)solidColorBrush.Color.ToUint32());
            }

            codeGen.Newobj(_constructor);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
