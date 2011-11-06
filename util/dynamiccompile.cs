/*
 * Copyright (c) 2010 Jopirop
 * 
 * All rights reserved.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;
using System.Reflection;

namespace Cyclops {
    /// <summary>
    /// This class allows compiling code from text files and using it
    /// in the application.
    /// </summary>
    public class DynamicCompile {
        private delegate Object CompileDelegate(Object args);
        private const string ASSEMBLY_ENDING = ".asm";
        private const string ASSEMBLY_FOLDER = "asm";

        /// <summary>
        /// The code header used for the file.
        /// </summary>
        /// <returns>The code header.</returns>
        private string GetHeader() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using System;");
            stringBuilder.Append("using Cyclops;");
            stringBuilder.Append("using System.Collections.Generic;");
            stringBuilder.Append("using System.Text;");
            stringBuilder.Append("namespace Dynamic {");
            stringBuilder.Append("  class DynamicCode {");
            stringBuilder.Append("      static public Object Get(Object arg) {");
            return stringBuilder.ToString();

        }

        /// <summary>
        /// Gets the code footer used.
        /// </summary>
        /// <returns>The code footer.</returns>
        private string GetFooter() {
            return "}}}";
        }

        private Object CompileCode(string rawCode, Object args, string path) {
            if (path.EndsWith(ASSEMBLY_ENDING)) {
                return null;
            }
            path = Path.GetFullPath(path);
            string name = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(path);
            string asmDirectory = directory + "/"  + ASSEMBLY_FOLDER;
            string asmEquivalent = asmDirectory + "/" + name + ASSEMBLY_ENDING;
			//string asmEquivalent = name + ASSEMBLY_ENDING;
            if (!Directory.Exists(asmDirectory)) {
                Directory.CreateDirectory(asmDirectory);
            }


            Module module = null;
            if (File.Exists(asmEquivalent)) {
                module = Assembly.LoadFile(asmEquivalent).GetModules()[0];
            } else {
                CompilerParameters CompilerParams = new CompilerParameters();
                string outputDirectory = Directory.GetCurrentDirectory();
                string code = GetHeader();
                code += rawCode;
                code += GetFooter();

                CompilerParams.GenerateInMemory = true;
                CompilerParams.TreatWarningsAsErrors = false;
                CompilerParams.GenerateExecutable = false;
                CompilerParams.CompilerOptions = "/optimize";
                CompilerParams.OutputAssembly = asmEquivalent;

                //TODO: Find out .exe name
                string[] references = { "System.dll", "cyclops.exe" };
                CompilerParams.ReferencedAssemblies.AddRange(references);

                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerResults compile = provider.
                    CompileAssemblyFromSource(CompilerParams, new string[] { code });
                if (compile.Errors.HasErrors) {
                    string text = "Compile error: ";
                    foreach (CompilerError ce in compile.Errors) {
                        text += "Error: " + ce.ToString() + "\n";
                    }
                    Tracer.Println(text);
                    throw new Exception(text);
                }
                module = compile.CompiledAssembly.GetModules()[0];
            }

            Type mt = null;
            MethodInfo methInfo = null;

            if (module != null) {
                mt = module.GetType("Dynamic.DynamicCode");
            }

            if (mt != null) {
                methInfo = mt.GetMethod("Get");
            }

            if (methInfo != null) {
                Object objToReturn = methInfo.Invoke(null, new object[] { args });
                return objToReturn;
            }

            return null;
        }

        /// <summary>
        /// Constructor used to initialize this object.
        /// </summary>
        public DynamicCompile() {
        }


        /// <summary>
        /// Given a path to a file, this method returns an Object
        /// compiled by that file.
        /// </summary>
        /// <param name="path">The path and name of the file.</param>
        /// <param name="args">Any arguments supplied to the file.</param>
        /// <returns>The compiled object.</returns>
        public Object Compile(string path, Object args) {
           TextReader tReader = new StreamReader(path);
           return CompileCode(tReader.ReadToEnd(), args, path);
        }
    }
}
