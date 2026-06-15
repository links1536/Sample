using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Aria.AssetManagement.Hash
{
	public class HashCalculator : IDisposable
	{
		HashAlgorithm m_HashAlgorithm;

		public string Hash
			=> m_Hash;

		string m_Hash;

		public HashCalculator(HashType hashType)
		{
			m_HashAlgorithm = hashType switch
			{
				HashType.MD5 => MD5.Create(),
				HashType.SHA1 => SHA1.Create(),
				HashType.SHA256 => SHA256.Create(),
				HashType.SHA384 => SHA384.Create(),
				HashType.SHA512 => SHA512.Create(),
				_ => throw new ArgumentException($"HashType: {hashType} is unknown"),
			};
		}

		public void Dispose()
		{
			if (m_HashAlgorithm != null)
			{
				m_HashAlgorithm.Dispose();
				m_HashAlgorithm = null;
			}
		}

		public void Reset()
		{
			m_HashAlgorithm.Initialize();
		}

		public string Calculate(Stream stream)
		{
			byte[] bytes = m_HashAlgorithm.ComputeHash(stream);
			m_Hash = Convert.ToBase64String(bytes);
			return m_Hash;
		}

		public async Task<string> CalculateAsync(Stream stream)
		{
			int inputSize = 1024;
			var inputBuffer = ArrayPool<byte>.Shared.Rent(inputSize);
			while (true)
			{
				int readLength = await stream.ReadAsync(inputBuffer, 0, inputSize).ConfigureAwait(false);
				if (readLength < inputSize)
				{
					m_HashAlgorithm.TransformFinalBlock(inputBuffer, 0, readLength);
					break;
				}
				m_HashAlgorithm.TransformBlock(inputBuffer, 0, readLength, null, 0);
			}
			byte[] bytes = m_HashAlgorithm.Hash;
			m_Hash = Convert.ToBase64String(bytes);
			return m_Hash;
		}

		public void PartialCompute(byte[] inputBuffer, int inputOffset, int inputCount, bool last)
		{
			if (last)
			{
				m_HashAlgorithm.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
				m_Hash = Convert.ToBase64String(m_HashAlgorithm.Hash);
				return;
			}
			m_HashAlgorithm.TransformBlock(inputBuffer, inputOffset, inputCount, null, 0);
		}
	}
}
