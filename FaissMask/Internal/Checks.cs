using System;

namespace FaissMask.Internal;

public static class Checks
{
	public static void RequireCountMatches(long vectorCount, ReadOnlySpan<float> data, int dimensions)
	{
		if (data.Length != (vectorCount * dimensions))
		{
			throw new ArgumentException(
				$"Invalid vectors size {data.Length} for {vectorCount} vectors of dimensionality {dimensions}",
				nameof(data));
		}
	}
}