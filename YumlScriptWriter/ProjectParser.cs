using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace YumlProjectDiagramScriptWriter
{
	/// <summary>
	/// Parses a MSBuild project file (e.g. .csproj) to build a list of other assemblies it references.
	/// For more information on the structure of the XML see http://msdn.microsoft.com/en-us/library/bcxfsh87.aspx
	/// </summary>
	public class ProjectParser
	{
		// The MSBuild project XML namespace.
		static XNamespace MSBUILD_NAMESPACE = "http://schemas.microsoft.com/developer/msbuild/2003";

		// The name of the assembly (or executable) the project is compiled to is stored in the AssemblyName element.
		const string ELEMENT_ASSEMBLY_NAME = "AssemblyName";

		// Every item that is used in a MSBuild project must be specified as a child of an ItemGroup element.
		const string ELEMENT_ITEM_GROUP = "ItemGroup";

		// References to other assemblies are stored in Reference elements in an ItemGroup.
		const string ELEMENT_REFERENCE = "Reference";

		// References to other projects in the same solution are stored in ProjectReference elements.
		const string ELEMENT_PROJECT_REFERENCE = "ProjectReference";

		// The name of the assembly for project references is stored in a child Name element.
		const string ELEMENT_NAME = "Name";

		// The name of the attribute that the referenced file or wildcard is stored in.
		const string ATTRIBUTE_INCLUDE = "Include";

		// The following constants are used to filter out assemblies we don't want to draw in the yUML diagram.
		// Assemblies starting with these prefixes are ignored.
		const string ASSEMBLY_PREFIX_SYSTEM = "System";
		const string ASSEMBLY_PREFIX_MICROSOFT = "Microsoft";

		public string GetProjectAssemblyName(XElement projectElement)
		{
			var assemblyNameElements = from element in projectElement.Descendants(MSBUILD_NAMESPACE + ELEMENT_ASSEMBLY_NAME)
									   select element;
			if (assemblyNameElements.Count() != 1)
			{
				// Assumption: Only one AssemblyName element per Project. This may not be a very safe assumption.
				string msg = String.Format("Project is missing the '{0}' element.", ELEMENT_ASSEMBLY_NAME);
				throw new Exception(msg);
			}

			return assemblyNameElements.Single().Value;
		}

		/// <summary>
		/// Parses the given project file to build a list of strings that contain all the project's references.
		/// </summary>
		/// <param name="projectElement">The project element from a MSBuild project.</param>
		/// <returns>A list of strings that contain all the project's references.</returns>
		public List<string> GetProjectReferences(XElement projectElement)
		{
			List<string> projectReferences = new List<string>();

			AddReferences(projectElement, projectReferences);

			return projectReferences;
		}

		/// <summary>
		/// This method assumes that all references are children of an ItemGroup element.
		/// It finds all the project's item groups and then adds the references from each item group to the list of references.
		/// </summary>
		/// <param name="projectElement">The project element from a MSBuild project.</param>
		/// <param name="projectReferences">The list to add any references into.</param>
		private void AddReferences(XElement projectElement, List<string> projectReferences)
		{
			var itemGroupElements = from element in projectElement.Descendants(MSBUILD_NAMESPACE + ELEMENT_ITEM_GROUP)
									select element;

			foreach (XElement itemGroupElement in itemGroupElements)
			{
				AddReferencesFromItemGroup(itemGroupElement, projectReferences);
			}
		}

		/// <summary>
		/// Adds all of the references in the item group into the list.
		/// </summary>
		/// <param name="itemGroupElement">An ItemGroup element from a MSBuild project.</param>
		/// <param name="projectReferences">The list to add any references into.</param>
		private void AddReferencesFromItemGroup(XElement itemGroupElement, List<string> projectReferences)
		{
			var itemElements = from element in itemGroupElement.Elements()
							   select element;

			foreach (XElement itemElement in itemElements)
			{
				if (itemElement.Name != MSBUILD_NAMESPACE + ELEMENT_REFERENCE &&
					itemElement.Name != MSBUILD_NAMESPACE + ELEMENT_PROJECT_REFERENCE)
				{
					// Ignore elements containing data other than references.
					continue;
				}

				XAttribute includeAttribute = itemElement.Attribute(ATTRIBUTE_INCLUDE);
				if (includeAttribute == null)
				{
					// Include is a required attribute on an item element. See http://msdn.microsoft.com/en-us/library/ms164283.aspx
					string msg = String.Format("The '{0}' attribute was missing from the '{1}' element.", ATTRIBUTE_INCLUDE, itemElement.Name);
					throw new Exception(msg);
				}

				string referenceName;
				if (itemElement.Name == MSBUILD_NAMESPACE + ELEMENT_PROJECT_REFERENCE)
				{
					// For project references the value in the Include attribute is a relative file path. We only want the project name.
					XElement nameElement = itemElement.Element(MSBUILD_NAMESPACE + ELEMENT_NAME);
					if (nameElement == null)
					{
						string msg = String.Format("The '{0}' element was missing from a '{1}' element.", ELEMENT_NAME, ELEMENT_PROJECT_REFERENCE);
						throw new Exception(msg);
					}

					referenceName = nameElement.Value;
				}
				else
				{
					referenceName = includeAttribute.Value;
				}

				// Ignore system or Microsoft references. In the future these could optionally included.
				if (referenceName.StartsWith(ASSEMBLY_PREFIX_SYSTEM) || referenceName.StartsWith(ASSEMBLY_PREFIX_MICROSOFT))
				{
					continue;
				}

				// Trim reference names to the first comma. E.g. trim "ProjectA, 1.0.0.0" to just "ProjectA"
				if (referenceName.Contains(','))
				{
					referenceName = referenceName.Substring(0, referenceName.IndexOf(','));
				}

				projectReferences.Add(referenceName);
			}
		}
	}
}
