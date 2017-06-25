using System.Collections.Generic;

namespace UserSpecificFunctions
{
	/// <summary>
	/// The configuration file.
	/// </summary>
	public sealed class Config
	{
		/// <summary>
		/// Gets a list of words users are not allowed to use in their chat tags.
		/// </summary>
		public List<string> ProhibitedWords { get; } = new List<string> { "Shit", "Fuck" };

		/// <summary>
		/// Gets the maximum prefix length.
		/// </summary>
		public int MaximumPrefixLength { get; } = 10;

		/// <summary>
		/// Gets the maximum suffix length.
		/// </summary>
		public int MaximumSuffixLength { get; } = 10;
	}
}
