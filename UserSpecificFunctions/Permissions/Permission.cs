namespace UserSpecificFunctions.Permissions
{
	/// <summary>
	/// Represents a permission.
	/// </summary>
	public sealed class Permission
	{
		/// <summary>
		/// Gets the permissions's name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the value indicating whether the permission is negated or not.
		/// </summary>
		public bool Negated { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Permission"/> class.
		/// </summary>
		/// <param name="name">The permission's name.</param>
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
		/// Checks to see whether the <see cref="Permission"/> is equal to an <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>True or false.</returns>
		public override bool Equals(object obj) => obj is Permission permission && Equals(permission);

		/// <inheritdoc />
		private bool Equals(Permission other)
		{
			return string.Equals(Name, other.Name) && Negated == other.Negated;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Negated.GetHashCode();
			}
		}

		public bool Equals(string permissionName)
		{
			var permission = new Permission(permissionName);
			return Equals(permission);
		}
	}
}
