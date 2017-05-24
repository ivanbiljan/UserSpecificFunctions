﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Terraria;
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
		private static readonly string configPath = Path.Combine(TShock.SavePath, "userspecificfunctions.json");

		private readonly DatabaseManager _database = new DatabaseManager();
		private Config _config = new Config();
		private CommandHandler _commandHandler;

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
				File.WriteAllText(configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));

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
			_database.Connect();
			if (File.Exists(configPath))
			{
				_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
			}

			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			PlayerHooks.PlayerPostLogin += OnPostLogin;
			PlayerHooks.PlayerPermission += OnPermission;

			_commandHandler = new CommandHandler(_config, _database);
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
				string prefix = playerData?.ChatData?.Prefix ?? player.Group.Prefix;
				string suffix = playerData?.ChatData?.Suffix ?? player.Group.Suffix;
				Color chatColor = playerData?.ChatData.Color?.ParseColor() ?? player.Group.ChatColor.ParseColor();

				if (!TShock.Config.EnableChatAboveHeads)
				{
					string message = string.Format(TShock.Config.ChatFormat, player.Group.Name, prefix, player.Name, suffix, e.Text);
					TSPlayer.All.SendMessage(message, chatColor);
					TSPlayer.Server.SendMessage(message, chatColor);
					TShock.Log.Info($"Broadcast: {message}");

					e.Handled = true;
				}
			}
		}

		private void OnPostLogin(PlayerPostLoginEventArgs e)
		{
			PlayerInfo playerInfo = _database.Get(e.Player.User);
			if (playerInfo != null)
			{
				e.Player.SetData(PlayerInfo.Data_Key, playerInfo);
			}
		}

		private void OnPermission(PlayerPermissionEventArgs e)
		{
			//throw new NotImplementedException();
		}
	}
}