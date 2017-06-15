using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The IO module is used for simple input-output operations.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Singleton, typeof(std_IO))]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_IO : INativeClassSelf
    {
        [SolLibraryVisibility(std.NAME, true)]
        private std_IO() { }

        /// <summary>
        /// The type name is "IO".
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)] public const string TYPE = "IO";

        private const double SECOND_TO_MS = 1000;

        private std_TextStream l_in_stream;
        private std_TextStream l_out_stream;

        /// <summary>
        ///     You can use this <see cref="std_TextStream" /> to manually retrieve data from the default input.
        /// </summary>
        /// <returns>The in stream.</returns>
        [SolContract(std_TextStream.TYPE, false)]
        public std_TextStream in_stream() => l_in_stream ?? (l_in_stream = new std_TextStream(Self.Assembly.Input, Self.Assembly.InputEncoding));

        /// <summary>
        ///     You can use this <see cref="std_TextStream" /> to manually write data to the default output.
        /// </summary>
        /// <returns>The out stream.</returns>
        [SolContract(std_TextStream.TYPE, false)]
        public std_TextStream out_stream() => l_out_stream ?? (l_out_stream = new std_TextStream(Self.Assembly.Output, Self.Assembly.OutputEncoding));

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
        ///     <see cref="SolMetaFunction.__to_string" /> call.
        /// </para>
        /// <returns>The <see cref="std_IO" /> singleton itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        [SolContract(TYPE, false)]
        public std_IO write(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue data)
        {
            SolString solString = data as SolString;
            out_stream().write(context, solString ?? SolString.ValueOf(data.ToString(context)));
            return this;
        }

        /// <inheritdoc cref="std_TextStream.writeln" />
        /// <returns>The <see cref="std_IO" /> singleton itself.</returns>
        /// <exception cref="SolRuntimeException">An error occured while writing or the type is invalid.</exception>
        public std_IO writeln(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue data)
        {
            SolString solString = data as SolString;
            out_stream().writeln(context, solString ?? SolString.ValueOf(data.ToString(context)));
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
                writeln(context, message);
            }
            if (!read_prefix.IsNil()) {
                out_stream().write(context, read_prefix);
            }
            return in_stream().readln(context);
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

        /// <inheritdoc />
        [SolLibraryVisibility(std.NAME, false)]
        public SolClass Self { get; set; }
    }
}