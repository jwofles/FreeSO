﻿using Dapper;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Database.DA.Shards;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public class SqlLots : AbstractSqlDA, ILots
    {
        public SqlLots(ISqlContext context) : base(context){
        }

        public DbLot Get(int id){
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE lot_id = @id", new { id = id }).FirstOrDefault();
        }

        /// <summary>
        /// Special. We need to create the lot and assign an owner level roommate entry immediately, so we need to use a transaction.
        /// </summary>
        /// <param name="lot"></param>
        /// <returns></returns>
        public uint Create(DbLot lot)
        {
            string failReason = "NAME";
            var t = Context.Connection.BeginTransaction();
            try
            {
                var result = (uint)Context.Connection.Query<int>("INSERT INTO fso_lots (shard_id, name, description, " +
                                        "owner_id, location, neighborhood_id, created_date, category_change_date, category, buildable_area) " +
                                        " VALUES (@shard_id, @name, @description, @owner_id, @location, " +
                                        " @neighborhood_id, @created_date, @category_change_date, @category, @buildable_area); SELECT LAST_INSERT_ID();", new
                                        {
                                            shard_id = lot.shard_id,
                                            name = lot.name,
                                            description = lot.description,
                                            owner_id = lot.owner_id,
                                            location = lot.location,
                                            neighborhood_id = lot.neighborhood_id,
                                            created_date = lot.created_date,
                                            category_change_date = lot.category_change_date,
                                            category = lot.category.ToString(),
                                            buildable_area = lot.buildable_area
                                        }).First();
                failReason = "ROOMIE";
                var roomie = new DbRoommate()
                {
                    avatar_id = lot.owner_id,
                    is_pending = 0,
                    lot_id = (int)result,
                    permissions_level = 2
                };
                var result2 = Context.Connection.Execute("INSERT INTO fso_roommates (avatar_id, lot_id, permissions_level, is_pending) " +
                    " VALUES (@avatar_id, @lot_id, @permissions_level, @is_pending);", roomie) > 0;
                if (result2)
                {
                    t.Commit();
                    return result;
                }
            } catch (SqlException)
            {
            }
            t.Rollback();
            throw new Exception(failReason);
        }

        public DbLot GetByOwner(uint owner_id)
        {
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE owner_id = @id", new { id = owner_id }).FirstOrDefault();
        }

        public IEnumerable<DbLot> All(int shard_id)
        {
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE shard_id = @shard_id", new { shard_id = shard_id });
        }

        public DbLot GetByLocation(int shard_id, uint location)
        {
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE location = @location AND shard_id = @shard_id", new { location = location, shard_id = shard_id }).FirstOrDefault();
        }

        public List<DbLot> GetAdjToLocation(int shard_id, uint location)
        {
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE "
                + "(ABS(CAST((location&65535) AS SIGNED) - CAST((@location&65535) AS SIGNED)) = 1 OR ABS(CAST((location/65536) AS SIGNED) - CAST((@location/65536) AS SIGNED)) = 1) "
                + "AND shard_id = @shard_id", new { location = location, shard_id = shard_id }).ToList();
        }

        public void RenameLot(int id, string newName)
        {
            Context.Connection.Query("UPDATE fso_lots SET name = @name WHERE lot_id = @id", new { name = newName, id = id });
        }


        public List<DbLot> SearchExact(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbLot>(
                "SELECT lot_id, location, name FROM fso_lots WHERE shard_id = @shard_id AND name = @name LIMIT @limit",
                new { shard_id = shard_id, name = name, limit = limit }
            ).ToList();
        }

        public List<DbLot> SearchWildcard(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbLot>(
                "SELECT lot_id, location, name FROM fso_lots WHERE shard_id = @shard_id AND name LIKE @name LIMIT @limit",
                new { shard_id = shard_id, name = "%" + name + "%", limit = limit }
            ).ToList();
        }

        public void UpdateRingBackup(int lot_id, sbyte ring_backup_num)
        {
            Context.Connection.Query("UPDATE fso_lots SET ring_backup_num = @ring_backup_num WHERE lot_id = @id", 
                new { ring_backup_num = ring_backup_num, id = lot_id });
        }


        public void CreateLotServerTicket(DbLotServerTicket ticket)
        {
            Context.Connection.Execute("INSERT INTO fso_lot_server_tickets VALUES (@ticket_id, @user_id, @date, @ip, @avatar_id, @lot_id, @avatar_claim_id, @avatar_claim_owner, @lot_owner)", ticket);
        }

        public void DeleteLotServerTicket(string id)
        {
            Context.Connection.Execute("DELETE FROM fso_lot_server_tickets WHERE ticket_id = @ticket_id", new { ticket_id = id });
        }

        public DbLotServerTicket GetLotServerTicket(string id)
        {
            return Context.Connection.Query<DbLotServerTicket>("SELECT * FROM fso_lot_server_tickets WHERE ticket_id = @ticket_id", new { ticket_id = id }).FirstOrDefault();
        }

        public List<DbLotServerTicket> GetLotServerTicketsForClaimedAvatar(int claim_id)
        {
            return Context.Connection.Query<DbLotServerTicket>("SELECT * FROM fso_lot_server_tickets WHERE avatar_claim_id = @claim_id", new { claim_id = claim_id }).ToList();
        }

        public void UpdateDescription(int lot_id, string description)
        {
            Context.Connection.Query("UPDATE fso_lots SET description = @desc WHERE lot_id = @id", new { id = lot_id, desc = description });
        }
    }
}
