using System;
using System.Globalization;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_Stream" /> is used to provide a uniform way to read and write byte based data structures.
    /// </summary>
    [SolLibraryClass(std.NAME, SolTypeMode.Default)]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_Stream
    {
        /// <summary>
        ///     Creates a new <see cref="std_Stream" />.
        /// </summary>
        public std_Stream() : this(new MemoryStream()) {}

        /// <summary>
        ///     Creates a new <see cref="std_Stream" /> ponting to an underlying native <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The native stream.</param>
        /// <remarks>Cannot be used from SolScript.</remarks>
        [SolLibraryVisibility(std.NAME, false)]
        public std_Stream(Stream stream)
        {
            m_Stream = stream;
            if (stream.CanRead) {
                m_Reader = new StreamReader(stream);
            }
        }

        /// <summary>
        ///     The type name is "Stream".
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)] public const string TYPE = "Stream";

        // Theses bytes represent the OS-specific line break sequence.
        private static readonly byte[] NewLineBytes = Encoding.Default.GetBytes(Environment.NewLine);
        // The underlying native stream. Every std_Stream has an underlying native stream.
        private readonly Stream m_Stream;
        // The reader used to ... read the stream.
        private StreamReader m_Reader;

        /// <summary>
        ///     Returns the underlying native <see cref="Stream" />. A <see cref="std_Stream" /> always has an underlying native
        ///     <see cref="Stream" />. <see cref="std_Stream" /> created directly in SolScript have an underlying
        ///     <see cref="MemoryStream" />.
        /// </summary>
        /// <returns>The native <see cref="Stream" />.</returns>
        [SolLibraryVisibility(std.NAME, false)]
        public Stream GetNativeStream()
        {
            return m_Stream;
        }

        /// <summary>
        ///     Indicates if data can be written to this <see cref="std_Stream" />.
        /// </summary>
        /// <returns>true if data can be written this this <see cref="std_Stream" />, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool can_write()
        {
            return SolBool.ValueOf(m_Stream.CanWrite);
        }

        /// <summary>
        ///     Indicates if data can be read from this <see cref="std_Stream" />.
        /// </summary>
        /// <returns>true if data can be read from this <see cref="std_Stream" />, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool can_read()
        {
            return SolBool.ValueOf(m_Stream.CanRead);
        }

        #region Sol Raw

        /// <summary>
        ///     Writes <paramref name="data" /> to this stream. The <paramref name="data" /> is always written as a string, and not
        ///     converted into proper binary form. This function is meant e.g. for usage with streams directly handing data
        ///     displayed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data">
        ///     Data data to write(Supported are: <see cref="SolString" />, <see cref="SolNumber" /> and
        ///     <see cref="SolBool" />).
        /// </param>
        /// <returns>The <see cref="std_Stream" /> itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        [SolContract(TYPE, false)]
        public std_Stream write(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue data)
        {
            AssertWrite(context);
            try {
                WriteRaw_Impl(data);
            } catch (IOException ex) {
                throw WriteException(context, ex);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            } catch (SolVariableException ex) {
                throw WriteException(context, ex);
            }
            return this;
        }

        /// <summary>
        ///     Writes <paramref name="data" /> followed by a line-break sequence to this stream. The <paramref name="data" /> is
        ///     always written as a string, and not
        ///     converted into proper binary form. This function is meant e.g. for usage with streams directly handing data
        ///     displayed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data">
        ///     Data data to write(Supported are: <see cref="SolString" />, <see cref="SolNumber" /> and
        ///     <see cref="SolBool" />).
        /// </param>
        /// <returns>The <see cref="std_Stream" /> itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        public std_Stream writeln(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue data)
        {
            AssertWrite(context);
            try {
                WriteRaw_Impl(data);
                WriteBytes(NewLineBytes);
            } catch (IOException ex) {
                throw WriteException(context, ex);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            } catch (SolVariableException ex) {
                throw WriteException(context, ex);
            }
            return this;
        }

        /// <summary>
        ///     Reads a line in this stream.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The <see cref="SolString" /> representing the read line.</returns>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        public SolString readln(SolExecutionContext context)
        {
            AssertRead(context);
            // todo: implement a stream reader directly in this class - see failed attempts in std_Stream - Copy.cs
            try {
                return SolString.ValueOf(m_Reader.ReadLine());
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            } catch (SolVariableException ex) {
                throw WriteException(context, ex);
            } catch (OutOfMemoryException ex) {
                throw ReadException(context, ex);
            }
        }

        /// <summary>
        ///     Writes the given <see cref="SolValue" /> to the stream by encoding it as a string in the
        ///     <see cref="Encoding.Default" /> <see cref="Encoding" />.
        /// </summary>
        /// <param name="data">
        ///     The <see cref="SolValue" /> to write. (Allowed are: <see cref="SolString" />,
        ///     <see cref="SolNumber" /> and <see cref="SolBool" />.
        /// </param>
        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Method was called after the stream was closed. </exception>
        /// <exception cref="SolVariableException">Invalid <paramref name="data" /> type.</exception>
        /// <remarks>No checks if the stream can be written to are performed.</remarks>
        /// <seealso cref="AssertWrite" />
        private void WriteRaw_Impl(SolValue data)
        {
            SolString dataString;
            switch (data.Type) {
                case SolString.TYPE: {
                    dataString = (SolString) data;
                    break;
                }
                case SolNumber.TYPE: {
                    dataString = ((SolNumber) data).Value.ToString(std_String.UseLocalCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture);
                    break;
                }
                case SolBool.TYPE: {
                    dataString = ((SolBool) data).Value ? SolBool.TRUE_STRING : SolBool.FALSE_STRING;
                    break;
                }
                default: {
                    throw new SolVariableException(SolSourceLocation.Native(),
                        "Cannot write a \"" + data.Type + "\" value. Only primitives of type \"" + SolString.TYPE + "\", \"" + SolNumber.TYPE + "\" and \"" + SolBool.TYPE +
                        "\" can directly be written to a stream.");
                }
            }
            byte[] bytes = Encoding.Default.GetBytes(dataString.Value);
            WriteBytes(bytes);
        }

        #endregion

        #region Sol Binary

        /// <summary>
        ///     Reads a single byte from this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The read byte.</returns>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber read_binary_byte(SolExecutionContext context)
        {
            AssertRead(context);
            try {
                return new SolNumber(ReadByte());
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
        }

        /// <summary>
        ///     Reads a string from this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>The read string.</returns>
        /// <exception cref="SolRuntimeException">An error occured while reading.</exception>
        /// <remarks>A string is represented as 4 bytes indicating its unsigned length, and each character as 2 bytes.</remarks>
        public SolString read_binary_string(SolExecutionContext context)
        {
            AssertRead(context);
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
        public SolNumber read_binary_number(SolExecutionContext context)
        {
            AssertRead(context);
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
        public SolBool read_binary_bool(SolExecutionContext context)
        {
            AssertRead(context);
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
        public std_Stream write_binary(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue data)
        {
            AssertWrite(context);
            try {
                switch (data.Type) {
                    case SolString.TYPE: {
                        SolString dataString = (SolString) data;
                        var charData = new byte[dataString.Value.Length * sizeof(char)];
                        Buffer.BlockCopy(dataString.Value.ToCharArray(), 0, charData, 0, charData.Length);
                        WriteUInt32((uint) dataString.Value.Length);
                        WriteBytes(charData);
                        break;
                    }
                    case SolNumber.TYPE: {
                        SolNumber dataNumber = (SolNumber) data;
                        WriteBytes(BitConverter.GetBytes(dataNumber.Value));
                        break;
                    }
                    case SolBool.TYPE: {
                        SolBool dataBool = (SolBool) data;
                        WriteByte(dataBool.Value ? (byte) 1 : (byte) 0);
                        break;
                    }
                    default: {
                        throw new SolRuntimeException(context,
                            "Cannot write a \"" + data.Type + "\" value in binary form. Only primites of type \"" + SolString.TYPE + "\", \"" + SolNumber.TYPE + "\" and \"" + SolBool.TYPE +
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

        /// <summary>
        ///     Writes a byte to this <see cref="std_Stream" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data">The byte to write.</param>
        /// <returns>The <see cref="std_Stream" /> itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing.</exception>
        [SolContract(TYPE, false)]
        public std_Stream write_binary_byte(SolExecutionContext context, [SolContract(SolNumber.TYPE, false)] SolNumber data)
        {
            AssertWrite(context);
            if (data.Value < 0 || data.Value > 255) {
                throw new SolRuntimeException(context, "The value of a byte value must be between 0 and 255. Got a value of " + data.Value + ".");
            }
            if (data.Value % 0 != 0) {
                throw new SolRuntimeException(context, "A byte must not have a decimal part. Got a value of " + data.Value + ".");
            }
            byte dataByte = (byte) data.Value;
            try {
                m_Stream.WriteByte(dataByte);
            } catch (IOException ex) {
                throw WriteException(context, ex);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            }
            return this;
        }

        #endregion

        #region Native Helpers

        /// <exception cref="SolRuntimeException">This stream does not support reading.</exception>
        private void AssertRead(SolExecutionContext context)
        {
            if (!m_Stream.CanRead) {
                throw new SolRuntimeException(context, "This stream does not support reading.");
            }
        }

        /// <exception cref="SolRuntimeException">This stream does not support writing.</exception>
        private void AssertWrite(SolExecutionContext context)
        {
            if (!m_Stream.CanWrite) {
                throw new SolRuntimeException(context, "This stream does not support writing.");
            }
        }

        #endregion

        #region Native Exception Handler

        private SolRuntimeException ReadException(SolExecutionContext context, IOException exception)
        {
            return new SolRuntimeException(context, "An error occured while trying to read from this stream.", exception);
        }

        private SolRuntimeException ReadException(SolExecutionContext context, NotSupportedException exception)
        {
            return new SolRuntimeException(context, "This stream does not support reading.", exception);
        }

        private SolRuntimeException ReadException(SolExecutionContext context, ObjectDisposedException exception)
        {
            return new SolRuntimeException(context, "This stream has already been closed.", exception);
        }

        private SolRuntimeException ReadException(SolExecutionContext context, OutOfMemoryException exception)
        {
            return new SolRuntimeException(context, "The underlying buffer is out of memory.", exception);
        }

        private SolRuntimeException WriteException(SolExecutionContext context, IOException exception)
        {
            return new SolRuntimeException(context, "An error occured while trying to write to this stream.", exception);
        }

        private SolRuntimeException WriteException(SolExecutionContext context, NotSupportedException exception)
        {
            return new SolRuntimeException(context, "This stream does not support writing.", exception);
        }

        private SolRuntimeException WriteException(SolExecutionContext context, ObjectDisposedException exception)
        {
            return new SolRuntimeException(context, "This stream has already been closed.", exception);
        }

        private SolRuntimeException WriteException(SolExecutionContext context, SolVariableException exception)
        {
            return new SolRuntimeException(context, "Invalid data type.", exception);
        }

        #endregion

        #region Native Write Methods

        // todo: have one single byte buffer in this class to avoid array allocations all the time.

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteByte(byte data)
        {
            m_Stream.WriteByte(data);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private void WriteUInt32(uint data)
        {
            WriteBytes(BitConverter.GetBytes(data));
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Method was called after the stream was closed. </exception>
        private void WriteBytes(byte[] bytes)
        {
            m_Stream.Write(bytes, 0, bytes.Length);
        }

        #endregion

        #region Native Read Methods

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private bool ReadBool()
        {
            return m_Stream.ReadByte() != 0;
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private byte ReadByte()
        {
            return (byte) m_Stream.ReadByte();
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(8), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private char ReadChar()
        {
            return BitConverter.ToChar(ReadBytes(2), 0);
        }

        /// <exception cref="IOException">An I/O error occurs. </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
        private byte[] ReadBytes(int count)
        {
            var array = new byte[count];
            m_Stream.Read(array, 0, count);
            return array;
        }

        #endregion
    }
}