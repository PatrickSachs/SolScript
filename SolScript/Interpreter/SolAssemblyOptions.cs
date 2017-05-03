using System;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Library;
using ps = PSUtility.Enumerables;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Options for creating a SolAssembly.
    /// </summary>
    public sealed class SolAssemblyOptions : ICloneable
    {
        static SolAssemblyOptions()
        {
            // == Methods ==
            // Default
            DefaultFallbackMethodPostProcessor = new NativeMethodPostProcessor.Default(info => true);
            // object Methods
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Fail(info => info.Name == nameof(GetHashCode)));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Fail(info => info.Name == nameof(GetType)));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Fail(info => info.Name == nameof(Equals)));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.RenameAccessReturn(info => info.Name == nameof(Equals),
                SolMetaFunction.__to_string.Name, SolAccessModifier.Internal, SolMetaFunction.__to_string.Type));
            // Annotations
            const SolAccessModifier @internal = SolAccessModifier.Internal;
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Access(info => info.Name == SolMetaFunction.__a_get_variable.Name, @internal));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Access(info => info.Name == SolMetaFunction.__a_set_variable.Name, @internal));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Access(info => info.Name == SolMetaFunction.__a_pre_new.Name, @internal));
            DefaultMethodPostProcessors.Add(new NativeMethodPostProcessor.Access(info => info.Name == SolMetaFunction.__a_post_new.Name, @internal));
            // todo: meta function post processors (detect operators).
            // == Fields ==
            // Default
            DefaultFallbackFieldPostProcessor = new NativeFieldPostProcessor.Default(info => true);
            // Attribute TypeId
            DefaultFieldPostProcessors.Add(new NativeFieldPostProcessor.FailOnType(info => info.Name == nameof(Attribute.TypeId), typeof(Attribute)));
            // Self of INativeClassSelf
            DefaultFieldPostProcessors.Add(new NativeFieldPostProcessor.FailOnInterface(info => info.Name == nameof(INativeClassSelf.Self), typeof(INativeClassSelf)));
        }

        /// <summary>
        ///     Creates a new options instance.
        /// </summary>
        /// <param name="name">The name of the assembly. Used for debugging. </param>
        /// <param name="copyDefaultPostProcessors">
        ///     Should the <see cref="DefaultMethodPostProcessors" />/
        ///     <see cref="DefaultFieldPostProcessors" /> be used in this assembly?
        /// </param>
        /// <exception cref="ArgumentNullException" accessor="set">The <paramref name="name" /> cannot be null.</exception>
        public SolAssemblyOptions([NotNull] string name, bool copyDefaultPostProcessors = true)
        {
            m_SourceFilePattern = "*.sol";
            WarningsAreErrors = false;
            Name = name;
            m_FallbackMethodPostProcessor = s_DefaultFallbackMethodPostProcessor;
            m_FallbackFieldPostProcessor = s_DefaultFallbackFieldPostProcessor;
            if (copyDefaultPostProcessors) {
                MethodPostProcessors.AddRange(DefaultMethodPostProcessors);
                FieldPostProcessors.AddRange(DefaultFieldPostProcessors);
            }
        }

        private static NativeFieldPostProcessor s_DefaultFallbackFieldPostProcessor;
        private static NativeMethodPostProcessor s_DefaultFallbackMethodPostProcessor;
        private NativeFieldPostProcessor m_FallbackFieldPostProcessor;
        private NativeMethodPostProcessor m_FallbackMethodPostProcessor;

        private string m_Name;
        private string m_SourceFilePattern;
        public static ps.PSHashSet<NativeFieldPostProcessor> DefaultFieldPostProcessors { get; } = new ps.PSHashSet<NativeFieldPostProcessor>();
        public static ps.PSHashSet<NativeMethodPostProcessor> DefaultMethodPostProcessors { get; } = new ps.PSHashSet<NativeMethodPostProcessor>();
        public ps.PSHashSet<NativeFieldPostProcessor> FieldPostProcessors { get; } = new ps.PSHashSet<NativeFieldPostProcessor>();
        public ps.PSHashSet<NativeMethodPostProcessor> MethodPostProcessors { get; } = new ps.PSHashSet<NativeMethodPostProcessor>();

        public static NativeFieldPostProcessor DefaultFallbackFieldPostProcessor {
            get { return s_DefaultFallbackFieldPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                s_DefaultFallbackFieldPostProcessor = value;
            }
        }

        public static NativeMethodPostProcessor DefaultFallbackMethodPostProcessor {
            get { return s_DefaultFallbackMethodPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                s_DefaultFallbackMethodPostProcessor = value;
            }
        }

        public NativeFieldPostProcessor FallbackFieldPostProcessor {
            get { return m_FallbackFieldPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                m_FallbackFieldPostProcessor = value;
            }
        }

        /*public T QueryForMethod<T>(MethodInfo method, Func<SolLibrary.NativeMethodPostProcessor, T> func)
        {
            foreach (SolLibrary.NativeMethodPostProcessor processor in MethodPostProcessors) {
                if (processor.AppliesTo(method)) {
                    return func(processor);
                }
            }
            return func(FallbackMethodPostProcessor);
        }*/

        public NativeMethodPostProcessor FallbackMethodPostProcessor {
            get { return m_FallbackMethodPostProcessor; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                m_FallbackMethodPostProcessor = value;
            }
        }

        /// <summary>
        ///     The name of the assembly.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">Cannot set name to null. <paramref name="value" /></exception>
        [NotNull]
        public string Name {
            get { return m_Name; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                m_Name = value;
            }
        }

        /// <summary>
        ///     The wildcard pattern for identifying source files. (Default: "*.sol")
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     Cannot set source file pattern to
        ///     null. <paramref name="value" />
        /// </exception>
        [NotNull]
        public string SourceFilePattern {
            get { return m_SourceFilePattern; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                m_SourceFilePattern = value;
            }
        }

        /// <summary>
        ///     Should warnings be treated as errors? (Default: false)
        /// </summary>
        public bool WarningsAreErrors { get; set; }

        #region ICloneable Members

        /// <inheritdoc />
        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        /// <inheritdoc cref="ICloneable.Clone" />
        public SolAssemblyOptions Clone()
        {
            SolAssemblyOptions options = new SolAssemblyOptions(m_Name) {
                m_SourceFilePattern = m_SourceFilePattern,
                WarningsAreErrors = WarningsAreErrors
            };
            return options;
        }

        /// <summary>
        ///     Gets the post processor used for the given method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The post processor.</returns>
        public NativeMethodPostProcessor GetPostProcessor(MethodInfo method)
        {
            foreach (NativeMethodPostProcessor processor in MethodPostProcessors) {
                if (processor.AppliesTo(method)) {
                    return processor;
                }
            }
            return FallbackMethodPostProcessor;
        }

        /// <summary>
        ///     Gets the post processor used for the given field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>The post processor.</returns>
        public NativeFieldPostProcessor GetPostProcessor(FieldOrPropertyInfo field)
        {
            foreach (NativeFieldPostProcessor processor in FieldPostProcessors) {
                if (processor.AppliesTo(field)) {
                    return processor;
                }
            }
            return FallbackFieldPostProcessor;
        }
    }
}