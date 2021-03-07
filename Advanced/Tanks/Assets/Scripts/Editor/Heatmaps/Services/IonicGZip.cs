using System;
using UnityEngine;
using System.IO;
using System.Text;
using Ionic.Zlib;
using System.Collections.Generic;


public class IonicGZip
{

    public static string[] DecompressFiles(string[] args)
    {
        var results = new List<string>();
        foreach(var fileName in args)
        {
            string response = DecompressFile(fileName);
            results.Add(response);
        }
        return results.ToArray();
    }

    public static string DecompressFile(string fileName)
    {
        byte[] file = File.ReadAllBytes(fileName);
        byte[] decompressed = Decompress(file);
        return Encoding.UTF8.GetString(decompressed);
    }

    static byte[] Decompress(byte[] gzip)
    {
        // Create a GZIP stream with decompression mode.
        // ... Then create a buffer and write into while reading from the GZIP stream.
        using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }

    public static void CompressAndSave(string savePath, string data)
    {
        // Write string to temporary file.
        string temp = Path.GetTempFileName();
        File.WriteAllText(temp, data);

        // Read file into byte array buffer.
        byte[] b;
        using (FileStream f = new FileStream(temp, FileMode.Open))
        {
            b = new byte[f.Length];
            f.Read(b, 0, (int)f.Length);
        }

        // Use GZipStream to write compressed bytes to target file.
        using (FileStream f2 = new FileStream(savePath, FileMode.Create))
        {
            using (GZipStream gz = new GZipStream(f2, CompressionMode.Compress, false))
            {
                gz.Write(b, 0, b.Length);
            }
        }
    }
}