using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CodeGeneration 
{
    public class Parser 
    {
        private LexicalAnalyzer lexan;
        private List<string> basicTypes = new List<string>(){
            "Integer", "Float", "Double", "Char", "String", "Boolean"
        };

        public List<TableAnnotation> Parse(string filePath) 
        {
            if (File.Exists(filePath))
            {
                lexan = new LexicalAnalyzer(File.ReadAllText(filePath));
            }
            else
            {
                throw new Exception($"File \'{filePath}\' does not exist.");
            }
            return dec();
        }

        private List<TableAnnotation> dec() 
        {
            var tables = new List<TableAnnotation>();
            do 
            {
                tables.Add(sdec());
            } while (lexan.Lookahead != Token.ENDOFINPUT);

            foreach (var tabAnn in tables)
            {
                foreach (var membAnn in tabAnn.MemberAnnotations)
                {
                    switch (membAnn.AnnotationName) 
                    {
                        case "Id":
                        case "Column":
                            if (!basicTypes.Contains(membAnn.FieldType))
                            {
                                throw new Exception(
                                    $"Use annotation @{membAnn.AnnotationName} over a basic type only."
                                );                                
                            }
                            break;

                        case "Many2One":
                            if (basicTypes.Contains(membAnn.FieldType))
                            {
                                throw new Exception("Can't use annotation @Many2One over a basic type.");
                            }

                            if (membAnn.FieldType != membAnn.Attributes["target"] ||
                                !tables.Exists(ann => ann.InterfaceName == membAnn.FieldType))
                            {
                                throw new Exception(
                                    "Incorrect value for attribute \"target\" of annotation @Many2One."
                                );
                            }
                            break;

                        case "One2Many":

                            var targetAnnotations = tables.Where(ann =>
                                membAnn.FieldType == $"List<{ann.InterfaceName}>"
                            );

                            if (targetAnnotations.Count() == 0)
                            {
                                throw new Exception(
                                    "Misuse of annotation @One2Many."
                                );
                            }

                            string elementsType = targetAnnotations.First().InterfaceName;
                            if (elementsType != membAnn.Attributes["target"] ||
                                !tables.Exists(ann =>
                                    ann.MemberAnnotations.Exists(memb =>
                                        memb.AnnotationName == "Many2One" &&
                                         memb.Attributes["name"] == membAnn.Attributes["mappedBy"])))
                            {
                                throw new Exception(
                                    "Incorrect values for attributes of annotation @One2Many."
                                );
                            }
                            
                            break;
                    }
                }
            }
            return tables;
        }

        private TableAnnotation sdec() 
        {
            string nameAttribute = table();
            string interfaceModifier = mod();
            Expect(Token.WHITESPACE, Token.INTERFACE, Token.WHITESPACE);
            string interfaceName = str();
            Expect(Token.WHITESPACE);
            var memberAnnotations = members();

            var tableAnnotation = new TableAnnotation(
                "Table", interfaceModifier, interfaceName
            );
            tableAnnotation.Attributes.Add("name", nameAttribute);
            tableAnnotation.MemberAnnotations.AddRange(memberAnnotations);

            return tableAnnotation;
        }

        private string table() 
        {
            Expect(
                Token.AT, 
                Token.TABLE, 
                Token.ROUNDOPEN, 
                Token.NAME, 
                Token.EQUALS, 
                Token.QUOTEDLITERAL
            );

            string nameAttribute = lexan.TokenValue;
            Expect(Token.ROUNDCLOSE);

            return nameAttribute;
        }

        private string mod() 
        {
            Expect(Token.MODIFIER);
            return lexan.TokenValue;
        }

        private List<MemberAnnotation> members() 
        {
            var memberAnnotations = new List<MemberAnnotation>();
            Expect(Token.CURLYOPEN);
            memberAnnotations.Add(idmemb());

            while (lexan.Lookahead != Token.CURLYCLOSE)
            {
                memberAnnotations.Add(memb());
            }

            Expect(Token.CURLYCLOSE);
            return memberAnnotations;
        }

        private MemberAnnotation idmemb() 
        {
            Expect(
                Token.AT, 
                Token.ID, 
                Token.ROUNDOPEN, 
                Token.NAME, 
                Token.EQUALS, 
                Token.QUOTEDLITERAL
            
            );
            string nameAttribute = lexan.TokenValue;

            Expect(Token.ROUNDCLOSE);
            string fieldType = str();

            Expect(Token.WHITESPACE);
            string fieldName = str();

            Expect(Token.SEMICOLON);

            var memberAnnotation = new MemberAnnotation(
                "Id", 
                fieldType, 
                fieldName
            );
            memberAnnotation.Attributes.Add("name", nameAttribute);

            return memberAnnotation;
        }

        private MemberAnnotation memb() 
        {
            Expect(Token.AT);
            switch (lexan.Lookahead) 
            {
                case Token.COLUMN:
                    return colmemb();

                case Token.RELATIONSHIP:
                    return relmemb();

                default:
                    throw new Exception($"Unexpected token: {lexan.Lookahead}.");
            }
        }

        private MemberAnnotation colmemb() 
        {
            Expect(
                Token.COLUMN, 
                Token.ROUNDOPEN, 
                Token.NAME, 
                Token.EQUALS, 
                Token.QUOTEDLITERAL
            );
            string nameAttribute = lexan.TokenValue;
            string lengthAttribute = null;

            if (lexan.Lookahead != Token.ROUNDCLOSE) 
            {
                Expect(
                    Token.COMMA, 
                    Token.WHITESPACE, 
                    Token.LENGTH, 
                    Token.EQUALS, 
                    Token.QUOTEDLITERAL
                );

                int numb = 0;
                if (!int.TryParse(lexan.TokenValue, out numb))
                {
                    throw new Exception("Not a number.");
                }

                if (numb <= 0)
                {
                    throw new Exception("Not a positive number.");
                }

                lengthAttribute = numb.ToString();
            }

            Expect(Token.ROUNDCLOSE);
            string fieldType = str();

            Expect(Token.WHITESPACE);
            string fieldName = str();

            Expect(Token.SEMICOLON);

            var memberAnnotation = new MemberAnnotation(
                "Column", 
                fieldType, 
                fieldName
            );
            memberAnnotation.Attributes.Add("name", nameAttribute);

            if (lengthAttribute != null)
            {
                memberAnnotation.Attributes.Add("length", lengthAttribute);
            }

            return memberAnnotation;
        }

        private MemberAnnotation relmemb() 
        {
            Expect(Token.RELATIONSHIP);
            string relType = lexan.TokenValue;

            Expect(Token.ROUNDOPEN, Token.NAME, Token.EQUALS, Token.QUOTEDLITERAL);
            string nameAttribute = lexan.TokenValue;
            Expect(Token.COMMA, Token.WHITESPACE, Token.TARGET, Token.EQUALS, Token.QUOTEDLITERAL);
            string targetAttribute = lexan.TokenValue;

            string mappedByAttribute = null;
            if (relType == "One2Many") 
            {
                Expect(
                    Token.COMMA, 
                    Token.WHITESPACE, 
                    Token.MAPPEDBY, 
                    Token.EQUALS, 
                    Token.QUOTEDLITERAL
                );
                mappedByAttribute = lexan.TokenValue;
            }

            Expect(Token.ROUNDCLOSE);
            string fieldType = str();

            Expect(Token.WHITESPACE);
            string fieldName = str();

            Expect(Token.SEMICOLON);

            var memberAnnotation = new MemberAnnotation(
                relType, 
                fieldType, 
                fieldName
            );
            memberAnnotation.Attributes.Add("name", nameAttribute);
            memberAnnotation.Attributes.Add("target", targetAttribute);

            if (mappedByAttribute != null)
            {
                memberAnnotation.Attributes.Add("mappedBy", mappedByAttribute);
            }

            return memberAnnotation;
        }

        private string str() 
        {
            Expect(Token.LITERAL);
            return lexan.TokenValue;
        }

        private string num() 
        {
            Expect(Token.LITERAL);

            int numb = 0;
            if (!int.TryParse(lexan.TokenValue, out numb))
            {
                throw new Exception("Not a number.");
            }

            if (numb <= 0)
            {
                throw new Exception("Not a positive number.");
            }

            return lexan.TokenValue;
        }

        private void Expect(params Token[] tokens) 
        {
            foreach (var token in tokens)
            {
                if (lexan.GetNextToken() != token)
                {
                    throw new Exception("Unexpected token.");
                }                
            }
        }
    }
}
