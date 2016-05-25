using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JShibo.Compression
{
    /// <summary>
    /// 解压缩类
    /// </summary>
    public sealed class ShiboDecompress 
    {
        #region 字段

        private static ShiboDecompress _instance;

        private int _srcIndex;
        private int _break32Offset;
        private int _breakOffset;
        private int _hold;
        private int _bits;
        private int _outCounter;
        private int _inCounter;
        private int _r0;
        private int _r1;
        private int _r2;

        private unsafe byte* _pSrcBytes;
        private unsafe byte* _pDstBytes;

        private unsafe int* _pCharTree;
        private unsafe int* _pDistTree;
        private unsafe int* _pChLenTree;
        private unsafe int* _pBitLen;
        private unsafe int* _pBitCount;
        private unsafe int* _pNextCode;

        private int[] _charTree;
        private int[] _distTree;
        private int[] _chLenTree;
        private int[] _bitLen;
        private int[] _bitCount;
        private int[] _nextCode;

        private unsafe int* _pCharExBitLength;
        private unsafe int* _pCharExBitBase;
        private unsafe int* _pDistExBitLength;
        private unsafe int* _pDistExBitBase;

        #endregion

        #region 属性

        public static ShiboDecompress Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ShiboDecompress();
                return _instance;
            }
        }

        #endregion

        #region 构造函数

        public ShiboDecompress()
        {
            _charTree = new int[Consts.CharTreeSize];
            _distTree = new int[Consts.DistTreeSize];
            _chLenTree = new int[Consts.ChLenTreeSize];
            _bitLen = new int[Consts.CharCount];
            _bitCount = new int[Consts.MaxBits + 1];
            _nextCode = new int[Consts.MaxBits + 1];
        }

        #endregion

        #region 内部方法

        private unsafe int GetNBits(int n)
        {
            //int hold = _hold;
            //int bits = _bits;
            ////使用For循环，39健康网测试数据中性能提升约1%
            //for (; bits < n; )
            //{
            //    if (bits < 8 && _srcIndex < _break32Offset)
            //    {
            //        _srcIndex += 3;
            //        hold |= (_pSrcBytes[0] | (_pSrcBytes[1] << 8) | (_pSrcBytes[2] << 16)) << bits;
            //        _pSrcBytes += 3;
            //        bits += 24;
            //    }
            //    else if (_srcIndex < _breakOffset)
            //    {
            //        _srcIndex++;
            //        hold |= (*_pSrcBytes) << bits;
            //        _pSrcBytes++;
            //        bits += 8;
            //    }
            //    else
            //        CompressException.ThrowReadBeyondTheEndException();
            //}
            //_hold = hold >> n;
            //_bits = bits - n;
            //return hold & ((1 << n) - 1);

            int hold = _hold;
            int bits = _bits;
            while (bits < n)
                if (bits < 8 && _srcIndex < _break32Offset)
                {
                    _srcIndex += 3;
                    hold |= (_pSrcBytes[0] | (_pSrcBytes[1] << 8) | (_pSrcBytes[2] << 16)) << bits;
                    _pSrcBytes += 3;
                    bits += 24;
                }
                else if (_srcIndex < _breakOffset)
                {
                    _srcIndex++;
                    hold |= (*_pSrcBytes) << bits;
                    _pSrcBytes++;
                    bits += 8;
                }
                else
                    CompressException.ThrowReadBeyondTheEndException();
            _hold = hold >> n;
            _bits = bits - n;
            return hold & ((1 << n) - 1);
        }

        private unsafe int GetBit()
        {
            uint hold = (uint)_hold;
            int bits = _bits;
            if (bits != 0)
                bits--;
            else if (_srcIndex < _break32Offset)
            {
                _srcIndex += 4;
                hold = *((uint*)_pSrcBytes);
                _pSrcBytes += 4;
                bits = 31;
            }
            else if (_srcIndex < _breakOffset)
            {
                _srcIndex++;
                hold = (uint)(*_pSrcBytes);
                _pSrcBytes++;
                bits = 7;
            }
            else
                CompressException.ThrowReadBeyondTheEndException();
            _hold = (int)(hold >> 1);
            _bits = bits;
            return (int)(hold & 1);
        }

        private unsafe int GetCode(int* tree)
        {
            //    int code = 1;
            //    int hold = _hold;
            //    do
            //    {
            //        for (; _bits != 0; )
            //        {
            //            code = tree[code + (hold & 1)];
            //            hold >>= 1;
            //            _bits--;
            //            if (code <= 0)
            //                goto CodeFound;
            //        }
            //        if (_srcIndex < _break32Offset)
            //        {
            //            _srcIndex += 4;
            //            hold = *((int*)_pSrcBytes);
            //            _pSrcBytes += 4;
            //            code = tree[code + (hold & 1)];
            //            hold = (int)((uint)hold >> 1);
            //            _bits = 31;
            //        }
            //        else if (_srcIndex < _breakOffset)
            //        {
            //            _srcIndex++;
            //            hold = (int)(*_pSrcBytes);
            //            _pSrcBytes++;
            //            code = tree[code + (hold & 1)];
            //            hold >>= 1;
            //            _bits = 7;
            //        }
            //        else
            //            CompressException.ThrowReadBeyondTheEndException();
            //    } while (code > 0);
            //CodeFound:
            //    _hold = hold;
            //    return -code;



            int code = 1;
            int hold = _hold;
            do
            {
                while (_bits != 0)
                {
                    code = tree[code + (hold & 1)];
                    hold >>= 1;
                    _bits--;
                    if (code <= 0)
                        goto CodeFound;
                }
                if (_srcIndex < _break32Offset)
                {
                    _srcIndex += 4;
                    hold = *((int*)_pSrcBytes);
                    _pSrcBytes += 4;
                    code = tree[code + (hold & 1)];
                    hold = (int)((uint)hold >> 1);
                    _bits = 31;
                }
                else if (_srcIndex < _breakOffset)
                {
                    _srcIndex++;
                    hold = (int)(*_pSrcBytes);
                    _pSrcBytes++;
                    code = tree[code + (hold & 1)];
                    hold >>= 1;
                    _bits = 7;
                }
                else
                    CompressException.ThrowReadBeyondTheEndException();
            } while (code > 0);
            CodeFound:
            _hold = hold;
            return -code;
        }

        private unsafe void LoadChLenTree()
        {
            int n, m, p, i, code;
            CompressionUtils.Fill(0, _pBitCount, Consts.MaxChLenBits + 1);
            for (i = 0; i < Consts.ChLenCount; i++)
            {
                n = GetNBits(3);
                _pBitLen[i] = n;
                _pBitCount[n]++;
            }
            CompressionUtils.Fill(0, _pChLenTree, Consts.ChLenTreeSize);
            _pNextCode[1] = 0;
            _pNextCode[2] = n = _pBitCount[1] << 1;
            _pNextCode[3] = n = (n + _pBitCount[2]) << 1;
            _pNextCode[4] = n = (n + _pBitCount[3]) << 1;
            _pNextCode[5] = n = (n + _pBitCount[4]) << 1;
            _pNextCode[6] = n = (n + _pBitCount[5]) << 1;
            _pNextCode[7] = n = (n + _pBitCount[6]) << 1;
            int treeLen = 2;
            for (i = 0; i < Consts.ChLenCount; i++)
            {
                n = _pBitLen[i];
                if (n == 0)
                    continue;
                m = _pNextCode[n];
                code = (int)CompressionUtils.ReverseBits((uint)m, n);
                _pNextCode[n] = m + 1;
                p = 1;
                while (true)
                {
                    p += code & 1;
                    code >>= 1;
                    n--;
                    if (n != 0)
                    {
                        m = p;
                        p = _pChLenTree[p];
                        if (p == 0)
                        {
                            p = treeLen + 1;
                            treeLen = p + 1;
                            _pChLenTree[m] = p;
                        }
                    }
                    else
                    {
                        _pChLenTree[p] = -i;
                        break;
                    }
                }
            }
        }

        private unsafe void LoadCharDistLengths(int count)
        {
            //int c, lastLen = 0;
            //int* p = _pBitLen;
            //CompressionUtil.Fill(0, _pBitCount, Consts.MaxBits + 1);
            //for (; count > 0; )
            //{
            //    c = GetCode(_pChLenTree);
            //    if (c < 15)
            //    {
            //        *p = c;
            //        _pBitCount[c]++;
            //        p++;
            //        lastLen = c;
            //        count--;
            //    }
            //    else
            //    {
            //        if (c < 17)
            //        {
            //            if (c == 15)
            //                c = 2;
            //            else
            //                c = GetBit() + 3;
            //        }
            //        else if (c == 17)
            //            c = GetNBits(2) + 5;
            //        else if (c == 18)
            //            c = GetNBits(3) + 9;
            //        else
            //            c = GetNBits(7) + 17;
            //        count -= c;
            //        _pBitCount[lastLen] += c;

            //        //for (; c > 0; )
            //        //{
            //        //    c--;
            //        //    *p = lastLen;
            //        //    p++;
            //        //}

            //        do
            //        {
            //            c--;
            //            *p = lastLen;
            //            p++;
            //        } while (c != 0);
            //    }
            //}


            int c, lastLen = 0;
            int* p = _pBitLen;
            CompressionUtils.Fill(0, _pBitCount, Consts.MaxBits + 1);
            while (count > 0)
            {
                c = GetCode(_pChLenTree);
                if (c < 15)
                {
                    *p = c;
                    _pBitCount[c]++;
                    p++;
                    lastLen = c;
                    count--;
                }
                else
                {
                    if (c < 17)
                    {
                        if (c == 15)
                            c = 2;
                        else
                            c = GetBit() + 3;
                    }
                    else if (c == 17)
                        c = GetNBits(2) + 5;
                    else if (c == 18)
                        c = GetNBits(3) + 9;
                    else
                        c = GetNBits(7) + 17;
                    count -= c;
                    _pBitCount[lastLen] += c;
                    do
                    {
                        c--;
                        *p = lastLen;
                        p++;
                    } while (c != 0);
                }
            }
        }

        private unsafe void LoadCharTree()
        {
            int n, m, p, code;
            CompressionUtils.Fill(0, _pBitLen, Consts.CharCount);
            LoadCharDistLengths(GetNBits(6) + 257);
            CompressionUtils.Fill(0, _pCharTree, Consts.CharTreeSize);
            _pNextCode[1] = 0;
            _pNextCode[2] = n = _pBitCount[1] << 1;
            _pNextCode[3] = n = (n + _pBitCount[2]) << 1;
            _pNextCode[4] = n = (n + _pBitCount[3]) << 1;
            _pNextCode[5] = n = (n + _pBitCount[4]) << 1;
            _pNextCode[6] = n = (n + _pBitCount[5]) << 1;
            _pNextCode[7] = n = (n + _pBitCount[6]) << 1;
            _pNextCode[8] = n = (n + _pBitCount[7]) << 1;
            _pNextCode[9] = n = (n + _pBitCount[8]) << 1;
            _pNextCode[10] = n = (n + _pBitCount[9]) << 1;
            _pNextCode[11] = n = (n + _pBitCount[10]) << 1;
            _pNextCode[12] = n = (n + _pBitCount[11]) << 1;
            _pNextCode[13] = n = (n + _pBitCount[12]) << 1;
            _pNextCode[14] = n = (n + _pBitCount[13]) << 1;
            int treeLen = 2;
            for (int i = 0; i < Consts.CharCount; i++)
            {
                n = _pBitLen[i];
                if (n == 0)
                    continue;
                m = _pNextCode[n];
                code = (int)CompressionUtils.ReverseBits((uint)m, n);
                _pNextCode[n] = m + 1;
                p = 1;
                while (true)
                {
                    p += code & 1;
                    code >>= 1;
                    n--;
                    if (n != 0)
                    {
                        m = p;
                        p = _pCharTree[p];
                        if (p == 0)
                        {
                            p = treeLen + 1;
                            treeLen = p + 1;
                            _pCharTree[m] = p;
                        }
                    }
                    else
                    {
                        _pCharTree[p] = -i;
                        break;
                    }
                }
            }
        }

        private unsafe void LoadDistTree()
        {
            int n, m, p, code;
            CompressionUtils.Fill(0, _pBitLen, Consts.DistCount);
            LoadCharDistLengths(GetNBits(6) + 1);
            CompressionUtils.Fill(0, _pDistTree, Consts.DistTreeSize);
            _pNextCode[1] = 0;
            _pNextCode[2] = n = _pBitCount[1] << 1;
            _pNextCode[3] = n = (n + _pBitCount[2]) << 1;
            _pNextCode[4] = n = (n + _pBitCount[3]) << 1;
            _pNextCode[5] = n = (n + _pBitCount[4]) << 1;
            _pNextCode[6] = n = (n + _pBitCount[5]) << 1;
            _pNextCode[7] = n = (n + _pBitCount[6]) << 1;
            _pNextCode[8] = n = (n + _pBitCount[7]) << 1;
            _pNextCode[9] = n = (n + _pBitCount[8]) << 1;
            _pNextCode[10] = n = (n + _pBitCount[9]) << 1;
            _pNextCode[11] = n = (n + _pBitCount[10]) << 1;
            _pNextCode[12] = n = (n + _pBitCount[11]) << 1;
            _pNextCode[13] = n = (n + _pBitCount[12]) << 1;
            _pNextCode[14] = n = (n + _pBitCount[13]) << 1;
            int treeLen = 2;
            for (int i = 0; i < Consts.DistCount; i++)
            {
                n = _pBitLen[i];
                if (n == 0)
                    continue;
                m = _pNextCode[n];
                code = (int)CompressionUtils.ReverseBits((uint)m, n);
                _pNextCode[n] = m + 1;
                p = 1;
                while (true)
                {
                    p += code & 1;
                    code >>= 1;
                    n--;
                    if (n != 0)
                    {
                        m = p;
                        p = _pDistTree[p];
                        if (p == 0)
                        {
                            p = treeLen + 1;
                            treeLen = p + 1;
                            _pDistTree[m] = p;
                        }
                    }
                    else
                    {
                        _pDistTree[p] = -i;
                        break;
                    }
                }
            }
        }

        private unsafe void ReadBlockHeader()
        {
            _inCounter = Consts.BlockSize;
            if (GetBit() == 0)
                ReadNonCompressedBlock();
            else
            {
                LoadChLenTree();
                LoadCharTree();
                LoadDistTree();
            }
        }

        private unsafe void ReadNonCompressedBlock()
        {
            //_inCounter += GetNBits(8);
            //int bits = _bits;
            //for (; _inCounter > 0 && _outCounter > 0; )
            //{
            //    int hold = _hold;
            //    if (bits < 8)
            //        if (_srcIndex < _break32Offset)
            //        {
            //            _srcIndex += 3;
            //            hold |= (_pSrcBytes[0] | (_pSrcBytes[1] << 8) | (_pSrcBytes[2] << 16)) << bits;
            //            _pSrcBytes += 3;
            //            bits += 24;
            //        }
            //        else if (_srcIndex < _breakOffset)
            //        {
            //            _srcIndex++;
            //            hold |= (*_pSrcBytes) << bits;
            //            _pSrcBytes++;
            //            bits += 8;
            //        }
            //        else
            //            CompressException.ThrowReadBeyondTheEndException();
            //    _hold = hold >> 8;
            //    bits -= 8;
            //    *_pDstBytes = (byte)hold;
            //    _inCounter--;
            //    _outCounter--;
            //    _pDstBytes++;
            //}
            //_bits = bits;


            _inCounter += GetNBits(8);
            int bits = _bits;
            while (_inCounter > 0 && _outCounter > 0)
            {
                int hold = _hold;
                if (bits < 8)
                    if (_srcIndex < _break32Offset)
                    {
                        _srcIndex += 3;
                        hold |= (_pSrcBytes[0] | (_pSrcBytes[1] << 8) | (_pSrcBytes[2] << 16)) << bits;
                        _pSrcBytes += 3;
                        bits += 24;
                    }
                    else if (_srcIndex < _breakOffset)
                    {
                        _srcIndex++;
                        hold |= (*_pSrcBytes) << bits;
                        _pSrcBytes++;
                        bits += 8;
                    }
                    else
                        CompressException.ThrowReadBeyondTheEndException();
                _hold = hold >> 8;
                bits -= 8;
                *_pDstBytes = (byte)hold;
                _inCounter--;
                _outCounter--;
                _pDstBytes++;
            }
            _bits = bits;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 根据压缩后的数据获得解压后的长度。
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <param name="sourceIndex"></param>
        /// <returns></returns>
        public static unsafe int GetDecompressedLength(byte[] source, int sourceIndex)
        {
            if (source == null)
                CompressException.ThrowArgumentNullException("sourceBytes");
            int byteCount;
            fixed (byte* pSrcBytes = &source[sourceIndex])
                byteCount = *((int*)pSrcBytes);
            if (byteCount < 0)
                byteCount = -byteCount;
            return byteCount;
        }

        /// <summary>
        /// 解压后直接返回一个字节数组。
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="beforeGap"></param>
        /// <param name="afterGap"></param>
        /// <returns></returns>
        public unsafe byte[] Decompress(byte[] source, int sourceIndex, int beforeGap, int afterGap)
        {
            //if (source == null)
            //    CompressException.ThrowArgumentNullException("sourceBytes");
            //int byteCount;
            //fixed (byte* pSrcBytes = &source[sourceIndex])
            //    byteCount = *((int*)pSrcBytes);
            //if (byteCount < 0)
            //    byteCount = -byteCount;

            int byteCount = GetDecompressedLength(source, sourceIndex);

            byte[] result = new byte[byteCount + beforeGap + afterGap];
            if (byteCount != 0)
                Decompress(source, sourceIndex, result, beforeGap);
            return result;
        }

        /// <summary>
        /// 返回一个解压后的数组，如果空间不足，会出现异常。
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="result"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public unsafe int Decompress(byte[] source, int sourceIndex, byte[] result, int offset)
        {
            if (source == null)
                CompressException.ThrowArgumentNullException("sourceBytes");
            if (result == null)
                return GetDecompressedLength(source, sourceIndex);
            fixed (byte* pSrcBytes = &source[sourceIndex], pDstBytes = &result[offset])
            {
                _pSrcBytes = pSrcBytes;
                _pDstBytes = pDstBytes;
                int byteCount = *((int*)_pSrcBytes);
                if (byteCount <= 0)
                {
                    byteCount = -byteCount;
                    if (result.Length - offset < byteCount)
                        CompressException.ThrowNoPlaceToStoreDecompressedDataException();
                    if (byteCount > 0)
                        Buffer.BlockCopy(source, sourceIndex + 4, result, offset, byteCount);
                    return byteCount;
                }
                if (result.Length - offset < byteCount)
                    CompressException.ThrowNoPlaceToStoreDecompressedDataException();
                fixed (int* pCharTree = &_charTree[0], pDistTree = &_distTree[0],
                           pChLenTree = &_chLenTree[0], pBitLen = &_bitLen[0],
                           pBitCount = &_bitCount[0], pNextCode = &_nextCode[0],
                           pCharExBitLength = &Consts.CharExBitLength[0],
                           pCharExBitBase = &Consts.CharExBitBase[0],
                           pDistExBitLength = &Consts.DistExBitLength[0],
                           pDistExBitBase = &Consts.DistExBitBase[0])
                {
                    _pCharTree = pCharTree;
                    _pDistTree = pDistTree;
                    _pChLenTree = pChLenTree;
                    _pBitLen = pBitLen;
                    _pBitCount = pBitCount;
                    _pNextCode = pNextCode;
                    _pCharExBitLength = pCharExBitLength;
                    _pCharExBitBase = pCharExBitBase;
                    _pDistExBitLength = pDistExBitLength;
                    _pDistExBitBase = pDistExBitBase;
                    _bits = 0;
                    _hold = 0;
                    _srcIndex = sourceIndex + 4;
                    _pSrcBytes += 4;
                    _breakOffset = source.Length;
                    _break32Offset = _breakOffset - 3;
                    int length, distance;
                    _outCounter = byteCount;
                    while (_outCounter > 0)
                    {
                        ReadBlockHeader();
                        while (_inCounter > 0 && _outCounter > 0)
                        {
                            int c = GetCode(_pCharTree);
                            _inCounter--;
                            if (c < Consts.FirstLengthChar)
                            {
                                *_pDstBytes = (byte)c;
                                _outCounter--;
                                _pDstBytes++;
                            }
                            else
                            {
                                c -= Consts.FirstCharWithExBit;
                                if (c < 0)
                                    length = c + 19;
                                else
                                    length = GetNBits(_pCharExBitLength[c]) + _pCharExBitBase[c];
                                c = GetCode(_pDistTree);
                                if (c < 3)
                                {
                                    if (c == 0)
                                        distance = _r0;
                                    else if (c == 1)
                                    {
                                        distance = _r1;
                                        _r1 = _r0;
                                        _r0 = distance;
                                    }
                                    else
                                    {
                                        distance = _r2;
                                        _r2 = _r0;
                                        _r0 = distance;
                                    }
                                }
                                else
                                {
                                    distance = _pDistExBitBase[c];
                                    if (c >= Consts.FirstDistWithExBit)
                                    {
                                        distance += GetNBits(_pDistExBitLength[c]);
                                        _r2 = _r1;
                                        _r1 = _r0;
                                        _r0 = distance;
                                    }
                                }
                                if (distance > length)// && length >= 16)
                                    CompressionUtils.CopyBytesBlock(_pDstBytes - distance, _pDstBytes, length);
                                else
                                    CompressionUtils.CopyBytes(_pDstBytes - distance, _pDstBytes, length);
                                _outCounter -= length;
                                _pDstBytes += length;
                            }
                        }
                    }
                }
                return byteCount;
            }

            #region old
            //if (source == null)
            //    CompressException.ThrowArgumentNullException("sourceBytes");
            //if (result == null)
            //    return GetDecompressedLength(source, sourceIndex);
            //fixed (byte* pSrcBytes = &source[sourceIndex], pDstBytes = &result[offset])
            //{
            //    _pSrcBytes = pSrcBytes;
            //    _pDstBytes = pDstBytes;
            //    int byteCount = *((int*)_pSrcBytes);
            //    if (byteCount <= 0)
            //    {
            //        byteCount = -byteCount;
            //        if (result.Length - offset < byteCount)
            //            CompressException.ThrowNoPlaceToStoreDecompressedDataException();
            //        if (byteCount > 0)
            //            Buffer.BlockCopy(source, sourceIndex + 4, result, offset, byteCount);
            //        return byteCount;
            //    }
            //    if (result.Length - offset < byteCount)
            //        CompressException.ThrowNoPlaceToStoreDecompressedDataException();
            //    fixed (int* pCharTree = &_charTree[0], pDistTree = &_distTree[0],
            //               pChLenTree = &_chLenTree[0], pBitLen = &_bitLen[0],
            //               pBitCount = &_bitCount[0], pNextCode = &_nextCode[0],
            //               pCharExBitLength = &Consts.CharExBitLength[0],
            //               pCharExBitBase = &Consts.CharExBitBase[0],
            //               pDistExBitLength = &Consts.DistExBitLength[0],
            //               pDistExBitBase = &Consts.DistExBitBase[0])
            //    {
            //        _pCharTree = pCharTree;
            //        _pDistTree = pDistTree;
            //        _pChLenTree = pChLenTree;
            //        _pBitLen = pBitLen;
            //        _pBitCount = pBitCount;
            //        _pNextCode = pNextCode;
            //        _pCharExBitLength = pCharExBitLength;
            //        _pCharExBitBase = pCharExBitBase;
            //        _pDistExBitLength = pDistExBitLength;
            //        _pDistExBitBase = pDistExBitBase;
            //        _bits = 0;
            //        _hold = 0;
            //        _srcIndex = sourceIndex + 4;
            //        _pSrcBytes += 4;
            //        _breakOffset = source.Length;
            //        _break32Offset = _breakOffset - 3;
            //        int length, distance;
            //        _outCounter = byteCount;
            //        while (_outCounter > 0)
            //        {
            //            ReadBlockHeader();
            //            while (_inCounter > 0 && _outCounter > 0)
            //            {
            //                int c = GetCode(_pCharTree);
            //                _inCounter--;
            //                if (c < Consts.FirstLengthChar)
            //                {
            //                    *_pDstBytes = (byte)c;
            //                    _outCounter--;
            //                    _pDstBytes++;
            //                }
            //                else
            //                {
            //                    c -= Consts.FirstCharWithExBit;
            //                    if (c < 0)
            //                        length = c + 19;
            //                    else
            //                        length = GetNBits(_pCharExBitLength[c]) + _pCharExBitBase[c];
            //                    c = GetCode(_pDistTree);
            //                    if (c < 3)
            //                    {
            //                        if (c == 0)
            //                            distance = _r0;
            //                        else if (c == 1)
            //                        {
            //                            distance = _r1;
            //                            _r1 = _r0;
            //                            _r0 = distance;
            //                        }
            //                        else
            //                        {
            //                            distance = _r2;
            //                            _r2 = _r0;
            //                            _r0 = distance;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        distance = _pDistExBitBase[c];
            //                        if (c >= Consts.FirstDistWithExBit)
            //                        {
            //                            distance += GetNBits(_pDistExBitLength[c]);
            //                            _r2 = _r1;
            //                            _r1 = _r0;
            //                            _r0 = distance;
            //                        }
            //                    }
            //                    CompressionUtil.CopyBytes(_pDstBytes - distance, _pDstBytes, length);
            //                    _outCounter -= length;
            //                    _pDstBytes += length;
            //                }
            //            }
            //        }
            //    }
            //    return byteCount;
            //}
            #endregion
        }

        public unsafe int Decompress(byte[] source, int sourceIndex, ref byte[] result)
        {
            return Decompress(source, sourceIndex, source.Length - sourceIndex, ref result, 0);
        }

        public unsafe int Decompress(byte[] source, int sourceIndex, int count, ref byte[] result)
        {
            return Decompress(source, sourceIndex, count, ref result, 0);
        }

        public unsafe int Decompress(byte[] source, int sourceIndex, int count, ref byte[] result, int offset)
        {
            int byteCount = GetDecompressedLength(source, sourceIndex);

            if (result == null)
            {
                offset = 0;
                result = new byte[byteCount];
            }
            else if (result.Length < byteCount + offset)
            {
                if (offset > result.Length)
                    CompressException.ThrowNoPlaceToCopyDataException();
                if (offset > 0)
                {
                    byte[] buffer = new byte[offset];
                    Buffer.BlockCopy(result, 0, buffer, 0, buffer.Length);
                    result = null;
                    result = new byte[byteCount + offset];
                    Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                    buffer = null;
                }
                else
                {
                    result = null;
                    result = new byte[byteCount + offset];
                }
            }
            if (byteCount != 0)
                byteCount = Decompress(source, sourceIndex, count, result, offset);
            return byteCount;
        }

        /// <summary>
        /// 解压缩指定位置和长度
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="count"></param>
        /// <param name="result"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private unsafe int Decompress(byte[] source, int sourceIndex, int count, byte[] result, int offset)
        {
            if (source == null)
                CompressException.ThrowArgumentNullException("sourceBytes");
            if (result == null)
                return GetDecompressedLength(source, sourceIndex);
            fixed (byte* pSrcBytes = &source[sourceIndex], pDstBytes = &result[offset])
            {
                _pSrcBytes = pSrcBytes;
                _pDstBytes = pDstBytes;
                int byteCount = *((int*)_pSrcBytes);
                if (byteCount <= 0)
                {
                    byteCount = -byteCount;
                    if (result.Length - offset < byteCount)
                        CompressException.ThrowNoPlaceToStoreDecompressedDataException();
                    if (byteCount > 0)
                        Buffer.BlockCopy(source, sourceIndex + 4, result, offset, byteCount);
                    return byteCount;
                }
                if (result.Length - offset < byteCount)
                    CompressException.ThrowNoPlaceToStoreDecompressedDataException();
                fixed (int* pCharTree = &_charTree[0], pDistTree = &_distTree[0],
                           pChLenTree = &_chLenTree[0], pBitLen = &_bitLen[0],
                           pBitCount = &_bitCount[0], pNextCode = &_nextCode[0],
                           pCharExBitLength = &Consts.CharExBitLength[0],
                           pCharExBitBase = &Consts.CharExBitBase[0],
                           pDistExBitLength = &Consts.DistExBitLength[0],
                           pDistExBitBase = &Consts.DistExBitBase[0])
                {
                    _pCharTree = pCharTree;
                    _pDistTree = pDistTree;
                    _pChLenTree = pChLenTree;
                    _pBitLen = pBitLen;
                    _pBitCount = pBitCount;
                    _pNextCode = pNextCode;
                    _pCharExBitLength = pCharExBitLength;
                    _pCharExBitBase = pCharExBitBase;
                    _pDistExBitLength = pDistExBitLength;
                    _pDistExBitBase = pDistExBitBase;
                    _bits = 0;
                    _hold = 0;
                    _srcIndex = sourceIndex + 4;
                    _pSrcBytes += 4;
                    //修改过
                    //_breakOffset = sourceBytes.Length;
                    _breakOffset = sourceIndex + count;
                    _break32Offset = _breakOffset - 3;
                    int length, distance;
                    _outCounter = byteCount;
                    while (_outCounter > 0)
                    {
                        ReadBlockHeader();
                        while (_inCounter > 0 && _outCounter > 0)
                        {
                            int c = GetCode(_pCharTree);
                            _inCounter--;
                            if (c < Consts.FirstLengthChar)
                            {
                                *_pDstBytes = (byte)c;
                                _outCounter--;
                                _pDstBytes++;
                            }
                            else
                            {
                                c -= Consts.FirstCharWithExBit;
                                if (c < 0)
                                    length = c + 19;
                                else
                                    length = GetNBits(_pCharExBitLength[c]) + _pCharExBitBase[c];
                                c = GetCode(_pDistTree);
                                if (c < 3)
                                {
                                    if (c == 0)
                                        distance = _r0;
                                    else if (c == 1)
                                    {
                                        distance = _r1;
                                        _r1 = _r0;
                                        _r0 = distance;
                                    }
                                    else
                                    {
                                        distance = _r2;
                                        _r2 = _r0;
                                        _r0 = distance;
                                    }
                                }
                                else
                                {
                                    distance = _pDistExBitBase[c];
                                    if (c >= Consts.FirstDistWithExBit)
                                    {
                                        distance += GetNBits(_pDistExBitLength[c]);
                                        _r2 = _r1;
                                        _r1 = _r0;
                                        _r0 = distance;
                                    }
                                }
                                //if (distance < length && length >= 16)
                                //{
                                //    ShiboLogService.Log.Info(distance + "   " + length);
                                //    ShiboLogService.Log.Flush();
                                //}
                                if (distance > length)// && length >= 16)
                                    CompressionUtils.CopyBytesBlock(_pDstBytes - distance, _pDstBytes, length);
                                else
                                    CompressionUtils.CopyBytes(_pDstBytes - distance, _pDstBytes, length);
                                _outCounter -= length;
                                _pDstBytes += length;
                            }
                        }
                    }
                }
                return byteCount;
            }
        }

        public byte[] Decompress(byte[] source, int sourceIndex)
        {
            return Decompress(source, sourceIndex, 0, 0);
        }

        public static void Free()
        {
            _instance = null;
        }


        #endregion

        #region IDecompress 成员

        public static byte[] Decode(byte[] source)
        {
            return Instance.Decompress(source, 0);
        }

        public static byte[] Decode(byte[] source, int sourceIndex)
        {
            return Instance.Decompress(source, sourceIndex);
        }

        #endregion
    }
}
