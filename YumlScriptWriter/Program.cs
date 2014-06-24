using System;

namespace YumlProjectDiagramScriptWriter
{
	/// <summary>
	/// Outputs a file containing the text required to input into yUML (http://yuml.me/)
	/// to draw a diagram that shows the relationship between all of the projects in the
	/// given root directory or any of its subdirectories.
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Usage: YumlScriptWriter.exe <rootDirectory>");
				Console.WriteLine();
				Console.WriteLine(@"  e.g. YumlScriptWriter.exe C:\repos\");
				Console.WriteLine();
				return;
			}

			string rootDirectoryPath = args[0];

			try
			{
				ScriptWriter scriptWriter = new ScriptWriter(rootDirectoryPath);
				scriptWriter.WriteScript();
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e);
			}
		}
	}
}
