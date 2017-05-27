using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.GameContent.NetModules;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using UserSpecificFunctions.Models;
using UserSpecificFunctions.Database;
using UserSpecificFunctions.Extensions;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
	[ApiVersion(2, 1)]
    public sealed class UserSpecificFunctionsPlugin : TerrariaPlugin
    {
		private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "userspecificfunctions.json");

		private CommandHandler _commandHandler;

		/// <summary>
		/// Gets the <see cref="Config"/> instance.
		/// </summary>
		public Config Configuration { get; private set; } = new Config();

		/// <summary>
		/// Gets the <see cref="DatabaseManager"/> instance.
		/// </summary>
		public DatabaseManager Database { get; } = new DatabaseManager();

		/// <summary>
		/// Gets the author.
		/// </summary>
		public override string Author => "Professor X";

		/// <summary>
		/// Gets the name.
		/// </summary>
		public override string Name => "User Specific Functions";

		/// <summary>
		/// Gets the description.
		/// </summary>
		public override string Description => "";

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
				_commandHandler.Deregister();
				File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Configuration, Formatting.Indented));

				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				PlayerHooks.PlayerPostLogin -= OnPostLogin;
				PlayerHooks.PlayerPermission -= OnPermission;
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Initializes the plugin.
		/// </summary>
		public override void Initialize()
		{
			Database.Connect();
			if (File.Exists(ConfigPath))
			{
				Configuration = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
			}

			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			PlayerHooks.PlayerPostLogin += OnPostLogin;
			PlayerHooks.PlayerPermission += OnPermission;

			_commandHandler = new CommandHandler(this);
			_commandHandler.Register();
		}

		private void OnChat(ServerChatEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}

			TSPlayer player = TShock.Players[e.Who];
			if (player == null)
			{
				return;
			}

			PlayerInfo playerData = player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
			if (!e.Text.StartsWith(TShock.Config.CommandSpecifier) && !e.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
			{
				string prefix = playerData?.ChatData.Prefix ?? player.Group.Prefix;
				string suffix = playerData?.ChatData.Suffix ?? player.Group.Suffix;
				Color chatColor = playerData?.ChatData.Color?.ParseColor() ?? player.Group.ChatColor.ParseColor();

				if (!TShock.Config.EnableChatAboveHeads)
				{
					string message = string.Format(TShock.Config.ChatFormat, player.Group.Name, prefix, player.Name, suffix, e.Text);
					TSPlayer.All.SendMessage(message, chatColor);
					TSPlayer.Server.SendMessage(message, chatColor);
					TShock.Log.Info($"Broadcast: {message}");

					e.Handled = true;
				}
				else
				{
					string playerName = player.TPlayer.name;
					player.TPlayer.name = string.Format(TShock.Config.ChatAboveHeadsFormat, player.Group.Name, prefix, player.Name, suffix);
					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, NetworkText.FromLiteral(player.TPlayer.name), e.Who);

					player.TPlayer.name = playerName;

					NetPacket packet = NetTextModule.SerializeServerMessage(NetworkText.FromLiteral(e.Text), chatColor, (byte)e.Who);
					NetManager.Instance.Broadcast(packet, e.Who);

					NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, NetworkText.FromLiteral(playerName), e.Who);

					string msg = string.Format("<{0}> {1}", string.Format(TShock.Config.ChatAboveHeadsFormat, player.Group.Name,
						prefix, player.Name, suffix), e.Text);

					player.SendMessage(msg, chatColor);
					TSPlayer.Server.SendMessage(msg, chatColor);
					TShock.Log.Info($"Broadcast: {msg}");

					e.Handled = true;
				}
			}
		}

		private void OnPostLogin(PlayerPostLoginEventArgs e)
		{
			PlayerInfo playerInfo = Database.Get(e.Player.User);
			if (playerInfo != null)
			{
				e.Player.SetData(PlayerInfo.Data_Key, playerInfo);
			}
		}

		private void OnPermission(PlayerPermissionEventArgs e)
		{
			if (e.Player == null || !e.Player.IsLoggedIn || !e.Player.ContainsData(PlayerInfo.Data_Key))
			{
				return;
			}

			PlayerInfo info = e.Player.GetData<PlayerInfo>(PlayerInfo.Data_Key);
			e.Handled = info.Permissions.ContainsPermission(e.Permission);
		}
	}
}
