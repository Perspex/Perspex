//
// BackingFieldResolver.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.Reflection {

	public static class BackingFieldResolver {

		class FieldPattern : ILPattern {

			public static object FieldKey = new object ();

			ILPattern pattern;

			public FieldPattern (ILPattern pattern)
			{
				this.pattern = pattern;
			}

			public override void Match (MatchContext context)
			{
				pattern.Match (context);
				if (!context.success)
					return;

				var match = GetLastMatchingInstruction (context);
				var field = (FieldInfo) match.Operand;
				context.AddData (FieldKey, field);
			}
		}

		static ILPattern Field (OpCode opcode)
		{
			return new FieldPattern (ILPattern.OpCode (opcode));
		}

		static ILPattern GetterPattern =
			ILPattern.Sequence (
				ILPattern.Optional (OpCodes.Nop),
				ILPattern.Either (
					Field (OpCodes.Ldsfld),
					ILPattern.Sequence (
						ILPattern.OpCode (OpCodes.Ldarg_0),
						Field (OpCodes.Ldfld))),
				ILPattern.Optional (
					ILPattern.Sequence (
						ILPattern.OpCode (OpCodes.Stloc_0),
						ILPattern.OpCode (OpCodes.Br_S),
						ILPattern.OpCode (OpCodes.Ldloc_0))),
				ILPattern.Optional(ILPattern.OpCode(OpCodes.Br_S)),
				ILPattern.OpCode (OpCodes.Ret));

		static ILPattern SetterPattern =
			ILPattern.Sequence (
				ILPattern.Optional (OpCodes.Nop),
				ILPattern.OpCode (OpCodes.Ldarg_0),
				ILPattern.Either (
					Field (OpCodes.Stsfld),
					ILPattern.Sequence (
						ILPattern.OpCode (OpCodes.Ldarg_1),
						Field (OpCodes.Stfld))),
				ILPattern.OpCode (OpCodes.Ret));

		static FieldInfo GetBackingField (MethodInfo method, ILPattern pattern)
		{
			var result = ILPattern.Match (method, pattern);
			if (!result.success)
				throw new ArgumentException ();

			object value;
			if (!result.TryGetData (FieldPattern.FieldKey, out value))
				throw new InvalidOperationException ();

			return (FieldInfo) value;
		}

		public static FieldInfo GetBackingField (this PropertyInfo self)
		{
			if (self == null)
				throw new ArgumentNullException ("self");

			var getter = self.GetGetMethod (true);
			if (getter != null)
				return GetBackingField (getter, GetterPattern);

			var setter = self.GetSetMethod (true);
			if (setter != null)
				return GetBackingField (setter, SetterPattern);

			throw new ArgumentException ();
		}
	}
}
