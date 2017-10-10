using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using JetBrains.Annotations;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using Permission = UserSpecificFunctions.Permissions.Permission;

namespace UserSpecificFunctions.Database
{
    /// <summary>
    /// Represents the database manager.
    /// </summary>
    public sealed class DatabaseManager : IDisposable
    {
        private readonly List<PlayerInfo> _cache = new List<PlayerInfo>();
        private readonly IDbConnection _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseManager"/> class.
        /// </summary>
        public DatabaseManager()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    var dbHost = TShock.Config.MySqlHost.Split(':');
                    _db = new MySqlConnection
                    {
                        ConnectionString =
                            $"Server={dbHost[0]}; " +
                            $"Port={(dbHost.Length == 1 ? "3306" : dbHost[1])}; " +
                            $"Database={TShock.Config.MySqlDbName}; " +
                            $"Uid={TShock.Config.MySqlUsername}; " +
                            $"Pwd={TShock.Config.MySqlPassword};"
                    };
                    break;

                default:
                    var sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
                    _db = new SqliteConnection($"uri=file://{sql},Version=3");
                    break;
            }

            _db.Query("CREATE TABLE IF NOT EXISTS UserSpecificFunctions (" +
                      "UserId INTEGER NOT NULL, " +
                      "Prefix TEXT DEFAULT NULL, " +
                      "Suffix TEXT DEFAULT NULL, " +
                      "Color TEXT DEFAULT NULL, " +
                      "PRIMARY KEY(UserId))");
            _db.Query("CREATE TABLE IF NOT EXISTS UserHasPermission (" +
                      "UserId INTEGER, " +
                      "Permission TEXT NOT NULL, " +
                      "IsNegated INTEGER, " +
                      "FOREIGN KEY(UserId) REFERENCES UserSpecificFunctions(UserId) ON DELETE CASCADE)");
        }

        /// <summary>
        /// Disposes the database connection.
        /// </summary>
        public void Dispose()
        {
            _db.Dispose();
        }

        /// <summary>
        /// Inserts a new object into the database.
        /// </summary>
        /// <param name="playerInfo">The object.</param>
        public void Add(PlayerInfo playerInfo)
        {
            _cache.Add(playerInfo);
            if ((playerInfo.ChatData.Prefix ?? playerInfo.ChatData.Suffix ?? playerInfo.ChatData.Color) != null)
            {
                _db.Query("INSERT INTO UserSpecificFunctions (UserId, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3)",
                    playerInfo.UserId, playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix,
                    playerInfo.ChatData.Color);
            }
            foreach (var permission in playerInfo.Permissions.GetPermissions()
                .Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                _db.Query("INSERT INTO UserHasPermission (UserId, Permission, IsNegated) VALUES (@0, @1, @2)",
                    playerInfo.UserId, permission.Name, permission.Negated ? 1 : 0);
            }
        }

        /// <summary>
        /// Returns a <see cref="PlayerInfo"/> object that matches the given user object.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object.</param>
        /// <returns>The <see cref="PlayerInfo"/> object associated with the user.</returns>
        [CanBeNull]
        public PlayerInfo Get(User user)
        {
            return _cache.SingleOrDefault(p => p.UserId == user.ID);
        }

        /// <summary>
        /// Loads the database.
        /// </summary>
        public void Load()
        {
            _cache.Clear();
            using (var reader = _db.QueryReader("SELECT * FROM UserSpecificFunctions"))
            {
                if (!reader.Read())
                {
                    using (var reader2 = _db.QueryReader("SELECT * FROM UserHasPermission"))
                    {
                        while (reader2.Read())
                        {
                            var userId = reader2.Get<int>("UserId");
                            var permission = reader2.Get<string>("Permission");
                            var negated = reader2.Get<int>("IsNegated") == 1;

                            var playerInfo = _cache.SingleOrDefault(p => p.UserId == userId);
                            if (playerInfo == null)
                            {
                                playerInfo = new PlayerInfo(userId, new ChatData());
                                playerInfo.Permissions.AddPermission(new Permission(permission, negated));
                                _cache.Add(playerInfo);
                            }
                            else
                            {
                                playerInfo.Permissions.AddPermission(new Permission(permission, negated));
                            }
                        }
                    }
                }
                else
                {
                    while (reader.Read())
                    {
                        var userId = reader.Get<int>("UserId");
                        var chatData = new ChatData(reader.Get<string>("Color"), reader.Get<string>("Prefix"),
                            reader.Get<string>("Suffix"));

                        var playerInfo = new PlayerInfo(userId, chatData);
                        using (var permissionReader =
                            _db.QueryReader("SELECT * FROM UserHasPermission WHERE UserId = @0", userId))
                        {
                            while (permissionReader.Read())
                            {
                                var permission = permissionReader.Get<string>("Permission");
                                var negated = permissionReader.Get<int>("IsNegated") == 1;
                                playerInfo.Permissions.AddPermission(new Permission(permission, negated));
                            }
                        }
                        _cache.Add(playerInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the given user from the database.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Remove(User user)
        {
            _cache.RemoveAll(p => p.UserId == user.ID);
            _db.Query("DELETE FROM UserSpecificFunctions WHERE UserID = @0", user.ID);
        }

        /// <summary>
        /// Updates the given player.
        /// </summary>
        /// <param name="playerInfo">The player.</param>
        public void Update(PlayerInfo playerInfo)
        {
            _db.Query("UPDATE UserSpecificFunctions SET Prefix = @0, Suffix = @1, Color = @2 WHERE UserId = @3",
                playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color, playerInfo.UserId);
            _db.Query("DELETE FROM UserHasPermission WHERE UserId = @0", playerInfo.UserId);
            using (var db = _db.CloneEx())
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        using (var command = (SqliteCommand) db.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO UserHasPermission (UserId, Permission, IsNegated) VALUES (@0, @1, @2)";
                            command.AddParameter("@0", playerInfo.UserId);
                            command.AddParameter("@1", null);
                            command.AddParameter("@2", null);

                            foreach (var permission in playerInfo.Permissions.GetPermissions()
                                .Where(p => !string.IsNullOrWhiteSpace(p.Name)))
                            {
                                command.Parameters["@1"].Value = permission.Name;
                                command.Parameters["@2"].Value = permission.Negated ? 1 : 0;
                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex)
                        {
                            TShock.Log.ConsoleError(ex.ToString());
                        }
                    }
                }
            }
            var player = TShock.Players.SingleOrDefault(p => p?.User?.ID == playerInfo.UserId);
            player?.SetData(PlayerInfo.PlayerInfoKey, playerInfo);
        }
    }
}
