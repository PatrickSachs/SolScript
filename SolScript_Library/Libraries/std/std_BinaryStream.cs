// ReSharper disable InconsistentNaming

using System;
using System.IO;
using JetBrains.Annotations;
using MiscUtil.Conversion;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_BinaryStream" /> is used to write primitive data in binary form.
    /// </summary>
    [SolLibraryClass(std.NAME, SolTypeMode.Default)]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_BinaryStream : std_Stream
    {
        /// <summary>
        ///     Creates a new binary stream.
        /// </summary>
        /// <param name="isLittleEndian">
        ///     (Optional) If this is true the stream will use little endian, if false big endian.
        ///     (Default: The default endianness of your OS)
        /// </param>
        /// <remarks>This is the SolScript constructor.</remarks>
        public std_BinaryStream(SolBool isLittleEndian)
            : this(
                new MemoryStream(),
                isLittleEndian != null ? (isLittleEndian.Value ? Endianness.LittleEndian : Endianness.BigEndian) : (BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian)) {}

        /// <summary>
        ///     Creates a new binary stream wrapping the given native stream.
        /// </summary>
        /// <param name="stream">The native stream.</param>
        /// <param name="endianness">The endianness of this stream.</param>
        [SolLibraryVisibility(std.NAME, false)]
        public std_BinaryStream(Stream stream, Endianness endianness) : base(stream)
        {
            Endianness = endianness;
        }

        /// <summary>
        ///     The type name is "BinaryStream"
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)] public new const string TYPE = "BinaryStream";

        /// <summary>
        ///     How many bytes should fit into the default buffer?
        /// </summary>
        private const int DEFAULT_BUFFER_SIZE = 16;

        // The converter used for big endian.
        private static readonly BigEndianBitConverter BigEndian = new BigEndianBitConverter();
        // The converter used for little endian.
        private static readonly LittleEndianBitConverter LittleEndian = new LittleEndianBitConverter();

        /// <summary>
        ///     The endianness of this stream.
        /// </summary>
        protected readonly Endianness Endianness;

        // Data is temporarily saved into this buffer, saving us from having to allocate new arrays all the time.
        private byte[] l_buffer = new byte[DEFAULT_BUFFER_SIZE];

        /// <summary>
        ///     Gets the bit converter used for this endianness.
        /// </summary>
        protected EndianBitConverter Converter => Endianness == Endianness.BigEndian ? (EndianBitConverter) BigEndian : LittleEndian;

        /// <summary>
        ///     Checks if this binary stream has the little endian endianness.
        /// </summary>
        /// <returns>true if it has, false if not. false means that the stream is in big endian endianness.</returns>
        public SolBool is_little_endian() => SolBool.ValueOf(Endianness == Endianness.LittleEndian);

        /// <summary>
        ///     Reads a string from this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The read string.</returns>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        /// <remarks>A string is represented as 4 bytes indicating its unsigned length, and each character as 2 bytes.</remarks>
        public SolString read_string(SolExecutionContext context)
        {
            try {
                uint length = ReadUInt32();
                var charArray = new char[length];
                for (int i = 0; i < length; i++) {
                    charArray[i] = ReadChar();
                }
                return SolString.ValueOf(new string(charArray));
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
        }

        /// <summary>
        ///     Reads a number from this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The read number.</returns>
        /// <remarks>A number is represented as 8 bytes.</remarks>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        public SolNumber read_number(SolExecutionContext context)
        {
            try {
                return new SolNumber(ReadDouble());
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
        }

        /// <summary>
        ///     Reads a bool from this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The read bool.</returns>
        /// <remarks>A bool is represented as one byte. If this byte is 0 the bool is false, otherwise true.</remarks>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        public SolBool read_bool(SolExecutionContext context)
        {
            try {
                return SolBool.ValueOf(ReadBool());
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
        }

        /// <summary>
        ///     Writes <paramref name="data" /> in binary form to this stream.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data">
        ///     The data to write(Supported types: <see cref="SolString" />, <see cref="SolNumber" /> and
        ///     <see cref="SolBool" />).
        /// </param>
        /// <returns>The <see cref="std_Stream" /> itself.</returns>
        /// <exception cref="SolRuntimeException">
        ///     An exception occured while writing or the type of <paramref name="data" /> is
        ///     invalid.
        /// </exception>
        [SolContract(TYPE, false)]
        public std_Stream write(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue data)
        {
            if (!NativeStream.CanWrite) {
                throw WriteException(context, (NotSupportedException) null);
            }
            try {
                switch (data.Type) {
                    case SolString.TYPE: {
                        SolString dataString = (SolString) data;
                        byte[] strLengthBytes = Converter.GetBytes((uint) dataString.Value.Length);
                        int arrayLength = dataString.Value.Length * 2 + strLengthBytes.Length;
                        EnsureBuffer(arrayLength);
                        Buffer.BlockCopy(strLengthBytes, 0, l_buffer, 0, strLengthBytes.Length);
                        for (int i = 0; i < dataString.Value.Length; i++) {
                            byte[] charBytes = Converter.GetBytes(dataString.Value[i]);
                            int i2 = i * 2;
                            l_buffer[strLengthBytes.Length + i2] = charBytes[0];
                            l_buffer[strLengthBytes.Length + i2 + 1] = charBytes[1];
                        }
                        WriteBytes(l_buffer, 0, arrayLength);
                        break;
                    }
                    case SolNumber.TYPE: {
                        SolNumber dataNumber = (SolNumber) data;
                        WriteDouble(dataNumber.Value);
                        break;
                    }
                    case SolBool.TYPE: {
                        SolBool dataBool = (SolBool) data;
                        WriteByte(dataBool.Value ? (byte) 1 : (byte) 0);
                        break;
                    }
                    default: {
                        throw new SolRuntimeException(context,
                            "Cannot write a \"" + data.Type + "\" value in binary form. Only primitives of type \"" + SolString.TYPE + "\", \"" + SolNumber.TYPE + "\" and \"" + SolBool.TYPE +
                            "\" can directly be written to a binary stream.");
                    }
                }
            } catch (IOException ex) {
                throw WriteException(context, ex);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            }
            return this;
        }

        // Ensures that l_buffer has at least a length of size.
        private void EnsureBuffer(int size)
        {
            if (l_buffer.Length < size) {
                l_buffer = new byte[size];
            }
        }

        #region Native Write Methods

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteByte(byte data)
        {
            NativeStream.WriteByte(data);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteChar(char data)
        {
            WriteBytes(Converter.GetBytes(data));
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteUInt32(uint data)
        {
            WriteBytes(Converter.GetBytes(data));
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteDouble(double data)
        {
            WriteBytes(Converter.GetBytes(data));
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Method was called after the stream was closed. </exception>
        private void WriteBytes(byte[] bytes)
        {
            NativeStream.Write(bytes, 0, bytes.Length);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Method was called after the stream was closed. </exception>
        private void WriteBytes(byte[] bytes, int offset, int amount)
        {
            NativeStream.Write(bytes, offset, amount);
        }

        #endregion

        #region Native Read Methods

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private bool ReadBool()
        {
            return NativeStream.ReadByte() != 0;
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private byte ReadByte()
        {
            return (byte) NativeStream.ReadByte();
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private double ReadDouble()
        {
            return Converter.ToDouble(ReadBytes(8), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private uint ReadUInt32()
        {
            return Converter.ToUInt32(ReadBytes(4), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private char ReadChar()
        {
            return Converter.ToChar(ReadBytes(2), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        /// <remarks>The return array is NOT guaranteed to have the same size as <paramref name="count" />.</remarks>
        private byte[] ReadBytes(int count)
        {
            EnsureBuffer(count);
            NativeStream.Read(l_buffer, 0, count);
            return l_buffer;
        }

        #endregion
    }
}