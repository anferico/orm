using System;
using System.IO;

namespace AnnotationsProject
{
	class MainClass
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
