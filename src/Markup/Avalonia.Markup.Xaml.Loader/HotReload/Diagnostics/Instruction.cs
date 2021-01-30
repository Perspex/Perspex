//
// Instruction.cs
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
using System.Reflection.Emit;
using System.Text;

namespace Mono.Reflection {

	public sealed class Instruction {

		int offset;
		OpCode opcode;
		object operand;

		Instruction previous;
		Instruction next;

		public int Offset {
			get { return offset; }
		}

		public OpCode OpCode {
			get { return opcode; }
		}

		public object Operand {
			get { return operand; }
			internal set { operand = value; }
		}

		public Instruction Previous {
			get { return previous; }
			internal set { previous = value; }
		}

		public Instruction Next	{
			get { return next; }
			internal set { next = value; }
		}

		public int Size {
			get {
				int size = opcode.Size;

				switch (opcode.OperandType) {
				case OperandType.InlineSwitch:
					size += (1 + ((Instruction []) operand).Length) * 4;
					break;
				case OperandType.InlineI8:
				case OperandType.InlineR:
					size += 8;
					break;
				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					size += 4;
					break;
				case OperandType.InlineVar:
					size += 2;
					break;
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					size += 1;
					break;
				}

				return size;
			}
		}

		internal Instruction (int offset, OpCode opcode)
		{
			this.offset = offset;
			this.opcode = opcode;
		}

		public override string ToString ()
		{
			var instruction = new StringBuilder ();

			AppendLabel (instruction, this);
			instruction.Append (':');
			instruction.Append (' ');
			instruction.Append (opcode.Name);

			if (operand == null)
				return instruction.ToString ();

			instruction.Append (' ');

			switch (opcode.OperandType) {
			case OperandType.ShortInlineBrTarget:
			case OperandType.InlineBrTarget:
				AppendLabel (instruction, (Instruction) operand);
				break;
			case OperandType.InlineSwitch:
				var labels = (Instruction []) operand;
				for (int i = 0; i < labels.Length; i++) {
					if (i > 0)
						instruction.Append (',');

					AppendLabel (instruction, labels [i]);
				}
				break;
			case OperandType.InlineString:
				instruction.Append ('\"');
				instruction.Append (operand);
				instruction.Append ('\"');
				break;
			default:
				instruction.Append (operand);
				break;
			}

			return instruction.ToString ();
		}

		static void AppendLabel (StringBuilder builder, Instruction instruction)
		{
			builder.Append ("IL_");
			builder.Append (instruction.offset.ToString ("x4"));
		}
	}
}
