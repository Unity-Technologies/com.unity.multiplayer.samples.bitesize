#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_OR_LATER
#endif

using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public static class EveryplayPostprocessor
{
	private static string[] frameworkDependencies = new string[]
	{
#if UNITY_IPHONE || UNITY_TVOS
		"Security",
		"StoreKit",
#endif
#if UNITY_IPHONE
		"AssetsLibrary",
		"MessageUI",
#endif
	};

	private static string[] weakFrameworkDependencies = new string[]
	{
#if UNITY_IPHONE || UNITY_TVOS
		"CoreImage",
#endif
#if UNITY_IPHONE
		"Social",
		"Twitter",
		"Accounts",
#endif
	};

	[PostProcessBuild(1080)]
	public static void OnPostProcessBuild(BuildTarget target, string path)
	{
		if (path.StartsWith("./"))   // Fix two erroneous path cases on Unity 5.4f03
		{
			path = Path.Combine(Application.dataPath.Replace("Assets", ""), path.Replace("./", ""));
		}
		else if (path.Contains("./"))
		{
			path = path.Replace("./", "");
		}

		EveryplaySettings settings = EveryplaySettingsEditor.LoadEveryplaySettings();

		if (settings != null)
		{
			if (settings.IsBuildTargetEnabled)
			{
				if (settings.IsValid)
				{
					if (target == kBuildTarget_iOS)
					{
						PostProcessBuild_iOS(path, settings.clientId);
					}
					else if (target == kBuildTarget_tvOS)
					{
						PostProcessBuild_iOS(path, settings.clientId);
					}
					else if (target == BuildTarget.Android)
					{
						PostProcessBuild_Android(path, settings.clientId);
					}
				}
				else
				{
					Debug.LogError("Everyplay will be disabled because client id, client secret or redirect URI was not valid.");
				}
			}
		}

		ValidateEveryplayState(settings);
	}

	[PostProcessBuild(-10)]
	public static void OnPostProcessBuildEarly(BuildTarget target, string path)
	{
		if (path.StartsWith("./") || !path.StartsWith("/"))
		{
			path = Path.Combine(Application.dataPath.Replace("Assets", ""), path.Replace("./", ""));
		}
		else if (path.Contains("./"))
		{
			path = path.Replace("./", "");
		}

		EveryplayLegacyCleanup.Clean(false);

		if (target == kBuildTarget_iOS || target == BuildTarget.Android)
		{
			ValidateAndUpdateFacebook();

			if (target == kBuildTarget_iOS)
			{
				FixUnityPlistAppendBug(path);
			}
		}
	}

	#if !UNITY_5_OR_LATER
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        EveryplaySettings settings = EveryplaySettingsEditor.LoadEveryplaySettings();

        if (settings != null)
        {
            if (settings.earlyInitializerEnabled && settings.IsValid && settings.IsEnabled)
            {
                GameObject everyplayEarlyInitializer = new GameObject("EveryplayEarlyInitializer");
                everyplayEarlyInitializer.AddComponent<EveryplayEarlyInitializer>();
            }
        }
    }

    #endif

	private static void PostProcessBuild_iOS(string path, string clientId)
	{
		// Disable PluginImporter on iOS and use xCode editor instead
		//#if !UNITY_5_OR_LATER
		bool osxEditor = (Application.platform == RuntimePlatform.OSXEditor);
		CreateModFile(path, !osxEditor || !EditorUserBuildSettings.symlinkLibraries);
		CreateEveryplayConfig(path);
		ProcessXCodeProject(path);
		//#endif
		ProcessInfoPList(path, clientId);
	}

	private static void PostProcessBuild_Android(string path, string clientId)
	{
	}

	public static void SetPluginImportEnabled(BuildTarget buildTarget, bool enabled)
	{
		#if UNITY_5_OR_LATER
		try
		{
			PluginImporter[] pluginImporters = PluginImporter.GetAllImporters();
#if UNITY_TVOS
            bool hasPlatform_tvOS = true;
#else
			bool hasPlatform_tvOS = false;
#endif

			foreach (PluginImporter pluginImporter in pluginImporters)
			{
				bool pluginImporter_iOS = pluginImporter.assetPath.Contains("Plugins/Everyplay/iOS");
				bool pluginImporter_tvOS = pluginImporter.assetPath.Contains("Plugins/Everyplay/tvOS");
				bool pluginImporter_Android = pluginImporter.assetPath.Contains("Plugins/Android/everyplay");

				if (pluginImporter_iOS ||
				    pluginImporter_tvOS ||
				    pluginImporter_Android)
				{
					pluginImporter.SetCompatibleWithAnyPlatform(false);
					pluginImporter.SetCompatibleWithEditor(false);

					if (((buildTarget == kBuildTarget_iOS) && pluginImporter_iOS) ||
					    ((buildTarget == kBuildTarget_tvOS && hasPlatform_tvOS) && pluginImporter_tvOS))
					{
						pluginImporter.SetCompatibleWithPlatform(buildTarget, enabled);

						if (enabled)
						{
							string dependencies = "";

							foreach (string framework in frameworkDependencies)
							{
								dependencies += framework + ";";
							}
							foreach (string framework in weakFrameworkDependencies)
							{
								dependencies += framework + ";";
							}

							// Is there a way to make some dependencies weak in PluginImporter?
							pluginImporter.SetPlatformData(buildTarget, "FrameworkDependencies", dependencies);
						}
					}
					else if ((buildTarget == BuildTarget.Android) && pluginImporter_Android)
					{
						pluginImporter.SetCompatibleWithPlatform(buildTarget, enabled);
					}
				}
			}
		}
		catch (Exception e)
		{
			Debug.Log("Changing plugin import settings failed: " + e);
		}
		#endif
	}

	private static void CreateEveryplayConfig(string path)
	{
		try
		{
			string configFile = System.IO.Path.Combine(path, PathWithPlatformDirSeparators("Everyplay/EveryplayConfig.h"));

			if (File.Exists(configFile))
			{
				File.Delete(configFile);
			}

			string version = GetUnityVersion();

			using (StreamWriter streamWriter = File.CreateText(configFile))
			{
				streamWriter.WriteLine("// Autogenerated by EveryplayPostprocess.cs");
				streamWriter.WriteLine("#define EVERYPLAY_UNITY_VERSION " + version);
			}
		}
		catch (Exception e)
		{
			Debug.Log("Creating EveryplayConfig.h failed: " + e);
		}
	}

	private static void ProcessXCodeProject(string path)
	{
		EveryplayEditorTools.XCodeEditor.XCProject project = new EveryplayEditorTools.XCodeEditor.XCProject(path);

		string modsPath = System.IO.Path.Combine(path, "Everyplay");
		string[] files = System.IO.Directory.GetFiles(modsPath, "*.projmods", System.IO.SearchOption.AllDirectories);

		foreach (string file in files)
		{
			project.ApplyMod(Application.dataPath, file);
		}

		project.Save();
	}

	private static void ProcessInfoPList(string path, string clientId)
	{
		try
		{
			string file = System.IO.Path.Combine(path, "Info.plist");

			if (!File.Exists(file))
			{
				return;
			}

			XmlDocument xmlDocument = new XmlDocument();

			xmlDocument.Load(file);

			XmlNode dict = xmlDocument.SelectSingleNode("plist/dict");

			if (dict != null)
			{
				// Add camera usage description for iOS 10
				PListItem cameraUsageDescription = GetPlistItem(dict, "NSCameraUsageDescription");

				if (cameraUsageDescription == null)
				{
					XmlElement key = xmlDocument.CreateElement("key");
					key.InnerText = "NSCameraUsageDescription";

					XmlElement str = xmlDocument.CreateElement("string");
					str.InnerText = "Everyplay requires access to the camera";

					dict.AppendChild(key);
					dict.AppendChild(str);
				}

				// Add microphone usage description for iOS 10
				PListItem microphoneUsageDescription = GetPlistItem(dict, "NSMicrophoneUsageDescription");

				if (microphoneUsageDescription == null)
				{
					XmlElement key = xmlDocument.CreateElement("key");
					key.InnerText = "NSMicrophoneUsageDescription";

					XmlElement str = xmlDocument.CreateElement("string");
					str.InnerText = "Everyplay requires access to the microphone";

					dict.AppendChild(key);
					dict.AppendChild(str);
				}

				// Add photo library usage description for iOS 10
				PListItem photoLibraryUsageDescription = GetPlistItem(dict, "NSPhotoLibraryUsageDescription");

				if (photoLibraryUsageDescription == null)
				{
					XmlElement key = xmlDocument.CreateElement("key");
					key.InnerText = "NSPhotoLibraryUsageDescription";

					XmlElement str = xmlDocument.CreateElement("string");
					str.InnerText = "Everyplay requires access to the photo library";

					dict.AppendChild(key);
					dict.AppendChild(str);
				}

				// Add Facebook application id if not defined

				PListItem facebookAppId = GetPlistItem(dict, "FacebookAppID");

				if (facebookAppId == null)
				{
					XmlElement key = xmlDocument.CreateElement("key");
					key.InnerText = "FacebookAppID";

					XmlElement str = xmlDocument.CreateElement("string");
					str.InnerText = FacebookAppId;

					dict.AppendChild(key);
					dict.AppendChild(str);
				}

				// Add url schemes

				PListItem bundleUrlTypes = GetPlistItem(dict, "CFBundleURLTypes");

				if (bundleUrlTypes == null)
				{
					XmlElement key = xmlDocument.CreateElement("key");
					key.InnerText = "CFBundleURLTypes";

					XmlElement array = xmlDocument.CreateElement("array");

					bundleUrlTypes = new PListItem(dict.AppendChild(key), dict.AppendChild(array));
				}

				AddUrlScheme(xmlDocument, bundleUrlTypes.itemValueNode, UrlSchemePrefixFB + clientId);
				AddUrlScheme(xmlDocument, bundleUrlTypes.itemValueNode, UrlSchemePrefixEP + clientId);

				xmlDocument.Save(file);

				// Remove extra gargabe added by the XmlDocument save
				UpdateStringInFile(file, "dtd\"[]>", "dtd\">");
			}
			else
			{
				Debug.Log("Info.plist is not valid");
			}
		}
		catch (Exception e)
		{
			Debug.Log("Unable to update Info.plist: " + e);
		}
	}

	private static void AddUrlScheme(XmlDocument xmlDocument, XmlNode dictContainer, string urlScheme)
	{
		if (!CheckIfUrlSchemeExists(dictContainer, urlScheme))
		{
			XmlElement dict = xmlDocument.CreateElement("dict");

			XmlElement str = xmlDocument.CreateElement("string");
			str.InnerText = urlScheme;

			XmlElement key = xmlDocument.CreateElement("key");
			key.InnerText = "CFBundleURLSchemes";

			XmlElement array = xmlDocument.CreateElement("array");
			array.AppendChild(str);

			dict.AppendChild(key);
			dict.AppendChild(array);

			dictContainer.AppendChild(dict);
		}
		else
		{
			//Debug.Log("URL Scheme " + urlScheme + " already existed");
		}
	}

	private static bool CheckIfUrlSchemeExists(XmlNode dictContainer, string urlScheme)
	{
		foreach (XmlNode dict in dictContainer.ChildNodes)
		{
			if (dict.Name.ToLower().Equals("dict"))
			{
				PListItem bundleUrlSchemes = GetPlistItem(dict, "CFBundleURLSchemes");

				if (bundleUrlSchemes != null)
				{
					if (bundleUrlSchemes.itemValueNode.Name.Equals("array"))
					{
						foreach (XmlNode str in bundleUrlSchemes.itemValueNode.ChildNodes)
						{
							if (str.Name.Equals("string"))
							{
								if (str.InnerText.Equals(urlScheme))
								{
									return true;
								}
							}
							else
							{
								Debug.Log("CFBundleURLSchemes array contains illegal elements.");
							}
						}
					}
					else
					{
						Debug.Log("CFBundleURLSchemes contains illegal elements.");
					}
				}
			}
			else
			{
				Debug.Log("CFBundleURLTypes contains illegal elements.");
			}
		}

		return false;
	}

	public class PListItem
	{
		public XmlNode itemKeyNode;
		public XmlNode itemValueNode;

		public PListItem(XmlNode keyNode, XmlNode valueNode)
		{
			itemKeyNode = keyNode;
			itemValueNode = valueNode;
		}
	}

	public static PListItem GetPlistItem(XmlNode dict, string name)
	{
		for (int i = 0; i < dict.ChildNodes.Count - 1; i++)
		{
			XmlNode node = dict.ChildNodes.Item(i);

			if (node.Name.ToLower().Equals("key") && node.InnerText.ToLower().Equals(name.Trim().ToLower()))
			{
				XmlNode valueNode = dict.ChildNodes.Item(i + 1);

				if (!valueNode.Name.ToLower().Equals("key"))
				{
					return new PListItem(node, valueNode);
				}
				else
				{
					Debug.Log("Value for key missing in Info.plist");
				}
			}
		}

		return null;
	}

	private static void UpdateStringInFile(string file, string subject, string replacement)
	{
		try
		{
			if (!File.Exists(file))
			{
				return;
			}

			string processedContents = "";

			using (StreamReader sr = new StreamReader(file))
			{
				while (sr.Peek() >= 0)
				{
					string line = sr.ReadLine();
					processedContents += line.Replace(subject, replacement) + "\n";
				}
			}

			File.Delete(file);

			using (StreamWriter streamWriter = File.CreateText(file))
			{
				streamWriter.Write(processedContents);
			}
		}
		catch (Exception e)
		{
			Debug.Log("Unable to update string in file: " + e);
		}
	}

	public static string GetUnityVersion()
	{
		#if UNITY_3_5
        return "350";
		#elif (UNITY_4_0 || UNITY_4_0_1)
        return "400";
		#elif UNITY_4_1
        return "410";
		#elif UNITY_4_2
        return "420";
		#else
		return "0";
		#endif
	}

	private static void CreateModFile(string path, bool copyDependencies)
	{
		string modPath = System.IO.Path.Combine(path, "Everyplay");

		if (Directory.Exists(modPath))
		{
			ClearDirectory(modPath, false);
		}
		else
		{
			Directory.CreateDirectory(modPath);
		}

		Dictionary<string, object> mod = new Dictionary<string, object>();

		List<string> patches = new List<string>();
		List<string> libs = new List<string>();
		List<string> librarysearchpaths = new List<string>();
		List<string> frameworksearchpaths = new List<string>();
		List<string> frameworks = new List<string>();
		List<string> headerpaths = new List<string>();
		List<string> files = new List<string>();
		List<string> folders = new List<string>();
		List<string> excludes = new List<string>();

		string pluginsPath = Path.Combine(Application.dataPath, PathWithPlatformDirSeparators("Plugins/Everyplay/iOS"));
#if UNITY_TVOS
        string platformPluginsPath = Path.Combine(Application.dataPath, PathWithPlatformDirSeparators("Plugins/Everyplay/tvOS"));
#else
		string platformPluginsPath = pluginsPath;
#endif

		frameworksearchpaths.Add(copyDependencies ? "$(SRCROOT)/Everyplay" : MacPath(platformPluginsPath));
		headerpaths.Add(copyDependencies ? "$(SRCROOT)/Everyplay" : MacPath(platformPluginsPath));

		foreach (string framework in frameworkDependencies)
		{
			frameworks.Add(framework + ".framework");
		}
		foreach (string framework in weakFrameworkDependencies)
		{
			frameworks.Add(framework + ".framework");
		}

		List<string> dependencyList = new List<string>();
		dependencyList.Add("EveryplayGlesSupport.h");
		dependencyList.Add("EveryplayGlesSupport.mm");
		dependencyList.Add("EveryplayUnity.h");
		dependencyList.Add("EveryplayUnity.mm");

		List<string> platformDependencyList = new List<string>();
#if UNITY_IPHONE
		platformDependencyList.Add("Everyplay.framework");
		platformDependencyList.Add("Everyplay.bundle");
#else
        platformDependencyList.Add("EveryplayCore.framework");
#endif

		string dependencyTargetPath = copyDependencies ? modPath : pluginsPath;
		foreach (string dependencyFile in dependencyList)
		{
			string targetFile = Path.Combine(dependencyTargetPath, dependencyFile);

			if (copyDependencies)
			{
				try
				{
					string source = Path.Combine(pluginsPath, dependencyFile);

					if (Directory.Exists(source))
					{
						DirectoryCopy(source, targetFile);
					}
					else if (File.Exists(source))
					{
						File.Copy(source, targetFile);
					}
				}
				catch (Exception e)
				{
					Debug.Log("Unable to copy file or directory, " + e);
				}
			}

			files.Add(MacPath(targetFile));
		}
		dependencyTargetPath = copyDependencies ? modPath : platformPluginsPath;
		foreach (string dependencyFile in platformDependencyList)
		{
			string targetFile = Path.Combine(dependencyTargetPath, dependencyFile);

			if (copyDependencies)
			{
				try
				{
					string source = Path.Combine(platformPluginsPath, dependencyFile);

					if (Directory.Exists(source))
					{
						DirectoryCopy(source, targetFile);
					}
					else if (File.Exists(source))
					{
						File.Copy(source, targetFile);
					}
				}
				catch (Exception e)
				{
					Debug.Log("Unable to copy file or directory, " + e);
				}
			}

			files.Add(MacPath(targetFile));
		}


		files.Add(MacPath(System.IO.Path.Combine(modPath, "EveryplayConfig.h")));

		mod.Add("group", "Everyplay");
		mod.Add("patches", patches);
		mod.Add("libs", libs);
		mod.Add("librarysearchpaths", librarysearchpaths);
		mod.Add("frameworksearchpaths", frameworksearchpaths);
		mod.Add("frameworks", frameworks);
		mod.Add("headerpaths", headerpaths);
		mod.Add("files", files);
		mod.Add("folders", folders);
		mod.Add("excludes", excludes);

		string jsonMod = EveryplayMiniJSON.Json.Serialize(mod);

		string file = System.IO.Path.Combine(modPath, "EveryplayXCode.projmods");

		if (!Directory.Exists(modPath))
		{
			Directory.CreateDirectory(modPath);
		}
		if (File.Exists(file))
		{
			File.Delete(file);
		}

		using (StreamWriter streamWriter = File.CreateText(file))
		{
			streamWriter.Write(jsonMod);
		}
	}

	private static void DirectoryCopy(string sourceDirName, string destDirName)
	{
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);
		DirectoryInfo[] dirs = dir.GetDirectories();

		if (!dir.Exists)
		{
			return;
		}

		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}

		FileInfo[] files = dir.GetFiles();

		foreach (FileInfo file in files)
		{
			string temppath = Path.Combine(destDirName, file.Name);
			file.CopyTo(temppath, false);
		}

		foreach (DirectoryInfo subdir in dirs)
		{
			string temppath = Path.Combine(destDirName, subdir.Name);
			DirectoryCopy(subdir.FullName, temppath);
		}
	}

	public static void ClearDirectory(string path, bool deleteParent)
	{
		if (path != null)
		{
			string[] folders = Directory.GetDirectories(path);

			foreach (string folder in folders)
			{
				ClearDirectory(folder, true);
			}

			string[] files = Directory.GetFiles(path);

			foreach (string file in files)
			{
				File.Delete(file);
			}

			if (deleteParent)
			{
				Directory.Delete(path);
			}
		}
	}

	public static string PathWithPlatformDirSeparators(string path)
	{
		if (Path.DirectorySeparatorChar == '/')
		{
			return path.Replace("\\", Path.DirectorySeparatorChar.ToString());
		}
		else if (Path.DirectorySeparatorChar == '\\')
		{
			return path.Replace("/", Path.DirectorySeparatorChar.ToString());
		}

		return path;
	}

	public static string MacPath(string path)
	{
		return path.Replace(@"\", "/");
	}

	public static void SetEveryplayEnabledForTarget(BuildTargetGroup target, bool enabled)
	{
		string targetDefine = "";

		if (target == kBuildTargetGroup_iOS)
		{
			targetDefine = "EVERYPLAY_IPHONE";
			// Disable PluginImporter on iOS and use xCode editor instead
			SetPluginImportEnabled(kBuildTarget_iOS, false);
		}
		else if (target == kBuildTargetGroup_tvOS)
		{
			targetDefine = "EVERYPLAY_TVOS";
			// Disable PluginImporter on tvOS and use xCode editor instead
			SetPluginImportEnabled(kBuildTarget_tvOS, false);
		}
		else if (target == BuildTargetGroup.Android)
		{
			targetDefine = "EVERYPLAY_ANDROID";
			SetPluginImportEnabled(BuildTarget.Android, enabled);
		}
		else if (target == BuildTargetGroup.Standalone)
		{
			targetDefine = "EVERYPLAY_STANDALONE";
		}

		#if UNITY_3_5
        SetGlobalCustomDefine("smcs.rsp", targetDefine, enabled);
        SetGlobalCustomDefine("gmcs.rsp", targetDefine, enabled);
		#else
		SetScriptingDefineSymbolForTarget(target, targetDefine, enabled);
		#endif
	}

	public static void ValidateEveryplayState(EveryplaySettings settings)
	{
		bool isValid = false;

		if (settings != null && settings.IsValid)
		{
			isValid = true;
		}

		foreach (BuildTargetGroup target in System.Enum.GetValues(typeof(BuildTargetGroup)))
		{
			if (target == kBuildTargetGroup_iOS)
			{
				EveryplayPostprocessor.SetEveryplayEnabledForTarget(kBuildTargetGroup_iOS, isValid ? settings.iosSupportEnabled : false);
			}
			else if (target == kBuildTargetGroup_tvOS)
			{
				EveryplayPostprocessor.SetEveryplayEnabledForTarget(kBuildTargetGroup_tvOS, isValid ? settings.tvosSupportEnabled : false);
			}
			else if (target == BuildTargetGroup.Android)
			{
				EveryplayPostprocessor.SetEveryplayEnabledForTarget(BuildTargetGroup.Android, isValid ? settings.androidSupportEnabled : false);
			}
			else if (target == BuildTargetGroup.Standalone)
			{
				EveryplayPostprocessor.SetEveryplayEnabledForTarget(BuildTargetGroup.Standalone, isValid ? settings.standaloneSupportEnabled : false);
			}
		}
	}

	private static void SetScriptingDefineSymbolForTarget(BuildTargetGroup target, string targetDefine, bool enabled)
	{
		#if !UNITY_3_5
		string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

		defines = defines.Replace(targetDefine, "");
		defines = defines.Replace(";;", ";");

		if (enabled)
		{
			if (defines.Length > 0)
			{
				defines = targetDefine + ";" + defines;
			}
			else
			{
				defines = targetDefine;
			}
		}

		PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
		#endif
	}

	private static void SetGlobalCustomDefine(string file, string targetDefine, bool enabled)
	{
		string fileWithPath = System.IO.Path.Combine(Application.dataPath, file);

		if (File.Exists(fileWithPath))
		{
			UpdateStringInFile(fileWithPath, targetDefine, "");
			UpdateStringInFile(fileWithPath, ";;", ";");
			UpdateStringInFile(fileWithPath, ":;", ":");

			string processedContents = "";
			bool foundDefine = false;

			using (StreamReader sr = new StreamReader(fileWithPath))
			{
				while (sr.Peek() >= 0)
				{
					string line = sr.ReadLine();
					if (line.Contains("-define:") && !foundDefine)
					{
						if (enabled)
						{
							processedContents += line.Replace("-define:", "-define:" + targetDefine + ";") + "\n";
						}
						else
						{
							if (!(line.ToLower().Trim().Equals("-define:")))
							{
								processedContents += line + "\n";
							}
						}
						foundDefine = true;
					}
					else
					{
						processedContents += line + "\n";
					}
				}
			}

			if (!foundDefine)
			{
				processedContents += "-define:" + targetDefine;
			}

			File.Delete(fileWithPath);

			if (processedContents.Length > 0)
			{
				using (StreamWriter streamWriter = File.CreateText(fileWithPath))
				{
					streamWriter.Write(processedContents);
				}
			}
			else
			{
				string metaFile = fileWithPath + ".meta";

				if (File.Exists(metaFile))
				{
					File.Delete(metaFile);
				}
			}
		}
		else if (enabled)
		{
			using (StreamWriter streamWriter = File.CreateText(fileWithPath))
			{
				streamWriter.Write("-define:" + targetDefine);
			}
		}
	}

	public static void ValidateAndUpdateFacebook()
	{
		bool usingFB7Plus = false;

		try
		{
			Type facebookSettingsType = Type.GetType("Facebook.Unity.FacebookSettings,Assembly-CSharp", false, true);
			if (facebookSettingsType != null)
			{
				usingFB7Plus = true;
			}
			else
			{
				facebookSettingsType = Type.GetType("FBSettings,Assembly-CSharp", false, true);
			}

			if (facebookSettingsType != null)
			{
				MethodInfo[] methodInfos = facebookSettingsType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

				MethodInfo getInstance = null;
				MethodInfo getAppIds = null;
				MethodInfo setAppIds = null;
				MethodInfo setAppLabels = null;
				MethodInfo getAppLabels = null;

				foreach (MethodInfo methodInfo in methodInfos)
				{
					if (methodInfo.Name.Equals("get_Instance"))
					{
						getInstance = methodInfo;
					}
					else if (methodInfo.Name.Equals("get_AppIds"))
					{
						getAppIds = methodInfo;
					}
					else if (methodInfo.Name.Equals("set_AppIds"))
					{
						setAppIds = methodInfo;
					}
					else if (methodInfo.Name.Equals("get_AppLabels"))
					{
						getAppLabels = methodInfo;
					}
					else if (methodInfo.Name.Equals("set_AppLabels"))
					{
						setAppLabels = methodInfo;
					}
				}

				if (getAppIds != null && getAppLabels != null && setAppIds != null && setAppLabels != null && getInstance != null)
				{
					object facebookSettings = getInstance.Invoke(null, null);

					if (facebookSettings != null)
					{
						List<string> currentAppIds;
						List<string> currentAppLabels;

						if (usingFB7Plus)
						{
							currentAppIds = (List<string>)getAppIds.Invoke(facebookSettings, null);
							currentAppLabels = (List<string>)getAppLabels.Invoke(facebookSettings, null);
						}
						else
						{
							currentAppIds = new List<string>((string[])getAppIds.Invoke(facebookSettings, null));
							currentAppLabels = new List<string>((string[])getAppLabels.Invoke(facebookSettings, null));
						}


						if (currentAppIds != null && currentAppLabels != null)
						{
							bool addEveryplay = true;
							bool updated = false;

							List<string> appLabelList = new List<string>();
							List<string> appIdList = new List<string>();

							for (int i = 0; i < Mathf.Min(currentAppIds.Count, currentAppLabels.Count); i++)
							{
								// Skip invalid items
								bool shouldSkipItem = (currentAppIds[i] == null || currentAppIds[i].Trim().Length < 1 || currentAppIds[i].Trim().Equals("0") || currentAppLabels[i] == null);

								// Check if we already have an Everyplay item or it is malformed or a duplicate
								if (!shouldSkipItem)
								{
									if (currentAppLabels[i].Equals("Everyplay") && currentAppIds[i].Equals(FacebookAppId))
									{
										if (addEveryplay)
										{
											addEveryplay = false;
										}
										else
										{
											shouldSkipItem = true;
										}
									}
									else if (currentAppIds[i].Trim().ToLower().Equals(FacebookAppId))
									{
										shouldSkipItem = true;
									}
								}

								if (!shouldSkipItem)
								{
									appIdList.Add(currentAppIds[i]);
									appLabelList.Add(currentAppLabels[i]);
								}
								else
								{
									updated = true;
								}
							}

							if (addEveryplay)
							{
								appLabelList.Add("Everyplay");
								appIdList.Add(FacebookAppId);
								updated = true;
							}

							if (updated)
							{
								if (usingFB7Plus)
								{
									setAppLabels.Invoke(facebookSettings, new object[] { appLabelList });
									setAppIds.Invoke(facebookSettings, new object[] { appIdList });
								}
								else
								{
									object[] setAppLabelsObjs = { appLabelList.ToArray() };
									setAppLabels.Invoke(facebookSettings, setAppLabelsObjs);
									object[] setAppIdsObjs = { appIdList.ToArray() };
									setAppIds.Invoke(facebookSettings, setAppIdsObjs);
								}
							}
						}
					}
				}
			}
			else
			{
				//Debug.Log("To use the Facebook native login with Everyplay, please import Facebook SDK for Unity.");
			}
		}
		catch (Exception e)
		{
			Debug.Log("Unable to validate and update Facebook: " + e);
		}
	}

	// This fixes an Info.plist append bug near UIInterfaceOrientation on some Unity versions (atleast on Unity 4.2.2)
	private static void FixUnityPlistAppendBug(string path)
	{
		try
		{
			string file = System.IO.Path.Combine(path, "Info.plist");

			if (!File.Exists(file))
			{
				return;
			}

			string processedContents = "";
			bool bugFound = false;

			using (StreamReader sr = new StreamReader(file))
			{
				bool previousWasEndString = false;
				while (sr.Peek() >= 0)
				{
					string line = sr.ReadLine();

					if (previousWasEndString && line.Trim().StartsWith("</string>"))
					{
						bugFound = true;
					}
					else
					{
						processedContents += line + "\n";
					}

					previousWasEndString = line.Trim().EndsWith("</string>");
				}
			}

			if (bugFound)
			{
				File.Delete(file);

				using (StreamWriter streamWriter = File.CreateText(file))
				{
					streamWriter.Write(processedContents);
				}

				Debug.Log("EveryplayPostprocessor found and fixed a known Unity plist append bug in the Info.plist.");
			}
		}
		catch (Exception e)
		{
			Debug.Log("Unable to process plist file: " + e);
		}
	}

	private const string FacebookAppId = "182473845211109";
	private const string UrlSchemePrefixFB = "fb182473845211109ep";
	private const string UrlSchemePrefixEP = "ep";

	private const BuildTarget kBuildTarget_iOS = (BuildTarget)9;
	// Avoid automatic API updater dialog (iPhone -> iOS)
	private const BuildTargetGroup kBuildTargetGroup_iOS = (BuildTargetGroup)4;
	// Avoid automatic API updater dialog (iPhone -> iOS)

	private const BuildTarget kBuildTarget_tvOS = (BuildTarget)37;
	private const BuildTargetGroup kBuildTargetGroup_tvOS = (BuildTargetGroup)25;
}
