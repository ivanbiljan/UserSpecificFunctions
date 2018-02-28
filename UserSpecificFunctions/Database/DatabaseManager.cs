using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using Permission = UserSpecificFunctions.Permissions.Permission;

namespace UserSpecificFunctions.Database
{
    /// <summary>
    ///     Represents the database manager.
    /// </summary>
    public sealed class DatabaseManager : IDisposable
    {
        private readonly List<PlayerInfo> _cache = new List<PlayerInfo>();
        private readonly IDbConnection _connection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseManager" /> class.
        /// </summary>
        public DatabaseManager()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    var dbHost = TShock.Config.MySqlHost.Split(':');
                    _connection = new MySqlConnection
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
                    _connection = new SqliteConnection($"uri=file://{sql},Version=3");
                    break;
            }

            _connection.Query("CREATE TABLE IF NOT EXISTS UserSpecificFunctions (" +
                      "UserId   INTEGER, " +
                      "Prefix   TEXT DEFAULT NULL, " +
                      "Suffix   TEXT DEFAULT NULL, " +
                      "Color    TEXT DEFAULT NULL, " +
                      "UNIQUE(UserId) ON CONFLICT REPLACE)");
            _connection.Query("CREATE TABLE IF NOT EXISTS UserHasPermission (" +
                      "UserId       INTEGER, " +
                      "Permission   TEXT NOT NULL, " +
                      "IsNegated    INTEGER, " +
                      "FOREIGN KEY(UserId) REFERENCES UserSpecificFunctions(UserId) ON DELETE CASCADE)");
        }

        /// <summary>
        ///     Disposes the database connection.
        /// </summary>
        public void Dispose()
        {
            _connection.Dispose();
        }

        /// <summary>
        ///     Inserts a new object into the database.
        /// </summary>
        /// <param name="playerInfo">The object.</param>
        public void Add(PlayerInfo playerInfo)
        {
            _connection.Query("INSERT INTO UserSpecificFunctions (UserId, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3)",
                playerInfo.UserId, playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color);
            foreach (var permission in playerInfo.Permissions.GetAll().Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                _connection.Query("INSERT INTO UserHasPermission (UserId, Permission, IsNegated) VALUES (@0, @1, @2)",
                    playerInfo.UserId, permission.Name, permission.Negated ? 1 : 0);
            }
        }

        /// <summary>
        ///     Returns a <see cref="PlayerInfo" /> object for the specified user.
        /// </summary>
        /// <param name="user">The <see cref="User" /> object.</param>
        /// <returns>The <see cref="PlayerInfo" /> object associated with the user.</returns>
        [CanBeNull]
        public PlayerInfo Get(User user)
        {
            return _cache.SingleOrDefault(p => p.UserId == user.ID);
        }

        /// <summary>
        ///     Loads the database.
        /// </summary>
        public void Load()
        {
            _cache.Clear();
            using (var reader = _connection.QueryReader("SELECT * FROM UserSpecificFunctions"))
            {
                while (reader.Read())
                {
                    var userId = reader.Get<int>("UserId");
                    var chatData = new ChatData(reader.Get<string>("Prefix"), reader.Get<string>("Suffix"),
                        reader.Get<string>("Color"));

                    var player = new PlayerInfo(userId, chatData);
                    using (var reader2 = _connection.QueryReader("SELECT * FROM UserHasPermission WHERE UserId = @0", userId))
                    {
                        while (reader2.Read())
                        {
                            var permissionName = reader.Get<string>("Permission");
                            var isNegated = reader.Get<int>("IsNegated") == 1;
                            player.Permissions.Add(new Permission(permissionName, isNegated));
                        }
                    }

                    _cache.Add(player);
                }
            }
        }

        /// <summary>
        ///     Removes the given user from the database.
        /// </summary>
        /// <param name="user">The user.</param>
        public void Remove(User user)
        {
            _cache.RemoveAll(p => p.UserId == user.ID);
            _connection.Query("DELETE FROM UserSpecificFunctions WHERE UserID = @0", user.ID);
        }

        /// <summary>
        ///     Updates the given player.
        /// </summary>
        /// <param name="playerInfo">The player.</param>
        public void Update(PlayerInfo playerInfo)
        {
            _connection.Query("UPDATE UserSpecificFunctions SET Prefix = @0, Suffix = @1, Color = @2 WHERE UserId = @3",
                playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color, playerInfo.UserId);
            _connection.Query("DELETE FROM UserHasPermission WHERE UserId = @0", playerInfo.UserId);
            using (var db = _connection.CloneEx())
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

                            foreach (var permission in playerInfo.Permissions.GetAll()
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