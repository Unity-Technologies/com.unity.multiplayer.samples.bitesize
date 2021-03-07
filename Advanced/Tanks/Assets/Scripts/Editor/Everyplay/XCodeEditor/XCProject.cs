using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EveryplayEditorTools.XCodeEditor
{
	public partial class XCProject : System.IDisposable
	{
		private PBXDictionary _datastore;
		public PBXDictionary _objects;
		private PBXDictionary _configurations;
		private PBXGroup _rootGroup;
		private string _defaultConfigurationName;
		private string _rootObjectKey;

		public string projectRootPath { get; private set; }

		private FileInfo projectFileInfo;

		public string filePath { get; private set; }

		private string sourcePathRoot;
		private bool modified = false;

		#region Data

		private PBXDictionary<PBXBuildFile> _buildFiles;
		private PBXDictionary<PBXGroup> _groups;
		private PBXDictionary<PBXFileReference> _fileReferences;
		private PBXDictionary<PBXNativeTarget> _nativeTargets;
		private PBXDictionary<PBXTargetDependency> _targetDependencies;
		private PBXDictionary<PBXContainerItemProxy> _containerItemProxies;
		private PBXDictionary<PBXReferenceProxy> _referenceProxies;
		private PBXDictionary<PBXVariantGroup> _variantGroups;
		private PBXDictionary<PBXFrameworksBuildPhase> _frameworkBuildPhases;
		private PBXDictionary<PBXResourcesBuildPhase> _resourcesBuildPhases;
		private PBXDictionary<PBXShellScriptBuildPhase> _shellScriptBuildPhases;
		private PBXDictionary<PBXSourcesBuildPhase> _sourcesBuildPhases;
		private PBXDictionary<PBXCopyFilesBuildPhase> _copyBuildPhases;
		private PBXDictionary<XCBuildConfiguration> _buildConfigurations;
		private PBXDictionary<XCConfigurationList> _configurationLists;
		private PBXProject _project;

		#endregion

		#region Constructor

		public XCProject()
		{
		}

		public XCProject(string filePath)
			: this()
		{
			if (!System.IO.Directory.Exists(filePath))
			{
				Debug.LogWarning("Path does not exists.");
				return;
			}

			if (filePath.EndsWith(".xcodeproj"))
			{
				this.projectRootPath = Path.GetDirectoryName(filePath);
				this.filePath = filePath;
			}
			else
			{
				string[] projects = System.IO.Directory.GetDirectories(filePath, "*.xcodeproj");
				if (projects.Length == 0)
				{
					Debug.LogWarning("Error: missing xcodeproj file");
					return;
				}

				this.projectRootPath = filePath;
				this.filePath = projects[0];
			}

			projectFileInfo = new FileInfo(Path.Combine(this.filePath, "project.pbxproj"));
			StreamReader sr = projectFileInfo.OpenText();
			string contents = sr.ReadToEnd();
			sr.Close();

			PBXParser parser = new PBXParser();
			_datastore = parser.Decode(contents);
			if (_datastore == null)
			{
				throw new System.Exception("Project file not found at file path " + filePath);
			}

			if (!_datastore.ContainsKey("objects"))
			{
				Debug.Log("Errore " + _datastore.Count);
				return;
			}

			_objects = (PBXDictionary)_datastore["objects"];
			modified = false;

			_rootObjectKey = (string)_datastore["rootObject"];
			if (!string.IsNullOrEmpty(_rootObjectKey))
			{
				_project = new PBXProject(_rootObjectKey, (PBXDictionary)_objects[_rootObjectKey]);
				_rootGroup = new PBXGroup(_rootObjectKey, (PBXDictionary)_objects[_project.mainGroupID]);
			}
			else
			{
				Debug.LogWarning("Error: project has no root object");
				_project = null;
				_rootGroup = null;
			}
		}

		#endregion

		#region Properties

		public PBXProject project
		{
			get
			{
				return _project;
			}
		}

		public PBXGroup rootGroup
		{
			get
			{
				return _rootGroup;
			}
		}

		public PBXDictionary<PBXBuildFile> buildFiles
		{
			get
			{
				if (_buildFiles == null)
				{
					_buildFiles = new PBXDictionary<PBXBuildFile>(_objects);
				}
				return _buildFiles;
			}
		}

		public PBXDictionary<PBXGroup> groups
		{
			get
			{
				if (_groups == null)
				{
					_groups = new PBXDictionary<PBXGroup>(_objects);
				}
				return _groups;
			}
		}

		public PBXDictionary<PBXFileReference> fileReferences
		{
			get
			{
				if (_fileReferences == null)
				{
					_fileReferences = new PBXDictionary<PBXFileReference>(_objects);
				}
				return _fileReferences;
			}
		}

		public PBXDictionary<PBXTargetDependency> targetDependencies
		{
			get
			{
				if (_targetDependencies == null)
				{
					_targetDependencies = new PBXDictionary<PBXTargetDependency>(_objects);
				}
				return _targetDependencies;
			}
		}

		public PBXDictionary<PBXContainerItemProxy> containerItemProxies
		{
			get
			{
				if (_containerItemProxies == null)
				{
					_containerItemProxies = new PBXDictionary<PBXContainerItemProxy>(_objects);
				}
				return _containerItemProxies;
			}
		}

		public PBXDictionary<PBXReferenceProxy> referenceProxies
		{
			get
			{
				if (_referenceProxies == null)
				{
					_referenceProxies = new PBXDictionary<PBXReferenceProxy>(_objects);
				}
				return _referenceProxies;
			}
		}

		public PBXDictionary<PBXVariantGroup> variantGroups
		{
			get
			{
				if (_variantGroups == null)
				{
					_variantGroups = new PBXDictionary<PBXVariantGroup>(_objects);
				}
				return _variantGroups;
			}
		}

		public PBXDictionary<PBXNativeTarget> nativeTargets
		{
			get
			{
				if (_nativeTargets == null)
				{
					_nativeTargets = new PBXDictionary<PBXNativeTarget>(_objects);
				}
				return _nativeTargets;
			}
		}

		public PBXDictionary<XCBuildConfiguration> buildConfigurations
		{
			get
			{
				if (_buildConfigurations == null)
				{
					_buildConfigurations = new PBXDictionary<XCBuildConfiguration>(_objects);
				}
				return _buildConfigurations;
			}
		}

		public PBXDictionary<XCConfigurationList> configurationLists
		{
			get
			{
				if (_configurationLists == null)
				{
					_configurationLists = new PBXDictionary<XCConfigurationList>(_objects);
				}
				return _configurationLists;
			}
		}

		public PBXDictionary<PBXFrameworksBuildPhase> frameworkBuildPhases
		{
			get
			{
				if (_frameworkBuildPhases == null)
				{
					_frameworkBuildPhases = new PBXDictionary<PBXFrameworksBuildPhase>(_objects);
				}
				return _frameworkBuildPhases;
			}
		}

		public PBXDictionary<PBXResourcesBuildPhase> resourcesBuildPhases
		{
			get
			{
				if (_resourcesBuildPhases == null)
				{
					_resourcesBuildPhases = new PBXDictionary<PBXResourcesBuildPhase>(_objects);
				}
				return _resourcesBuildPhases;
			}
		}

		public PBXDictionary<PBXShellScriptBuildPhase> shellScriptBuildPhases
		{
			get
			{
				if (_shellScriptBuildPhases == null)
				{
					_shellScriptBuildPhases = new PBXDictionary<PBXShellScriptBuildPhase>(_objects);
				}
				return _shellScriptBuildPhases;
			}
		}

		public PBXDictionary<PBXSourcesBuildPhase> sourcesBuildPhases
		{
			get
			{
				if (_sourcesBuildPhases == null)
				{
					_sourcesBuildPhases = new PBXDictionary<PBXSourcesBuildPhase>(_objects);
				}
				return _sourcesBuildPhases;
			}
		}

		public PBXDictionary<PBXCopyFilesBuildPhase> copyBuildPhases
		{
			get
			{
				if (_copyBuildPhases == null)
				{
					_copyBuildPhases = new PBXDictionary<PBXCopyFilesBuildPhase>(_objects);
				}
				return _copyBuildPhases;
			}
		}

		#endregion

		#region PBXMOD

		public bool AddOtherCFlags(string flag)
		{
			return AddOtherCFlags(new PBXList(flag));
		}

		public bool AddOtherCFlags(PBXList flags)
		{
			foreach (KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations)
			{
				buildConfig.Value.AddOtherCFlags(flags);
			}
			modified = true;
			return modified;
		}

		public bool AddLibrarySearchPaths(string path)
		{
			return AddLibrarySearchPaths(new PBXList(path));
		}

		public bool AddLibrarySearchPaths(PBXList paths)
		{
			foreach (KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations)
			{
				buildConfig.Value.AddLibrarySearchPaths(paths, false);
			}
			modified = true;
			return modified;
		}

		public bool AddHeaderSearchPaths(string path)
		{
			return AddHeaderSearchPaths(new PBXList(path));
		}

		public bool AddHeaderSearchPaths(PBXList paths)
		{
			foreach (KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations)
			{
				buildConfig.Value.AddHeaderSearchPaths(paths, false);
			}
			modified = true;
			return modified;
		}

		public bool AddFrameworkSearchPaths(string path)
		{
			return AddFrameworkSearchPaths(new PBXList(path));
		}

		public bool AddFrameworkSearchPaths(PBXList paths)
		{
			foreach (KeyValuePair<string, XCBuildConfiguration> buildConfig in buildConfigurations)
			{
				buildConfig.Value.AddFrameworkSearchPaths(paths, false);
			}
			modified = true;
			return modified;
		}

		public object GetObject(string guid)
		{
			return _objects[guid];
		}

		public PBXDictionary AddFile(string filePath, PBXGroup parent = null, string tree = "SOURCE_ROOT", bool createBuildFiles = true, bool weak = false)
		{
			PBXDictionary results = new PBXDictionary();
			string absPath = string.Empty;

			if (Path.IsPathRooted(filePath))
			{
				absPath = filePath;
			}
			else if (tree.CompareTo("SDKROOT") != 0)
			{
				absPath = Path.Combine(Application.dataPath, filePath);
			}

			if (tree.CompareTo("SOURCE_ROOT") == 0)
			{
				System.Uri fileURI = new System.Uri(absPath);
				System.Uri rootURI = new System.Uri((projectRootPath + "/."));
				filePath = rootURI.MakeRelativeUri(fileURI).ToString();
			}

			if (parent == null)
			{
				parent = _rootGroup;
			}

			// TODO: Aggiungere controllo se file già presente
			PBXFileReference fileReference = GetFile(System.IO.Path.GetFileName(filePath));
			if (fileReference != null)
			{
				return null;
			}

			fileReference = new PBXFileReference(filePath, (TreeEnum)System.Enum.Parse(typeof(TreeEnum), tree));
			parent.AddChild(fileReference);
			fileReferences.Add(fileReference);
			results.Add(fileReference.guid, fileReference);

			// Find test target
			PBXNativeTarget nativeTestTarget = null;
			foreach (KeyValuePair<string, PBXNativeTarget> currentObject in nativeTargets)
			{
				PBXNativeTarget nativeTargetObj = currentObject.Value;

				if (nativeTargetObj != null && nativeTargetObj.data.ContainsKey("name"))
				{
					string name = (string)nativeTargetObj.data["name"];

					if (name != null && name.Contains("Unity-iPhone Tests"))
					{
						nativeTestTarget = nativeTargetObj;
						break;
					}
				}
			}

			//Create a build file for reference
			if (!string.IsNullOrEmpty(fileReference.buildPhase) && createBuildFiles)
			{
				PBXBuildFile buildFile;
				switch (fileReference.buildPhase)
				{
					case "PBXFrameworksBuildPhase":
						foreach (KeyValuePair<string, PBXFrameworksBuildPhase> currentObject in frameworkBuildPhases)
						{
							if (nativeTestTarget == null || !nativeTestTarget.HasBuildphase(currentObject.Key.Split(' ')[0]))
							{
								buildFile = new PBXBuildFile(fileReference, weak);
								buildFiles.Add(buildFile);
								currentObject.Value.AddBuildFile(buildFile);
							}
						}
						if (!string.IsNullOrEmpty(absPath) && (tree.CompareTo("SOURCE_ROOT") == 0) && File.Exists(absPath))
						{
							string libraryPath = Path.Combine("$(SRCROOT)", Path.GetDirectoryName(filePath));
							this.AddLibrarySearchPaths(new PBXList(libraryPath));
						}
						break;
					case "PBXResourcesBuildPhase":
						foreach (KeyValuePair<string, PBXResourcesBuildPhase> currentObject in resourcesBuildPhases)
						{
							if (nativeTestTarget == null || !nativeTestTarget.HasBuildphase(currentObject.Key.Split(' ')[0]))
							{
								buildFile = new PBXBuildFile(fileReference, weak);
								buildFiles.Add(buildFile);
								currentObject.Value.AddBuildFile(buildFile);
							}
						}
						break;
					case "PBXShellScriptBuildPhase":
						foreach (KeyValuePair<string, PBXShellScriptBuildPhase> currentObject in shellScriptBuildPhases)
						{
							if (nativeTestTarget == null || !nativeTestTarget.HasBuildphase(currentObject.Key.Split(' ')[0]))
							{
								buildFile = new PBXBuildFile(fileReference, weak);
								buildFiles.Add(buildFile);
								currentObject.Value.AddBuildFile(buildFile);
							}
						}
						break;
					case "PBXSourcesBuildPhase":
						foreach (KeyValuePair<string, PBXSourcesBuildPhase> currentObject in sourcesBuildPhases)
						{
							if (nativeTestTarget == null || !nativeTestTarget.HasBuildphase(currentObject.Key.Split(' ')[0]))
							{
								buildFile = new PBXBuildFile(fileReference, weak);
								buildFiles.Add(buildFile);
								currentObject.Value.AddBuildFile(buildFile);
							}
						}
						break;
					case "PBXCopyFilesBuildPhase":
						foreach (KeyValuePair<string, PBXCopyFilesBuildPhase> currentObject in copyBuildPhases)
						{
							if (nativeTestTarget == null || !nativeTestTarget.HasBuildphase(currentObject.Key.Split(' ')[0]))
							{
								buildFile = new PBXBuildFile(fileReference, weak);
								buildFiles.Add(buildFile);
								currentObject.Value.AddBuildFile(buildFile);
							}
						}
						break;
					case null:
						Debug.LogWarning("fase non supportata null");
						break;
					default:
						Debug.LogWarning("fase non supportata def");
						return null;
				}
			}

			return results;
		}

		public bool AddFolder(string folderPath, PBXGroup parent = null, string[] exclude = null, bool recursive = true, bool createBuildFile = true)
		{
			if (!Directory.Exists(folderPath))
			{
				return false;
			}
			DirectoryInfo sourceDirectoryInfo = new DirectoryInfo(folderPath);

			if (exclude == null)
			{
				exclude = new string[] { };
			}


			if (parent == null)
			{
				parent = rootGroup;
			}

			// Create group
			PBXGroup newGroup = GetGroup(sourceDirectoryInfo.Name, null /*relative path*/, parent);

			foreach (string directory in Directory.GetDirectories(folderPath))
			{
				Debug.Log("DIR: " + directory);
				if (directory.EndsWith(".bundle"))
				{
					// Treath it like a file and copy even if not recursive
					Debug.LogWarning("This is a special folder: " + directory);
					AddFile(directory, newGroup, "SOURCE_ROOT", createBuildFile);
					Debug.Log("fatto");
					continue;
				}

				if (recursive)
				{
					Debug.Log("recursive");
					AddFolder(directory, newGroup, exclude, recursive, createBuildFile);
				}
			}

			// Adding files.
			string regexExclude = string.Format(@"{0}", string.Join("|", exclude));
			foreach (string file in Directory.GetFiles(folderPath))
			{
				if (Regex.IsMatch(file, regexExclude))
				{
					continue;
				}
				AddFile(file, newGroup, "SOURCE_ROOT", createBuildFile);
			}


			modified = true;
			return modified;
		}

		#endregion

		#region Getters

		public PBXFileReference GetFile(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			foreach (KeyValuePair<string, PBXFileReference> current in fileReferences)
			{
				if (!string.IsNullOrEmpty(current.Value.name) && current.Value.name.CompareTo(name) == 0)
				{
					return current.Value;
				}
			}

			return null;
		}

		public PBXGroup GetGroup(string name, string path = null, PBXGroup parent = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			if (parent == null)
			{
				parent = rootGroup;
			}

			foreach (KeyValuePair<string, PBXGroup> current in groups)
			{
				if (string.IsNullOrEmpty(current.Value.name))
				{
					if (current.Value.path.CompareTo(name) == 0)
					{
						return current.Value;
					}
				}
				else if (current.Value.name.CompareTo(name) == 0)
				{
					return current.Value;
				}
			}

			PBXGroup result = new PBXGroup(name, path);
			groups.Add(result);
			parent.AddChild(result);

			modified = true;
			return result;
		}

		#endregion

		#region Mods

		public void ApplyMod(string rootPath, string pbxmod)
		{
			XCMod mod = new XCMod(rootPath, pbxmod);
			ApplyMod(mod);
		}

		internal static string AddXcodeQuotes(string path)
		{
			return "\"\\\"" + path + "\\\"\"";
		}

		internal static string MacPath(string path)
		{
			return path.Replace(@"\", "/");
		}

		public void ApplyMod(XCMod mod)
		{
			PBXGroup modGroup = this.GetGroup(mod.group);

			foreach (XCModFile libRef in mod.libs)
			{
				string completeLibPath;
				if (libRef.sourceTree.Equals("SDKROOT"))
				{
					completeLibPath = System.IO.Path.Combine("usr/lib", libRef.filePath);
				}
				else
				{
					completeLibPath = System.IO.Path.Combine(mod.path, libRef.filePath);
				}

				this.AddFile(MacPath(completeLibPath), modGroup, libRef.sourceTree, true, libRef.isWeak);
			}

			PBXGroup frameworkGroup = this.GetGroup("Frameworks");

			if (mod.frameworks != null)
			{
				foreach (string framework in mod.frameworks)
				{
					string[] filename = framework.Split(':');
					bool isWeak = (filename.Length > 1) ? true : false;
					string completePath = System.IO.Path.Combine("System/Library/Frameworks", filename[0]);
					this.AddFile(MacPath(completePath), frameworkGroup, "SDKROOT", true, isWeak);
				}
			}

			if (mod.files != null)
			{
				foreach (string filePath in mod.files)
				{
					string absoluteFilePath = System.IO.Path.Combine(mod.path, filePath);
					this.AddFile(MacPath(absoluteFilePath), modGroup);
				}
			}

			if (mod.folders != null)
			{
				foreach (string folderPath in mod.folders)
				{
					string absoluteFolderPath = AddXcodeQuotes(System.IO.Path.Combine(mod.path, folderPath));
					this.AddFolder(MacPath(absoluteFolderPath), modGroup, (string[])mod.excludes.ToArray());
				}
			}

			if (mod.headerpaths != null)
			{
				foreach (string headerpath in mod.headerpaths)
				{
					string absoluteHeaderPath = System.IO.Path.Combine(mod.path, headerpath);
					this.AddHeaderSearchPaths(AddXcodeQuotes(MacPath(headerpath.Contains("$(SRCROOT)") ? headerpath : absoluteHeaderPath)));
				}
			}

			if (mod.librarysearchpaths != null)
			{
				foreach (string librarypath in mod.librarysearchpaths)
				{
					string absolutePath = System.IO.Path.Combine(mod.path, librarypath);
					this.AddLibrarySearchPaths(AddXcodeQuotes(MacPath(librarypath.Contains("$(SRCROOT)") ? librarypath : absolutePath)));
				}
			}

			if (mod.frameworksearchpath != null)
			{
				foreach (string frameworksearchpath in mod.frameworksearchpath)
				{
					string absoluteHeaderPath = System.IO.Path.Combine(mod.path, frameworksearchpath);
					this.AddFrameworkSearchPaths(AddXcodeQuotes(MacPath(frameworksearchpath.Contains("$(SRCROOT)") ? frameworksearchpath : absoluteHeaderPath)));
				}
			}

			this.Consolidate();
		}

		#endregion

		#region Savings

		public void Consolidate()
		{
			PBXDictionary consolidated = new PBXDictionary();
			consolidated.internalNewlines = true;
			consolidated.Append<PBXBuildFile>(this.buildFiles);
			consolidated.Append<PBXContainerItemProxy>(this.containerItemProxies);
			consolidated.Append<PBXCopyFilesBuildPhase>(this.copyBuildPhases);
			consolidated.Append<PBXFileReference>(this.fileReferences);
			consolidated.Append<PBXFrameworksBuildPhase>(this.frameworkBuildPhases);
			consolidated.Append<PBXGroup>(this.groups);
			consolidated.Append<PBXNativeTarget>(this.nativeTargets);
			consolidated.Append<PBXReferenceProxy>(this.referenceProxies);
			consolidated.Add(project.guid, project.data);
			consolidated.Append<PBXResourcesBuildPhase>(this.resourcesBuildPhases);
			consolidated.Append<PBXShellScriptBuildPhase>(this.shellScriptBuildPhases);
			consolidated.Append<PBXSourcesBuildPhase>(this.sourcesBuildPhases);
			consolidated.Append<PBXTargetDependency>(this.targetDependencies);
			consolidated.Append<PBXVariantGroup>(this.variantGroups);
			consolidated.Append<XCBuildConfiguration>(this.buildConfigurations);
			consolidated.Append<XCConfigurationList>(this.configurationLists);

			_objects = consolidated;
			consolidated = null;
		}

		public void Backup()
		{
			string backupPath = Path.Combine(this.filePath, "project.backup.pbxproj");

			// Delete previous backup file
			if (File.Exists(backupPath))
			{
				File.Delete(backupPath);
			}

			// Backup original pbxproj file first
			File.Copy(System.IO.Path.Combine(this.filePath, "project.pbxproj"), backupPath);
		}

		/// <summary>
		/// Saves a project after editing.
		/// </summary>
		public void Save()
		{
			PBXDictionary result = new PBXDictionary();
			result.internalNewlines = true;
			result.Add("archiveVersion", 1);
			result.Add("classes", new PBXDictionary());
			result.Add("objectVersion", 46);

			Consolidate();
			result.Add("objects", _objects);

			result.Add("rootObject", _rootObjectKey);

			Backup();

			PBXParser parser = new PBXParser();
			StreamWriter saveFile = File.CreateText(System.IO.Path.Combine(this.filePath, "project.pbxproj"));
			saveFile.Write(parser.Encode(result));
			saveFile.Close();
		}

		/**
    * Raw project data.
    */
		public Dictionary<string, object> objects
		{
			get
			{
				return null;
			}
		}

		#endregion

		public void Dispose()
		{
		}
	}
}
