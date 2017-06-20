using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter.Types.Marshal
{
    /// <summary>
    ///     Takes care of converting enums to SolScript strings.
    /// </summary>
    public class NativeEnumMarshaller : ISolNativeMarshaller
    {
        private static readonly PSDictionary<Type, EnumData> s_EnumData = new PSDictionary<Type, EnumData>();

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type.IsEnum;
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolString.TYPE, false);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Found no matching enum representation.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            // todo: support flags
            string text;
            if (!GetEnumData(type).QueryEnum((Enum) value, out text)) {
                throw new SolMarshallingException(type, SolString.TYPE, "Found no matching enum representation: " + value);
            }
            return SolString.ValueOf(text);
        }

        #endregion

        /// <summary>
        ///     Gets the enum data for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The enum data.</returns>
        public static EnumData GetEnumData([NotNull] Type type)
        {
            EnumData data;
            if (!s_EnumData.TryGetValue(type, out data)) {
                s_EnumData[type] = data = new EnumData(type);
            }
            return data;
        }

        #region Nested type: EnumData

        /// <summary>
        ///     Contains easily accessible marshalling data about enums.
        /// </summary>
        public class EnumData
        {
            internal EnumData(Type type)
            {
                Type = type;
                foreach (Enum e in Enum.GetValues(type)) {
                    var attributes = (Attribute[]) type.GetMember(e.ToString()).First(m => m.MemberType == MemberTypes.Field).GetCustomAttributes(typeof(SolLibraryNameAttribute), true);
                    if (attributes.Length > 0) {
                        SolLibraryNameAttribute nameAttribute = (SolLibraryNameAttribute) attributes[0];
                        m_ValueMap.Add(nameAttribute.Name, e);
                        m_NameMap.Add(e, nameAttribute.Name);
                    } else {
                        m_ValueMap.Add(e.ToString(), e);
                        m_NameMap.Add(e, e.ToString());
                    }
                }
            }

            private readonly PSDictionary<Enum, string> m_NameMap = new PSDictionary<Enum, string>();
            private readonly PSDictionary<string, Enum> m_ValueMap = new PSDictionary<string, Enum>();

            /// <summary>
            ///     The enum type represented by this data lookup.
            /// </summary>
            public Type Type { get; }

            /// <summary>
            ///     Gets a read pnly map of this data.
            /// </summary>
            /// <returns>The rad only dictionary.</returns>
            public ReadOnlyDictionary<string, Enum> AsReadOnly() => m_ValueMap.AsReadOnly();

            /// <summary>
            ///     Tries to get the value associated with the given name.
            /// </summary>
            /// <param name="e">The value.</param>
            /// <param name="name">The name.</param>
            /// <returns>Could a value be found?</returns>
            public bool QueryName(string name, out Enum e)
            {
                return m_ValueMap.TryGetValue(name, out e);
            }

            /// <summary>
            ///     Tries to get the name associated with the given enum value.
            /// </summary>
            /// <param name="e">The value.</param>
            /// <param name="name">The name.</param>
            /// <returns>Could a name be found?</returns>
            public bool QueryEnum(Enum e, out string name)
            {
                return m_NameMap.TryGetValue(e, out name);
            }
        }

        #endregion
    }
}