using System;
using System.Collections.Generic;
using FaissMask.Extensions;
using FaissMask.Internal;

namespace FaissMask;

public class VectorTransform : IDisposable
{
	public VectorTransform()
	{
	}

	public VectorTransform(VectorTransformSafeHandle handle)
	{
		Handle = handle;
	}

	public int DimensionsIn => Handle.DimensionsIn;

	public int DimensionsOut => Handle.DimensionsOut;

	public void Train(long count, ReadOnlySpan<float> trainingData) => Handle.Train(count, trainingData);

	public void Train(IReadOnlyCollection<float[]> trainingData) =>
		Handle.Train(trainingData.Count, trainingData.Flatten());

	public IEnumerable<float[]> Apply(long count, ReadOnlySpan<float> vectors)
	{
		var dOut = DimensionsOut;
		var destination = new float[dOut * count];
		Handle.Apply(count, vectors, destination);

		return Chunk(destination, count, dOut);
	}

	public IEnumerable<float[]> Apply(IReadOnlyCollection<float[]> vectors) => Apply(vectors.Count, vectors.Flatten());

	public IEnumerable<float[]> Reverse(long count, ReadOnlySpan<float> vectors)
	{
		var dIn = DimensionsIn;
		var destination = new float[dIn * count];
		Handle.Reverse(count, vectors, destination);

		return Chunk(destination, count, dIn);
	}

	public IEnumerable<float[]> Reverse(IReadOnlyCollection<float[]> vectors) =>
		Reverse(vectors.Count, vectors.Flatten());

	public void Dispose()
	{
		Handle?.Dispose();
	}

	public VectorTransformSafeHandle Handle { get; private set; }

	public static VectorTransform RandomRotationMatrix(int dimensionsIn, int dimensionsOut)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode = NativeMethods.faiss_RandomRotationMatrix_new_with(ref result, dimensionsIn, dimensionsOut);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform PcaMatrix(int dimensionsIn, int dimensionsOut, float eigenPower, int randomRotation)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode =
			NativeMethods.faiss_PCAMatrix_new_with(ref result, dimensionsIn, dimensionsOut, eigenPower, randomRotation);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform ItqMatrix(int d)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode = NativeMethods.faiss_ITQMatrix_new_with(ref result, d);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform ItqTransform(int dimensionsIn, int dimensionsOut, bool doPca)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode =
			NativeMethods.faiss_ITQTransform_new_with(ref result, dimensionsIn, dimensionsOut, doPca ? 1 : 0);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform OpqMatrix(int d, int m, int d2)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode = NativeMethods.faiss_OPQMatrix_new_with(ref result, d, m, d2);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform RemapDimensionsTransform(int dimensionsIn, int dimensionsOut, bool uniform)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode =
			NativeMethods.faiss_RemapDimensionsTransform_new_with(ref result, dimensionsIn, dimensionsOut,
				uniform ? 1 : 0);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}

	public static VectorTransform NormalizationTransform(int d, float norm)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode =
			NativeMethods.faiss_NormalizationTransform_new_with(ref result, d, norm);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}
	
	public static VectorTransform CenteringTransform(int d)
	{
		var result = new VectorTransformSafeHandle();
		int returnCode =
			NativeMethods.faiss_CenteringTransform_new_with(ref result, d);
		ValidateReturnCode(returnCode);

		return new VectorTransform(result);
	}
	
	private static void ValidateReturnCode(int returnCode)
	{
		if (returnCode != 0)
		{
			var lastError = NativeMethods.faiss_get_last_error();

			if (string.IsNullOrEmpty(lastError))
			{
				throw new ArgumentException(
					$"An unknown error occurred while creating the transform (return code {returnCode})");
			}

			throw new ArgumentException(
				$"Invalid arguments for transform creation: {lastError} (return code {returnCode})");
		}
	}

	private IEnumerable<float[]> Chunk(float[] flattened, long count, int dOut)
	{
		for (long i = 0; i < count; i++)
		{
			var applied = new float[dOut];
			Array.Copy(flattened, i * dOut, applied, 0, dOut);
			yield return applied;
		}
	}
}