using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UserSpecificFunctions.Permissions
{
    /// <summary>
    ///     Represents a permission collection.
    /// </summary>
    public sealed class PermissionCollection : ICollection
    {
        private readonly List<Permission> _permissions;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PermissionCollection" /> class.
        /// </summary>
        public PermissionCollection()
        {
            _permissions = new List<Permission>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PermissionCollection" /> class and parses the permissions from the
        ///     given list.
        /// </summary>
        /// <param name="permissions">The permission list to parse.</param>
        public PermissionCollection(IEnumerable<string> permissions)
        {
            _permissions = new List<Permission>();
            foreach (var permission in permissions)
            {
                Add(permission);
            }
        }

        /// <summary>
        ///     Gets the number of elements in this collection.
        /// </summary>
        public int Count => _permissions.Count;

        /// <summary>
        ///     Indicates whether access to the collection is thread safe.
        /// </summary>
        public bool IsSynchronized => ((ICollection) _permissions).IsSynchronized;

        /// <summary>
        ///     The object that can be used to synchronize access to this collection.
        /// </summary>
        public object SyncRoot => ((ICollection) _permissions).SyncRoot;

        /// <summary>
        ///     Adds a permission to the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        public void Add(Permission permission)
        {
            if (Contains(permission))
            {
                return;
            }

            _permissions.Add(permission);
        }

        /// <summary>
        ///     Adds a permission to the collection.
        /// </summary>
        /// <param name="permission">The string representation of the permission.</param>
        public void Add(string permission)
        {
            Add(new Permission(permission));
        }

        /// <summary>
        ///     Checks whether the <paramref name="permission" /> is a part of the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Contains(Permission permission)
        {
            if (permission == null || string.IsNullOrWhiteSpace(permission.Name))
            {
                return true;
            }

            return _permissions.Any(p => p.Name.Equals(permission.Name));
        }

        /// <summary>
        ///     Checks whether the <paramref name="permission" /> is a part of the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Contains(string permission)
        {
            return Contains(new Permission(permission));
        }

        /// <summary>
        ///     Copies the elements of the collection to an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="index">The index to start copying at.</param>
        public void CopyTo(Array array, int index = 0)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "The destination array may not be null.");
            }

            if (index < 0 || index >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    "The starting index may not be negative or greater than the size of the array.");
            }

            ((ICollection) _permissions).CopyTo(array, index);
        }

        /// <summary>
        ///     Removes all elements of this collection.
        /// </summary>
        public void Flush()
        {
            _permissions.Clear();
        }

        /// <summary>
        ///     Gets an enumerable collection of all permissions.
        /// </summary>
        /// <returns>An enumerable collection of all permissions.</returns>
        public IEnumerable<Permission> GetAll()
        {
            return _permissions.AsReadOnly();
        }

        /// <summary>
        ///     Gets the collection's enumerator.
        /// </summary>
        /// <returns>An <see cref="IEnumerator" />.</returns>
        public IEnumerator GetEnumerator()
        {
            return ((ICollection) _permissions).GetEnumerator();
        }

        /// <summary>
        ///     Checks whether the given permission is negated.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Negated(Permission permission)
        {
            return _permissions.Any(p => p.Name.Equals(permission.Name) && p.Negated);
        }

        /// <summary>
        ///     Checks whether the given permission is negated.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns>True or false.</returns>
        public bool Negated(string permission)
        {
            return Negated(new Permission(permission));
        }

        /// <summary>
        ///     Removes a permission from the collection.
        /// </summary>
        /// <param name="permission">The permission.</param>
        public void Remove(Permission permission)
        {
            _permissions.RemoveAll(p => p.Equals(permission));
        }

        /// <summary>
        ///     Removes a permission from the collection.
        /// </summary>
        /// <param name="permission">The string representation of the permission.</param>
        public void Remove(string permission)
        {
            Remove(new Permission(permission));
        }

        /// <summary>
        ///     Gets the string representation of this collection.
        /// </summary>
        /// <returns>The string representation of this collection.</returns>
        public override string ToString()
        {
            return string.Join(", ", _permissions.Select(p => p.ToString()));
        }
    }
}