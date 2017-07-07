using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using UserSpecificFunctions.Permissions;
using UserSpecificFunctions.Database;
using Microsoft.Xna.Framework;

namespace UserSpecificFunctions
{
	/// <summary>
	/// Represents the command handler.
	/// </summary>
	internal sealed class CommandHandler
	{
		private readonly UserSpecificFunctionsPlugin _plugin;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandHandler"/> class.
		/// </summary>
		/// <param name="plugin">The <see cref="UserSpecificFunctionsPlugin"/> instance.</param>
		public CommandHandler(UserSpecificFunctionsPlugin plugin)
		{
			_plugin = plugin;
		}

		/// <summary>
		/// Registers the command handler.
		/// </summary>
		public void Register()
		{
			Commands.ChatCommands.RemoveAll(c => c.HasAlias("help"));
			Commands.ChatCommands.Add(new Command(Help, "help"));

			Commands.ChatCommands.Add(new Command(UsCommandHandler, "us"));
			Commands.ChatCommands.Add(new Command(PermissionCommandHandler, "permission"));
		}

		/// <summary>
		/// Deregisters the command handler.
		/// </summary>
		public void Deregister()
		{
			Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == UsCommandHandler);
			Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == PermissionCommandHandler);
		}

		private static void Help(CommandArgs e)
		{
			if (e.Parameters.Count > 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}help <command/page>", TShock.Config.CommandSpecifier);
				return;
			}

			if (e.Parameters.Count == 0 || int.TryParse(e.Parameters[0], out int pageNumber))
			{
				if (!PaginationTools.TryParsePageNumber(e.Parameters, 0, e.Player, out pageNumber))
				{
					return;
				}

				var playerInfo = e.Player.GetData<PlayerInfo>(PlayerInfo.DataKey);
				var cmdNames = from cmd in Commands.ChatCommands
											   where cmd.CanRun(e.Player) && (!playerInfo?.Permissions.ContainsPermission(cmd.Permissions.ElementAtOrDefault(0)) ?? true)
											   || ((playerInfo?.Permissions.ContainsPermission(cmd.Permissions.ElementAtOrDefault(0)) ?? true) && (cmd.Name != "auth" || TShock.AuthToken != 0))
											   orderby cmd.Name
											   select TShock.Config.CommandSpecifier + cmd.Name;

				PaginationTools.SendPage(e.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
					new PaginationTools.Settings
					{
						HeaderFormat = "Commands ({0}/{1}):",
						FooterFormat = "Type {0}help {{0}} for more.".SFormat(TShock.Config.CommandSpecifier)
					});
			}
			else
			{
				var commandName = e.Parameters[0].ToLower();
				if (commandName.StartsWith(TShock.Config.CommandSpecifier))
				{
					commandName = commandName.Substring(1);
				}

				var command = Commands.ChatCommands.Find(c => c.Names.Contains(commandName));
				if (command == null)
				{
					e.Player.SendErrorMessage("Invalid command.");
					return;
				}
				if (!command.CanRun(e.Player) && !e.Player.HasPermission(command.Permissions[0]))
				{
					e.Player.SendErrorMessage("You do not have access to this command.");
					return;
				}

				e.Player.SendSuccessMessage("{0}{1} help: ", TShock.Config.CommandSpecifier, command.Name);
				if (command.HelpDesc == null)
				{
					e.Player.SendInfoMessage(command.HelpText);
					return;
				}
				foreach (var line in command.HelpDesc)
				{
					e.Player.SendInfoMessage(line);
				}
			}
		}

		private void UsCommandHandler(CommandArgs e)
		{
			if (e.Parameters.Count < 1 || e.Parameters.Count > 3 || e.Parameters[0].ToLowerInvariant() == "help")
			{
				SendInvalidSyntax(e);
				return;
			}

			switch (e.Parameters[0].ToLowerInvariant())
			{
				case "prefix":
					HandleSetPrefix(e);
					break;
				case "suffix":
					HandleSetSuffix(e);
					break;
				case "color":
				case "colour":
					HandleSetColor(e);
					break;
				case "remove":
					HandleRemoveCustomData(e);
					break;
				case "read":
					HandleGetPlayerData(e);
					break;
			}
		}

		private void PermissionCommandHandler(CommandArgs e)
		{
			if (e.Parameters.Count < 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax:");
				e.Player.SendErrorMessage($"{Commands.Specifier}permission add <player name> <permissions>");
				e.Player.SendErrorMessage($"{Commands.Specifier}permission delete <player name> <permissions>");
				e.Player.SendErrorMessage($"{Commands.Specifier}permission list <player name> [page]");
				return;
			}

			switch (e.Parameters[0].ToLower())
			{
				case "add":
					HandleAddPermission(e);
					return;
				case "del":
				case "rem":
				case "delete":
				case "remove":
					HandleRemovePermission(e);
					return;
				case "list":
					HandleGetPermissions(e);
					return;
			}
		}

		private static void SendInvalidSyntax(CommandArgs e)
		{
			var help = new Dictionary<string, string>()
			{
				{ "prefix", "Sets the player's chat prefix" },
				{ "suffix", "Sets the player's chat suffix" },
				{ "color", "Sets the player's chat color" },
				{ "remove", "Removes the player's (pre/suf)fix or chat color" },
				{ "reset", "Resets the player's chat information" },
				{ "purge", "Removes all empty entries from the database" },
				{ "read", "Outputs the player's chat information" }
			};

			if (!PaginationTools.TryParsePageNumber(e.Parameters, 1, e.Player, out int pageNum))
			{
				if (!help.ContainsKey(e.Parameters[1]))
				{
					e.Player.SendErrorMessage("Invalid command name or page provided.");
					return;
				}

				e.Player.SendInfoMessage($"Sub-command: {e.Parameters[1]}");
				e.Player.SendInfoMessage($"Help: {help[e.Parameters[1]]}");
			}
			else
			{
				PaginationTools.SendPage(e.Player, pageNum, help.Keys.Select(p => $"{p} - {help[p]}"), help.Count,
					new PaginationTools.Settings()
					{
						HeaderFormat = $"UserSpecificFunctions Help ({{0}}/{{1}})",
						FooterFormat = $"Type {Commands.Specifier}us help {{0}} for more."
					});
			}
		}

		private void HandleSetPrefix(CommandArgs e)
		{
			if (!e.Player.IsLoggedIn && e.Player.RealPlayer)
			{
				e.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (e.Parameters.Count < 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us prefix <player name> <prefix>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat prefix.");
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				var prefix = string.Join(" ", e.Parameters);
				if (prefix.Length > _plugin.Configuration.MaximumPrefixLength)
				{
					e.Player.SendErrorMessage($"Your prefix cannot be longer than {_plugin.Configuration.MaximumPrefixLength} characters.");
					return;
				}

				if (_plugin.Configuration.ProhibitedWords.Any(prefix.Contains))
				{
					e.Player.SendErrorMessage(
						$"Your chat prefix cannot contain the following word(s): {string.Join(", ", from w in _plugin.Configuration.ProhibitedWords where prefix.ToLowerInvariant().Contains(w.ToLowerInvariant()) select w)}");
					return;
				}

				var target = _plugin.Database.Get(users[0]);
				if (target == null)
				{
					target = new PlayerInfo(users[0].ID, new ChatData(prefix), new PermissionCollection());
					_plugin.Database.Add(target);
				}
				else
				{
					target.ChatData.Prefix = prefix;
					_plugin.Database.Update(target, DatabaseUpdate.Prefix);
				}

				e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
			}
		}

		private void HandleSetSuffix(CommandArgs e)
		{
			if (!e.Player.IsLoggedIn && e.Player.RealPlayer)
			{
				e.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (e.Parameters.Count < 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us suffix <player name> <prefix>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat suffix.");
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				var suffix = string.Join(" ", e.Parameters);
				if (suffix.Length > _plugin.Configuration.MaximumSuffixLength)
				{
					e.Player.SendErrorMessage($"Your suffix cannot be longer than {_plugin.Configuration.MaximumSuffixLength} characters.");
					return;
				}

				if (_plugin.Configuration.ProhibitedWords.Any(suffix.Contains))
				{
					e.Player.SendErrorMessage(
						$"Your chat suffix cannot contain the following word(s): {string.Join(", ", from w in _plugin.Configuration.ProhibitedWords where suffix.ToLowerInvariant().Contains(w.ToLowerInvariant()) select w)}");
					return;
				}

				var target = _plugin.Database.Get(users[0]);
				if (target == null)
				{
					target = new PlayerInfo(users[0].ID, new ChatData(suffix: suffix), new PermissionCollection());
					_plugin.Database.Add(target);
				}
				else
				{
					target.ChatData.Suffix = suffix;
					_plugin.Database.Update(target, DatabaseUpdate.Suffix);
				}

				e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
			}
		}

		private void HandleSetColor(CommandArgs e)
		{
			if (!e.Player.IsLoggedIn && e.Player.RealPlayer)
			{
				e.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (e.Parameters.Count != 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us color <player name> <color>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else if (users[0].Name != e.Player.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat color.");
			}
			else
			{
				var color = e.Parameters[2].Split(',');
				if (color.Length == 3 && byte.TryParse(color[0], out byte r) && byte.TryParse(color[1], out byte g) && byte.TryParse(color[2], out byte b))
				{
					var target = _plugin.Database.Get(users[0]);
					if (target == null)
					{
						target = new PlayerInfo(users[0].ID, new ChatData(color: e.Parameters[2]), new PermissionCollection());
						_plugin.Database.Add(target);
					}
					else
					{
						target.ChatData.Color = e.Parameters[2];
						_plugin.Database.Update(target, DatabaseUpdate.Color);
					}

					e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
				}
				else
				{
					e.Player.SendErrorMessage("Invalid color format!");
				}
			}
		}

		private void HandleRemoveCustomData(CommandArgs e)
		{
			if (!e.Player.IsLoggedIn && e.Player.RealPlayer)
			{
				e.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			if (e.Parameters.Count != 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax: {Commands.Specifier}us remove <player name> <prefix/suffix/color>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You can't modify this player's chat data.");
			}
			else
			{
				var target = _plugin.Database.Get(users[0]);
				if (target == null)
				{
					e.Player.SendErrorMessage("This user has no custom chat data.");
					return;
				}

				switch (e.Parameters[2].ToLowerInvariant())
				{
					case "prefix":
						{
							if (!e.Player.HasPermission("us.remove.prefix"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
							}
							else
							{
								target.ChatData.Prefix = null;
								_plugin.Database.Update(target, DatabaseUpdate.Prefix);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
					case "suffix":
						{
							if (!e.Player.HasPermission("us.remove.suffix"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
							}
							else
							{
								target.ChatData.Suffix = null;
								_plugin.Database.Update(target, DatabaseUpdate.Suffix);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
					case "color":
					case "colour":
						{
							if (!e.Player.HasPermission("us.remove.color"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
							}
							else
							{
								target.ChatData.Color = null;
								_plugin.Database.Update(target, DatabaseUpdate.Color);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
					case "all":
						{
							if (!e.Player.HasPermission("us.resetall"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
							}
							else
							{
								target.ChatData = new ChatData();
								_plugin.Database.Update(target, DatabaseUpdate.Prefix | DatabaseUpdate.Suffix | DatabaseUpdate.Color);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
				}
			}
		}

		private void HandleGetPlayerData(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}us read <player name>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else
			{
				var target = _plugin.Database.Get(users[0]);

				if (target == null)
				{
					e.Player.SendErrorMessage("This user has no player specific information to read.");
				}
				else
				{
					e.Player.SendInfoMessage($"Username: {users[0].Name}");
					e.Player.SendMessage($"  * Prefix: {target.ChatData.Prefix ?? "None"}", Color.LawnGreen);
					e.Player.SendMessage($"  * Suffix: {target.ChatData.Suffix ?? "None"}", Color.LawnGreen);
					e.Player.SendMessage($"  * Chat color: {target.ChatData.Color ?? "None"}", Color.LawnGreen);
				}
			}
		}

		private void HandleAddPermission(CommandArgs e)
		{
			if (e.Parameters.Count < 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}permission add <player name> <permissions>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage($"Could not find a user under the name '{e.Parameters[1]}'");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(u => u.Name));
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				var target = _plugin.Database.Get(users[0]);

				if (target == null)
				{
					target = new PlayerInfo(users[0].ID, new ChatData(), new PermissionCollection(e.Parameters));
					_plugin.Database.Add(target);
				}
				else
				{
					e.Parameters.ForEach(p => target.Permissions.AddPermission(p));
					_plugin.Database.Update(target, DatabaseUpdate.Permissions);
				}

				e.Player.SendSuccessMessage($"Modified {users[0].Name}'s permissions successfully.");
			}
		}

		private void HandleRemovePermission(CommandArgs e)
		{
			if (e.Parameters.Count < 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}permission remove <player name> <permissions>");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage($"Could not find a user under the name '{e.Parameters[1]}'");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(u => u.Name));
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				var target = _plugin.Database.Get(users[0]);

				if (target == null)
				{
					e.Player.SendErrorMessage("This user has no custom permissions.");
				}
				else
				{
					e.Parameters.ForEach(p => target.Permissions.RemovePermission(p));
					_plugin.Database.Update(target, DatabaseUpdate.Permissions);
					e.Player.SendSuccessMessage($"Modified {users[0].Name}'s permissions successfully.");
				}
			}
		}

		private void HandleGetPermissions(CommandArgs e)
		{
			if (e.Parameters.Count < 2 || e.Parameters.Count > 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}permission list <player name> [page]");
				return;
			}

			var users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
			}
			else
			{
				var target = _plugin.Database.Get(users[0]);

				if (target == null || target.Permissions.Count == 0)
				{
					e.Player.SendErrorMessage("This user has no permissions to list.");
					return;
				}

				e.Player.SendSuccessMessage($"{users[0].Name}'s permissions:");
				e.Player.SendInfoMessage(target.Permissions.ToString());
			}
		}
	}
}
