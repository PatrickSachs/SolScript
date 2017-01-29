using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Annotations
{
    [SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Annotation)]
    [SolLibraryName("prector")]
    public class PreCtorTest
    {
        [SolLibraryAccessModifier(AccessModifier.Internal)]
        [SolContract("table", false)]
        public SolTable __a_pre_new(SolExecutionContext ctx, SolTable args, SolTable rawArgs)
        {
            SolDebug.WriteLine("====================================");
            SolDebug.WriteLine("__a_pre_new");
            SolDebug.WriteLine("=== args:    ===");
            SolDebug.WriteLine(args.ToString(ctx));
            SolDebug.WriteLine("=== rawArgs: ===");
            SolDebug.WriteLine(rawArgs.ToString(ctx));
            return new SolTable();
        }
    }
}
