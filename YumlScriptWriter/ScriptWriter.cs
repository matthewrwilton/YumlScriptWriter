using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace YumlProjectDiagramScriptWriter
{
	class ScriptWriter
	{
		public ScriptWriter(string rootDirectoryPath)
		{
			RootDirectoryPath = rootDirectoryPath;
			ProjectParser = new ProjectParser();
		}

		private string RootDirectoryPath
		{
			get;
			set;
		}

		private ProjectParser ProjectParser
		{
			get;
			set;
		}

		public void WriteScript()
		{
			string fileName = GetFileName();

			using (FileStream fileStream = File.OpenWrite(fileName))
			using (StreamWriter streamWriter = new StreamWriter(fileStream))
			{
				DirectoryInfo rootDirectory = new DirectoryInfo(RootDirectoryPath);
				FindAndWriteScriptsForProjects(streamWriter, rootDirectory);
			}
		}

		private string GetFileName()
		{
			string fileName = "yumlScript.txt";
			int fileAdjustment = 1;
			while (File.Exists(fileName))
			{
				fileName = String.Format("yumlScript{0}.txt", fileAdjustment++);
			}

			return fileName;
		}

		private void FindAndWriteScriptsForProjects(StreamWriter streamWriter, DirectoryInfo directory)
		{
			// Assumption: any files with an extension ending with proj are a MSBuild project file.
			FileInfo[] projectFiles = directory.GetFiles("*.*proj");
			foreach (FileInfo projectFile in projectFiles)
			{
				WriteScriptForProject(streamWriter, projectFile.FullName);
			}

			foreach (DirectoryInfo subDirectory in directory.GetDirectories())
			{
				FindAndWriteScriptsForProjects(streamWriter, subDirectory);
			}
		}

		private void WriteScriptForProject(StreamWriter streamWriter, string projectFilePath)
		{
			XElement projectElement = XElement.Load(projectFilePath);

			string projectName = ProjectParser.GetProjectAssemblyName(projectElement);
			List<string> projectReferences = ProjectParser.GetProjectReferences(projectElement);

			streamWriter.Write("// ");
			streamWriter.WriteLine(projectName);
			foreach (string projectReference in projectReferences)
			{
				streamWriter.WriteLine(String.Format("[{0}]->[{1}]", projectName, projectReference));
			}
		}
	}
}
