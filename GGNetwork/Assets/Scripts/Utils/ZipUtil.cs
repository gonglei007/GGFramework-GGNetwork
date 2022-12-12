using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using System.IO.Compression;

public static class ZipUtil
{

    public static int GetZipFileCount(string archiveFilenameIn, string password)
    {
        int extractCount = 0;
        FileStream fs = null;
        ICSharpCode.SharpZipLib.Zip.ZipFile zf = null;
        try
        {
            fs = File.OpenRead(archiveFilenameIn);
            zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs);
            if (!String.IsNullOrEmpty(password))
            {
                zf.Password = password;     // AES encrypted entries are handled automatically
            }
            extractCount = (int)zf.Count;
        }
        catch (Exception e)
        {
        }
        finally
        {
            if (zf != null)
            {
                zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                zf.Close(); // Ensure we release resources
            }
            if (fs != null)
            {
                fs.Close();
            }
        }

        return extractCount;
    }

    public static void FastExtractZipFile(string archiveFilenameIn, string password, string outFolder, Action<bool> onFinish = null, Action<int, int> onProcess = null)
    {
        int extractNumber = 0;
        int extractCount = 0;

        FastZipEvents events = new FastZipEvents();
        FastZip zf = new FastZip(events);
        //events.ProcessFile = ProcessFileMethod;
        events.ProcessFile = (object sender, ScanEventArgs args) => {
            Debug.Log("file-" + args.Name.ToString());
            if (onProcess != null)
            {
                onProcess(extractNumber, extractCount);
            }
            extractNumber++;
        };
        try
        {
            //FileStream fs = File.OpenRead(archiveFilenameIn);
            if (!String.IsNullOrEmpty(password))
            {
                zf.Password = password;     // AES encrypted entries are handled automatically
            }
            zf.UseZip64 = UseZip64.On;
            extractCount = 0;//(int)zf.Count;
            Debug.LogFormat("[unzip]extract count:{0} password:{1}", extractCount, password);
            extractNumber = 0;
            zf.ExtractZip(archiveFilenameIn, outFolder, null);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            if (onFinish != null)
            {
                onFinish(false);
            }
        }
        finally
        {
            if (onFinish != null)
            {
                Debug.LogFormat("[unzip]onfinish:{0}-{1}", extractCount, extractNumber);
                //onFinish(extractCount > 0 && extractCount == extractNumber);
                //onFinish(extractCount > 0);
                onFinish(true);
            }
        }
    }

    public static void ExtractGZipFile(string archiveFilenameIn, string password, string outFolder, Action<bool> onFinish = null, Action<int, int> onProcess = null)
    {
        Debug.LogFormat("Extract start:{0}", archiveFilenameIn);
        bool result = true;
        FileStream fs = null;
        ZipInputStream zipStream = null;
        ZipEntry zipEntry = null;
        string fileName;

        if (!File.Exists(archiveFilenameIn))
        {
            if (onFinish != null) { onFinish(false); }
            return;
        }

        if (!Directory.Exists(outFolder))
        {
            Directory.CreateDirectory(outFolder);
        }

        try
        {
            int process = 1;
            int entryCount = GetZipFileCount(archiveFilenameIn, password);
            zipStream = new ZipInputStream(File.OpenRead(archiveFilenameIn.Trim()));
            if (!string.IsNullOrEmpty(password)) zipStream.Password = password;
            while ((zipEntry = zipStream.GetNextEntry()) != null)
            {
                if (!string.IsNullOrEmpty(zipEntry.Name))
                {
                    String entryFileName = zipEntry.Name;
                    String fullZipToPath = Path.Combine(outFolder, zipEntry.Name);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    fileName = Path.Combine(outFolder, zipEntry.Name);
                    fileName = fileName.Replace('/', '\\');

                    /*
                    if (fileName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(fileName);
                        continue;
                    }
                    */
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                        //Debug.Log(directoryName);
                        //continue;
                    }
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }

                    using (fs = File.Create(fullZipToPath))
                    {
                        int size = 2048;
                        byte[] data = new byte[size];
                        while (true)
                        {
                            size = zipStream.Read(data, 0, data.Length);
                            if (size > 0)
                                fs.Write(data, 0, size);
                            else
                                break;
                        }
                    }
                    if (onProcess != null) { onProcess(process, entryCount); }
                    process++;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            result = false;
            onFinish(result);
        }
        finally
        {
            if (fs != null)
            {
                fs.Close();
                fs.Dispose();
            }
            if (zipStream != null)
            {
                zipStream.Close();
                zipStream.Dispose();
            }
            if (zipEntry != null)
            {
                zipEntry = null;
            }
            GC.Collect();
            GC.Collect(1);
        }
        Debug.LogFormat("Extract finish:{0}", archiveFilenameIn);
        onFinish(result);
        //return result;
    }

    public static void CopyTo(Stream src, Stream dest)
    {
        byte[] bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }

    public static byte[] Zip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (GZipOutputStream gs = new GZipOutputStream(mso))
            //using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                //msi.CopyTo(gs);
                CopyTo(msi, gs);
            }

            return mso.ToArray();
        }
    }

    public static string Unzip(byte[] bytes)
    {
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (GZipInputStream gs = new GZipInputStream(msi))
            //using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                //gs.CopyTo(mso);
                CopyTo(gs, mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
        }
    }

    public static string Unzip(string data)
    {
        byte[] bytes = Convert.FromBase64String(data);
        return Unzip(bytes);
    }
}
