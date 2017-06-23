using System;

namespace UserSpecificFunctions.Database
{
	[Flags]
	public enum DatabaseUpdate
	{
		Prefix = 1,
		Suffix = 2,
		Color = 4,
		Permissions = 8
	}
}
