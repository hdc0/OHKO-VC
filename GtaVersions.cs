using System;

namespace ohko_gtavc
{
	enum GtaVersion
	{
		Unknown,
		VC_1_0,
		VC_1_1,
		VC_JP
	}

	static class GtaVersions
	{
		/// <summary>
		/// Tries to detect the version of GTA Vice City that is running in the specified process.
		/// </summary>
		/// <param name="process">The process to check.</param>
		/// <returns>The detected GTA version or GtaVersion.Unknown.</returns>
		public static GtaVersion Detect(LocalProcess process)
		{
			// Uses the method zoton2 uses in his LiveSplit autosplitter for version detection
			// (https://github.com/zoton2/LiveSplit.Scripts/blob/master/LiveSplit.GTAVC.asl)

			// Read byte at 0x608578
			byte gameVersion;
			try
			{
				gameVersion = process.ReadMemory(new IntPtr(0x608578), 1)[0];
			}
			catch
			{
				return GtaVersion.Unknown;
			}

			switch (gameVersion)
			{
				case 0x5D: return GtaVersion.VC_1_0;
				case 0x81: return GtaVersion.VC_1_1;
				case 0x44: return GtaVersion.VC_JP;
				default: return GtaVersion.Unknown;
			}
		}
	}
}
