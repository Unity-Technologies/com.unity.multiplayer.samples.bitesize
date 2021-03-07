using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EveryplayEditorTools.XCodeEditor
{
	public class PBXObject
	{
		protected const string ISA_KEY = "isa";
		protected const string BUILDPHASES_KEY = "buildPhases";
		protected string _guid;
		protected PBXDictionary _data;
		private static string guidRegex = @"[A-Fa-f0-9]{24}\s*/\*[^*]+\*/";
		public bool internalNewlines;

		#region Properties

		public string guid
		{
			get
			{
				if (string.IsNullOrEmpty(_guid))
				{
					_guid = GenerateGuid();
				}

				return _guid;
			}
		}

		public PBXDictionary data
		{
			get
			{
				if (_data == null)
				{
					_data = new PBXDictionary();
				}

				return _data;
			}
		}

		#endregion

		#region Constructors

		public PBXObject()
		{
			_data = new PBXDictionary();
			_data[ISA_KEY] = this.GetType().Name;
			_guid = GenerateGuid();
			internalNewlines = false;
		}

		public PBXObject(string guid)
			: this()
		{
			if (IsGuid(guid))
			{
				_guid = guid;
			}
		}

		public PBXObject(string guid, PBXDictionary dictionary)
			: this(guid)
		{
			if (!dictionary.ContainsKey(ISA_KEY) || ((string)dictionary[ISA_KEY]).CompareTo(this.GetType().Name) != 0)
			{
				Debug.LogError("PBXDictionary is not a valid ISA object");
			}

			foreach (KeyValuePair<string, object> item in dictionary)
			{
				_data[item.Key] = item.Value;
			}
		}

		#endregion

		#region Static methods

		public static bool IsGuid(string aString)
		{
			return System.Text.RegularExpressions.Regex.IsMatch(aString, guidRegex);
		}

		public static string GenerateGuid()
		{
			return System.Guid.NewGuid().ToString("N").Substring(8).ToUpper();
		}

		#endregion

		#region Data manipulation

		public void Add(string key, object obj)
		{
			_data.Add(key, obj);
		}

		public bool Remove(string key)
		{
			return _data.Remove(key);
		}

		public bool ContainsKey(string key)
		{
			return _data.ContainsKey(key);
		}

		#endregion
	}

	public class PBXTargetDependency : PBXObject
	{
		public PBXTargetDependency()
			: base()
		{
			internalNewlines = true;
		}

		public PBXTargetDependency(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
			internalNewlines = true;
		}
	}

	public class PBXNativeTarget : PBXObject
	{
		public PBXNativeTarget()
			: base()
		{
			internalNewlines = true;
		}

		public PBXNativeTarget(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
			internalNewlines = true;
		}

		public bool HasBuildphase(string buildPhaseId)
		{
			if (buildPhaseId != null && data != null && data.ContainsKey(BUILDPHASES_KEY))
			{
				PBXList buildPhases = (PBXList)data[BUILDPHASES_KEY];

				if (buildPhases != null && buildPhases.Count > 0)
				{
					foreach (string buildPhase in buildPhases)
					{
						if (buildPhase.Contains(buildPhaseId))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}

	public class PBXContainerItemProxy : PBXObject
	{
		public PBXContainerItemProxy()
			: base()
		{
			internalNewlines = true;
		}

		public PBXContainerItemProxy(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
			internalNewlines = true;
		}
	}

	public class PBXReferenceProxy : PBXObject
	{
		public PBXReferenceProxy()
			: base()
		{
			internalNewlines = true;
		}

		public PBXReferenceProxy(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
			internalNewlines = true;
		}
	}

	public class PBXVariantGroup : PBXObject
	{
		public PBXVariantGroup()
			: base()
		{
			internalNewlines = true;
		}

		public PBXVariantGroup(string guid, PBXDictionary dictionary)
			: base(guid, dictionary)
		{
			internalNewlines = true;
		}
	}
}
