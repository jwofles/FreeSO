﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.SimAntics.Primitives
{
    public class VMSleep : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMSleepOperand>();

            if (context.Thread.Interrupt)
            {
                context.Thread.Interrupt = false;
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            var ticks = VMMemory.GetVariable(context, FSO.SimAntics.Engine.Scopes.VMVariableScope.Parameters, operand.StackVarToDec);
            ticks--;

            if (ticks < 0)
            {
                return VMPrimitiveExitCode.GOTO_TRUE;
            }
            else
            {
                VMMemory.SetVariable(context, FSO.SimAntics.Engine.Scopes.VMVariableScope.Parameters, operand.StackVarToDec, ticks);
                return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
            }
        }
    }

    public class VMSleepOperand : VMPrimitiveOperand
    {
        public ushort StackVarToDec;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                StackVarToDec = io.ReadUInt16();
            }
        }
        #endregion
    }
}
