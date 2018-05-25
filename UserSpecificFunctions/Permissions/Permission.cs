using System;
using JetBrains.Annotations;

namespace UserSpecificFunctions.Permissions
{
    /// <summary>
    ///     Represents a permission.
    /// </summary>
    public sealed class Permission : IEquatable<Permission>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Permission" /> class with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        public Permission([NotNull] string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.StartsWith("!"))
            {
                name = name.Remove(0, 1);
                Negated = true;
            }

            Name = name;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Permission" /> class with the specified name and negation status.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="negated">The negation status.</param>
        public Permission([NotNull] string name, bool negated)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Negated = negated;
        }

        /// <summary>
        ///     Gets the permission's name.
        /// </summary>
        [NotNull]
        public string Name { get; }

        /// <summary>
        ///     Gets the value indicating whether the permission is negated or not.
        /// </summary>
        public bool Negated { get; }

        /// <summary>
        ///     Compares the permission to the given <see cref="Permission" /> object in order to check if they are equal.
        /// </summary>
        /// <param name="other">The <see cref="Permission" /> object.</param>
        /// <returns><c>true</c> if the permissions are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Permission other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name) && Negated == other.Negated;
        }

        /// <summary>
        ///     Compares the permission to the given object in order to check if they are equal.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if the permission and the object are equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is Permission permission && Equals(permission);
        }

        /// <summary>
        ///     Returns the hash code of the permission.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ Negated.GetHashCode();
            }
        }

        /// <summary>
        ///     Returns the string representation of this permission.
        /// </summary>
        /// <returns>The string representation of this permission.</returns>
        public override string ToString()
        {
            return Negated ? $"!{Name}" : Name;
        }

        /// <summary>
        ///     Tests for equality of two permission objects.
        /// </summary>
        /// <param name="left">The first permission.</param>
        /// <param name="right">The second permission.</param>
        /// <returns><c>true</c> if the permissions are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Permission left, Permission right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Tests for inequality of two permission objects.
        /// </summary>
        /// <param name="left">The first permission.</param>
        /// <param name="right">The second permission.</param>
        /// <returns><c>true</c> if the permissions are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Permission left, Permission right)
        {
            return !left.Equals(right);
        }
    }
}