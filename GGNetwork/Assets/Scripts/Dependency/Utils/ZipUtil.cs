using System;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>
/// GZip 压缩/解压工具。基于 .NET BCL 的 System.IO.Compression.GZipStream 实现，
/// 不再依赖第三方 SharpZipLib。仅保留框架实际使用的字符串 GZip 编解码路径。
/// </summary>
public static class ZipUtil
{
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
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
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
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
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
