using System;

namespace Cyclops
{
	public static class Log
	{
        static int startTextLength = 0;
		static int startTime = 0;
		
		public static void WriteBegin(string text)
		{
			string s = String.Format("{0}: {1}", DateTime.Now, text);
			
			Console.Write(s);
			
			startTextLength = s.Length;
			startTime = System.Environment.TickCount;
		}
		
		public static void WriteEnd()
		{
			int elapsed = System.Environment.TickCount - startTime;
			string done = "[done]";
			string doneTime = "";
			ConsoleColor doneColor = ConsoleColor.Green;
			
			if (elapsed < 1000)
			{
				doneTime = String.Format("({0} ms)", elapsed);
			}
			else
			{
				doneTime = String.Format("({0:0.00} s)", elapsed / 1000.0);
			}
			
			Console.Write(Log.RepeatString(" ", Console.WindowWidth - startTextLength - done.Length - 12));
			Console.ForegroundColor = doneColor;
			Console.Write(done);
			Console.ResetColor();
			Console.Write(Log.RepeatString(" ", 11 - doneTime.Length));
			Console.Write(doneTime);
			Console.WriteLine();
		}
		
		public static void WriteError(string errorText)
		{
			string error = "[error]";
			ConsoleColor errorColor = ConsoleColor.Red;
			
			Console.Write(Log.RepeatString(" ", Console.WindowWidth - startTextLength - error.Length - 12));
			Console.ForegroundColor = errorColor;
			Console.WriteLine(error);
			Console.ResetColor();
			Console.WriteLine(errorText);
		}
		
		public static void WriteDebug(string debugText)
		{
			string s = String.Format("{0}: {1}", DateTime.Now, debugText);
			string debug = "[debug]";
			ConsoleColor debugColor = ConsoleColor.Cyan;
			
			Console.Write(s);
			Console.Write(Log.RepeatString(" ", Console.WindowWidth - s.Length - debug.Length - 12));
			Console.ForegroundColor = debugColor;
			Console.WriteLine(debug);
			Console.ResetColor();
		}
		
		public static void Write(string text, params object[] args)
		{
			Console.Write("{0}: {1}", DateTime.Now, String.Format(text, args));
		}
		
		public static void WriteLine(string text, params object[] args)
		{
			Console.WriteLine("{0}: {1}", DateTime.Now, String.Format(text, args));
		}
		
		private static string RepeatString(string toRepeat, int count)
		{
			try
			{
				return string.Join(toRepeat, new string[count + 1]);
			}
			catch
			{
				return toRepeat;
			}
		}
	}
}
