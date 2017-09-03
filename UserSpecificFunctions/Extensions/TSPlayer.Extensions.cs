using TShockAPI;
using UserSpecificFunctions.Database;

namespace UserSpecificFunctions.Extensions
{
	/// <summary>
	/// Extends the <see cref="TSPlayer"/> type.
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public static class TSPlayerExtensions
	{
		/// <summary>
		/// Gets the player's info.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <returns>The player info.</returns>
		public static PlayerInfo GetPlayerInfo(this TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return default(PlayerInfo);
			}

			var playerInfo = player.GetData<PlayerInfo>(PlayerInfo.PlayerInfoKey);
			if (playerInfo == null)
			{
				using (var database = new DatabaseManager())
				{
					playerInfo = database.Get(player.User);
					player.SetData(PlayerInfo.PlayerInfoKey, playerInfo);
				}
			}
			return playerInfo;
		}
	}
}
