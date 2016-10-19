using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library
{
    [SolLibraryClass("std", TypeDef.TypeMode.Default)]
    [SolLibraryName("File")]
    public class FileHandle {
        public string Path;

        public FileHandle(string path) {
            Path = path;
        }

        public SolString get_path() {
            return new SolString(Path);
        }
    }
}
