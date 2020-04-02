using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace CodeGeneration 
{
    public class CodeGenerator 
    {
        Dictionary<string, string> langToCSharp = new Dictionary<string, string>() {
            {"Integer", "int"}, 
            {"Float", "float"}, 
            {"Double", "double"},
            {"Boolean", "bool"}, 
            {"Char", "char"}, 
            {"String", "string"}
        };

        Dictionary<string, string> langToSQL = new Dictionary<string, string>() {
            {"Integer", "INT"}, 
            {"Float", "FLOAT"}, 
            {"Double", "DOUBLE PRECISION"},
            {"Boolean", "BOOLEAN"}, 
            {"Char", "CHARACTER"}, 
            {"String", "VARCHAR"}
        };

        List<TableAnnotation> tableAnnotations;

        public CodeGenerator(List<TableAnnotation> tableAnnotations) 
        {
            this.tableAnnotations = tableAnnotations;
        }

        public void GeneratePlainCode() 
        {
            foreach (var tabAnn in tableAnnotations) 
            {
                string csharpCode = GenerateInterfaceCode(tabAnn);
                byte[] csharpCodeBinary = Encoding.ASCII.GetBytes(csharpCode);

                FileStream fileStream = File.Create(
                    $"./{tabAnn.InterfaceName}.cs"
                );
                
                fileStream.Write(
                    csharpCodeBinary, 0, csharpCodeBinary.Length
                );

                fileStream.Close();
            }
        }

        private string GenerateInterfaceCode(TableAnnotation tabAnn) 
        {
            var strBuilder = new StringBuilder();

            strBuilder.Append(
                $"using System.Collections.Generic; namespace CodeGeneration {{" +
                $"{tabAnn.InterfaceModifier} class {tabAnn.InterfaceName} {{"
            );

            foreach (var membAnn in tabAnn.MemberAnnotations)
            {
                strBuilder.Append(GenerateInterfaceCode(membAnn));
            }
            
            strBuilder.Append("}}");
            return strBuilder.ToString();
        }

        private string GenerateInterfaceCode(MemberAnnotation membAnn) 
        {
            if (membAnn.AnnotationName == "One2Many" ||
                membAnn.AnnotationName == "Many2One")
            {
                return $"public {membAnn.FieldType} {membAnn.FieldName};";   
            }

            return $"public {langToCSharp[membAnn.FieldType]} {membAnn.FieldName};";
        }

        public void GenerateDatabaseSchema() 
        {
            foreach (var tabAnn in tableAnnotations) 
            {
                string sqlCode = GenerateSQLCode(tabAnn);
                byte[] sqlCodeBinary = Encoding.ASCII.GetBytes(sqlCode);

                FileStream fileStream = File.Create(
                    $"./{tabAnn.Attributes["name"]}.sql"
                );
                
                fileStream.Write(
                    sqlCodeBinary, 0, sqlCodeBinary.Length
                );

                fileStream.Close();
            }
        }

        private string GenerateSQLCode(TableAnnotation tabAnn) 
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append($"CREATE TABLE {tabAnn.Attributes["name"]} (");

            for (int i = 0; i < tabAnn.MemberAnnotations.Count; i++) 
            {
                string attrCode = GenerateSQLCode(tabAnn.MemberAnnotations[i]);
                if (string.IsNullOrEmpty(attrCode) && strBuilder.Length >= 2)
                {
                    strBuilder.Remove(strBuilder.Length - 2, 2);
                }
                else
                {
                    strBuilder.Append(attrCode);
                }

                if (i < tabAnn.MemberAnnotations.Count - 1)
                {
                    strBuilder.Append(", ");
                }
            }

            strBuilder.Append(");");
            return strBuilder.ToString();
        }

        private string GenerateSQLCode(MemberAnnotation membAnn) 
        {
            var strBuilder = new StringBuilder();
            switch (membAnn.AnnotationName) 
            {
                case "Id":
                    strBuilder.Append(
                        $"{membAnn.Attributes["name"]} {GetSQLType(membAnn.FieldType)} " +
                        "NOT NULL PRIMARY KEY"
                    );

                    break;

                case "Column":
                    strBuilder.Append($"{membAnn.Attributes["name"]} {GetSQLType(membAnn.FieldType)}");

                    if (membAnn.Attributes.ContainsKey("length") && membAnn.FieldType == "String")
                    {
                        strBuilder.Append($"({membAnn.Attributes["length"]})");
                    }

                    break;

                case "Many2One":
                    string name = membAnn.Attributes["name"];
                    strBuilder.Append($"{name} ");

                    var tabAnn = tableAnnotations.Find(ann =>
                        ann.InterfaceName == membAnn.Attributes["target"]
                    );

                    var referencedMemb = tabAnn.MemberAnnotations.Find(mem =>
                        mem.AnnotationName == "Id"
                    );

                    string sqlType = langToSQL[referencedMemb.FieldType];
                    strBuilder.Append($"{sqlType}, FOREIGN KEY ({name}) REFERENCES " +
                                      $"{tabAnn.Attributes["name"]}" +
                                      $"({referencedMemb.Attributes["name"]})");
                    
                    break;
            }

            return strBuilder.ToString();
        }

        private string GetCSharpType(string type) 
        {
            if (!langToCSharp.ContainsKey(type))
            {
                throw new Exception($"Unknown type {type}.");
            }

            return langToCSharp[type];
        }

        private string GetSQLType(string type) 
        {
            if (!langToSQL.ContainsKey(type))
            {
                throw new Exception($"Unknown type {type}.");
            }

            return langToSQL[type];
        }
    }
}
