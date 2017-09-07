using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using UserSpecificFunctions.Permissions;

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

				case "sqlite":
					var sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
					_db = new SqliteConnection($"uri=file://{sql},Version=3");
					break;
			}

			var sqlcreator = new SqlTableCreator(_db, _db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("UserSpecificFunctions",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("Prefix", MySqlDbType.Text),
				new SqlColumn("Suffix", MySqlDbType.Text),
				new SqlColumn("Color", MySqlDbType.Text),
				new SqlColumn("Permissions", MySqlDbType.Text)));
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
			_db.Query(
				"INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);",
				playerInfo.UserId, playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color,
				string.Join(",", playerInfo.Permissions));
		}

		///// <summary>
		///// Returns a <see cref="PlayerInfo"/> object by matching the user ID in the database.
		///// </summary>
		///// <param name="userId">The user's ID.</param>
		///// <returns>The <see cref="PlayerInfo"/> object associated with the user.</returns>
		//public PlayerInfo Get(int userId)
		//{
		//	return _cache.SingleOrDefault(p => p.UserId == userId);
		//}

		/// <summary>
		/// Returns a <see cref="PlayerInfo"/> object by matching the user from the database.
		/// </summary>
		/// <param name="user">The <see cref="User"/> object.</param>
		/// <returns>The <see cref="PlayerInfo"/> object associated with the user.</returns>
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
				while (reader.Read())
				{
					var userId = reader.Get<int>("UserID");
					var chatData = new ChatData(reader.Get<string>("Color"), reader.Get<string>("Prefix"),
						reader.Get<string>("Suffix"));
					var permissions = new PermissionCollection(reader.Get<string>("Permissions").Replace(" ", string.Empty)
						.Split(','));

					var playerInfo = new PlayerInfo(userId, chatData, permissions);
					_cache.Add(playerInfo);
				}
			}
		}

		/// <summary>
		/// Removes a user from the database.
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
			_db.Query(
				"UPDATE UserSpecificFunctions SET Prefix = @0, Suffix = @1, Color = @2, Permissions = @3 WHERE UserID = @4",
				playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color,
				string.Join(",", playerInfo.Permissions), playerInfo.UserId);

			var player = TShock.Players.SingleOrDefault(p => p?.User?.ID == playerInfo.UserId);
			player?.SetData(PlayerInfo.PlayerInfoKey, playerInfo);
		}
	}
}
