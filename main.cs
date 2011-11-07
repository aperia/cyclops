using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Cyclops
{
	class Program
	{
		static void Main(string[] args)
		{
			// Print distribution information
			Tracer.Println("Cyclops 1.0");
			Tracer.Println("");
			
#if DEBUG
			Tracer.Println("Debugging: On!");
#endif
			
			// Load configuration
			// Remove configuration asm/
			if (Directory.Exists("asm"))
				Directory.Delete("asm", true);
			
			string configFile = "config.cs";
			
			// Read config file in command line argument
			if (args.Length > 0)
			{
				if (!File.Exists(args[0]))
				{
					Tracer.Println("Usage: mono cyclops.exe [config file]");
					Tracer.Println("");
					Pause();
					
					return;
				}
				else
				{
					configFile = args[0];
				}
			}
			
			if (!File.Exists(configFile))
			{
				// TODO: Create new template config file
				configFile = "config.cs";
			}
			
			Tracer.Print("Loading configuration [" + configFile + "]: ");
			
			try
			{
				DynamicCompile compiler = new DynamicCompile();
				Dictionary<string, string> values = (Dictionary<string, string>) compiler.Compile(configFile, null);
				
				Config.Load(values);
			}
			catch (Exception e)
			{
				Tracer.Println("Failed!");
				Tracer.Println("");
				Tracer.Println(e.ToString());
				Pause();
				
				return;
			}
			
			Tracer.Println("Done!");
			
			// Start server
			new Server().Start();
			
			Pause();
			
			return;
		}

		
		private static void Pause()
		{
			Tracer.Println("Press any key to continue... ");
			Console.ReadLine();
		}
	}
}
