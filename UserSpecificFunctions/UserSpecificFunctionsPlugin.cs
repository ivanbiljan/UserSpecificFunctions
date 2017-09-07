using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.Localization;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using UserSpecificFunctions.Database;
using UserSpecificFunctions.Extensions;
using UserSpecificFunctions.Permissions;

namespace UserSpecificFunctions
{
	/// <summary>
	/// Represents the User Specific Functions plugin.
	/// </summary>
	[ApiVersion(2, 1)]
	public sealed class UserSpecificFunctionsPlugin : TerrariaPlugin
	{
		private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "userspecificfunctions.json");

		private UserSpecificFunctionsConfig _config;
		private DatabaseManager _database;

		/// <summary>
		/// Gets the author.
		/// </summary>
		public override string Author => "Professor X";

		/// <summary>
		/// Gets the description.
		/// </summary>
		public override string Description => "N/A";

		/// <summary>
		/// Gets the name.
		/// </summary>
		public override string Name => "User Specific Functions";

		/// <summary>
		/// Gets the version.
		/// </summary>
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserSpecificFunctionsPlugin"/> class.
		/// </summary>
		/// <param name="game">The <see cref="Main"/> instance.</param>
		public UserSpecificFunctionsPlugin(Main game) : base(game)
		{
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_database.Dispose();
				File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_config, Formatting.Indented));

				AccountHooks.AccountDelete -= OnAccountDelete;
				GeneralHooks.ReloadEvent -= OnReload;
				PlayerHooks.PlayerPermission -= OnPlayerPermission;
				ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);

				Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == UsCommandHandler);
				Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == UsPermissionCommandHandler);
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Initializes the plugin.
		/// </summary>
		public override void Initialize()
		{
			_config = UserSpecificFunctionsConfig.ReadOrCreate(ConfigPath);
			_database = new DatabaseManager();
			_database.Load();

			AccountHooks.AccountDelete += OnAccountDelete;
			GeneralHooks.ReloadEvent += OnReload;
			PlayerHooks.PlayerPermission += OnPlayerPermission;
			ServerApi.Hooks.ServerChat.Register(this, OnServerChat);

			Commands.ChatCommands.Add(new Command(UsCommandHandler, "us"));
			Commands.ChatCommands.Add(new Command(UsPermissionCommandHandler, "permission"));
		}

		private void OnAccountDelete(AccountDeleteEventArgs e)
		{
			if (_database.Get(e.User) != null)
			{
				_database.Remove(e.User);
			}
		}

		private void OnPlayerPermission(PlayerPermissionEventArgs e)
		{
			if (!e.Player.IsLoggedIn)
			{
				e.Result = PermissionHookResult.Unhandled;
				return;
			}

			var playerInfo = _database.Get(e.Player.User);
			if (playerInfo == null)
			{
				e.Result = PermissionHookResult.Unhandled;
				return;
			}

			if (playerInfo.Permissions.ContainsPermission(e.Permission))
			{
				e.Result = !playerInfo.Permissions.Negated(e.Permission)
					? PermissionHookResult.Granted
					: PermissionHookResult.Denied;
			}
			else
			{
				e.Result = PermissionHookResult.Unhandled;
			}
		}

		private void OnReload(ReloadEventArgs e)
		{
			_config = UserSpecificFunctionsConfig.ReadOrCreate(ConfigPath);
			_database.Load();
		}

		private void OnServerChat(ServerChatEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}

			var player = TShock.Players[e.Who];
			if (player == null || !player.IsLoggedIn)
			{
				return;
			}

			if (!player.HasPermission(TShockAPI.Permissions.canchat) || player.mute)
			{
				return;
			}

			if (e.Text.StartsWith(TShock.Config.CommandSpecifier) || e.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
			{
				return;
			}

			var playerData = _database.Get(player.User);
			var prefix = playerData?.ChatData.Prefix ?? player.Group.Prefix;
			var suffix = playerData?.ChatData.Suffix ?? player.Group.Suffix;
			var chatColor = playerData?.ChatData.Color?.ParseColor() ?? player.Group.ChatColor.ParseColor();

			if (!TShock.Config.EnableChatAboveHeads)
			{
				var message = string.Format(TShock.Config.ChatFormat, player.Group.Name, prefix, player.Name, suffix, e.Text);
				TSPlayer.All.SendMessage(message, chatColor);
				TSPlayer.Server.SendMessage(message, chatColor);
				TShock.Log.Info($"Broadcast: {message}");
			}
			else
			{
				var playerName = player.TPlayer.name;
				player.TPlayer.name = string.Format(TShock.Config.ChatAboveHeadsFormat, player.Group.Name, prefix, player.Name,
					suffix);
				NetMessage.SendData((int) PacketTypes.PlayerInfo, -1, -1, NetworkText.FromLiteral(player.TPlayer.name), e.Who);

				player.TPlayer.name = playerName;

				var packet = NetTextModule.SerializeServerMessage(NetworkText.FromLiteral(e.Text), chatColor, (byte) e.Who);
				NetManager.Instance.Broadcast(packet, e.Who);

				NetMessage.SendData((int) PacketTypes.PlayerInfo, -1, -1, NetworkText.FromLiteral(playerName), e.Who);

				var msg =
					$"<{string.Format(TShock.Config.ChatAboveHeadsFormat, player.Group.Name, prefix, player.Name, suffix)}> {e.Text}";

				player.SendMessage(msg, chatColor);
				TSPlayer.Server.SendMessage(msg, chatColor);
				TShock.Log.Info($"Broadcast: {msg}");
			}

			e.Handled = true;
		}

		private void UsCommandHandler(CommandArgs e)
		{
			var player = e.Player;
			if (e.Parameters.Count < 1)
			{
				player.SendErrorMessage($"Invalid syntax! Use {Commands.Specifier}us help for help.");
				return;
			}

			var command = e.Parameters[0];
			if (command.Equals("color", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 3)
				{
					player.SendErrorMessage(
						$"Invalid syntax! Proper syntax: {Commands.Specifier}us color <player name> <rrr,ggg,bbb>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}
				if (user.Name != player.User?.Name && !e.Player.HasPermission("us.setother"))
				{
					e.Player.SendErrorMessage("You do not have permission to modify another user's chat data.");
					return;
				}

				var color = e.Parameters[2].Split(',');
				var target = _database.Get(user);
				if (color.Length != 3 || !byte.TryParse(color[0], out byte _) || !byte.TryParse(color[1], out byte _) ||
				    !byte.TryParse(color[2], out byte _))
				{
					player.SendErrorMessage("Invalid color format.");
					return;
				}

				if (target == null)
				{
					target = new PlayerInfo(user.ID, new ChatData(e.Parameters[2]), new PermissionCollection());
					_database.Add(target);
				}
				else
				{
					target.ChatData.Color = e.Parameters[2];
					_database.Update(target);
				}

				player.SendSuccessMessage($"Successfully set {user.Name}'s color.");
			}
			else if (command.Equals("prefix", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 3)
				{
					player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us prefix <player name> <prefix>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}
				if (user.Name != player.User?.Name && !e.Player.HasPermission("us.setother"))
				{
					e.Player.SendErrorMessage("You do not have permission to modify another user's chat data.");
					return;
				}

				e.Parameters.RemoveRange(0, 2);
				var prefix = string.Join(" ", e.Parameters);
				if (prefix.Length > _config.MaximumPrefixLength)
				{
					player.SendErrorMessage($"The prefix cannot contain more than {_config.MaximumPrefixLength} characters.");
					return;
				}

				if (_config.ProhibitedWords.Any(prefix.Contains))
				{
					player.SendErrorMessage(
						$"The prefix cannot contain the following words: {string.Join(", ", from w in _config.ProhibitedWords where prefix.Contains(w) select w)}");
					return;
				}

				var target = _database.Get(user);
				if (target == null)
				{
					target = new PlayerInfo(user.ID, new ChatData(prefix: prefix), new PermissionCollection());
					_database.Add(target);
				}
				else
				{
					target.ChatData.Prefix = prefix;
					_database.Update(target);
				}

				player.SendSuccessMessage($"Successfully set {user.Name}'s prefix.");
			}
			else if (command.Equals("read", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 2)
				{
					player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us read <player name>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}

				var target = _database.Get(user);
				if (target == null)
				{
					player.SendErrorMessage("This user has no chat data to display.");
					return;
				}

				player.SendInfoMessage($"Username: {user.Name}");
				player.SendMessage($"  * Prefix: {target.ChatData.Prefix ?? "None"}", Color.LawnGreen);
				player.SendMessage($"  * Suffix: {target.ChatData.Suffix ?? "None"}", Color.LawnGreen);
				player.SendMessage($"  * Chat color: {target.ChatData.Color ?? "None"}", Color.LawnGreen);
			}
			else if (command.Equals("remove", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 3)
				{
					player.SendErrorMessage(
						$"Invalid syntax! Proper syntax: {Commands.Specifier}us remove <player name> <prefix/suffix/color/all>");
					return;
				}

				var inputOption = e.Parameters[2];
				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}
				if (user.Name != player.User?.Name && !player.HasPermission("us.setother"))
				{
					player.SendErrorMessage("You do not have permission to modify another user's chat data.");
					return;
				}

				var target = _database.Get(user);
				if (target == null)
				{
					player.SendErrorMessage($"No information found for user '{user.Name}'.");
					return;
				}

				switch (inputOption.ToLowerInvariant())
				{
					case "all":
						if (!player.HasPermission("us.resetall"))
						{
							player.SendErrorMessage("You do not have access to this command.");
							return;
						}

						target.ChatData = new ChatData();
						player.SendSuccessMessage("Reset successful.");
						break;
					case "color":
						if (!player.HasPermission("us.remove.color"))
						{
							player.SendErrorMessage("You do not have access to this command.");
							return;
						}

						target.ChatData.Color = null;
						player.SendSuccessMessage($"Modified {user.Name}'s chat color successfully.");
						break;
					case "prefix":
						if (!player.HasPermission("us.remove.prefix"))
						{
							player.SendErrorMessage("You do not have access to this command.");
							return;
						}

						target.ChatData.Prefix = null;
						player.SendSuccessMessage($"Modified {user.Name}'s chat prefix successfully.");
						break;
					case "suffix":
						if (!player.HasPermission("us.remove.suffix"))
						{
							player.SendErrorMessage("You do not have access to this command.");
							return;
						}

						target.ChatData.Suffix = null;
						player.SendSuccessMessage($"Modified {user.Name}'s chat suffix successfully.");
						break;
					default:
						player.SendErrorMessage("Invalid option!");
						break;
				}
				_database.Update(target);
			}
			else if (command.Equals("suffix", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 3)
				{
					player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us suffix <player name> <suffix>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}
				if (user.Name != player.User.Name && !player.HasPermission("us.setother"))
				{
					player.SendErrorMessage("You do not have permission to modify another user's chat data.");
					return;
				}

				e.Parameters.RemoveRange(0, 2);
				var suffix = string.Join(" ", e.Parameters);
				if (suffix.Length > _config.MaximumSuffixLength)
				{
					player.SendErrorMessage($"The suffix cannot contain more than {_config.MaximumSuffixLength} characters.");
					return;
				}
				if (_config.ProhibitedWords.Any(suffix.Contains))
				{
					player.SendErrorMessage(
						$"The suffix cannot contain the following words: {string.Join(", ", from w in _config.ProhibitedWords where suffix.Contains(w) select w)}");
					return;
				}

				var target = _database.Get(user);
				if (target == null)
				{
					target = new PlayerInfo(user.ID, new ChatData(suffix: suffix), new PermissionCollection());
					_database.Add(target);
				}
				else
				{
					target.ChatData.Suffix = suffix;
					_database.Update(target);
				}

				player.SendSuccessMessage($"Successfully set {user.Name}'s suffix.");
			}
		}

		private void UsPermissionCommandHandler(CommandArgs e)
		{
			var player = e.Player;
			if (e.Parameters.Count < 1)
			{
				player.SendErrorMessage("Invalid syntax! Proper syntax:");
				player.SendErrorMessage($"{Commands.Specifier}permission add <player name> <permissions>");
				player.SendErrorMessage($"{Commands.Specifier}permission delete <player name> <permissions>");
				player.SendErrorMessage($"{Commands.Specifier}permission list <player name> [page]");
				return;
			}

			var command = e.Parameters[0];
			if (command.Equals("add", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count < 3)
				{
					player.SendErrorMessage(
						$"Invalid syntax! Proper syntax: {Commands.Specifier}permission add <player name> <permission1 permission2 permissionN>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}

				e.Parameters.RemoveRange(0, 2);
				var target = _database.Get(user);
				if (target == null)
				{
					target = new PlayerInfo(user.ID, new ChatData(), new PermissionCollection(e.Parameters));
					_database.Add(target);
				}
				else
				{
					e.Parameters.ForEach(p => target.Permissions.AddPermission(p));
					_database.Update(target);
				}

				player.SendSuccessMessage($"Modified '{user.Name}''s permissions successfully.");
			}
			else if (command.Equals("list", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count != 2)
				{
					player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}permission list <player name>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}

				var target = _database.Get(user);
				if (target == null || target.Permissions.Count == 0)
				{
					player.SendInfoMessage("This player has no permissions to list.");
					return;
				}

				player.SendInfoMessage($"{user.Name}'s permissions: {target.Permissions}");
			}
			else if (command.Equals("remove", StringComparison.CurrentCultureIgnoreCase))
			{
				if (e.Parameters.Count < 3)
				{
					player.SendErrorMessage(
						$"Invalid syntax! Proper syntax: {Commands.Specifier}permission remove <player name> <permission1 permission2 permissionN>");
					return;
				}

				var username = e.Parameters[1];
				var user = TShock.Users.GetUserByName(username);
				if (user == null)
				{
					player.SendErrorMessage($"Couldn't find any users under the name of '{username}'.");
					return;
				}

				e.Parameters.RemoveRange(0, 2);
				var target = _database.Get(user);
				if (target == null || target.Permissions.Count == 0)
				{
					player.SendInfoMessage("This user has no permissions to remove.");
					return;
				}

				e.Parameters.ForEach(p => target.Permissions.RemovePermission(p));
				_database.Update(target);
				player.SendSuccessMessage($"Modified '{user.Name}''s permissions successfully.");
			}
			else
			{
				player.SendErrorMessage("Invalid sub-command.");
			}
		}
	}
}
