using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter;

namespace SolScript.Compiler
{
    /*public class SolScriptCompiler {
        public readonly SolAssembly Assembly;
        public SolScriptCompiler(SolAssembly assembly) {
            Assembly = assembly;
        }

        public void CompileTo(Stream stream) {
            BinaryWriter writer = new BinaryWriter(stream);
            foreach (ClassDef classDef in Assembly.TypeRegistry.Classes)
            {
                if (classDef.ClrType == null) continue;
                writer.Write(classDef.Name);
                writer.Write((byte)classDef.Mode);
                writer.Write(classDef.Annotations.Length);
                for (int i = 0; i < classDef.Annotations.Length; i++) {
                    writer.Write(classDef.Annotations[i].Name);
                    //writer.Write(classDef.Annotations[i].Arguments[0].);
                }
            }
        }
    }*/
}
