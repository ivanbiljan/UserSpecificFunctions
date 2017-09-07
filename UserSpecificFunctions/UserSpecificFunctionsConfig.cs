using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UserSpecificFunctions
{
	/// <summary>
	/// The configuration file.
	/// </summary>
	public sealed class UserSpecificFunctionsConfig
	{
		/// <summary>
		/// Gets the maximum prefix length.
		/// </summary>
		public int MaximumPrefixLength { get; } = 10;

		/// <summary>
		/// Gets the maximum suffix length.
		/// </summary>
		public int MaximumSuffixLength { get; } = 10;

		/// <summary>
		/// Gets a list of words users are not allowed to use in their chat tags.
		/// </summary>
		public List<string> ProhibitedWords { get; } = new List<string> { "Shit", "Fuck" };

		/// <summary>
		/// Reads the configuration file from the given path, or creates one if it doesn't exist.
		/// </summary>
		/// <param name="configPath">The path.</param>
		/// <returns>The config.</returns>
		public static UserSpecificFunctionsConfig ReadOrCreate(string configPath)
		{
			if (File.Exists(configPath))
			{
				return JsonConvert.DeserializeObject<UserSpecificFunctionsConfig>(File.ReadAllText(configPath));
			}

			var config = new UserSpecificFunctionsConfig();
			File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
			return config;
		}
	}
}
