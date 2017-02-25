using System;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The IO module is used for simple input-output operations.
    /// </summary>
    [SolLibraryClass(std.NAME, SolTypeMode.Singleton)]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_IO
    {
        public std_IO()
        {
            NativeInStream = Console.OpenStandardInput();
            NativeOutStream = Console.OpenStandardOutput();
        }

        [SolLibraryVisibility(std.NAME, false)] public const string TYPE = "IO";

        private const double SECOND_TO_MS = 1000;

        private static std_Stream m_InStream;
        private static Stream m_NativeInStream;
        private static Stream m_NativeOutStream;
        private static std_Stream m_OutStream;

        /// <summary>
        ///     The native <see cref="Stream" /> the input will be retrieved from. Keep in mind that this value may be overwritten
        ///     at any time if a new <see cref="in_stream" /> is assigned.
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)]
        public static Stream NativeInStream {
            get { return m_NativeInStream; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("Cannot get the native input stream to null.");
                }
                if (value != m_NativeInStream) {
                    m_NativeInStream = value;
                    m_InStream = new std_Stream(value);
                }
            }
        }

        /// <summary>
        ///     The native <see cref="Stream" /> the output will be written to. Keep in mind that this value may be overwritten at
        ///     any time if a new <see cref="in_stream" /> is assigned.
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)]
        public static Stream NativeOutStream {
            get { return m_NativeOutStream; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("Cannot get the native output stream to null.");
                }
                if (value != m_NativeOutStream) {
                    m_NativeOutStream = value;
                    m_OutStream = new std_Stream(value);
                }
            }
        }

        /// <summary>
        ///     You can use this <see cref="std_Stream" /> to manually retrieve data from the default input.
        /// </summary>
        [SolContract(std_Stream.TYPE, false)]
        public std_Stream in_stream {
            get { return m_InStream; }
            set {
                m_InStream = value;
                m_NativeInStream = value.GetNativeStream();
            }
        }

        /// <summary>
        ///     You can use this <see cref="std_Stream" /> to manually write data to the default output.
        /// </summary>
        [SolContract(std_Stream.TYPE, false)]
        public std_Stream out_stream {
            get { return m_OutStream; }
            set {
                m_OutStream = value;
                m_NativeOutStream = value.GetNativeStream();
            }
        }

        /// <summary>
        ///     The prefix to write whenever requesting user input. Set to <see cref="SolNil" /> to disable the prefix.
        ///     (Default: <c><![CDATA[> ]]></c>)
        /// </summary>
        [SolContract(SolString.TYPE, true)]
        [CanBeNull]
        public SolString read_prefix { get; set; } = SolString.ValueOf("> ").Intern();

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return "IO Singleton";
        }

        #endregion

        /// <summary>Writes <paramref name="data" /> to the standard output.</summary>
        /// <para name="data">
        ///     The data to write. The data will automatically be converted to a string using a
        ///     <see cref="SolMetaKey.Stringify" /> call.
        /// </para>
        /// <returns>The <see cref="std_IO" /> singleton itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        [SolContract(TYPE, false)]
        public std_IO write(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue data)
        {
            m_OutStream.write(context, data.Type == SolString.TYPE ? data : SolString.ValueOf(data.ToString(context)));
            return this;
        }

        /// <inheritdoc cref="std_Stream.writeln" />
        /// <returns>The <see cref="std_IO" /> singleton itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        public std_IO writeln(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, false)] SolValue data)
        {
            m_OutStream.writeln(context, data);
            return this;
        }

        /// <summary>
        ///     Requests user input from the currently active stream. The input line is prefixed by <see cref="read_prefix" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message">
        ///     (Optional) The info message about the the requested input. (Supported types:
        ///     <see cref="SolString" />, <see cref="SolNumber" />, <see cref="SolBool" />)
        /// </param>
        /// <returns>The read <see cref="SolString" />.</returns>
        /// <exception cref="SolRuntimeException">An error occured while reading/writing the message.</exception>
        [SolContract(SolString.TYPE, false)]
        public SolString read(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue message)
        {
            if (!message.IsNil()) {
                m_OutStream.writeln(context, message);
            }
            if (!read_prefix.IsNil()) {
                m_OutStream.write(context, read_prefix);
            }
            return m_InStream.readln(context);
        }

        /// <summary>
        ///     Waits for the given amount of seconds.
        /// </summary>
        /// <param name="time">How many seconds to wait?</param>
        /// <remarks>This freezes the entire main thread.</remarks>
        public void wait([SolContract(SolNumber.TYPE, false)] SolNumber time)
        {
            Thread.Sleep((int) (time.Value * SECOND_TO_MS));
        }
    }
}