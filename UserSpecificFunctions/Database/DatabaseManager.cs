using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using UserSpecificFunctions.Models;

namespace UserSpecificFunctions.Database
{
	/// <summary>
	/// Represents the database manager.
	/// </summary>
	public sealed class DatabaseManager
	{
		private static IDbConnection db;

		/// <summary>
		/// Connects the database.
		/// </summary>
		public void Connect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("UserSpecificFunctions",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("Prefix", MySqlDbType.Text),
				new SqlColumn("Suffix", MySqlDbType.Text),
				new SqlColumn("Color", MySqlDbType.Text),
				new SqlColumn("Permissions", MySqlDbType.Text)));
		}

		/// <summary>
		/// Inserts a new object into the database.
		/// </summary>
		/// <param name="playerInfo">The object.</param>
		public void Add(PlayerInfo playerInfo)
		{
			db.Query("INSERT INTO UserSpecificFunctions (UserID, Prefix, Suffix, Color, Permissions) VALUES (@0, @1, @2, @3, @4);",
				playerInfo.UserId, playerInfo.ChatData.Prefix, playerInfo.ChatData.Suffix, playerInfo.ChatData.Color, playerInfo.Permissions.ToString());
		}

		/// <summary>
		/// Returns a <see cref="PlayerInfo"/> object by matching the user ID in the database.
		/// </summary>
		/// <param name="userId">The user's ID.</param>
		/// <returns>The <see cref="PlayerInfo"/> object associated with the user.</returns>
		public PlayerInfo Get(int userId)
		{
			using (var result = db.QueryReader("SELECT * FROM UserSpecificFunctions WHERE UserID = @0;", userId))
			{
				if (result.Read())
				{
					return new PlayerInfo().ParseFromQuery(result);
				}
			}
			return default(PlayerInfo);
		}

		/// <summary>
		/// Returns a <see cref="PlayerInfo"/> object by matching the user from the database.
		/// </summary>
		/// <param name="user">The <see cref="User"/> object.</param>
		/// <returns>The <see cref="PlayerInfo"/> object associated with the user.</returns>
		public PlayerInfo Get(User user)
		{
			return Get(user.ID);
		}

		/// <summary>
		/// Updates the database with new values.
		/// </summary>
		/// <param name="playerInfo">The <see cref="PlayerInfo"/> object.</param>
		/// <param name="updateType">The update type.</param>
		public void Update(PlayerInfo playerInfo, UpdateType updateType)
		{
			if (updateType == 0)
			{
				return;
			}

			List<string> updates = new List<string>();
			if ((updateType & UpdateType.Prefix) == UpdateType.Prefix)
			{
				updates.Add($"Prefix = '{playerInfo.ChatData.Prefix}'");
			}
			if ((updateType & UpdateType.Suffix) == UpdateType.Suffix)
			{
				updates.Add($"Suffix = '{playerInfo.ChatData.Suffix}'");
			}
			if ((updateType & UpdateType.Color) == UpdateType.Color)
			{
				updates.Add($"Color = '{playerInfo.ChatData.Color}'");
			}
			if ((updateType & UpdateType.Permissions) == UpdateType.Permissions)
			{
				updates.Add($"Permissions = '{playerInfo.Permissions.ToString()}'");
			}

			db.Query($"UPDATE UserSpecificFunctions SET {string.Join(", ", updates)} WHERE UserID = {playerInfo.UserId}");

			// Check if the player is online and update accordingly
			TSPlayer player = TShock.Players.FirstOrDefault(p => p?.User?.ID == playerInfo.UserId);
			if (player != null)
			{
				player.SetData(PlayerInfo.Data_Key, playerInfo);
			}
		}
	}
}
