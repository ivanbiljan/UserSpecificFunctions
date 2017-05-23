using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public List<string> ProhibitedWords { get; } = new List<string>() { "Shit", "Fuck" };

		/// <summary>
		/// Gets the maximum prefix length.
		/// </summary>
		public int MaximumPrefixLength { get; }

		/// <summary>
		/// Gets the maximum suffix length.
		/// </summary>
		public int MaximumSuffixLength { get; }
	}
}
