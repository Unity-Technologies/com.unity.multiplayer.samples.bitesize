using UnityEngine;
using System.IO;

namespace Tanks.Data
{
	/// <summary>
	/// Json implementation of data persistence
	/// </summary>
	public class JsonSaver : IDataSaver
	{
		#if UNITY_EDITOR
		private static readonly string s_Filename = "tanks_editor.sav";
		#else
		private static readonly string s_Filename = "tanks.sav";
		#endif

		public static string GetSaveFilename()
		{
			return string.Format("{0}/{1}", Application.persistentDataPath, s_Filename);
		}

		protected virtual StreamWriter GetWriteStream()
		{
			return new StreamWriter(new FileStream(GetSaveFilename(), FileMode.Create));
		}

		protected virtual StreamReader GetReadStream()
		{
			return new StreamReader(new FileStream(GetSaveFilename(), FileMode.Open));
		}

		/// <summary>
		/// Save the specified data store
		/// </summary>
		/// <param name="data">Data.</param>
		public void Save(DataStore data)
		{
			string json = JsonUtility.ToJson(data);

			using (StreamWriter writer = GetWriteStream())
			{
				writer.Write(json);
			}
		}

		/// <summary>
		/// Load the specified data store
		/// </summary>
		/// <param name="data">Data.</param>
		public bool Load(DataStore data)
		{
			string loadFilename = GetSaveFilename();

			if (File.Exists(loadFilename))
			{
				using (StreamReader reader = GetReadStream())
				{
					JsonUtility.FromJsonOverwrite(reader.ReadToEnd(), data);
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Deletes the save file
		/// </summary>
		public void Delete()
		{
			File.Delete(GetSaveFilename());
		}
	}
}