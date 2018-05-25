using JetBrains.Annotations;

namespace UserSpecificFunctions.Database
{
    /// <summary>
    ///     Holds information on a user's chat data.
    /// </summary>
    public sealed class ChatInformation
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ChatInformation" /> class with the specified prefix, suffix and chat
        ///     color.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="suffix">The suffix.</param>
        /// <param name="color">The color.</param>
        public ChatInformation(string prefix = null, string suffix = null, string color = null)
        {
            Prefix = prefix;
            Suffix = suffix;
            Color = color;
        }

        /// <summary>
        ///     Gets or sets the user's chat color.
        /// </summary>
        [CanBeNull]
        public string Color { get; set; }

        /// <summary>
        ///     Gets or sets the user's chat prefix.
        /// </summary>
        [CanBeNull]
        public string Prefix { get; set; }

        /// <summary>
        ///     Gets or sets the user's chat suffix.
        /// </summary>
        [CanBeNull]
        public string Suffix { get; set; }
    }
}