using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;
using UserSpecificFunctions.Models;
using UserSpecificFunctions.Permissions;
using UserSpecificFunctions.Database;
using Microsoft.Xna.Framework;

namespace UserSpecificFunctions
{
	/// <summary>
	/// Represents the command handler.
	/// </summary>
	public sealed class CommandHandler
	{
		private readonly Config _config;
		private readonly DatabaseManager _database;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandHandler"/> class.
		/// </summary>
		/// <param name="config">The <see cref="Config"/> instance.</param>
		/// <param name="databaseManager">The <see cref="DatabaseManager"/> instance.</param>
		public CommandHandler(Config config, DatabaseManager databaseManager)
		{
			_config = config;
			_database = databaseManager;
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

		private void Help(CommandArgs e)
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

				PlayerInfo playerInfo = e.Player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
				IEnumerable<string> cmdNames = from cmd in Commands.ChatCommands
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
				string commandName = e.Parameters[0].ToLower();
				if (commandName.StartsWith(TShock.Config.CommandSpecifier))
				{
					commandName = commandName.Substring(1);
				}

				Command command = Commands.ChatCommands.Find(c => c.Names.Contains(commandName));
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
				foreach (string line in command.HelpDesc)
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

		private void SendInvalidSyntax(CommandArgs e)
		{
			Dictionary<string, string> help = new Dictionary<string, string>()
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
						HeaderFormat = $"UserSpecificFunctions Help({{0}}/{{1}})",
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat prefix.");
				return;
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				string prefix = string.Join(" ", e.Parameters);
				//if (prefix.Length > _config.MaximumPrefixLength)
				//{
				//	e.Player.SendErrorMessage($"Your prefix cannot be longer than {_config.MaximumPrefixLength} characters.");
				//	return;
				//}

				IEnumerable<string> matches = _config.ProhibitedWords.Where(p => prefix.ToLowerInvariant().Contains(p.ToLowerInvariant()));
				if (matches.Any())
				{
					e.Player.SendErrorMessage($"Your chat prefix cannot contain the following word(s): {string.Join(", ", matches)}");
					return;
				}

				//PlayerInfo target;

				//TSPlayer player = TShock.Players.FirstOrDefault(p => p?.User == users[0]);
				//if (player != null && player.GetData<PlayerInfo>(PlayerInfo.Data_Key) != null)
				//{
				//	target = player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
				//}
				//else
				//{
				//	target = _database.Get(users[0]);
				//}

				PlayerInfo target = _database.Get(users[0]);

				if (target == null)
				{
					target = new PlayerInfo()
					{
						UserId = users[0].ID,
						ChatData = new ChatData(prefix, null, null),
						Permissions = new PermissionCollection()
					};

					_database.Add(target);
				}
				else
				{
					target.ChatData.Prefix = prefix;
					_database.Update(target, UpdateType.Prefix);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat suffix.");
				return;
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				string suffix = string.Join(" ", e.Parameters);
				//if (suffix.Length > _config.MaximumSuffixLength)
				//{
				//	e.Player.SendErrorMessage($"Your suffix cannot be longer than {_config.MaximumSuffixLength} characters.");
				//	return;
				//}

				IEnumerable<string> matches = _config.ProhibitedWords.Where(p => suffix.ToLowerInvariant().Contains(p.ToLowerInvariant()));
				if (matches.Any())
				{
					e.Player.SendErrorMessage($"Your chat suffix cannot contain the following word(s): {string.Join(", ", matches)}");
					return;
				}

				//PlayerInfo target;

				//TSPlayer player = TShock.Players.FirstOrDefault(p => p?.User == users[0]);
				//if (player != null && player.GetData<PlayerInfo>(PlayerInfo.Data_Key) != null)
				//{
				//	target = player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
				//}
				//else
				//{
				//	target = _database.Get(users[0]);
				//}

				PlayerInfo target = _database.Get(users[0]);

				if (target == null)
				{
					target = new PlayerInfo()
					{
						UserId = users[0].ID,
						ChatData = new ChatData(null, suffix, null),
						Permissions = new PermissionCollection()
					};

					_database.Add(target);
				}
				else
				{
					target.ChatData.Suffix = suffix;
					_database.Update(target, UpdateType.Suffix);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != e.Player.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You do not have permission to change this player's chat color.");
				return;
			}
			else
			{
				//PlayerInfo target;

				string[] color = e.Parameters[2].Split(',');
				if (color.Length == 3 && byte.TryParse(color[0], out byte r) && byte.TryParse(color[1], out byte g) && byte.TryParse(color[2], out byte b))
				{
					//TSPlayer player = TShock.Players.FirstOrDefault(p => p?.User == users[0]);
					//if (player != null && player.GetData<PlayerInfo>(PlayerInfo.Data_Key) != null)
					//{
					//	target = player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
					//}
					//else
					//{
					//	target = _database.Get(users[0]);
					//}

					PlayerInfo target = _database.Get(users[0]);

					if (target == null)
					{
						target = new PlayerInfo()
						{
							UserId = users[0].ID,
							ChatData = new ChatData(null, null, e.Parameters[2]),
							Permissions = new PermissionCollection()
						};

						_database.Add(target);
					}
					else
					{
						target.ChatData.Color = e.Parameters[2];
						_database.Update(target, UpdateType.Color);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else if (users[0].Name != e.Player.User.Name && !e.Player.HasPermission("us.setother"))
			{
				e.Player.SendErrorMessage("You can't modify this player's chat data.");
				return;
			}
			else
			{
				PlayerInfo target = _database.Get(users[0]);

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
								return;
							}
							else
							{
								target.ChatData.Prefix = null;
								_database.Update(target, UpdateType.Prefix);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
					case "suffix":
						{
							if (!e.Player.HasPermission("us.remove.suffix"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}
							else
							{
								target.ChatData.Suffix = null;
								_database.Update(target, UpdateType.Suffix);
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
								return;
							}
							else
							{
								target.ChatData.Color = null;
								_database.Update(target, UpdateType.Color);
								e.Player.SendSuccessMessage($"Modified {users[0].Name}'s chat data successfully.");
							}
						}
						break;
					case "all":
						{
							if (!e.Player.HasPermission("us.resetall"))
							{
								e.Player.SendErrorMessage("You do not have access to this command.");
								return;
							}
							else
							{
								target.ChatData = new ChatData();
								_database.Update(target, UpdateType.Prefix | UpdateType.Suffix | UpdateType.Color);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else
			{
				PlayerInfo target = _database.Get(users[0]);

				if (target == null)
				{
					e.Player.SendErrorMessage("This user has no player specific information to read.");
					return;
				}
				else
				{
					e.Player.SendMessage($"Username: {users[0].Name}", Color.LawnGreen);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage($"Could not find a user under the name '{e.Parameters[1]}'");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(u => u.Name));
				return;
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				PlayerInfo target = _database.Get(users[0]);

				if (target == null)
				{
					target = new PlayerInfo()
					{
						UserId = users[0].ID,
						ChatData = new ChatData(),
						Permissions = new PermissionCollection(e.Parameters)
					};
				}
				else
				{
					e.Parameters.ForEach(p => target.Permissions.AddPermission(p));
					_database.Update(target, UpdateType.Permissions);
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

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage($"Could not find a user under the name '{e.Parameters[1]}'");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(u => u.Name));
				return;
			}
			else
			{
				e.Parameters.RemoveRange(0, 2);
				PlayerInfo target = _database.Get(users[0]);

				if (target == null)
				{
					e.Player.SendErrorMessage("This user has no custom permissions.");
					return;
				}
				else
				{
					e.Parameters.ForEach(p => target.Permissions.RemovePermission(p));
					_database.Update(target, UpdateType.Permissions);
				}

				e.Player.SendSuccessMessage($"Modified {users[0].Name}'s permissions successfully.");
			}
		}

		private void HandleGetPermissions(CommandArgs e)
		{
			if (e.Parameters.Count < 2 || e.Parameters.Count > 3)
			{
				e.Player.SendErrorMessage($"Invalid syntax! Proper syntax: {Commands.Specifier}permission list <player name> [page]");
				return;
			}

			List<User> users = TShock.Users.GetUsersByName(e.Parameters[1]);
			if (users.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid player!");
				return;
			}
			else if (users.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(e.Player, users.Select(p => p.Name));
				return;
			}
			else
			{
				PlayerInfo target = _database.Get(users[0]);

				if (target == null || target.Permissions.Count == 0)
				{
					e.Player.SendErrorMessage("This user has no permissions to list.");
					return;
				}

				if (!PaginationTools.TryParsePageNumber(e.Parameters, 2, e.Player, out int pageNumber))
				{
					return;
				}

				e.Player.SendSuccessMessage($"{users[0].Name}'s permissions:");
				e.Player.SendInfoMessage(target.Permissions.ToString());
			}
		}
	}
}
