using System;

namespace UserSpecificFunctions.Permissions
{
	/// <summary>
	/// Represents a permission.
	/// </summary>
	public class Permission : IEquatable<Permission>
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

		/// <inheritdoc />
		public bool Equals(Permission other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Name, other.Name) && Negated == other.Negated;
		}

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is Permission permission && Equals(permission);

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Negated.GetHashCode();
			}
		}

		/// <inheritdoc />
		public static bool operator ==(Permission left, Permission right) => left.Equals(right);

		/// <inheritdoc />
		public static bool operator !=(Permission left, Permission right) => !left.Equals(right);

		/// <summary>
		/// Returns the string representation of this permission.
		/// </summary>
		/// <returns>The string representation of this permission.</returns>
		public override string ToString() => Negated ? $"!{Name}" : Name;
	}
}
