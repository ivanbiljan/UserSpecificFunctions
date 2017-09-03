using UserSpecificFunctions.Permissions;

namespace UserSpecificFunctions.Database
{
	/// <summary>
	/// Extends the <see cref="TShockAPI.TSPlayer"/> class.
	/// </summary>
	public sealed class PlayerInfo
	{
		public const string PlayerInfoKey = "UserSpecificFunctions_PlayerInfo";

		/// <summary>
		/// Gets or sets the <see cref="ChatData"/> instance associated with this <see cref="PlayerInfo"/> instance.
		/// </summary>
		public ChatData ChatData { get; set; }

		/// <summary>
		/// Gets or sets the list of custom permissions the user has.
		/// </summary>
		public PermissionCollection Permissions { get; set; }

		/// <summary>
		/// Gets the ID of the user that this instance belongs to.
		/// </summary>
		public int UserId { get; }

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
	}
}
