using System;
using System.IO;

namespace CodeGeneration
{
    class Program
    {
        public static void Main(string[] args)
        {
            var parser = new Parser();
            var tableAnnotations = parser.Parse("annotated_interfaces");

            var generator = new CodeGenerator(tableAnnotations);
            generator.GeneratePlainCode();
            generator.GenerateDatabaseSchema();
        }
    }
}
