﻿using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    /// <summary>
    ///     This interface is used to create custom amrshallers for converting native objects into <see cref="SolValue" />s.
    ///     Register your custom marshallers using <see cref="SolMarshal.RegisterMarshaller" />.
    /// </summary>
    /// <remarks>As a general rule of thumb, one Marshaller should typically only be responsible for marshalling one class.</remarks>
    public interface ISolNativeMarshaller
    {
        /// <summary>
        ///     The priority of the the marshaller. Marshallers with a higher priority will be promted to marshal the value before
        ///     ones with a lower value.
        /// </summary>
        /// <remarks><see cref="int.MaxValue" /> and <see cref="int.MinValue" /> are reserved for internal usage.</remarks>
        /// <seealso cref="SolMarshal.PRIORITY_VERY_HIGH" />
        /// <seealso cref="SolMarshal.PRIORITY_HIGH" />
        /// <seealso cref="SolMarshal.PRIORITY_DEFAULT" />
        /// <seealso cref="SolMarshal.PRIORITY_LOW" />
        /// <seealso cref="SolMarshal.PRIORITY_VERY_LOW" />
        int Priority { get; }

        /// <summary>
        ///     Checks if this marshaller can marshal the given type to a <see cref="SolValue" />. If the method returns true
        ///     <see cref="Marshal" /> will be called.
        /// </summary>
        /// <param name="assembly">The assembly we are marshalling the value for.</param>
        /// <param name="type">The type of the object which should be marshalled.</param>
        /// <returns>true if this Marshaller can marshals types of the given type, false if not.</returns>
        bool DoesHandle(SolAssembly assembly, Type type);

        /// <summary>
        ///     Gets the <see cref="SolType" /> associated with values marshalled by this marshaller. All values created by this
        ///     marshaller must be compatible with this type.
        /// </summary>
        /// <param name="assembly">The assembly this information if produced for.</param>
        /// <param name="type">The type which <see cref="SolType" /> we wish to obtain.</param>
        /// <returns>The marshalled <see cref="SolType" /> of <paramref name="type" />.</returns>
        /// <exception cref="SolMarshallingException">
        ///     An error occured while generating the <see cref="SolType"/>. All possible exceptions are wrapped
        ///     inside this exception.
        /// </exception>
        SolType GetSolType(SolAssembly assembly, Type type);

        /// <summary>
        ///     This is the core marshalling method of the marshaller. The method will only be called after a
        ///     <see cref="DoesHandle" /> call returned true.<br />Converts the given value into its <see cref="SolValue" />
        ///     representation.
        /// </summary>
        /// <param name="assembly">The assembly we are marshalling the value for.</param>
        /// <param name="value">The actual value supposed to be marshalled.</param>
        /// <param name="type">The type we wish to marshal to.</param>
        /// <returns>The marshalled SolValue.</returns>
        /// <remarks>Go Wild. These marshallers are supposted to try really hard to marshal the values to SolScript.</remarks>
        /// <exception cref="SolMarshallingException">
        ///     An error occured while marshalling. All possible exceptions are wrapped
        ///     inside this exception.
        /// </exception>
        SolValue Marshal(SolAssembly assembly, object value, Type type);
    }
}