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
		public string Name { get; set; }

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
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (GetType() != obj.GetType())
			{
				return false;
			}

			Permission permission = (Permission)obj;
			if (this.Name == permission.Name && this.Negated == permission.Negated)
			{
				return true;
			}

			return false;
		}

		public bool Equals(string permissionName)
		{
			Permission permission = new Permission(permissionName);
			return this.Equals(permission);
		}
	}
}
