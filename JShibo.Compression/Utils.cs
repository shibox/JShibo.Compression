using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JShibo.Compression
{
    internal sealed class CompressionUtils
    {
        internal static uint ReverseBits(uint n, int bits)
        {
            n = ((n & 0x0000FFFF) << 16) | (n >> 16);
            n = ((n & 0x00FF00FF) << 8) | ((n & 0xFF00FF00u) >> 8);
            n = ((n & 0x0F0F0F0F) << 4) | ((n & 0xF0F0F0F0u) >> 4);
            n = ((n & 0x33333333) << 2) | ((n & 0xCCCCCCCCu) >> 2);
            n = ((n & 0x55555555) << 1) | ((n & 0xAAAAAAAAu) >> 1);
            return n >> (32 - bits);
        }

        /// <summary>
        /// 用于计算数据校验和，相比CRC32、有SHA128等具有更高的效率，但可靠性只比它们差一点。
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static unsafe int Adler32(byte[] bytes, int offset, int length)
        {
            if (bytes == null || length == 0)
                return 1;
            uint s1 = 1;
            uint s2 = 0;
            fixed (byte* pBytes = &bytes[offset])
            {
                byte* p = pBytes;
                while (length > 0)
                {
                    int k = length < 5552 ? length : 5552;
                    length -= k;
                    while (k >= 16)
                    {
                        s1 += *p; s2 += s1;
                        s1 += *(p + 1); s2 += s1;
                        s1 += *(p + 2); s2 += s1;
                        s1 += *(p + 3); s2 += s1;
                        s1 += *(p + 4); s2 += s1;
                        s1 += *(p + 5); s2 += s1;
                        s1 += *(p + 6); s2 += s1;
                        s1 += *(p + 7); s2 += s1;
                        s1 += *(p + 8); s2 += s1;
                        s1 += *(p + 9); s2 += s1;
                        s1 += *(p + 10); s2 += s1;
                        s1 += *(p + 11); s2 += s1;
                        s1 += *(p + 12); s2 += s1;
                        s1 += *(p + 13); s2 += s1;
                        s1 += *(p + 14); s2 += s1;
                        s1 += *(p + 15); s2 += s1;
                        p += 16;
                        k -= 16;
                    }
                    while (k > 0)
                    {
                        s1 += *p; s2 += s1;
                        k--;
                        p++;
                    }
                    s1 %= 65521u;
                    s2 %= 65521u;
                }
            }
            return (int)(s1 | (s2 << 16));
        }

        /// <summary>
        /// 因为数据存在交叉的情况，索引不能整体拷贝，否则会出错。因为两个指针可能是同一个数据源，数据存在交错的情况
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="pd"></param>
        /// <param name="length"></param>
        internal static unsafe void CopyBytes(byte* ps, byte* pd, int length)
        {
            while (length >= 16)
            {
                pd[0] = ps[0];
                pd[1] = ps[1];
                pd[2] = ps[2];
                pd[3] = ps[3];
                pd[4] = ps[4];
                pd[5] = ps[5];
                pd[6] = ps[6];
                pd[7] = ps[7];
                pd[8] = ps[8];
                pd[9] = ps[9];
                pd[10] = ps[10];
                pd[11] = ps[11];
                pd[12] = ps[12];
                pd[13] = ps[13];
                pd[14] = ps[14];
                pd[15] = ps[15];
                length -= 16;
                ps += 16;
                pd += 16;
            }
            while (length >= 4)
            {
                pd[0] = ps[0];
                pd[1] = ps[1];
                pd[2] = ps[2];
                pd[3] = ps[3];
                length -= 4;
                ps += 4;
                pd += 4;
            }
            if (length == 1)
            {
                pd[0] = ps[0];
            }
            else if (length == 2)
            {
                pd[0] = ps[0];
                pd[1] = ps[1];
            }
            else
            {
                pd[0] = ps[0];
                pd[1] = ps[1];
                pd[2] = ps[2];
            }
        }

        /// <summary>
        /// 在某些情况下，可以整体拷贝数据，性能差不多如以下提升
        /// 至于是否要将while改为for，性能是否有提升，不太容易测试出来
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="pd"></param>
        /// <param name="length"></param>
        internal static unsafe void CopyBytesBlock(byte* ps, byte* pd, int length)
        {
            while (length >= 16)
            {
                //以下代码在三九健康网的数据测试中性能提升22.5%
                *((uint*)pd) = *((uint*)ps);
                *((uint*)(pd + 4)) = *((uint*)(ps + 4));
                *((uint*)(pd + 8)) = *((uint*)(ps + 8));
                *((uint*)(pd + 12)) = *((uint*)(ps + 12));

                length -= 16;
                ps += 16;
                pd += 16;
            }

            ////以下代码在三九健康网的数据测试中使用For循环性能提升1.5%
            //int step = length >> 4;
            //for (int i = 0; i < step; i++)
            //{
            //    //以下代码在三九健康网的数据测试中性能提升22.5%
            //    *((int*)pd) = *((int*)ps);
            //    *((int*)(pd + 4)) = *((int*)(ps + 4));
            //    *((int*)(pd + 8)) = *((int*)(ps + 8));
            //    *((int*)(pd + 12)) = *((int*)(ps + 12));

            //    length -= 16;
            //    ps += 16;
            //    pd += 16;
            //}

            if ((length & 8) != 0)
            {
                *((uint*)pd) = *((uint*)ps);
                *((uint*)(pd + 4)) = *((uint*)(ps + 4));
                pd += 8;
                ps += 8;
            }
            if ((length & 4) != 0)
            {
                *((uint*)pd) = *((uint*)ps);
                pd += 4;
                ps += 4;
            }
            if ((length & 2) != 0)
            {
                *((ushort*)pd) = *((ushort*)ps);
                pd += 2;
                ps += 2;
            }
            if ((length & 1) != 0)
            {
                *pd = *ps;
            }
        }

        /// <summary>
        /// 性能还需要使用整体拷贝优化，代码已经修改，准确性还未测试
        /// </summary>
        /// <param name="value"></param>
        /// <param name="p"></param>
        /// <param name="length"></param>
        internal static unsafe void Fill(int value, int* p, int length)
        {
            //据测试，在跑pv数据压缩的时候会出现数组越界的bug，目前不能使用
            //while (length >= 16)
            //{
            //    *((int*)p) = value;
            //    *((int*)(p + 1)) = value;
            //    *((int*)(p + 2)) = value;
            //    *((int*)(p + 3)) = value;
            //    *((int*)(p + 4)) = value;
            //    *((int*)(p + 5)) = value;
            //    *((int*)(p + 6)) = value;
            //    *((int*)(p + 7)) = value;
            //    *((int*)(p + 8)) = value;
            //    *((int*)(p + 9)) = value;
            //    *((int*)(p + 10)) = value;
            //    *((int*)(p + 11)) = value;
            //    *((int*)(p + 12)) = value;
            //    *((int*)(p + 13)) = value;
            //    *((int*)(p + 14)) = value;
            //    *((int*)(p + 15)) = value;
            //    length -= 16;
            //    p += 16;
            //}
            //while (length >= 4)
            //{
            //    *((int*)p) = value;
            //    *((int*)(p + 1)) = value;
            //    *((int*)(p + 2)) = value;
            //    *((int*)(p + 3)) = value;
            //    length -= 4;
            //    p += 4;
            //}
            //if (length == 1)
            //{
            //    *((int*)p) = value;  
            //}
            //else if (length == 2)
            //{
            //    *((int*)p) = value;
            //    *((int*)(p + 1)) = value;
            //}
            //else
            //{
            //    *((int*)p) = value;
            //    *((int*)(p + 1)) = value;
            //    *((int*)(p + 2)) = value;
            //}

            #region old
            while (length >= 16)
            {
                p[0] = value;
                p[1] = value;
                p[2] = value;
                p[3] = value;
                p[4] = value;
                p[5] = value;
                p[6] = value;
                p[7] = value;
                p[8] = value;
                p[9] = value;
                p[10] = value;
                p[11] = value;
                p[12] = value;
                p[13] = value;
                p[14] = value;
                p[15] = value;
                length -= 16;
                p += 16;
            }
            while (length >= 4)
            {
                p[0] = value;
                p[1] = value;
                p[2] = value;
                p[3] = value;
                length -= 4;
                p += 4;
            }
            if (length < 2)
            {
                if (length == 1)
                    p[0] = value;
            }
            else if (length == 2)
            {
                p[0] = value;
                p[1] = value;
            }
            else
            {
                p[0] = value;
                p[1] = value;
                p[2] = value;
            }
            #endregion
        }

        private CompressionUtils() { }
    }

    /// <summary>
    /// 压缩选项
    /// </summary>
    [Serializable]
    public enum CompressionLevel
    {
        /// <summary>
        /// 最快的压缩
        /// </summary>
        Fastest,
        /// <summary>
        /// 比较快的压缩。
        /// </summary>
        Fast,
        /// <summary>
        /// 标准的压缩
        /// </summary>
        Normal,
        /// <summary>
        /// 压缩率最大的压缩。
        /// </summary>
        Maximum
    }

    /// <summary>
    /// 内部常量
    /// </summary>
    internal sealed class Consts
    {
        internal const int
            ChunkShift = 17,
            ChunkCapacity = 32768,
            BlockSize = 8192,
            MaxLength = 17010,
            MaxDistance = 524288,
            InitPosValue = -(MaxDistance + 1),
            CharCount = 320,
            FirstLengthChar = 256,
            FirstCharWithExBit = 272,
            CharTreeSize = CharCount * 2,
            DistCount = 64,
            FirstDistWithExBit = 5,
            DistTreeSize = DistCount * 2,
            MaxBits = 14,
            ChLenCount = 20,
            ChLenTreeSize = ChLenCount * 2,
            MaxChLenBits = 7;

        internal static readonly int[] CharExBitLength = new int[]
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 6, 6, 8, 14
        };

        internal static readonly int[] CharExBitBase = new int[]
        {
            19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 55, 59, 63,
            67, 71, 75, 79, 83, 87, 91, 95, 99, 103, 107, 111, 115, 123, 131, 139, 147, 155,
            163, 171, 179, 195, 211, 227, 243, 307, 371, 627
        };

        internal static readonly int[] DistExBitLength = new int[]
        {
            0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5,
            6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10, 11, 11, 11, 11, 12,
            12, 12, 12, 14, 14, 15, 15, 16, 16, 17, 17
        };

        internal static readonly int[] DistExBitBase = new int[]
        {
            0, 0, 0, 1, 2, 3, 5, 7, 9, 11, 13, 15, 17, 21, 25, 29, 33, 41, 49, 57, 65, 81, 97, 113,
            129, 161, 193, 225, 257, 321, 385, 449, 513, 641, 769, 897, 1025, 1281, 1537, 1793,
            2049, 2561, 3073, 3585, 4097, 5121, 6145, 7169, 8193, 10241, 12289, 14337, 16385, 20481,
            24577, 28673, 32769, 49153, 65537, 98305, 131073, 196609, 262145, 393217
        };

        internal static readonly int[] ChLenExBitLength = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 7
        };

        private Consts() { }
    }


    /// <summary>
    /// 异常处理
    /// </summary>
    public class CompressException : Exception
    {
        internal static void ThrowArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        internal static void ThrowNoPlaceToStoreCompressedDataException()
        {
            throw new CompressException("没有足够的空间存储压缩数据。");
        }

        internal static void ThrowNoPlaceToStoreDecompressedDataException()
        {
            throw new CompressException("没有足够的空间存储解压缩数据。");
        }

        internal static void ThrowNoPlaceToCopyDataException()
        {
            throw new CompressException("没有足够的空间拷贝数组，请检查偏移是否正确。");
        }

        internal static void ThrowReadBeyondTheEndException()
        {
            throw new CompressException("读出数据出错。");
        }

        private CompressException(string message)
            : base(message)
        {
        }
    }
}
