using System;
using System.Reflection.Metadata;

namespace FaissMask.Internal;

public class RangeSearchResultSafeHandle : SafeHandleZeroIsInvalid
{
	protected override bool ReleaseHandle()
	{
		if (!IsFree)
			Free();
		return true;
	}

	public ref struct LabelResults
	{
		public ReadOnlySpan<long> Labels;
		public ReadOnlySpan<float> Distances;
	}
	
	public bool IsFree { get; internal set; } = false;

	public static RangeSearchResultSafeHandle New(long nq)
	{
		RangeSearchResultSafeHandle result = new RangeSearchResultSafeHandle();
		int rt = NativeMethods.faiss_RangeSearchResult_new(ref result, nq);
		
		if (rt != 0)
		{
			var lastError = NativeMethods.faiss_get_last_error();

			if (string.IsNullOrEmpty(lastError))
			{
				throw new ArgumentException(
					$"An unknown error occurred trying to create the range search result (return code {rt})");
			}
			else
			{
				throw new ArgumentException(
					$"Invalid creation of range search result: {lastError} (return code {rt})");
			}
		}
		return result;
	}

	private void Free()
	{
		if (!IsInvalid)
		{
			NativeMethods.faiss_RangeSearchResult_free(handle);
			IsFree = true;
		}
	}
	
	public ulong Nq => NativeMethods.faiss_RangeSearchResult_nq(this);
	
	public ulong BufferSize => NativeMethods.faiss_RangeSearchResult_buffer_size(this);

	public ReadOnlySpan<ulong> Limits
	{
		get
		{
			IntPtr lims = IntPtr.Zero;
			NativeMethods.faiss_RangeSearchResult_lims(this, ref lims);
			if (lims == IntPtr.Zero)
			{
				throw new MemberAccessException("Limits was null");
			}

			unsafe
			{
				return new ReadOnlySpan<ulong>(lims.ToPointer(), (int)Nq + 1);
			}
		}
	}

	public LabelResults  Labels
	{
		get
		{
			IntPtr labels = IntPtr.Zero;
			IntPtr distances = IntPtr.Zero;
			NativeMethods.faiss_RangeSearchResult_labels(this, ref labels, ref distances);
			if (labels == IntPtr.Zero || distances == IntPtr.Zero)
			{
				throw new MemberAccessException("Result Labels and/or Distances was null");
			}

			int nq = (int)Nq;
			var lims = Limits;

			unsafe
			{
				return new LabelResults()
				{
					Distances = new ReadOnlySpan<float>(distances.ToPointer(), (int)lims[nq]),
					Labels = new ReadOnlySpan<long>(labels.ToPointer(), (int)lims[nq])
				};
			}
		}
	}
}