using System;
using System.IO;

namespace AnnotationsProject
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//string fileContent = File.ReadAllText("annotated_interfaces");
			//var lexan = new LexicalAnalyzer(fileContent);

			//Token token;
			//do
			//{
			//	token = lexan.GetNextToken();
			//	Console.WriteLine(token);
			//} while (token != Token.ENDOFINPUT);

			var parser = new Parser();
			var tableAnnotations = parser.Parse("annotated_interfaces");

			var generator = new CodeGenerator(tableAnnotations);
			generator.GeneratePlainCode();
			generator.GenerateDatabaseSchema();
		}
	}
}