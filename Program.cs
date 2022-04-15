using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace WebServer
{
	class Program
	{
		private static Timer LogSaveTimer = new Timer(500);

		static async Task Main(string[] args)
		{
			Logger.Print("Core", "Server started!");

			// Set saving log to file
			LogSaveTimer.Elapsed += SaveLogToFile;
			LogSaveTimer.AutoReset = true;
			LogSaveTimer.Enabled = true;

			PagesManager.Init();

			await Server.Start();
		}

		private static void SaveLogToFile(Object source, ElapsedEventArgs e)
		{
			Logger.SaveToFile();
		}
	}
}
