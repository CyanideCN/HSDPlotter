using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
namespace HSDPlotter
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block01
    {
        public byte HeaderBlockNum;
        public short BlockLength;
        public short HeaderNum;
        public byte ByteOrder;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string SatName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string ProcessCenterName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string ObsArea;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
        public string OtherInfo;
        public short ObsTimeLine;
        public double ObsStartTime;
        public double ObsEndTime;
        public double FileCreationTime;
        public int TotalHeaderLength;
        public int TotalDataLength;
        public byte QF1;
        public byte QF2;
        public byte QF3;
        public byte QF4;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FileVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string FileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block02
    {
        public byte HeaderBlockNum;
        public short BlockLength;
        public short NBitsPerPixel;
        public short NCols;
        public short NLines;
        public byte Compression;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block03
    {
        public byte HeaderBlockNum;
        public short BlockLength;
        public double SubLon;
        public int CFAC;
        public int LFAC;
        public float COFF;
        public float LOFF;
        public double Rs;
        public double req;
        public double rpol;
        public double Const1;
        public double Const2;
        public double Const3;
        public double ConstStd;
        public short ResamplingTypes;
        public short ResamplingSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block04
    {
        // Useless block, just skip
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 139)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block05
    {
        public byte HeaderBlockNum;
        public short BlockLength;
        public short BandNum;
        public double CentralWaveLength;
        public short ValidNBitsPerPixel;
        public short CountValueOfErrorPixels;
        public short CountValueOfPixelsOutsideScanArea;
        public double Gain;
        public double Constant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block05IR
    {
        public double c0;
        public double c1;
        public double c2;
        public double C0;
        public double C1;
        public double C2;
        public double c;
        public double h;
        public double k;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block05VIS
    {
        public double c;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 104)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block06
    {
        // Useless block, just skip
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 259)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Block07
    {
        public byte HeaderBlockNum;
        public short BlockLength;
        public byte TotalSegNum;
        public byte SegSeqNum;
        public short FirstLineNum;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Spare;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CommonHeader
    {
        public byte HeaderBlockNum;
        public short BlockLength;
    }
    class HSDReader
    {
        private readonly BinaryReader br;
        private MemoryStream content;
        public float[,] data;
        public int ncol;
        public int nline;
        public Block01 b01;
        public Block02 b02;
        public Block03 b03;
        public Block04 b04;
        public Block06 b06;
        public Block07 b07;
        public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theStructure;
        }

        static double Log1p(double x)
        => Math.Abs(x) > 1e-4 ? Math.Log(1.0 + x) : (-0.5 * x + 1.0) * x;

        public HSDReader(string filename)
        {
            content = new MemoryStream();
            
            if (filename.EndsWith(".bz2"))
            {
                var bz2file = File.OpenRead(filename);
                var stream = new BZip2Stream(bz2file, CompressionMode.Decompress, true);
                //var bz2file = File.OpenRead(filename);
                //var reader = ReaderFactory.Open(bz2file);
                stream.CopyTo(content);
                bz2file.Close();
            }
            else
            {
                var f = new FileStream(filename, FileMode.Open);
                f.CopyTo(content);
                f.Close();
            }
            content.Seek(0, SeekOrigin.Begin);
            br = new BinaryReader(content);
            b01 = ByteToType<Block01>(br);
            b02 = ByteToType<Block02>(br);
            b03 = ByteToType<Block03>(br);
            b04 = ByteToType<Block04>(br);
            var b05 = ByteToType<Block05>(br);
            Block05VIS? b05vis = null;
            Block05IR? b05ir = null;
            if (b05.BandNum <= 6)
            {
                b05vis = ByteToType<Block05VIS>(br);
            }
            else
            {
                b05ir = ByteToType<Block05IR>(br);
            }
            b06 = ByteToType<Block06>(br);
            b07 = ByteToType<Block07>(br);
            // Leap rest header blocks
            int leap_block_num = 4;
            for (int i = 0; i < leap_block_num; i++)
            {
                var header = ByteToType<CommonHeader>(br);
                var section_length = header.BlockLength;
                content.Seek(section_length - 3, SeekOrigin.Current);
            }
            ncol = b02.NCols;
            nline = b02.NLines;
            int[,] dn = new int[nline, ncol];
            for (int i = 0; i < nline; i++)
            {
                for (int j = 0; j < ncol; j++)
                {
                    dn[i, j] = br.ReadInt16();
                }
            }
            if (b05.BandNum <= 6)
            {
                data = CalibrateVIS(dn, b05, b05vis);
            }
            else
            {
                data = CalibrateIR(dn, b05, b05ir);
            }
        }

        public void GetLonLat(ref float[,] lon, ref float[,] lat)
        {
            var DEG2RAD = Math.PI / 180;
            var RAD2DEG = 180 / Math.PI;
            var SCLUNIT = Math.Pow(2.0, -16);
            var DIS = b03.Rs;
            var CON = b03.Const3;
            var COFF = b03.COFF;
            var CFAC = b03.CFAC;
            var LOFF = b03.LOFF;
            var LFAC = b03.LFAC;
            var first_line_num = b07.FirstLineNum;
            //float[,] lon = new float[nline, ncol];
            for (int i = 0; i < nline; i++)
            {
                for (int j = 0; j < ncol; j++)
                {
                    var x = first_line_num + i;
                    var y = j;
                    var xx = DEG2RAD * (x - COFF) / (SCLUNIT * CFAC);
                    var yy = DEG2RAD * (y - LOFF) / (SCLUNIT * LFAC);
                    var Sd = Math.Sqrt(Math.Pow(DIS * Math.Cos(xx) * Math.Cos(yy), 2) -
                        (Math.Pow(Math.Cos(yy), 2) + CON * Math.Pow(Math.Sin(yy), 2)) * b03.ConstStd);
                    var Sn = (DIS * Math.Cos(xx) * Math.Cos(yy) - Sd) / (Math.Pow(Math.Cos(yy), 2)
                        + CON * Math.Pow(Math.Sin(yy), 2));
                    var S1 = DIS - Sn * Math.Cos(xx) * Math.Cos(yy);
                    var S2 = Sn * Math.Sin(xx) * Math.Cos(yy);
                    var S3 = -Sn * Math.Sin(yy);
                    var Sxy = Math.Sqrt(Math.Pow(S1, 2) + Math.Pow(S2, 2));
                    lon[i, j] = (float)(RAD2DEG * Math.Atan2(S2, S1) + b03.SubLon);
                    lat[i, j] = (float)(RAD2DEG * Math.Atan(CON * S3 / Sxy));
                }
            }
        }

        private static float[,] CalibrateIR(int[,] dn, Block05 b05, Block05IR? blockornull)
        {
            var cal = (Block05IR)blockornull;
            int xdim = dn.GetLength(0);
            int ydim = dn.GetLength(1);
            float[,] data = new float[xdim, ydim];
            var lam = b05.CentralWaveLength * 1e-6;
            var gain = b05.Gain;
            var constant = b05.Constant;
            var c0 = cal.c0;
            var c1 = cal.c1;
            var c2 = cal.c2;
            var const1 = cal.h * cal.c / (cal.k * lam);
            var const2 = 2 * cal.h * Math.Pow(cal.c, 2) * Math.Pow(lam, -5);
            for (int i = 0; i < xdim; i++)
            {
                for (int j = 0; j < ydim; j++)
                {
                    var I = (gain * dn[i, j] + constant) * 1e6;
                    var EBT = const1 / Log1p(const2 / I);
                    data[i, j] = (float)(c0 + c1 * EBT + c2 * Math.Pow(EBT, 2) - 273.15);
                }
            }
            return data;
        }

        private static float[,] CalibrateVIS(int[,] dn, Block05 b05, Block05VIS? blockornull)
        {
            var gain = b05.Gain;
            var constant = b05.Constant;
            var cal = (Block05VIS)blockornull;
            var c = cal.c;
            int xdim = dn.GetLength(0);
            int ydim = dn.GetLength(1);
            float[,] data = new float[xdim, ydim];
            for (int i = 0; i < xdim; i++)
            {
                for (int j = 0; j < ydim; j++)
                {
                    data[i, j] = (float)(c * gain * dn[i, j] + c * constant);
                }
            }
            return data;
        }
    }
}
