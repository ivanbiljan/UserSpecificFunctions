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
        private readonly List<Permission> _permissions;

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _permissions.Count;

        /// <summary>
        /// Indicates whether access to the collection is thread safe.
        /// </summary>
        public bool IsSynchronized => ((ICollection) _permissions).IsSynchronized;

        /// <summary>
        /// The object that can be used to synchronize access to the collection.
        /// </summary>
        public object SyncRoot => ((ICollection) _permissions).SyncRoot;

        /// <summary>
        /// Copies the elements of the collection to an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The index to start copying at.</param>
        public void CopyTo(Array array, int index = 0)
        {
            ((ICollection) _permissions).CopyTo(array, index);
        }

        /// <summary>
        /// Gets the collection's enumerator.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/></returns>
        public IEnumerator GetEnumerator() => ((ICollection) _permissions).GetEnumerator();

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
            foreach (var permission in permissions)
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
            return string.Join(", ", _permissions.Select(p => p.ToString()));
        }

        /// <summary>
        /// Adds a permission to the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        public void AddPermission(Permission permission)
        {
            if (ContainsPermission(permission))
            {
                return;
            }

            _permissions.Add(permission);
        }

        /// <summary>
        /// Adds a permission to the collection.
        /// </summary>
        /// <param name="permission">The string representation of the permission.</param>
        public void AddPermission(string permission) => AddPermission(new Permission(permission));

        /// <summary>
        /// Checks whether the <paramref name="permission"/> is a part of the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool ContainsPermission(Permission permission)
        {
            if (permission == null || string.IsNullOrWhiteSpace(permission.Name))
            {
                return true;
            }

            return _permissions.Any(p => p.Name.Equals(permission.Name));
        }

        /// <summary>
        /// Checks whether the <paramref name="permission"/> is a part of the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool ContainsPermission(string permission) => ContainsPermission(new Permission(permission));

        /// <summary>
        /// Gets all permissions.
        /// </summary>
        /// <returns>An enumerable collection of all permissions.</returns>
        public IEnumerable<Permission> GetPermissions() => _permissions.AsReadOnly();

        /// <summary>
        /// Checks whether the given permission is negated.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Negated(Permission permission) => _permissions.Any(p => p.Name.Equals(permission.Name) && p.Negated);

        /// <summary>
        /// Checks whether the given permission is negated.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Negated(string permission) => Negated(new Permission(permission));

        /// <summary>
        /// Removes a permission from the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        public void RemovePermission(Permission permission) => _permissions.RemoveAll(p => p.Equals(permission));

        /// <summary>
        /// Removes a permission from the collection.
        /// </summary>
        /// <param name="permission">The string representation of the permission.</param>
        public void RemovePermission(string permission) => RemovePermission(new Permission(permission));
    }
}
