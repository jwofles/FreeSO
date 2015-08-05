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
using FSO.SimAntics.Engine.Scopes;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Utils;

namespace FSO.SimAntics.Primitives
{
    public class VMTestObjectType : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMTestObjectTypeOperand>();
            var objectID = VMMemory.GetVariable(context, operand.IdOwner, operand.IdData);

            var obj = context.VM.GetObjectById(objectID);
            //var obj = context.StackObject;
            if (obj == null){
                return VMPrimitiveExitCode.ERROR;
            }

            if (obj.Object.GUID == operand.GUID) return VMPrimitiveExitCode.GOTO_TRUE; //is my guid same?
            else if (obj.MasterDefinition != null && (obj.MasterDefinition.GUID == operand.GUID)) return VMPrimitiveExitCode.GOTO_TRUE; //is master guid same?
            else return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMTestObjectTypeOperand : VMPrimitiveOperand
    {
        public uint GUID;
        public ushort IdData;
        public VMVariableScope IdOwner;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                GUID = io.ReadUInt32();
                IdData = io.ReadUInt16();
                IdOwner = (VMVariableScope)io.ReadByte();
            }
        }
        #endregion
    }
}
