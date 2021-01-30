//
// MethodBodyReader.cs
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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.Reflection {

	class MethodBodyReader {

		static readonly OpCode [] one_byte_opcodes;
		static readonly OpCode [] two_bytes_opcodes;

		static MethodBodyReader ()
		{
			one_byte_opcodes = new OpCode [0xe1];
			two_bytes_opcodes = new OpCode [0x1f];

			var fields = typeof (OpCodes).GetFields (
				BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields) {
				var opcode = (OpCode) field.GetValue (null);
				if (opcode.OpCodeType == OpCodeType.Nternal)
					continue;

				if (opcode.Size == 1)
					one_byte_opcodes [opcode.Value] = opcode;
				else
					two_bytes_opcodes [opcode.Value & 0xff] = opcode;
			}
		}

		readonly MethodBase method;
		readonly MethodBody body;
		readonly Module module;
		readonly Type [] type_arguments;
		readonly Type [] method_arguments;
		readonly ByteBuffer il;
		readonly ParameterInfo this_parameter;
		readonly ParameterInfo [] parameters;
		readonly IList<LocalVariableInfo> locals;
		readonly List<Instruction> instructions;

		MethodBodyReader (MethodBase method)
		{
			this.method = method;

			this.body = method.GetMethodBody ();
			if (this.body == null)
				throw new ArgumentException ("Method has no body");

			var bytes = body.GetILAsByteArray ();
			if (bytes == null)
				throw new ArgumentException ("Can not get the body of the method");

			if (!(method is ConstructorInfo))
				method_arguments = method.GetGenericArguments ();

			if (method.DeclaringType != null)
				type_arguments = method.DeclaringType.GetGenericArguments ();

			if (!method.IsStatic)
				this.this_parameter = new ThisParameter (method);
			this.parameters = method.GetParameters ();
			this.locals = body.LocalVariables;
			this.module = method.Module;
			this.il = new ByteBuffer (bytes);
			this.instructions = new List<Instruction> ((bytes.Length + 1) / 2);
		}

		void ReadInstructions ()
		{
			Instruction previous = null;

			while (il.position < il.buffer.Length) {
				var instruction = new Instruction (il.position, ReadOpCode ());

				ReadOperand (instruction);

				if (previous != null) {
					instruction.Previous = previous;
					previous.Next = instruction;
				}

				instructions.Add (instruction);
				previous = instruction;
			}

			ResolveBranches ();
		}

		void ReadOperand (Instruction instruction)
		{
			switch (instruction.OpCode.OperandType) {
			case OperandType.InlineNone:
				break;
			case OperandType.InlineSwitch:
				int length = il.ReadInt32 ();
				int base_offset = il.position + (4 * length);
				int [] branches = new int [length];
				for (int i = 0; i < length; i++)
					branches [i] = il.ReadInt32 () + base_offset;

				instruction.Operand = branches;
				break;
			case OperandType.ShortInlineBrTarget:
				instruction.Operand = (((sbyte) il.ReadByte ()) + il.position);
				break;
			case OperandType.InlineBrTarget:
				instruction.Operand = il.ReadInt32 () + il.position;
				break;
			case OperandType.ShortInlineI:
				if (instruction.OpCode == OpCodes.Ldc_I4_S)
					instruction.Operand = (sbyte) il.ReadByte ();
				else
					instruction.Operand = il.ReadByte ();
				break;
			case OperandType.InlineI:
				instruction.Operand = il.ReadInt32 ();
				break;
			case OperandType.ShortInlineR:
				instruction.Operand = il.ReadSingle ();
				break;
			case OperandType.InlineR:
				instruction.Operand = il.ReadDouble ();
				break;
			case OperandType.InlineI8:
				instruction.Operand = il.ReadInt64 ();
				break;
			case OperandType.InlineSig:
				instruction.Operand = module.ResolveSignature (il.ReadInt32 ());
				break;
			case OperandType.InlineString:
				instruction.Operand = module.ResolveString (il.ReadInt32 ());
				break;
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.InlineMethod:
			case OperandType.InlineField:
				instruction.Operand = module.ResolveMember (il.ReadInt32 (), type_arguments, method_arguments);
				break;
			case OperandType.ShortInlineVar:
				instruction.Operand = GetVariable (instruction, il.ReadByte ());
				break;
			case OperandType.InlineVar:
				instruction.Operand = GetVariable (instruction, il.ReadInt16 ());
				break;
			default:
				throw new NotSupportedException ();
			}
		}

		void ResolveBranches ()
		{
			foreach (var instruction in instructions) {
				switch (instruction.OpCode.OperandType) {
				case OperandType.ShortInlineBrTarget:
				case OperandType.InlineBrTarget:
					instruction.Operand = GetInstruction (instructions, (int) instruction.Operand);
					break;
				case OperandType.InlineSwitch:
					var offsets = (int []) instruction.Operand;
					var branches = new Instruction [offsets.Length];
					for (int j = 0; j < offsets.Length; j++)
						branches [j] = GetInstruction (instructions, offsets [j]);

					instruction.Operand = branches;
					break;
				}
			}
		}

		static Instruction GetInstruction (List<Instruction> instructions, int offset)
		{
			var size = instructions.Count;
			if (offset < 0 || offset > instructions [size - 1].Offset)
				return null;

			int min = 0;
			int max = size - 1;
			while (min <= max) {
				int mid = min + ((max - min) / 2);
				var instruction = instructions [mid];
				var instruction_offset = instruction.Offset;

				if (offset == instruction_offset)
					return instruction;

				if (offset < instruction_offset)
					max = mid - 1;
				else
					min = mid + 1;
			}

			return null;
		}

		object GetVariable (Instruction instruction, int index)
		{
			return TargetsLocalVariable (instruction.OpCode)
				? (object) GetLocalVariable (index)
				: (object) GetParameter (index);
		}

		static bool TargetsLocalVariable (OpCode opcode)
		{
			return opcode.Name.Contains ("loc");
		}

		LocalVariableInfo GetLocalVariable (int index)
		{
			return locals [index];
		}

		ParameterInfo GetParameter (int index)
		{
			if (method.IsStatic)
				return parameters [index];

			if (index == 0)
				return this_parameter;

			return parameters [index - 1];
		}

		OpCode ReadOpCode ()
		{
			byte op = il.ReadByte ();
			return op != 0xfe
				? one_byte_opcodes [op]
				: two_bytes_opcodes [il.ReadByte ()];
		}

		public static List<Instruction> GetInstructions (MethodBase method)
		{
			var reader = new MethodBodyReader (method);
			reader.ReadInstructions ();
			return reader.instructions;
		}

		class ThisParameter : ParameterInfo
		{
			public ThisParameter (MethodBase method)
			{
				this.MemberImpl = method;
				this.ClassImpl = method.DeclaringType;
				this.NameImpl = "this";
				this.PositionImpl = -1;
			}
		}
	}
}
