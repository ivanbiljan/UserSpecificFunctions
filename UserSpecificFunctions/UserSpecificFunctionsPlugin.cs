using System;
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
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
	[ApiVersion(2, 0)]
    public sealed class UserSpecificFunctionsPlugin : TerrariaPlugin
    {
		private static readonly string configPath = Path.Combine(TShock.SavePath, "userspecificfunctions.json");

		private Config _config;
		private readonly DatabaseManager _database;
		private readonly CommandHandler _commandHandler;

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
			_config = new Config();
			_database = new DatabaseManager();
			_commandHandler = new CommandHandler(_config, _database);
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
			_commandHandler.Register();
			if (File.Exists(configPath))
			{
				_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
			}

			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			PlayerHooks.PlayerPostLogin += OnPostLogin;
			PlayerHooks.PlayerPermission += OnPermission;
		}

		private void OnChat(ServerChatEventArgs e)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}
	}
}
