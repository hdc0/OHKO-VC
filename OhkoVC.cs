using System;
using System.Collections.Generic;

namespace ohko_gtavc
{
	static class OhkoVC
	{
		private class Addresses
		{
			// 0F B6 05 ? ? ? ? 6B C0 2E ? ? ? ? ? ? ? C3
			/// <summary>
			/// Address of GetPlayerPed function.
			/// </summary>
			public int GetPlayerPed { get; private set; }

			// D9 05 ? ? ? ? 53 56 57 55 83 EC 50
			/// <summary>
			/// Address of CObject::ObjectDamage method.
			/// </summary>
			public int ObjectDamage { get; private set; }

			// 75 0A A1 ? ? ? ? 83 E0 08 75 5A
			/// <summary>
			/// Address of first instruction to skip in DrawHud function.
			/// </summary>
			public int DrawHud_SkipFrom { get; private set; }

			public Addresses(int GetPlayerPed, int ObjectDamage, int DrawHud_SkipFrom)
			{
				this.GetPlayerPed = GetPlayerPed;
				this.ObjectDamage = ObjectDamage;
				this.DrawHud_SkipFrom = DrawHud_SkipFrom;
			}
		}

		/// <summary>
		/// Enables OHKO for the specified process.
		/// </summary>
		/// <param name="process">The target process.</param>
		public static void Enable(LocalProcess process)
		{
			var addrs = GetAddresses(process);
			PatchObjectDamage(process, addrs);
			PatchDrawHud(process, addrs);
		}

		// Tries to identify the GTA version of the process and returns the corresponding addresses.
		// Throws an exception if the version is unknown.
		private static Addresses GetAddresses(LocalProcess process)
		{
			// Create address dictionary
			var versions = new Dictionary<GtaVersion, Addresses>();
			versions.Add(GtaVersion.VC_1_0, new Addresses(0x4BC120, 0x525B20, 0x558A26));
			versions.Add(GtaVersion.VC_1_1, new Addresses(0x4BC140, 0x525B40, 0x558A46));
			versions.Add(GtaVersion.VC_JP,  new Addresses(0x4BBA40, 0x5253D0, 0x558C1C));

			// Try to find the addresses corresponding to the process' GTA version
			Addresses addrs;
			if (!versions.TryGetValue(GtaVersions.Detect(process), out addrs))
			{
				throw new Exception("Unsupported GTA version");
			}
			return addrs;
		}

		// Installs a hook for CObject::ObjectDamage.
		// Whenever ObjectDamage gets called, Tommy's body armor is set to 0 and his health
		// is set to the minimum value where he is still alive. So this call to ObjectDamage
		// will kill Tommy if it removes any amount of health from him.
		//
		// The hook is installed by replacing the first instruction (a fld instruction) of
		// CObject::ObjectDamage with a jmp to the injected hook code.
		private static void PatchObjectDamage(LocalProcess process, Addresses addrs)
		{
			var hook = new byte[]
			{
				0xE8, 0xCC, 0xCC, 0xCC, 0xCC,                               // call 0XXXXXXXXh                     // eax = GetPlayerPed()
				0x3B, 0xC1,                                                 // cmp eax, ecx                        // Is eax == this?
				0x75, 0x14,                                                 // jne 14h                             // If not, then skip the following two instructions
				0xC7, 0x81, 0x54, 0x03, 0x00, 0x00, 0x01, 0x00, 0x80, 0x3F, // mov dword ptr [ecx+354h], 3F800001h // Set health to 1.0000001f
				0xC7, 0x81, 0x58, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov dword ptr [ecx+358h], 00000000h // Set armor to 0.0f
				0xD9, 0x05, 0xCC, 0xCC, 0xCC, 0xCC,                         // fld 0XXXXXXXXh                      // Execute the overwritten fld instruction
				0xE9, 0xCC, 0xCC, 0xCC, 0xCC                                // jmp rel32 0XXXXXXXXh                // Return to ObjectDamage
			};

			// Save the operand of the fld instruction since the instruction will be overwritten
			var fldOperand = process.ReadMemoryInt(new IntPtr(addrs.ObjectDamage + 2));

			// Allocate memory for injected code
			var injectedCodeAddr = process.AllocateMemory(hook.Length).ToInt32();

			// Set relative call address of GetPlayerPed
			BitConverter.GetBytes(addrs.GetPlayerPed - (injectedCodeAddr + 5)).CopyTo(hook, 1);
			// Restore fld instruction
			BitConverter.GetBytes(fldOperand).CopyTo(hook, 31);
			// Set return address
			BitConverter.GetBytes(addrs.ObjectDamage + 6 - (injectedCodeAddr + 40)).CopyTo(hook, 36);

			// Copy code to process
			process.WriteMemory(new IntPtr(injectedCodeAddr), hook);

			// Overwrite the first instruction of ObjectDamage with a jmp to the injected code
			WriteJmpRel32(process, addrs.ObjectDamage, injectedCodeAddr - addrs.ObjectDamage);

			process.FlushInstructionCache();
		}

		// Hides health and armor from the HUD
		private static void PatchDrawHud(LocalProcess process, Addresses addrs)
		{
			// Skip 0x546 bytes
			WriteJmpRel32(process, addrs.DrawHud_SkipFrom, 0x546);
		}

		private static void WriteJmpRel32(LocalProcess process, int addr, int relativeTargetAddr)
		{
			var bytes = new byte[5];
			bytes[0] = 0xE9; // jmp rel32
			BitConverter.GetBytes(relativeTargetAddr - bytes.Length).CopyTo(bytes, 1);
			process.WriteMemory(new IntPtr(addr), bytes);
		}
	}
}
