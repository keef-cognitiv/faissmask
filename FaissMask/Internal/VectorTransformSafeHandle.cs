using System;
using System.Collections.Generic;

namespace FaissMask.Internal;

public class VectorTransformSafeHandle : SafeHandleZeroIsInvalid
{
	public VectorTransformSafeHandle()
	{
	}

	public VectorTransformSafeHandle(IntPtr handle)
		: base(handle)
	{
	}
	
	protected override bool ReleaseHandle()
	{
		if (!IsFree)
			Free();
		return true;
	}

	public int DimensionsIn
	{
		get
		{
			return NativeMethods.faiss_VectorTransform_d_in(this);
		}
	}

	public int DimensionsOut
	{
		get
		{
			return NativeMethods.faiss_VectorTransform_d_out(this);
		}
	}
	
	public unsafe void Train(long count, ReadOnlySpan<float> trainingData)
	{
		Checks.RequireCountMatches(count, trainingData, DimensionsIn);
		
		int returnCode;
		fixed (float* data = trainingData)
		{
			returnCode = NativeMethods.faiss_VectorTransform_train(this, count, data);
		}
			
		if (returnCode != 0)
		{
			var lastError = NativeMethods.faiss_get_last_error();

			if (string.IsNullOrEmpty(lastError))
			{
				throw new ArgumentException(
					$"An unknown error occurred while training the transform (return code {returnCode})");
			}

			throw new ArgumentException(
				$"Invalid arguments for a train or train on a transform that doesn't support it: {lastError} (return code {returnCode})");
		}
	}

	public unsafe void Apply(long count, ReadOnlySpan<float> vectors, Span<float> destination)
	{
		Checks.RequireCountMatches(count, vectors, DimensionsIn);

		var expectedLength = DimensionsOut * count;
		if (destination.Length != expectedLength)
		{
			throw new ArgumentException($"Expected destination to be of length {expectedLength}. Got {destination.Length}", nameof(destination));
		}

		int returnCode;
		fixed (float* vects = vectors)
		{
			fixed (float* dest = destination)
			{
				returnCode = NativeMethods.faiss_VectorTransform_apply_noalloc(this, count, vects, dest);
			}
		}
		
		if (returnCode != 0)
		{
			var lastError = NativeMethods.faiss_get_last_error();

			if (string.IsNullOrEmpty(lastError))
			{
				throw new ArgumentException(
					$"An unknown error occurred while transforming vectors (return code {returnCode})");
			}

			throw new ArgumentException(
				$"Invalid arguments for vector transform: {lastError} (return code {returnCode})");
		}
	}
	
	public unsafe void Reverse(long count, ReadOnlySpan<float> vectors, Span<float> destination)
	{
		Checks.RequireCountMatches(count, vectors, DimensionsOut);

		var expectedLength = DimensionsIn * count;
		if (destination.Length != expectedLength)
		{
			throw new ArgumentException($"Expected destination to be of length {expectedLength}. Got {destination.Length}", nameof(destination));
		}

		int returnCode;
		fixed (float* vects = vectors)
		{
			fixed (float* dest = destination)
			{
				returnCode = NativeMethods.faiss_VectorTransform_reverse_transform(this, count, vects, dest);
			}
		}
		
		if (returnCode != 0)
		{
			var lastError = NativeMethods.faiss_get_last_error();

			if (string.IsNullOrEmpty(lastError))
			{
				throw new ArgumentException(
					$"An unknown error occurred while reversing transformed vectors (return code {returnCode})");
			}

			throw new ArgumentException(
				$"Invalid arguments for vector reverse transform: {lastError} (return code {returnCode})");
		}
	}

	public bool IsFree { get; private set; }

	private void Free()
	{
		NativeMethods.faiss_VectorTransform_free(handle);
		IsFree = true;
	}
}