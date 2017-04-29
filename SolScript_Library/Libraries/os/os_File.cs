// ReSharper disable InconsistentNaming

using System;
using System.IO;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Libraries.std;

namespace SolScript.Libraries.os
{
    /// <summary>
    ///     This class represents a file Handle in the <see cref="os" /> standard library. A file handle is used to create
    ///     new os, read existing os or append data to os.
    /// </summary>
    [SolTypeDescriptor(os.NAME, SolTypeMode.Default, typeof(os_File))]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class os_File
    {
        /// <summary>
        ///     Creates a new File handle.
        /// </summary>
        /// <param name="location">The path to the os contents.</param>
        /// <param name="mode">The file mode.</param>
        /// <seealso cref="get_mode" />
        public os_File([SolContract(SolString.TYPE, false)] SolString location, [SolContract(SolString.TYPE, false)] SolString mode)
        {
            m_Location = location;
            switch (mode.Value) {
                case "a":
                    m_Mode = FileMode.Append;
                    break;
                case "a+":
                    m_Mode = FileMode.Truncate;
                    break;
                case "c":
                    m_Mode = FileMode.Create;
                    break;
                case "c+":
                    m_Mode = FileMode.CreateNew;
                    break;
                case "o":
                    m_Mode = FileMode.Open;
                    break;
                case "o+":
                    m_Mode = FileMode.OpenOrCreate;
                    break;
            }
        }

        /// <summary>
        ///     The type name is "File".
        /// </summary>
        [SolLibraryVisibility(std.std.NAME, false)] public const string TYPE = "File";

        private static readonly SolString Str_mode_append = SolString.ValueOf("a").Intern();
        private static readonly SolString Str_mode_appendPlus = SolString.ValueOf("a+").Intern();
        private static readonly SolString Str_mode_create = SolString.ValueOf("c").Intern();
        private static readonly SolString Str_mode_createPlus = SolString.ValueOf("c+").Intern();
        private static readonly SolString Str_mode_open = SolString.ValueOf("o").Intern();
        private static readonly SolString Str_mode_openPlus = SolString.ValueOf("o+").Intern();

        private readonly SolString m_Location;
        private readonly FileMode m_Mode;

        private bool m_IsLoaded;
        private std_Stream m_SolStream;
        private FileStream m_Stream;

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return "File @ " + m_Location.Value + " (" + (m_IsLoaded ? "loaded" : "not loaded") + ")";
        }

        #endregion

        /// <summary>
        ///     Returns a <see cref="std_Stream" /> to this file.
        /// </summary>
        /// <returns>The <see cref="std_Stream" />.</returns>
        [SolContract(std_Stream.TYPE, false)]
        public std_Stream get_stream()
        {
            return m_SolStream ?? (m_SolStream = new std_Stream(m_Stream));
        }

        /// <summary>
        ///     Gets the file mode.
        /// </summary>
        /// <returns>A string representing the used file mode.</returns>
        /// <remarks>Avilable file modes: a(Append), a+(Truncate), c(Create), c+(Create New), o(Open) and o+(Open Or Create).</remarks>
        [SolContract(SolString.TYPE, false)]
        public SolString get_mode()
        {
            switch (m_Mode) {
                case FileMode.CreateNew:
                    return Str_mode_createPlus;
                case FileMode.Create:
                    return Str_mode_create;
                case FileMode.Open:
                    return Str_mode_open;
                case FileMode.OpenOrCreate:
                    return Str_mode_open;
                case FileMode.Truncate:
                    return Str_mode_appendPlus;
                case FileMode.Append:
                    return Str_mode_append;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Is this file loaded? Only loaded os are safe for usage.
        /// </summary>
        /// <returns>true if the file has been loaded, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool is_loaded()
        {
            return SolBool.ValueOf(m_IsLoaded);
        }

        /// <summary>
        ///     Opens the file.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>Itself.</returns>
        /// <exception cref="SolRuntimeException">The file was already loaded.</exception>
        [SolContract(TYPE, false)]
        public os_File open(SolExecutionContext context)
        {
            if (m_IsLoaded) {
                throw new SolRuntimeException(context, "The file at \"" + m_Location.Value + "\" was already opened.");
            }
            try {
                m_Stream = File.Open(m_Location.Value, m_Mode, FileAccess.ReadWrite);
            } catch (DirectoryNotFoundException ex) {
                throw new SolRuntimeException(context, "The file at \"" + m_Location.Value + "\" does not exist.", ex);
            } catch (FileNotFoundException ex) {
                throw new SolRuntimeException(context, "The file at \"" + m_Location.Value + "\" does not exist.", ex);
            } catch (IOException ex) {
                throw new SolRuntimeException(context, "An error occured while opening the file at \"" + m_Location.Value + "\".", ex);
            } catch (UnauthorizedAccessException ex) {
                throw new SolRuntimeException(context, "No access to the file at \"" + m_Location.Value + "\".", ex);
            } catch (NotSupportedException ex) {
                throw new SolRuntimeException(context, "The path of file \"" + m_Location.Value + "\" is in an invalid format.", ex);
            } catch (ArgumentException ex) {
                throw new SolRuntimeException(context, "The path of file \"" + m_Location.Value + "\" is in an invalid format.", ex);
            }
            m_IsLoaded = true;
            return this;
        }

        public os_File close()
        {
            m_IsLoaded = false;
            m_Stream.Dispose();
            m_Stream = null;
            return this;
        }
    }
}