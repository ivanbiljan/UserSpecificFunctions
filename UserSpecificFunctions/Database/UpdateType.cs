using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserSpecificFunctions.Database
{
	[Flags]
	public enum UpdateType
	{
		Prefix = 1,
		Suffix = 2,
		Color = 4,
		Permissions = 8
	}
}
