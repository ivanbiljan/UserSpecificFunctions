using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
	/// <summary>
	///	 The configuration file.
	/// </summary>
	public sealed class UserSpecificFunctionsConfig
	{
		/// <summary>
		///	 Maximum prefix length.
		/// </summary>
		public int MaximumPrefixLength = 10;

		/// <summary>
		///	 Maximum suffix length.
		/// </summary>
		public int MaximumSuffixLength = 10;

		/// <summary>
		///	 A list of words users are not allowed to use in their chat tags.
		/// </summary>
		public List<string> ProhibitedWords;

		/// <summary>
		///	 Reads the configuration file from the given path, or creates one if it doesn't exist.
		/// </summary>
		/// <param name="configPath">The path.</param>
		/// <returns>The config.</returns>
		public static UserSpecificFunctionsConfig ReadOrCreate(string configPath)
		{
			UserSpecificFunctionsConfig config;

			if (File.Exists(configPath))
			{
				try
				{
					config = JsonConvert.DeserializeObject<UserSpecificFunctionsConfig>(File.ReadAllText(configPath));
				}
				catch
				{
					TShockAPI.TShock.Log.ConsoleError("[UserSpecificFunctions] Invalid config, loading a blank instead.");
					return new UserSpecificFunctionsConfig()
					{
						ProhibitedWords = new List<string> { "Shit", "Fuck" }
					};
				}
			}
			else
			{
				config = new UserSpecificFunctionsConfig()
				{
					ProhibitedWords = new List<string> { "Shit", "Fuck" }
				};
			}
			
			File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
			return config;
		}
	}
}