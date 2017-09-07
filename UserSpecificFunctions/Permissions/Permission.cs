using System;

namespace UserSpecificFunctions.Permissions
{
	/// <summary>
	/// Represents a permission.
	/// </summary>
	public sealed class Permission : IEquatable<Permission>
	{
		/// <summary>
		/// Gets the permission's name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the value indicating whether the permission is negated or not.
		/// </summary>
		public bool Negated { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Permission"/> class with the specified name.
		/// </summary>
		/// <param name="name"></param>
		public Permission(string name)
		{
			if (name.StartsWith("!"))
			{
				name = name.Remove(0, 1);
				Negated = true;
			}

			Name = name;
		}

		/// <summary>
		/// Compares the permission to the given <see cref="Permission"/> object in order to check if they are equal.
		/// </summary>
		/// <param name="other">The <see cref="Permission"/> object.</param>
		/// <returns><c>true</c> if the permissions are equal.</returns>
		public bool Equals(Permission other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Name, other.Name);
		}

		/// <summary>
		/// Compares the permission to the given object in order to check if they are equal.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns><c>true</c> if the permission and the object are equal.</returns>
		public override bool Equals(object obj) => obj is Permission permission && Equals(permission);

		/// <summary>
		/// Returns the hash code of the permission.
		/// </summary>
		/// <returns>The hash.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Negated.GetHashCode();
			}
		}

		/// <summary>
		/// Tests for equality of two permission objects.
		/// </summary>
		/// <param name="left">The first permission.</param>
		/// <param name="right">The second permission.</param>
		/// <returns><c>true</c> if the permissions are equal.</returns>
		public static bool operator ==(Permission left, Permission right) => left?.Equals(right) ?? false;

		/// <summary>
		/// Tests for inequality of two permission objects.
		/// </summary>
		/// <param name="left">The first permission.</param>
		/// <param name="right">The second permission.</param>
		/// <returns><c>true</c> if the permissions are not equal.</returns>
		public static bool operator !=(Permission left, Permission right) => !left?.Equals(right) ?? false;

		/// <summary>
		/// Returns the string representation of this permission.
		/// </summary>
		/// <returns>The string representation of this permission.</returns>
		public override string ToString() => Negated ? $"!{Name}" : Name;
	}
}
