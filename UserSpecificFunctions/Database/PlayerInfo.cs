using System;
using JetBrains.Annotations;
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
        [NotNull]
        public ChatData ChatData { get; set; }

        /// <summary>
        /// Gets or sets the list of custom permissions the user has.
        /// </summary>
        [NotNull]
        public PermissionCollection Permissions { get; } = new PermissionCollection();

        /// <summary>
        /// Gets the ID of the user that this instance belongs to.
        /// </summary>
        public int UserId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo"/> class with the specified user ID and chat data.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="chatData">The user's chat information.</param>
        public PlayerInfo(int userId, [NotNull] ChatData chatData)
        {
            UserId = userId;
            ChatData = chatData ?? throw new ArgumentNullException(nameof(chatData));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfo"/> class with the specified user ID, chat data and permissions.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="chatData">The user's chat information.</param>
        /// <param name="permissions">A collection of the user's permissions.</param>
        public PlayerInfo(int userId, [NotNull] ChatData chatData, [NotNull] PermissionCollection permissions) : this(userId, chatData)
        {
            Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }
    }
}
