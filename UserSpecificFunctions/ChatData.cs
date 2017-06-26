namespace UserSpecificFunctions
{
	/// <summary>
	/// Contains information on a user's chat data.
	/// </summary>
	public sealed class ChatData
	{
		/// <summary>
		/// Gets or sets the user's chat prefix.
		/// </summary>
		public string Prefix { get; set; }

		/// <summary>
		/// Gets or sets the user's chat suffix.
		/// </summary>
		public string Suffix { get; set; }

		/// <summary>
		/// Gets or sets the user's chat color.
		/// </summary>
		public string Color { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatData"/> class.
		/// </summary>
		public ChatData()
		{
			Prefix = null;
			Suffix = null;
			Color = null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatData"/> class.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <param name="suffix">The suffix.</param>
		/// <param name="color">The color.</param>
		public ChatData(string prefix, string suffix, string color)
		{
			Prefix = prefix;
			Suffix = suffix;
			Color = color;
		}
	}
}
