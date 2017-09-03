namespace UserSpecificFunctions.Database
{
	/// <summary>
	/// Contains information on a user's chat data.
	/// </summary>
	public sealed class ChatData
	{
		/// <summary>
		/// Gets or sets the user's chat color.
		/// </summary>
		public string Color { get; set; }

		/// <summary>
		/// Gets or sets the user's chat prefix.
		/// </summary>
		public string Prefix { get; set; }

		/// <summary>
		/// Gets or sets the user's chat suffix.
		/// </summary>
		public string Suffix { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatData"/> class with the specified prefix, suffix and chat color.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <param name="suffix">The suffix.</param>
		/// <param name="color">The color.</param>
		public ChatData(string color = null, string prefix = null, string suffix = null)
		{
			Color = color;
			Prefix = prefix;
			Suffix = suffix;
		}
	}
}
