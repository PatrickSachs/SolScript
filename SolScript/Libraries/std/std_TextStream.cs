// ReSharper disable InconsistentNaming

using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_TextStream" /> is used to write and read data in text(string) form.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Default, typeof(std_TextStream))]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_TextStream : std_Stream
    {
        /// <summary>
        ///     Creates a new <see cref="std_TextStream" /> with a backing <see cref="MemoryStream" />. Uses the
        ///     <see cref="Encoding.Default" /> encoding.
        /// </summary>
        /// <remarks>This is the SolScript constructor.</remarks>
        public std_TextStream() : this(new MemoryStream(), Encoding.Default) {}

        /// <summary>
        ///     Creates a new <see cref="std_TextStream" /> pointing to an underlying native <see cref="Stream" />. Uses the
        ///     <see cref="Encoding.Default" /> encoding.
        /// </summary>
        /// <param name="stream">The native stream.</param>
        /// <remarks>Cannot be used from SolScript.</remarks>
        [SolVisibility( false)]
        public std_TextStream(Stream stream) : this(stream, Encoding.Default) {}

        /// <summary>
        ///     Creates a new <see cref="std_TextStream" /> pointing to an underlying native <see cref="Stream" /> also allows you
        ///     to
        ///     set the encoding.
        /// </summary>
        /// <param name="stream">The native stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <remarks>Cannot be used from SolScript.</remarks>
        [SolVisibility( false)]
        public std_TextStream(Stream stream, Encoding encoding) : base(stream)
        {
            NativeEncoding = encoding;
            if (stream.CanRead) {
                m_Reader = new StreamReader(stream, encoding);
            }
        }

        /// <summary>
        ///     The type name is "FileStream".
        /// </summary>
        [SolVisibility( false)] public new const string TYPE = "TextStream";

        // The stream reader is used to read data from the stream (if reading is supported, otherwise null)
        [CanBeNull] private readonly StreamReader m_Reader;

        /// <summary>
        ///     The encoding of the native stream. Defaults to the system encoding.
        /// </summary>
        [SolVisibility( false)]
        public virtual Encoding NativeEncoding { get; }

        /// <exception cref="SolRuntimeException">The <see cref="NativeEncoding" /> is not supported on this system.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while writing to this stream.</exception>
        /// <exception cref="SolRuntimeException">This stream does not support writing.</exception>
        /// <exception cref="SolRuntimeException">This stream output has been closed.</exception>
        private void write_Impl(SolExecutionContext context, string text)
        {
            byte[] bytes;
            try {
                bytes = NativeEncoding.GetBytes(text);
            } catch (EncoderFallbackException ex) {
                throw new SolRuntimeException(context, "The encoding \"" + NativeEncoding.EncodingName + "\" is not supported on this system.", ex);
            }
            try {
                NativeStream.Write(bytes, 0, bytes.Length);
            } catch (IOException ex) {
                throw WriteException(context, ex);
            } catch (NotSupportedException ex) {
                throw WriteException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw WriteException(context, ex);
            }
        }

        /// <summary>
        ///     Writes <paramref name="text" /> to this <see cref="std_TextStream" />.
        /// </summary>
        /// <param name="context" />
        /// <param name="text">The text to write.</param>
        /// <returns>The stream itself.</returns>
        /// <exception cref="SolRuntimeException">The <see cref="NativeEncoding" /> is not supported on this system.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while writing to this stream.</exception>
        /// <exception cref="SolRuntimeException">This stream does not support writing.</exception>
        /// <exception cref="SolRuntimeException">This stream has been closed.</exception>
        [SolContract(TYPE, false)]
        public std_TextStream write(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString text)
        {
            write_Impl(context, text.Value);
            return this;
        }

        /// <summary>
        ///     Writes <paramref name="text" /> followed by a new line to this <see cref="std_TextStream" />.
        /// </summary>
        /// <param name="context" />
        /// <param name="text">The text to write.</param>
        /// <exception cref="SolRuntimeException">The <see cref="NativeEncoding" /> is not supported on this system.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while writing to this stream.</exception>
        /// <exception cref="SolRuntimeException">This stream does not support writing.</exception>
        /// <exception cref="SolRuntimeException">This stream has been closed.</exception>
        [SolContract(TYPE, false)]
        public std_TextStream writeln(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString text)
        {
            write_Impl(context, text.Value + Environment.NewLine);
            return this;
        }

        /// <summary>Reads a line of text from this <see cref="std_TextStream" />.</summary>
        /// <param name="context" />
        /// <exception cref="SolRuntimeException">This stream does not support reading.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while reading from this stream.</exception>
        /// <exception cref="SolRuntimeException">The stream buffer ran out of memory.</exception>
        /// <exception cref="SolRuntimeException">This stream has been closed.</exception>
        [SolContract(SolString.TYPE, false)]
        public SolString readln(SolExecutionContext context)
        {
            if (m_Reader == null) {
                throw ReadException(context, (NotSupportedException) null);
            }
            string text;
            try {
                text = m_Reader.ReadLine();
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (OutOfMemoryException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
            return SolString.ValueOf(text);
        }

        /// <summary>Reads all text from this <see cref="std_TextStream" />.</summary>
        /// <param name="context" />
        /// <exception cref="SolRuntimeException">This stream does not support reading.</exception>
        /// <exception cref="SolRuntimeException">An I/O error occured while reading from this stream.</exception>
        /// <exception cref="SolRuntimeException">The stream buffer ran out of memory.</exception>
        /// <exception cref="SolRuntimeException">This stream has been closed.</exception>
        [SolContract(SolString.TYPE, false)]
        public SolString read_to_end(SolExecutionContext context)
        {
            if (m_Reader == null) {
                throw ReadException(context, (NotSupportedException) null);
            }
            string text;
            try {
                text = m_Reader.ReadToEnd();
            } catch (IOException ex) {
                throw ReadException(context, ex);
            } catch (OutOfMemoryException ex) {
                throw ReadException(context, ex);
            } catch (ObjectDisposedException ex) {
                throw ReadException(context, ex);
            }
            return SolString.ValueOf(text);
        }
    }
}