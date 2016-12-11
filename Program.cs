using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ohko_gtavc
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Show process selection
			Process process;
			using (var form = new ProcessSelectionForm("gta-vc.exe"))
			{
				Application.Run(form);
				process = form.SelectedProcess;
			}
			if (process == null) return;

			// Create LocalProcess from Process object
			LocalProcess localProcess;
			using (process)
			{
				localProcess = new LocalProcess(process.Id);
			}

			// Enable OHKO
			using (localProcess)
			{
				try
				{
					OhkoVC.Enable(localProcess);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
	}
}
