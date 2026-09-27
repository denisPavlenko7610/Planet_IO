using System;
using System.IO;
using System.IO.Compression;

namespace UnityTemplates.Foundation
{
	public static class GZipHelper
	{
		public static byte[] Compress(byte[] data, CompressionLevel level = CompressionLevel.Optimal)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			using MemoryStream output = new MemoryStream();

			using (GZipStream gzip = new GZipStream(output, level))
			{
				gzip.Write(data, 0, data.Length);
			}

			return output.ToArray();
		}

		public static byte[] Decompress(byte[] compressed)
		{
			if (compressed == null)
			{
				throw new ArgumentNullException(nameof(compressed));
			}

			using MemoryStream input = new MemoryStream(compressed);
			using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
			using MemoryStream output = new MemoryStream();

			gzip.CopyTo(output);

			return output.ToArray();
		}
	}
}
