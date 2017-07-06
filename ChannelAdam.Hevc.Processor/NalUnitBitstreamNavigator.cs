using System;

namespace ChannelAdam.Hevc.Processor
{
    /// <summary>
    /// Primitives for navigating through an H.265 / HEVC Bitstream and performing bit manipulation.
    /// </summary>
    /// <remarks>
    /// Reference:
    /// H.265(12/16) Approved in 2016-12-22 (http://www.itu.int/rec/T-REC-H.265-201612-I/en)  Article:E 41298  Posted:2017-03-16
    /// Rec. ITU-T H.265 v4 (12/2016)
    /// </remarks>
    public class NalUnitBitstreamNavigator
    {
        #region Private Fields

        private const int BitStart = 7;

        private int _byteBitIndex;
        private int _byteIndex;
        private byte[] _bytes;

        #endregion Private Fields

        #region Public Constructors

        public NalUnitBitstreamNavigator(byte[] bytes)
        {
            _bytes = bytes;
            _byteIndex = 0;
            _byteBitIndex = BitStart;
        }

        #endregion Public Constructors

        #region Public Methods

        public void ClearBit()
        {
#if DEBUG
            if (_byteIndex >= _bytes.Length)
            {
                throw new IndexOutOfRangeException("Out of data");
            }
#endif
            _bytes[_byteIndex] = (byte)(_bytes[_byteIndex] & ~(1 << _byteBitIndex));

            MoveBitPointerForwardByOneBit();
        }

        public bool ReadBit()
        {
#if DEBUG
            if (_byteIndex >= _bytes.Length)
            {
                throw new IndexOutOfRangeException("Out of data");
            }
#endif
            bool result = (_bytes[_byteIndex] & (1 << _byteBitIndex)) != 0;

            MoveBitPointerForwardByOneBit();

            return result;
        }

        public byte ReadBitsAsByte(int numberOfBits)
        {
            const uint maxNumBits = 8;

            if (numberOfBits <= 0 || numberOfBits > maxNumBits)
            {
                throw new IndexOutOfRangeException("numberOfBits must be > 0 and <= 8");
            }

            int result = 0;

            for (int i = 0; i < numberOfBits; i++)
            {
                if (ReadBit())
                {
                    result |= 1 << (numberOfBits - i - 1);
                }
            }

            return (byte)result;
        }

        public int ReadBitsAsInt32(int numberOfBits)
        {
            const uint maxNumBits = 32;

            if (numberOfBits <= 0 || numberOfBits > maxNumBits)
            {
                throw new IndexOutOfRangeException("numberOfBits must be > 0 and <= 32");
            }

            int result = 0;

            for (int i = 0; i < numberOfBits; i++)
            {
                if (ReadBit())
                {
                    result |= 1 << (numberOfBits - i - 1);
                }
            }

            return result;
        }

        public uint ReadBitsAsUInt32(int numberOfBits)
        {
            const uint maxNumBits = 32;

            if (numberOfBits <= 0 || numberOfBits > maxNumBits)
            {
                throw new IndexOutOfRangeException("numberOfBits must be > 0 and <= 32");
            }

            uint result = 0;

            for (int i = 0; i < numberOfBits; i++)
            {
                if (ReadBit())
                {
                    result |= (uint)1 << (numberOfBits - i - 1);
                }
            }

            return result;
        }

        public uint ReadNonNegativeExponentialGolombAsUInt32()
        {
            int leadingZeroBits = -1;

            for (bool hasBit = false; !hasBit && leadingZeroBits < 32; leadingZeroBits++)
            {
                hasBit = ReadBit();
            }

            if (leadingZeroBits == 0 || leadingZeroBits == 32)
            {
                return 0;
            }

            return (uint)(1 << leadingZeroBits) - 1 + ReadBitsAsUInt32(leadingZeroBits);
        }

        public int ReadSignedExponentialGolombAsInt32()
        {
            uint golomb = ReadNonNegativeExponentialGolombAsUInt32();

            if ((golomb & 1) != 0)
            {
                return ((int)golomb + 1) >> 1;
            }
            else
            {
                return -((int)golomb >> 1);
            }
        }

        public void RewindBit()
        {
            MoveBitPointerBackwardsByOneBit();
        }

        public void RewindBits(int numberOfBits)
        {
            for (int i = 0; i < numberOfBits; i++)
            {
                MoveBitPointerBackwardsByOneBit();
            }
        }

        public void SetBit()
        {
#if DEBUG
            if (_byteIndex >= _bytes.Length)
            {
                throw new IndexOutOfRangeException("Out of data");
            }
#endif

            _bytes[_byteIndex] |= (byte)(_bytes[_byteIndex] | 1 << _byteBitIndex);

            MoveBitPointerForwardByOneBit();
        }

        public void SetBits(ulong value, int numberOfBits)
        {
            const uint maxNumBits = 64;

            if (numberOfBits <= 0 || numberOfBits > maxNumBits)
            {
                throw new IndexOutOfRangeException("numberOfBits must be > 0 and <= 64");
            }

            for (int i = numberOfBits - 1; i >= 0; i--)
            {
                ulong mask = (ulong)1 << i;
                if ((value & mask) == 0)
                {
                    ClearBit();
                }
                else
                {
                    SetBit();
                }
            }
        }

        public void SkipBit()
        {
            MoveBitPointerForwardByOneBit();
        }

        public void SkipBits(int numberOfBits)
        {
            for (int i = 0; i < numberOfBits; i++)
            {
                MoveBitPointerForwardByOneBit();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void MoveBitPointerBackwardsByOneBit()
        {
            _byteBitIndex++;
            if (_byteBitIndex > BitStart)
            {
                _byteIndex--;
                _byteBitIndex = 0;

                if (_byteIndex >= 3)
                {
                    // 7.3.1.1 General NAL unit syntax
                    // Unskip the emulation_prevention_three_byte (0x03) when the previous 3 set of bytes are 0x000003
                    if (_bytes[_byteIndex - 3] == 0 && _bytes[_byteIndex - 2] == 0 && _bytes[_byteIndex - 1] == 3)
                    {
                        _byteIndex--;
                    }
                }
            }
        }

        private void MoveBitPointerForwardByOneBit()
        {
            _byteBitIndex--;
            if (_byteBitIndex < 0)
            {
                _byteIndex++;
                _byteBitIndex = BitStart;

                if (_byteIndex >= 2)
                {
                    // 7.3.1.1 General NAL unit syntax
                    // Skip the emulation_prevention_three_byte (0x03) when the current 3 set of bytes are 0x000003
                    if (_bytes[_byteIndex - 2] == 0 && _bytes[_byteIndex - 1] == 0 && _bytes[_byteIndex] == 3)
                    {
                        _byteIndex++;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}