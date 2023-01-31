using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
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
        private readonly List<PlayerMetadata> _cache = new List<PlayerMetadata>();
        private readonly IDbConnection _connection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseManager" /> class.
        /// </summary>
        public DatabaseManager()
        {
            switch (TShock.Config.Settings.StorageType.ToLower())
            {
                case "mysql":
                    var dbHost = TShock.Config.Settings.MySqlHost.Split(':');
                    _connection = new MySqlConnection
                    {
                        ConnectionString =
                            $"Server={dbHost[0]}; " +
                            $"Port={(dbHost.Length == 1 ? "3306" : dbHost[1])}; " +
                            $"Database={TShock.Config.Settings.MySqlDbName}; " +
                            $"Uid={TShock.Config.Settings.MySqlUsername}; " +
                            $"Pwd={TShock.Config.Settings.MySqlPassword};"
                    };
                    break;

                default:
                    var sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
                    _connection = new SqliteConnection($"DataSource={sql}");
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
                              "FOREIGN KEY(UserId) REFERENCES UserSpecificFunctions(UserId))");
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
        /// <param name="playerInfo">The object, which must not be <c>null</c>.</param>
        public void Add([NotNull] PlayerMetadata playerInfo)
        {
            if (playerInfo == null)
            {
                throw new ArgumentNullException(nameof(playerInfo));
            }

            _connection.Query(
                "INSERT INTO UserSpecificFunctions (UserId, Prefix, Suffix, Color) VALUES (@0, @1, @2, @3)",
                playerInfo.UserId, playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color);
            foreach (var permission in playerInfo.Permissions.GetAll().Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                _connection.Query("INSERT INTO UserHasPermission (UserId, Permission, IsNegated) VALUES (@0, @1, @2)",
                    playerInfo.UserId, permission.Name, permission.Negated ? 1 : 0);
            }

			_cache.Add(playerInfo);
		}

        /// <summary>
        ///     Returns a <see cref="PlayerMetadata" /> object for the specified user.
        /// </summary>
        /// <param name="user">The <see cref="User" /> object.</param>
        /// <returns>The <see cref="PlayerMetadata" /> object associated with the user.</returns>
        [CanBeNull]
        public PlayerMetadata Get(UserAccount user)
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
                    var chatData = new ChatInformation(reader.Get<string>("Prefix"), reader.Get<string>("Suffix"),
                        reader.Get<string>("Color"));

                    var player = new PlayerMetadata(userId, chatData);
                    using (var reader2 =
                        _connection.QueryReader("SELECT * FROM UserHasPermission WHERE UserId = @0", userId))
                    {
                        while (reader2.Read())
                        {
                            var permissionName = reader2.Get<string>("Permission");
                            var isNegated = reader2.Get<int>("IsNegated") == 1;
                            player.Permissions.Add(new Permission(permissionName, isNegated));
                        }
                    }

                    _cache.Add(player);
                }
            }
        }

        /// <summary>
        ///     Removes the specified user from the database.
        /// </summary>
        /// <param name="user">The user, which must not be <c>null</c>.</param>
        public void Remove([NotNull] UserAccount user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _cache.RemoveAll(p => p.UserId == user.ID);
            _connection.Query("DELETE FROM UserSpecificFunctions WHERE UserID = @0", user.ID);
        }

        /// <summary>
        ///     Updates the specified player's database information.
        /// </summary>
        /// <param name="playerInfo">The player, which must not be <c>null</c>.</param>
        public void Update([NotNull] PlayerMetadata playerInfo)
        {
            if (playerInfo == null)
            {
                throw new ArgumentNullException(nameof(playerInfo));
            }

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
                        using (var command = (SqliteCommand)db.CreateCommand())
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

            var player = TShock.Players.SingleOrDefault(p => p?.Account?.ID == playerInfo.UserId);
            player?.SetData(PlayerMetadata.PlayerInfoKey, playerInfo);
        }
    }
}