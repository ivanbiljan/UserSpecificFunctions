using System;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

namespace UserSpecificFunctions.Extensions
{
    /// <summary>
    ///     Provides extensions for the <see cref="string" /> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Attempts to parse a color from the given string.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>A <see cref="Color" /> object.</returns>
        public static Color ParseColor([NotNull] this string color)
        {
            if (color == null)
            {
                throw new ArgumentNullException(nameof(color));
            }

            var colorPayload = color.Split(',');
            if (colorPayload.Length != 3)
            {
                throw new ArgumentException("The color provided was not in the correct format.", nameof(color));
            }
            if (!byte.TryParse(colorPayload[0], out var r))
            {
                throw new ArgumentException("The color provided was not in the correct format.", nameof(color));
            }
            if (!byte.TryParse(colorPayload[1], out var g))
            {
                throw new ArgumentException("The color provided was not in the correct format.", nameof(color));
            }
            if (!byte.TryParse(colorPayload[2], out var b))
            {
                throw new ArgumentException("The color provided was not in the correct format.", nameof(color));
            }

            return new Color(r, g, b);
        }
    }
}