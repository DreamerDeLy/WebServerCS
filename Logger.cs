using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;

namespace WebServer
{
	public enum MessageType
	{
		Debug = 3,
		Info = 2,
		Warning = 1,
		Error = 0
	}

	public struct LogItem
	{
		public LogItem(string tag, string message, MessageType type = MessageType.Debug)
		{
			this.Tag = tag == null ? "null" : tag.Trim();
			this.Message = message;
			this.Time = DateTime.Now;
			this.Type = type;
		}

		public DateTime Time { get; }

		public string Message { get; }

		public MessageType Type;

		public string Tag { get; }

		public override string ToString()
		{
			return $"[{this.Time.ToString("u")}] {this.Type.ToString()} / {Tag}: {this.Message.TrimEnd()}";
		}
	}

    public static class Logger
    {
		private static List<LogItem> log = new List<LogItem>();
		private static ReaderWriterLock locker = new ReaderWriterLock();

		static public void Print(string tag, string message)
		{
			Console.WriteLine(message);
			AddToLog(tag, message, MessageType.Debug);
		}

		static public void Info(string tag, string message)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(message);
			SetConsoleColorsToDefault();

			AddToLog(tag, message, MessageType.Info);
		}

		static public void Error(string tag, string message)
		{
			// Console.ForegroundColor = ConsoleColor.Black;
			// Console.BackgroundColor = ConsoleColor.Red;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			SetConsoleColorsToDefault();

			AddToLog(tag, message, MessageType.Error);
		}

		static public void Warning(string tag, string message)
		{
			// Console.ForegroundColor = ConsoleColor.Black;
			// Console.BackgroundColor = ConsoleColor.Yellow;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(message);
			SetConsoleColorsToDefault();

			AddToLog(tag, message, MessageType.Warning);
		}

		static private void AddToLog(string tag, string message, MessageType type)
		{
			string[] lines = message.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries).ToArray();

			foreach (var line in lines)
			{
				log.Add(new LogItem(tag, line.TrimEnd(), type));	
			}
		}
		
		static private void SetConsoleColorsToDefault()
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.BackgroundColor = ConsoleColor.Black;
		}

		static public void SaveToFile()
		{
			string LogPath = "logs/" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";

			if (log.Count() > 0)
			{
				// Console.WriteLine("Saving log to file...");

				try
				{
					locker.AcquireWriterLock(100); 

					bool newFile = !File.Exists(LogPath);

					using (StreamWriter stream = new StreamWriter(LogPath, append: true))
					{
						if (newFile) stream.WriteLine("Time | Type | Tag | Message");

						foreach (LogItem l in log)
						{
							stream.WriteLine(l.Time.ToString("u") + " | " 
								+ l.Type.ToString().PadRight(7) + " | " 
								+ l.Tag.PadRight(7) + " | "
								+ l.Message
							);
						}
					}

					log.Clear();

					// Console.WriteLine("Saved!");
				}
				catch (Exception e)
				{
					Error("Logger", "Log saving error: " + e.Message);
				}
				finally
				{
					locker.ReleaseWriterLock();
				}
			}
		}
    }
}