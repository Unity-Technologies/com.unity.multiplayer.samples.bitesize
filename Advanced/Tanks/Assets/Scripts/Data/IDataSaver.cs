namespace Tanks.Data
{
	/// <summary>
	/// Interface for saving data
	/// </summary>
	public interface IDataSaver
	{
		void Save(DataStore data);

		bool Load(DataStore data);

		void Delete();
	}
}