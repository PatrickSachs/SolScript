using System;
using System.IO;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_Stream" /> is the base class for implementation streams. Streams allow you to dynamically access
    ///     and write data at runtime.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Default, typeof(std_Stream)), SolLibraryName(TYPE), PublicAPI]
    public class std_Stream
    {
        /// <summary>
        ///     Creates a new <see cref="std_Stream" />.
        /// </summary>
        public std_Stream() : this(new MemoryStream()) {}

        /// <summary>
        ///     Creates a new <see cref="std_Stream" /> pointing to an underlying native <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The native stream.</param>
        /// <remarks>Cannot be used from SolScript.</remarks>
        [SolVisibility( false)]
        public std_Stream(Stream stream)
        {
            NativeStream = stream;
        }

        /// <summary>
        ///     The type name is "Stream".
        /// </summary>
        [SolVisibility( false)]
        public const string TYPE = "Stream";

        /// <summary>
        ///     Used to represent the <see cref="SeekOrigin.Begin" /> <see cref="SeekOrigin" />.
        /// </summary>
        private const string MODE_BEGIN = "begin";

        /// <summary>
        ///     Used to represent the <see cref="SeekOrigin.Current" /> <see cref="SeekOrigin" />.
        /// </summary>
        private const string MODE_CURRENT = "current";

        /// <summary>
        ///     Used to represent the <see cref="SeekOrigin.End" /> <see cref="SeekOrigin" />.
        /// </summary>
        private const string MODE_END = "end";

        /// <summary>
        ///     The underlying native stream.
        /// </summary>
        [SolVisibility( false)]
        public virtual Stream NativeStream { get; }

        /// <summary>
        ///     Is it possible to write to this stream?
        /// </summary>
        /// <returns>True if it is, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool can_write()
        {
            return SolBool.ValueOf(NativeStream.CanWrite);
        }

        /// <summary>
        ///     Is it possible to read from this stream?
        /// </summary>
        /// <returns>True if it is, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool can_read()
        {
            return SolBool.ValueOf(NativeStream.CanRead);
        }

        /// <summary>
        ///     Is it possible to seek on this stream?
        /// </summary>
        /// <returns>True if it is, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool can_seek()
        {
            return SolBool.ValueOf(NativeStream.CanSeek);
        }

        /// <summary>
        ///     Get the length of this stream.
        /// </summary>
        /// <param name="context" />
        /// <returns>The length as an integer.</returns>
        /// <remarks>Requires seeking.</remarks>
        /// <seealso cref="can_seek" />
        /// <exception cref="SolRuntimeException">Cannot seek on this stream.</exception>
        /// <exception cref="SolRuntimeException">This stream has already been disposed.</exception>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber get_length(SolExecutionContext context)
        {
            try {
                return new SolNumber(NativeStream.Length);
            } catch (NotSupportedException ex) {
                throw SeekException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw SeekException(context, ex);
            }
        }

        /// <summary>
        ///     Seeks on the stream.
        /// </summary>
        /// <param name="context" />
        /// <param name="position">The position to seek to. (Must be integer)</param>
        /// <param name="mode">
        ///     (Optional) The seek mode. (Default: "begin") Valid are:<br />"begin" - The <paramref name="position" /> specifies
        ///     the offset
        ///     from the stream start to seek to.<br />"current" - The <paramref name="position" /> specifies the offset from the
        ///     current
        ///     position to seek to.<br />"end" - The <paramref name="position" /> specifies the negative offset from the stream
        ///     end
        ///     to seek to.
        /// </param>
        /// <exception cref="SolRuntimeException"><paramref name="position" /> is not an integer.</exception>
        public void seek(SolExecutionContext context, [SolContract(SolNumber.TYPE, false)] SolNumber position, [SolContract(SolString.TYPE, true)] SolString mode)
        {
            int posInt;
            if (!InternalHelper.NumberToInteger(position, out posInt)) {
                throw new SolRuntimeException(context, "Can only seek to an integer value. Got: " + position);
            }
            ;
            SeekOrigin origin;
            if (mode != null) {
                switch (mode.Value) {
                    case MODE_BEGIN: {
                        origin = SeekOrigin.Begin;
                        break;
                    }
                    case MODE_CURRENT: {
                        origin = SeekOrigin.Current;
                        break;
                    }
                    case MODE_END: {
                        origin = SeekOrigin.End;
                        break;
                    }
                    default: {
                        throw new SolRuntimeException(context, "Invalid seek mode \"" + mode + "\". Valid are: " + new[] {MODE_BEGIN, MODE_CURRENT, MODE_END}.JoinToString());
                    }
                }
            } else {
                origin = SeekOrigin.Begin;
            }
            try {
                NativeStream.Seek(posInt, origin);
            } catch (NotSupportedException ex) {
                throw SeekException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw SeekException(context, ex);
            } catch (IOException ex) {
                throw SeekException(context, ex);
            }
        }

        /// <summary>
        ///     Closes the stream.
        /// </summary>
        public void close()
        {
            NativeStream.Close();
        }

        /// <summary>
        ///     Reads a byte from this stream.
        /// </summary>
        /// <param name="context" />
        /// <returns>The byte, or nil if at the end of the stream.</returns>
        /// <exception cref="SolRuntimeException">Cannot read from this stream.</exception>
        /// <exception cref="SolRuntimeException">This stream has already been closed.</exception>
        [SolContract(SolNumber.TYPE, true)]
        public SolValue read_byte(SolExecutionContext context)
        {
            int b;
            try {
                b = NativeStream.ReadByte();
            } catch (NotSupportedException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            } catch (IOException ex) {
                throw ReadException(context, ex);
            }
            if (b == -1) {
                return SolNil.Instance;
            }
            return new SolNumber(b);
        }

        /// <summary>
        ///     Writes a byte to this stream.
        /// </summary>
        /// <param name="context" />
        /// <param name="number">
        ///     The byte to write. (Must be integer between <see cref="byte.MinValue" /> and
        ///     <see cref="byte.MaxValue" />)
        /// </param>
        /// <exception cref="SolRuntimeException">The <paramref name="number" /> is not a valid byte.</exception>
        public void write_byte(SolExecutionContext context, SolNumber number)
        {
            byte b = (byte) number.Value;
            if (number.Value < byte.MinValue || number.Value > byte.MaxValue || b != number.Value) {
                throw new SolRuntimeException(context, "The value " + number.Value + " is not a valid byte. Bytes must be integers between " + byte.MinValue + " and " + byte.MaxValue + ".");
            }
            try {
                NativeStream.WriteByte(b);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            } catch (IOException ex) {
                throw WriteException(context, ex);
            }
        }

        #region Native Exception Creators

        /// <summary>
        ///     Creates a new exception indicating that something went wrong while reading.
        /// </summary>
        /// <param name="context">The current context, required for the stack trace.</param>
        /// <param name="exception">The underlying native exception.</param>
        /// <returns>The SolException.</returns>
        protected static SolRuntimeException ReadException(SolExecutionContext context, [CanBeNull] IOException exception)
        {
            return new SolRuntimeException(context, "An I/O error occured while trying to read from this stream.", exception);
        }

        /// <inheritdoc cref="ReadException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException ReadException(SolExecutionContext context, [CanBeNull] NotSupportedException exception)
        {
            return new SolRuntimeException(context, "This stream does not support reading.", exception);
        }

        /// <inheritdoc cref="ReadException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException ReadException(SolExecutionContext context, [CanBeNull] ObjectDisposedException exception)
        {
            return new SolRuntimeException(context, "This stream has already been closed.", exception);
        }

        /// <inheritdoc cref="ReadException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException ReadException(SolExecutionContext context, [CanBeNull] OutOfMemoryException exception)
        {
            return new SolRuntimeException(context, "The underlying buffer is out of memory.", exception);
        }

        /// <summary>
        ///     Creates a new exception indicating that something went wrong while writing.
        /// </summary>
        /// <param name="context">The current context, required for the stack trace.</param>
        /// <param name="exception">The underlying native exception.</param>
        /// <returns>The SolException.</returns>
        protected static SolRuntimeException WriteException(SolExecutionContext context, [CanBeNull] IOException exception)
        {
            return new SolRuntimeException(context, "An I/O error occured while trying to write to this stream.", exception);
        }

        /// <inheritdoc cref="WriteException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException WriteException(SolExecutionContext context, [CanBeNull] NotSupportedException exception)
        {
            return new SolRuntimeException(context, "This stream does not support writing.", exception);
        }

        /// <inheritdoc cref="WriteException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException WriteException(SolExecutionContext context, [CanBeNull] ObjectDisposedException exception)
        {
            return new SolRuntimeException(context, "This stream has already been closed.", exception);
        }

        /// <inheritdoc cref="WriteException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException WriteException(SolExecutionContext context, [CanBeNull] SolVariableException exception)
        {
            return new SolRuntimeException(context, "Invalid data type.", exception);
        }

        /// <summary>
        ///     Creates a new exception indicating that something went wrong while seeking.
        /// </summary>
        /// <param name="context">The current context, required for the stack trace.</param>
        /// <param name="exception">The underlying native exception.</param>
        /// <returns>The SolException.</returns>
        protected static SolRuntimeException SeekException(SolExecutionContext context, [CanBeNull] IOException exception)
        {
            return new SolRuntimeException(context, "An I/O error occured while trying to seek on this stream.", exception);
        }

        /// <inheritdoc cref="SeekException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException SeekException(SolExecutionContext context, [CanBeNull] NotSupportedException exception)
        {
            return new SolRuntimeException(context, "This stream does not support seeking.", exception);
        }

        /// <inheritdoc cref="SeekException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException SeekException(SolExecutionContext context, [CanBeNull] ObjectDisposedException exception)
        {
            return new SolRuntimeException(context, "This stream has already been closed.", exception);
        }

        /// <inheritdoc cref="SeekException(SolScript.Interpreter.SolExecutionContext,System.IO.IOException)" />
        protected static SolRuntimeException SeekException(SolExecutionContext context, [CanBeNull] SolVariableException exception)
        {
            return new SolRuntimeException(context, "Invalid data type.", exception);
        }

        #endregion
    }
}