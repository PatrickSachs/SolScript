using System.Collections.Generic;
using System.IO;
using Ionic.Zip;

namespace SolScript.Reader {
    public class SolAssemblyReader {
        public static void ToAssembly(string source, string target) {
            File.Delete(target);
            using (ZipFile zip = new ZipFile(target)) {
                zip.AddEntry(".metadata", "KEK");
                ToAssembly_Recursive(source, string.Empty, zip);
                zip.Save();
            }
        }

        private static void ToAssembly_Recursive(string currentDir, string writePath, ZipFile zip) {
            foreach (string file in Directory.GetFiles(currentDir)) {
                zip.AddFile(file, writePath);
            }
            foreach (string directory in Directory.GetDirectories(currentDir)) {
                ToAssembly_Recursive(directory, writePath + "/" + Path.GetFileName(directory), zip);
            }
        }
    }
}