using System.Text.RegularExpressions;
using TShockAPI.DB;
using UserSpecificFunctions.Permissions;

namespace UserSpecificFunctions
{
	/// <summary>
	/// Extends the <see cref="TShockAPI.TSPlayer"/> class.
	/// </summary>
	public sealed class PlayerInfo
	{
		public const string DataKey = "Usf_Data";

		/// <summary>
		/// Gets the ID of the user that this instance belongs to.
		/// </summary>
		public int UserId { get; }

		/// <summary>
		/// Gets the <see cref="UserSpecificFunctions.Models.ChatData"/> instance associated with this <see cref="PlayerInfo"/> instance.
		/// </summary>
		public ChatData ChatData { get; set; }

		/// <summary>
		/// Gets a list of custom permissions the user has.
		/// </summary>
		public PermissionCollection Permissions { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PlayerInfo"/> class.
		/// </summary>
		public PlayerInfo()
		{
			UserId = -1;
			ChatData = new ChatData();
			Permissions = new PermissionCollection();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PlayerInfo"/> class.
		/// </summary>
		/// <param name="userId">The user's ID.</param>
		/// <param name="chatData">The user's chat information.</param>
		/// <param name="permissions">A collection of the user's permissions.</param>
		public PlayerInfo(int userId, ChatData chatData, PermissionCollection permissions)
		{
			UserId = userId;
			ChatData = chatData;
			Permissions = permissions;
		}

		/// <summary>
		/// Parses a <see cref="PlayerInfo"/> object from the given QueryResult.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns>A <see cref="PlayerInfo"/> object.</returns>
		public PlayerInfo ParseFromQuery(QueryResult result)
		{
			//return new PlayerInfo
			//{
			//	UserId = result.Get<int>("UserID"),
			//	ChatData = new ChatData
			//	{
			//		Prefix = result.Get<string>("Prefix"),
			//		Suffix = result.Get<string>("Suffix"),
			//		Color = result.Get<string>("Color")
			//	},
			//	Permissions = new PermissionCollection(Regex.Replace(result.Get<string>("Permissions"), @"\s+", "").Split(','))
			//};

			return new PlayerInfo(result.Get<int>("UserID"),
				new ChatData(result.Get<string>("Prefix"), result.Get<string>("Suffix"), result.Get<string>("Color")),
				new PermissionCollection(Regex.Replace(result.Get<string>("Permissions"), @"\s+", "").Split(',')));
		}
	}
}
