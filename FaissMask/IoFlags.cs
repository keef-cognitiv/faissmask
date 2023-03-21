using System;

namespace FaissMask;

[Flags]
public enum IoFlags
{
	None = 0,
	MMap = 1,
	ReadOnly = 2
}