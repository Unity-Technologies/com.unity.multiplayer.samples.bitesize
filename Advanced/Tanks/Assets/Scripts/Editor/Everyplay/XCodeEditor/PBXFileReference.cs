using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EveryplayEditorTools.XCodeEditor
{
	public class PBXFileReference : PBXObject
	{
		protected const string PATH_KEY = "path";
		protected const string NAME_KEY = "name";
		protected const string SOURCETREE_KEY = "sourceTree";
		protected const string EXPLICIT_FILE_TYPE_KEY = "explicitFileType";
		protected const string LASTKNOWN_FILE_TYPE_KEY = "lastKnownFileType";
		protected const string ENCODING_KEY = "fileEncoding";
		public string buildPhase;
		public readonly Dictionary<TreeEnum, string> trees = new Dictionary<TreeEnum, string>
		{
			{ TreeEnum.ABSOLUTE, "\"<absolute>\"" },
			{ TreeEnum.GROUP, "\"<group>\"" },
			{ TreeEnum.BUILT_PRODUCTS_DIR, "BUILT_PRODUCTS_DIR" },
			{ TreeEnum.DEVELOPER_DIR, "DEVELOPER_DIR" },
			{ TreeEnum.SDKROOT, "SDKROOT" },
			{ TreeEnum.SOURCE_ROOT, "SOURCE_ROOT" }
		};
		public static readonly Dictionary<string, string> typeNames = new Dictionary<string, string>
		{
			{ ".a", "archive.ar" },
			{ ".app", "wrapper.application" },
			{ ".s", "sourcecode.asm" },
			{ ".c", "sourcecode.c.c" },
			{ ".cpp", "sourcecode.cpp.cpp" },
			{ ".cs", "sourcecode.cpp.cpp" },
			{ ".framework", "wrapper.framework" },
			{ ".h", "sourcecode.c.h" },
			{ ".icns", "image.icns" },
			{ ".m", "sourcecode.c.objc" },
			{ ".mm", "sourcecode.cpp.objcpp" },
			{ ".nib", "wrapper.nib" },
			{ ".plist", "text.plist.xml" },
			{ ".png", "image.png" },
			{ ".rtf", "text.rtf" },
			{ ".tiff", "image.tiff" },
			{ ".txt", "text" },
			{ ".xcodeproj", "wrapper.pb-project" },
			{ ".xib", "file.xib" },
			{ ".strings", "text.plist.strings" },
			{ ".bundle", "wrapper.plug-in" },
			{ ".dylib", "compiled.mach-o.dylib" }
		};
		public static readonly Dictionary<string, string> typePhases = new Dictionary<string, string>
		{
			{ ".a", "PBXFrameworksBuildPhase" },
			{ ".app", null },
			{ ".s", "PBXSourcesBuildPhase" },
			{ ".c", "PBXSourcesBuildPhase" },
			{ ".cpp", "PBXSourcesBuildPhase" },
			{ ".cs", null },
			{ ".framework", "PBXFrameworksBuildPhase" },
			{ ".h", null },
			{ ".icns", "PBXResourcesBuildPhase" },
			{ ".m", "PBXSourcesBuildPhase" },
			{ ".mm", "PBXSourcesBuildPhase" },
			{ ".nib", "PBXResourcesBuildPhase" },
			{ ".plist", "PBXResourcesBuildPhase" },
			{ ".png", "PBXResourcesBuildPhase" },
			{ ".rtf", "PBXResourcesBuildPhase" },
			{ ".tiff", "PBXResourcesBuildPhase" },
			{ ".txt", "PBXResourcesBuildPhase" },
			{ ".xcodeproj", null },
			{ ".xib", "PBXResourcesBuildPhase" },
			{ ".strings", "PBXResourcesBuildPhase" },
			{ ".bundle", "PBXResourcesBuildPhase" },
			{ ".dylib", "PBXFrameworksBuildPhase" }
		};

		public PBXFileReference(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
		}

		public PBXFileReference(string filePath, TreeEnum tree = TreeEnum.SOURCE_ROOT)
			: base()
		{
			string temp = "\"" + filePath + "\"";
			this.Add(PATH_KEY, temp);
			this.Add(NAME_KEY, System.IO.Path.GetFileName(filePath));
			this.Add(SOURCETREE_KEY, (string)(System.IO.Path.IsPathRooted(filePath) ? trees[TreeEnum.ABSOLUTE] : trees[tree]));
			this.GuessFileType();
		}

		public string name
		{
			get
			{
				if (!ContainsKey(NAME_KEY))
				{
					return null;
				}
				return (string)_data[NAME_KEY];
			}
		}

		private void GuessFileType()
		{
			this.Remove(EXPLICIT_FILE_TYPE_KEY);
			this.Remove(LASTKNOWN_FILE_TYPE_KEY);
			string extension = System.IO.Path.GetExtension((string)_data[NAME_KEY]);
			if (!PBXFileReference.typeNames.ContainsKey(extension))
			{
				Debug.LogWarning("Unknown file extension: " + extension + "\nPlease add extension and Xcode type to PBXFileReference.types");
				return;
			}

			this.Add(LASTKNOWN_FILE_TYPE_KEY, PBXFileReference.typeNames[extension]);
			this.buildPhase = PBXFileReference.typePhases[extension];
		}

		private void SetFileType(string fileType)
		{
			this.Remove(EXPLICIT_FILE_TYPE_KEY);
			this.Remove(LASTKNOWN_FILE_TYPE_KEY);

			this.Add(EXPLICIT_FILE_TYPE_KEY, fileType);
		}

		//	class PBXFileReference(PBXType):
		//	  def __init__(self, d=None):
		//		  PBXType.__init__(self, d)
		//		  self.build_phase = None
		//
		//	  types = {
		//		  '.a':('archive.ar', 'PBXFrameworksBuildPhase'),
		//		  '.app': ('wrapper.application', None),
		//		  '.s': ('sourcecode.asm', 'PBXSourcesBuildPhase'),
		//		  '.c': ('sourcecode.c.c', 'PBXSourcesBuildPhase'),
		//		  '.cpp': ('sourcecode.cpp.cpp', 'PBXSourcesBuildPhase'),
		//		  '.framework': ('wrapper.framework','PBXFrameworksBuildPhase'),
		//		  '.h': ('sourcecode.c.h', None),
		//		  '.icns': ('image.icns','PBXResourcesBuildPhase'),
		//		  '.m': ('sourcecode.c.objc', 'PBXSourcesBuildPhase'),
		//		  '.mm': ('sourcecode.cpp.objcpp', 'PBXSourcesBuildPhase'),
		//		  '.nib': ('wrapper.nib', 'PBXResourcesBuildPhase'),
		//		  '.plist': ('text.plist.xml', 'PBXResourcesBuildPhase'),
		//		  '.png': ('image.png', 'PBXResourcesBuildPhase'),
		//		  '.rtf': ('text.rtf', 'PBXResourcesBuildPhase'),
		//		  '.tiff': ('image.tiff', 'PBXResourcesBuildPhase'),
		//		  '.txt': ('text', 'PBXResourcesBuildPhase'),
		//		  '.xcodeproj': ('wrapper.pb-project', None),
		//		  '.xib': ('file.xib', 'PBXResourcesBuildPhase'),
		//		  '.strings': ('text.plist.strings', 'PBXResourcesBuildPhase'),
		//		  '.bundle': ('wrapper.plug-in', 'PBXResourcesBuildPhase'),
		//		  '.dylib': ('compiled.mach-o.dylib', 'PBXFrameworksBuildPhase')
		//	  }
		//
		//	  trees = [
		//		  '<absolute>',
		//		  '<group>',
		//		  'BUILT_PRODUCTS_DIR',
		//		  'DEVELOPER_DIR',
		//		  'SDKROOT',
		//		  'SOURCE_ROOT',
		//	  ]
		//
		//	  def guess_file_type(self):
		//		  self.remove('explicitFileType')
		//		  self.remove('lastKnownFileType')
		//		  ext = os.path.splitext(self.get('name', ''))[1]
		//
		//		  f_type, build_phase = PBXFileReference.types.get(ext, ('?', None))
		//
		//		  self['lastKnownFileType'] = f_type
		//		  self.build_phase = build_phase
		//
		//		  if f_type == '?':
		//			  print 'unknown file extension: %s' % ext
		//			  print 'please add extension and Xcode type to PBXFileReference.types'
		//
		//		  return f_type
		//
		//	  def set_file_type(self, ft):
		//		  self.remove('explicitFileType')
		//		  self.remove('lastKnownFileType')
		//
		//		  self['explicitFileType'] = ft
		//
		//	  @classmethod
		//	  def Create(cls, os_path, tree='SOURCE_ROOT'):
		//		  if tree not in cls.trees:
		//			  print 'Not a valid sourceTree type: %s' % tree
		//			  return None
		//
		//		  fr = cls()
		//		  fr.id = cls.GenerateId()
		//		  fr['path'] = os_path
		//		  fr['name'] = os.path.split(os_path)[1]
		//		  fr['sourceTree'] = '<absolute>' if os.path.isabs(os_path) else tree
		//		  fr.guess_file_type()
		//
		//		  return fr
	}

	public enum TreeEnum
	{
		ABSOLUTE,
		GROUP,
		BUILT_PRODUCTS_DIR,
		DEVELOPER_DIR,
		SDKROOT,
		SOURCE_ROOT
	}
}
