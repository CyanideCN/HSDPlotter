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
        public double[] data;
        public int ncol;
        public int nline;
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
            var b01 = ByteToType<Block01>(br);
            var b02 = ByteToType<Block02>(br);
            var b03 = ByteToType<Block03>(br);
            var b04 = ByteToType<Block04>(br);
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
            var b06 = ByteToType<Block06>(br);
            var b07 = ByteToType<Block07>(br);
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
            var npixels = ncol * nline;
            int[] dn = new int[npixels];
            for (int i = 0; i < npixels; i++)
            {
                dn[i] = br.ReadInt16();
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

        private static double[] CalibrateIR(int[] dn, Block05 b05, Block05IR? blockornull)
        {
            var cal = (Block05IR)blockornull;
            var length = dn.Length;
            double[] data = new double[length];
            var lam = b05.CentralWaveLength * 1e-6;
            var gain = b05.Gain;
            var constant = b05.Constant;
            var c0 = cal.c0;
            var c1 = cal.c1;
            var c2 = cal.c2;
            var const1 = cal.h * cal.c / (cal.k * lam);
            var const2 = 2 * cal.h * Math.Pow(cal.c, 2) * Math.Pow(lam, -5);
            for (int i = 0; i < length; i++)
            {
                var I = (gain * dn[i] + constant) * 1e6;
                var EBT = const1 / Log1p(const2 / I);
                data[i] = c0 + c1 * EBT + c2 * Math.Pow(EBT, 2) - 273.15;
            }
            return data;
        }

        private static double[] CalibrateVIS(int[] dn, Block05 b05, Block05VIS? blockornull)
        {
            var gain = b05.Gain;
            var constant = b05.Constant;
            var cal = (Block05VIS)blockornull;
            var c = cal.c;
            var length = dn.Length;
            double[] data = new double[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = c * gain * dn[i] + c * constant;
            }
            return data;
        }
    }
}
