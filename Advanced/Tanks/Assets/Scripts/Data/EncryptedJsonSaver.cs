using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;

#if !NETFX_CORE
namespace Tanks.Data
{
	/// <summary>
	/// Encryption extension to Json Saver
	/// </summary>
	public class EncryptedJsonSaver : JsonSaver
	{
		//Salt for encryption
		private static readonly byte[] s_Salt = new byte[]
		{
			0x4a, 0xdd, 0x1a, 0x2c, 0x0c, 0xe8, 0x9a, 0x8a, 0x2c, 0x6b, 0xa1, 0x7d, 0xf8, 0x09, 0x3b, 0xad,
			0xae, 0xcb, 0xab, 0xcc, 0x45, 0xc0, 0x0d, 0x94, 0xfc, 0x19, 0xaf, 0x13, 0xfd, 0x2b, 0xf1, 0x86
		};

		//Get device bytes to prevent copying save file to different device
		private byte[] GetUniqueDeviceBytes()
		{
			byte[] deviceIdentifier = Encoding.ASCII.GetBytes(SystemInfo.deviceUniqueIdentifier);

			return deviceIdentifier;
		}

		/// <summary>
		/// Gets encrypted write stream
		/// </summary>
		/// <returns>The write stream.</returns>
		protected override StreamWriter GetWriteStream()
		{
			FileStream underlyingStream;
			CryptoStream encryptedStream;

			underlyingStream = new FileStream(GetSaveFilename(), FileMode.Create);

			Rfc2898DeriveBytes byteGenerator = new Rfc2898DeriveBytes(GetUniqueDeviceBytes(), s_Salt, 1000);
			RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
			byte[] key = byteGenerator.GetBytes(32);
			byte[] iv = new byte[16];
			random.GetBytes(iv);

			Rijndael rijndael = Rijndael.Create();
			rijndael.Key = key;
			rijndael.IV = iv;

			underlyingStream.Write(iv, 0, 16);
			encryptedStream = new CryptoStream(underlyingStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);

			return new StreamWriter(encryptedStream);
		}

		/// <summary>
		/// Gets decrypted read stream
		/// </summary>
		/// <returns>The read stream.</returns>
		protected override StreamReader GetReadStream()
		{
			FileStream underlyingStream;
			CryptoStream encryptedStream;

			underlyingStream = new FileStream(GetSaveFilename(), FileMode.Open);

			Rfc2898DeriveBytes byteGenerator = new Rfc2898DeriveBytes(GetUniqueDeviceBytes(), s_Salt, 1000);
			RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
			byte[] key = byteGenerator.GetBytes(32);
			byte[] iv = new byte[16];
			random.GetBytes(iv);

			underlyingStream.Read(iv, 0, 16);

			Rijndael rijndael = Rijndael.Create();
			rijndael.Key = key;
			rijndael.IV = iv;

			encryptedStream = new CryptoStream(underlyingStream, rijndael.CreateDecryptor(), CryptoStreamMode.Read);

			return new StreamReader(encryptedStream);
		}
	}
}
#endif