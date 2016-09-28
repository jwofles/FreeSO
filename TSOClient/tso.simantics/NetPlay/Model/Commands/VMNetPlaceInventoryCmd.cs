﻿using FSO.LotView.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetPlaceInventoryCmd : VMNetCommandBodyAbstract
    {
        //request from client
        public uint ObjectPID;
        public short x;
        public short y;
        public sbyte level;
        public Direction dir;

        //data sent back to the client only
        public uint GUID;
        public byte[] Data;

        //internal
        private VMMultitileGroup CreatedGroup;
        public bool Verified;

        /// <summary>
        /// Sent to the client after the object data is retrieved by the client. If data is 0 length, intantiate a new object of the type.
        /// Can rarely cause the object to be sent BACK to inventory if it cannot be placed in the intended location.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;

            //careful here! if the object can't be placed, we have to give the user their money back.
            if (TryPlace(vm, caller))
            {
                return true;
            }
            else
            {
                //oops, we can't place this object or some other issue occured. move it back to inventory.
                if (CreatedGroup != null) CreatedGroup.Delete(vm.Context);
                if (vm.GlobalLink != null)
                {
                    //do a force move to inventory here. do not need to resave state, since it has not changed since the place.
                    //if the server crashes between setting the object on this lot and now, the
                    //server should detect that an object says it is "on" this lot, but it really isn't physically
                    //and move it back to inventory without a state save.
                    //
                    //owned objects that are "out of world" should be moved to inventory on load. (FULL move, for safety)
                    vm.GlobalLink.ForceInInventory(vm, ObjectPID, (bool success, uint pid) => {}); 
                }
            }
            return false;
        }

        private bool TryPlace(VM vm, VMAvatar caller)
        {
            VMStandaloneObjectMarshal state;

            if ((Data?.Length ?? 0) == 0) state = null;
            else
            {
                state = new VMStandaloneObjectMarshal();
                try
                {
                    using (var reader = new BinaryReader(new MemoryStream(Data)))
                    {
                        state.Deserialize(reader);
                    }
                }
                catch (Exception)
                {
                    //failed to restore state
                    state = null;
                }
            }

            
            if (state != null)
            {
                CreatedGroup = state.CreateInstance(vm);
                CreatedGroup.ChangePosition(new LotTilePos(x, y, level), dir, vm.Context, VMPlaceRequestFlags.UserPlacement);
                if (CreatedGroup.Objects.Count == 0) return false;
                if (CreatedGroup.BaseObject.Position == LotTilePos.OUT_OF_WORLD)
                {
                    return false;
                }
            }
            else
            {
                var catalog = Content.Content.Get().WorldCatalog;
                var item = catalog.GetItemByGUID(GUID);

                CreatedGroup = vm.Context.CreateObjectInstance(GUID, LotTilePos.OUT_OF_WORLD, dir);
                if (CreatedGroup == null) return false;
                CreatedGroup.ChangePosition(new LotTilePos(x, y, level), dir, vm.Context, VMPlaceRequestFlags.UserPlacement);

                CreatedGroup.ExecuteEntryPoint(11, vm.Context); //User Placement
                if (CreatedGroup.Objects.Count == 0) return false;

                int salePrice = 0;
                if (item != null) salePrice = (int)item.Value.Price;
                var def = CreatedGroup.BaseObject.MasterDefinition;
                if (def == null) def = CreatedGroup.BaseObject.Object.OBJ;
                var limit = def.DepreciationLimit;
                if (salePrice > limit) //only try to deprecate if we're above the limit. Prevents objects with a limit above their price being money fountains.
                {
                    salePrice -= def.InitialDepreciation;
                    if (salePrice < limit) salePrice = limit;
                }

                CreatedGroup.Price = (int)salePrice;

                if (CreatedGroup.BaseObject.Position == LotTilePos.OUT_OF_WORLD)
                {
                    return false;
                }
            }

            foreach (var obj in CreatedGroup.Objects)
            {
                if (obj is VMGameObject) ((VMTSOObjectState)obj.TSOState).OwnerID = caller.PersistID;
                obj.PersistID = ObjectPID;
            }

            //is this my sim's object? try remove it from our local inventory representaton
            if (((VMTSOObjectState)CreatedGroup.BaseObject.TSOState).OwnerID == vm.MyUID)
            {
                var index = vm.MyInventory.FindIndex(x => x.ObjectPID == ObjectPID);
                if (index != -1) vm.MyInventory.RemoveAt(index);
            }

            vm.SignalChatEvent(new VMChatEvent(caller.PersistID, VMChatEventType.Arch,
                caller.Name,
                vm.GetUserIP(caller.PersistID),
                "placed (from inventory) " + CreatedGroup.BaseObject.ToString() + " at (" + x / 16f + ", " + y / 16f + ", " + level + ")"
            ));
            return true;
        }

        /// <summary>
        /// Called on server when someone requests to place an object from inventory. 
        /// Immediately "fails" verification to be queued later, once the data has been retrieved.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true; //set internally when transaction succeeds. trust that the verification happened.
            if (caller == null || //caller must be on lot, be a roommate.
                ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate)
                return false;

            vm.GlobalLink.RetrieveFromInventory(vm, ObjectPID, caller.PersistID, (uint guid, byte[] data) =>
            {
                if (guid == 0) return; //todo: error feedback?
                GUID = guid;
                Data = data;
                Verified = true;
                vm.ForwardCommand(this);
            });

            return false;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectPID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write((byte)dir);

            writer.Write(GUID);
            writer.Write((Data?.Length)??0);
            if (Data != null) writer.Write(Data);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectPID = reader.ReadUInt32();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();

            GUID = reader.ReadUInt32();
            var length = reader.ReadInt32();
            if (length > 4096) throw new Exception("Object data cannot be this large!");
            Data = reader.ReadBytes(length);
        }

        #endregion
    }
}
