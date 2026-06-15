using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aria.AssetManagement.Data;
using Aria.AssetManagement.Hash;

namespace Aria.AssetManagement.IO.Core
{
	public enum DownloadStatus
	{
		Waiting,
		Downloading,
		Success,
		Failed,
	}

	public abstract class DownloadHandle
	{
		public readonly DownloadingEvent? DownloadingEvent;
		public DownloadStatus Status;
		public long DownloadedBytes;
		public abstract string Label { get; }
		public abstract long FileSize { get; }

		public bool IsDownloadWait
			=> Status == DownloadStatus.Waiting;

		public bool IsDownloadEnd
			=> Status != DownloadStatus.Waiting
			&& Status != DownloadStatus.Downloading;

		public bool IsDownloadSuccess
			=> Status == DownloadStatus.Success;

		public virtual bool ValidationHash
			=> false;

		public virtual HashType HashType
			=> HashType.MD5;

		public virtual string Hash
			=> string.Empty;

		public DownloadHandle(DownloadingEvent? downloadingEvent)
		{
			DownloadingEvent = downloadingEvent;
			Status = DownloadStatus.Waiting;
		}
	}
}
