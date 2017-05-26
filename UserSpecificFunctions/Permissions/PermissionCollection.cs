using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UserSpecificFunctions.Permissions
{
	/// <summary>
	/// Represents a permission collection.
	/// </summary>
	public sealed class PermissionCollection : ICollection
	{
		private List<Permission> _permissions;

		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		public int Count => ((ICollection)_permissions).Count;

		/// <summary>
		/// Indicates whether access to the collection is thread safe.
		/// </summary>
		public bool IsSynchronized => ((ICollection)_permissions).IsSynchronized;

		/// <summary>
		/// The object that can be used to synchronize access to the collection.
		/// </summary>
		public object SyncRoot => ((ICollection)_permissions).SyncRoot;

		/// <summary>
		/// Copies the elements of the collection to an array.
		/// </summary>
		/// <param name="array">The array to copy to.</param>
		/// <param name="index">The index to start copying at.</param>
		public void CopyTo(Array array, int index = 0)
		{
			((ICollection)_permissions).CopyTo(array, index);
		}

		/// <summary>
		/// Gets the collection's enumerator.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/></returns>
		public IEnumerator GetEnumerator() => ((ICollection)_permissions).GetEnumerator();

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCollection"/> class.
		/// </summary>
		public PermissionCollection()
		{
			_permissions = new List<Permission>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCollection"/> class and parses the permissions from the given list.
		/// </summary>
		/// <param name="permissions">The permission list to parse.</param>
		public PermissionCollection(IEnumerable<string> permissions)
		{
			_permissions = new List<Permission>();
			foreach (string permission in permissions)
			{
				AddPermission(permission);
			}
		}

		/// <summary>
		/// Gets the string representation of this collection.
		/// </summary>
		/// <returns>A string representation of this collection.</returns>
		public override string ToString()
		{
			return string.Join(", ", _permissions.Select(p => p.Negated ? $"!{p.Name}" : p.Name));
		}

		/// <summary>
		/// Adds a permission to the collection.
		/// </summary>
		/// <param name="permission"></param>
		public void AddPermission(string permission)
		{
			if (!ContainsPermission(permission))
			{
				_permissions.Add(new Permission(permission));
			}
		}

		/// <summary>
		/// Removes a permission from the collection.
		/// </summary>
		/// <param name="permission">The permission.</param>
		public void RemovePermission(Permission permission)
		{
			_permissions.RemoveAll(p => p.Equals(permission));
		}

		/// <summary>
		/// Removes a permission from the collection.
		/// </summary>
		/// <param name="permission">The permission.</param>
		public void RemovePermission(string permission)
		{
			//_permissions.RemoveAll(p => p.Equals(permission));
			RemovePermission(new Permission(permission));
		}

		/// <summary>
		/// Checks whether the <paramref name="permission"/> is a part of the collection.
		/// </summary>
		/// <param name="permission">The permission.</param>
		/// <returns>True or false.</returns>
		public bool ContainsPermission(Permission permission)
		{
			if (permission == null)
			{
				return true;
			}

			if (permission.Negated)
			{
				return false;
			}

			return _permissions.Any(p => p.Equals(permission));
		}

		/// <summary>
		/// Checks whether the <paramref name="permission"/> is a part of the collection.
		/// </summary>
		/// <param name="permission">The permission.</param>
		/// <returns>True or false.</returns>
		public bool ContainsPermission(string permission)
		{
			if (string.IsNullOrWhiteSpace(permission))
			{
				return true;
			}

			//if (permission.StartsWith("!"))
			//{
			//	return false;
			//}

			//return _permissions.Any(p => p.Equals(permission));
			return ContainsPermission(new Permission(permission));
		}
	}
}
