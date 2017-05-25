using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TShockAPI.DB;
using UserSpecificFunctions.Permissions;

namespace UserSpecificFunctions.Models
{
	/// <summary>
	/// Extends the <see cref="TSPlayer"/> class.
	/// </summary>
	public sealed class PlayerInfo
	{
		public const string Data_Key = "Usf_Data";

		/// <summary>
		/// Gets the ID of the user that this instance belongs to.
		/// </summary>
		public int UserId { get; set; }

		/// <summary>
		/// Gets the <see cref="UserSpecificFunctions.Models.ChatData"/> instance associated with this instance.
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
		/// <param name="chatInfo">The user's chat information.</param>
		/// <param name="permissions">A collection of the user's permissions.</param>
		public PlayerInfo(int userId, ChatData chatData, PermissionCollection permissions)
		{
			UserId = userId;
			ChatData = chatData;
			Permissions = permissions;
		}

		public PlayerInfo ParseFromQuery(QueryResult result)
		{
			return new PlayerInfo()
			{
				UserId = result.Get<int>("UserID"),
				ChatData = new ChatData()
				{
					Prefix = result.Get<string>("Prefix"),
					Suffix = result.Get<string>("Suffix"),
					Color = result.Get<string>("Color")
				},
				Permissions = new PermissionCollection(Regex.Replace(result.Get<string>("Permissions"), @"\s+", "").Split(','))
			};
		}
	}
}
