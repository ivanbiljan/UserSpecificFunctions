using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using Jint.Native;
using Wolfje.Plugins.Jist;
using Wolfje.Plugins.Jist.Framework;
using UserSpecificFunctions;

namespace UserSpecificFunctionsScripting
{
	/// <summary>
	/// Represents the UserSpecificFunctionsScripting plugin.
	/// </summary>
	[ApiVersion(2, 1)]
	public class UserSpecificFunctionsScriptPlugin : TerrariaPlugin
	{
		/// <summary>
		/// Gets the author.
		/// </summary>
		public override string Author => "Professor X";

		/// <summary>
		/// Gets the description.
		/// </summary>
		public override string Description => "Provides JIST scripting support for UserSpecificFunctions.";

		/// <summary>
		/// Gets the name.
		/// </summary>
		public override string Name => "UserSpecificFunctionsScript";

		/// <summary>
		/// Gets the version.
		/// </summary>
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserSpecificFunctionsScriptPlugin"/> class with the specified Main instance.
		/// </summary>
		/// <param name="game">The Main instance.</param>
		public UserSpecificFunctionsScriptPlugin(Main game) : base(game)
		{
			
		}

		/// <summary>
		/// Disposes the plugin.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release managed resources; otherwise <c>false</c></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Initializes the plugin.
		/// </summary>
		public override void Initialize()
		{
			JistPlugin.JavascriptFunctionsNeeded += OnJavascriptFunctionsNeeded;
		}

		private void OnJavascriptFunctionsNeeded(object sender, JavascriptFunctionsNeededEventArgs e)
		{
			e.Engine.CreateScriptFunctions(GetType(), this);
		}

		/// <summary>
		/// Returns a <see cref="PlayerInfo"/> object based on the given playerRef.
		/// </summary>
		/// <param name="playerRef">The playerRef.</param>
		/// <returns>A <see cref="PlayerInfo"/> object.</returns>
		[JavascriptFunction("usf_getPlayer")]
		public PlayerInfo GetPlayerInfo(object playerRef)
		{
			if (playerRef == null || UserSpecificFunctionsPlugin.Instance == null)
			{
				return null;
			}

			if (playerRef is int)
			{
				return UserSpecificFunctionsPlugin.Instance.Database.Get((int) playerRef);
			}

			if (playerRef is string)
			{
				return UserSpecificFunctionsPlugin.Instance.Database.Get(TShock.Users.GetUserByName((string) playerRef));
			}

			return null;
		}

		[JavascriptFunction("usf_setUserPrefix")]
		public void SetAccountPrefix(PlayerInfo player, string prefix)
		{
			if (UserSpecificFunctionsPlugin.Instance == null || player == null)
			{
				return;
			}

			player.ChatData.Prefix = prefix;
			UserSpecificFunctionsPlugin.Instance.Database.Update(player, UserSpecificFunctions.Database.DatabaseUpdate.Prefix);
		}
	}
}
