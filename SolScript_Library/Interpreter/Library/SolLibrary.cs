using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;

namespace SolScript.Interpreter.Library {
    public class SolLibrary {
        public SolLibrary(string libraryName, params Assembly[] sourceAssemblies) {
            Name = libraryName;
            BuildLibrary(sourceAssemblies);
        }

        public readonly Dictionary<string, TypeDef.FieldDef> GlobalFields =
            new Dictionary<string, TypeDef.FieldDef>();

        public readonly Dictionary<string, TypeDef.FuncDef> GlobalFuncs = new Dictionary<string, TypeDef.FuncDef>();

        //private readonly Dictionary<string, MethodInfo> m_functions = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, TypeDef> m_Types = new Dictionary<string, TypeDef>();

        private readonly HashSet<string> s_ExcludedClrFields = new HashSet<string>();

        private readonly HashSet<string> s_ExcludedClrMethods = new HashSet<string> {
            "GetHashCode",
            "GetType"
        };

        public string Name { get; }
        public static SolLibrary StandardLibrary { get; } = new SolLibrary("std", typeof (SolAssembly).Assembly);

        //public ICollection<string> Functions => m_functions.Keys;
        public ICollection<string> Types => m_Types.Keys;

        private void BuildLibrary(Assembly[] assemblies) {
            foreach (Assembly assembly in assemblies) {
                BuildLibrary(assembly);
            }
        }

        private void BuildLibrary(Assembly assembly) {
            foreach (Type type in assembly.GetTypes()) {
                BuildLibrary(type);
            }
        }

        private void BuildLibrary(Type type) {
            // =======================================================
            // == DEFAULT/SINGLETON CLASS
            // =======================================================
            TypeDef.FuncDef[] functions;
            TypeDef.FieldDef[] fields;
            TypeDef.TypeMode mode;
            GenerateDefinitions(type, out functions, out fields, out mode);
            if (functions != null && fields != null) {
                SolLibraryNameAttribute classNameAttribute = type.GetCustomAttribute<SolLibraryNameAttribute>();
                TypeDef typeDef = new TypeDef {
                    ClrType = type,
                    Mode = mode,
                    Name = classNameAttribute?.Name ?? type.Name,
                    Mixins = new string[0], // todo: library mixins
                    Annotations = new TypeDef.AnnotDef[0],
                    Functions = functions,
                    Fields = fields
                };
                m_Types.Add(typeDef.Name, typeDef);
            }
            // =======================================================
            // == GLOBALS CLASS
            // =======================================================
            foreach (
                SolLibraryGlobalsAttribute globalsAttribute in type.GetCustomAttributes<SolLibraryGlobalsAttribute>()) {
                if (globalsAttribute.LibraryName != Name) {
                    continue;
                }
                if (!type.IsAbstract || !type.IsSealed) {
                    // static classes are declared abstract and sealed at the IL level. 
                    // todo: make this a warning somehow, not only a debug message
                    SolDebug.WriteLine("Warning: Type " + type + " contains globals, but the type is not marked as static. Non-Static globals will NOT work and throw very weird errors.");
                }
                var gFunctions =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                    BindingFlags.Instance)
                        .Select(GenerateFuncDef)
                        .Where(funcDef => funcDef != null)
                        .ToArray();
                var gFields =
                    InspectorField.GetInspectorFields(type)
                        .Select(GenerateFieldDef)
                        .Where(fieldDef => fieldDef != null)
                        .ToArray();
                foreach (TypeDef.FuncDef function in gFunctions) {
                    GlobalFuncs.Add(function.Name, function);
                }
                foreach (TypeDef.FieldDef field in gFields) {
                    GlobalFields.Add(field.Name, field);
                }
            }
        }

        private void GenerateDefinitions(Type type, [CanBeNull] out TypeDef.FuncDef[] outFunctions,
            [CanBeNull] out TypeDef.FieldDef[] outFields, out TypeDef.TypeMode outMode) {
            foreach (
                SolLibraryClassAttribute attribute in
                    type.GetCustomAttributes<SolLibraryClassAttribute>()
                        .Where(attribute => attribute.LibraryName == Name)) {
                outFunctions =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                    BindingFlags.Instance)
                        .Select(GenerateFuncDef)
                        .Where(funcDef => funcDef != null)
                        .ToArray();
                outFields =
                    InspectorField.GetInspectorFields(type)
                        .Select(GenerateFieldDef)
                        .Where(fieldDef => fieldDef != null)
                        .ToArray();
                outMode = attribute.Mode;
                return;
            }
            outFunctions = null;
            outFields = null;
            outMode = default(TypeDef.TypeMode);
        }

        [CanBeNull]
        private TypeDef.FuncDef GenerateFuncDef(MethodInfo method) {
            bool visible = method.IsPublic;
            bool didOverrideVisible = false;
            foreach (
                SolLibraryVisibilityAttribute visibilityAttribute in
                    method.GetCustomAttributes<SolLibraryVisibilityAttribute>()) {
                if (visibilityAttribute.LibraryName != Name) {
                    continue;
                }
                visible = visibilityAttribute.Visible;
                didOverrideVisible = true;
            }
            if (!didOverrideVisible) {
                // If the method is explicitly marked as visible despite being on 
                // the exclude list it will be included.
                if (s_ExcludedClrMethods.Contains(method.Name)) {
                    return null;
                }
            }
            if (!visible) {
                return null;
            }
            SolLibraryNameAttribute nameAttribute = method.GetCustomAttribute<SolLibraryNameAttribute>();
            return new TypeDef.FuncDef {
                Name = nameAttribute?.Name ?? method.Name,
                Local = false,
                Annotations = new TypeDef.AnnotDef[0], // todo: library annotations
                Creator2 = method
            };
        }

        [CanBeNull]
        private TypeDef.FieldDef GenerateFieldDef(InspectorField field) {
            bool visible = field.IsPublic;
            bool didOverrideVisible = false;
            foreach (
                SolLibraryVisibilityAttribute visibilityAttribute in
                    field.GetAttributes<SolLibraryVisibilityAttribute>(true)) {
                if (visibilityAttribute.LibraryName != Name) {
                    continue;
                }
                visible = visibilityAttribute.Visible;
                didOverrideVisible = true;
            }
            if (!didOverrideVisible) {
                // If the method is explicitly marked as visible despite being on 
                // the exclude list it will be included.
                if (s_ExcludedClrFields.Contains(field.Name)) {
                    return null;
                }
            }
            if (!visible) {
                return null;
            }
            SolLibraryNameAttribute nameAttribute = field.GetAttribute<SolLibraryNameAttribute>(true);
            return new TypeDef.FieldDef {
                Name = nameAttribute?.Name ?? field.Name,
                Annotations = new TypeDef.AnnotDef[0],
                Creator2 = field,
                Local = false,
                Type = SolMarshal.GetSolType(field.DataType)
            };
        }

        public void Clear() {
            //m_functions.Clear();
            m_Types.Clear();
        }

        /*public MethodInfo GetFunction(string name) {
            MethodInfo value;
            if (m_functions.TryGetValue(name, out value)) {
                return value;
            }
            throw new KeyNotFoundException("No function with the name \"" + name + "\" has been registered.");
        }*/

        public TypeDef GetType(string name) {
            TypeDef value;
            if (m_Types.TryGetValue(name, out value)) {
                return value;
            }
            throw new KeyNotFoundException("No type with the name \"" + name + "\" has been registered.");
        }

        /*public void RegisterType(string name, Type type) {
            m_types.Add(name, type);
        }

        public void RegisterFunction(string name, MethodInfo method) {
            m_functions.Add(name, method);
        }*/
    }
}