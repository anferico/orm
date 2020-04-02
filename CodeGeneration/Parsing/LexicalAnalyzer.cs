using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGeneration 
{
	public class LexicalAnalyzer 
    {
		private string inputString;
		private int cursorPosition = 0;
		private List<char> redundantCharacters = new List<char>() {
			'\n', '\t', '\r'
		};
		private List<char?> specialCharacters = new List<char?>() {
			'@', '(', ')', '=', '"', ' ', '{', '}', ',', ';', null
		};
		private Token? lookahead;
		public string TokenValue;
		private string nextTokenValue;
		private bool withinParentheses = false;

		public LexicalAnalyzer(string inputString) 
        {
			this.inputString = inputString;
		}

		private char? Peek() 
        {
			if (inputString.Length > cursorPosition)
			{
                return inputString[cursorPosition];
            }
			return null;
		}

		private void BuildNextToken() 
        {
			char? currentChar;
			do 
            {
				currentChar = Peek();
				cursorPosition++;
			} while (currentChar != null && redundantCharacters.Contains((char)currentChar));
			
            nextTokenValue = null;

			switch (currentChar) 
            {
				case '@':
					lookahead = Token.AT;
					break;

				case '(':
					lookahead = Token.ROUNDOPEN;
					withinParentheses = true;
					break;

				case ')':
					lookahead = Token.ROUNDCLOSE;
					withinParentheses = false;
					break;

				case '=':
					lookahead = Token.EQUALS;
					break;

				case '"':
					var strBuilder = new StringBuilder();
					currentChar = Peek();

					while (currentChar != null && currentChar != '"') 
                    {
						strBuilder.Append(currentChar);
						cursorPosition++;
						currentChar = Peek();
					}
					cursorPosition++;

					lookahead = Token.QUOTEDLITERAL;
					nextTokenValue = strBuilder.ToString();
					break;

				case ' ':
					lookahead = Token.WHITESPACE;
					break;

				case '{':
					lookahead = Token.CURLYOPEN;
					break;

				case '}':
					lookahead = Token.CURLYCLOSE;
					break;

				case ',':
					lookahead = Token.COMMA;
					break;

				case ';':
					lookahead = Token.SEMICOLON;
					break;

				case null:
					lookahead = Token.ENDOFINPUT;
					break;

				default:
					strBuilder = new StringBuilder();
					while (currentChar != null && 
                          !specialCharacters.Contains(currentChar)) 
                    {
						strBuilder.Append(currentChar);
						currentChar = Peek();
						cursorPosition++;
					}
					cursorPosition--;

					string parsedString = strBuilder.ToString();
					switch (parsedString) 
                    {
						case "Table":
							lookahead = Token.TABLE;
							break;

						case "Id":
							lookahead = Token.ID;
							break;

						case "Column":
							lookahead = Token.COLUMN;
							break;

						case "One2Many":
						case "Many2One":
							lookahead = Token.RELATIONSHIP;
							nextTokenValue = parsedString;
							break;

						case "name":
							if (withinParentheses)
							{
                                lookahead = Token.NAME;
                            }
							else 
                            {
								lookahead = Token.LITERAL;
								nextTokenValue = parsedString;
							}
							break;

						case "length":
							if (withinParentheses)
							{
                                lookahead = Token.LENGTH;
                            }
							else 
                            {
								lookahead = Token.LITERAL;
								nextTokenValue = parsedString;
							}
							break;

						case "target":
							if (withinParentheses)
							{
                                lookahead = Token.TARGET;
                            }
							else 
                            {
								lookahead = Token.LITERAL;
								nextTokenValue = parsedString;
							}
							break;

						case "mappedBy":
							if (withinParentheses)
							{
                                lookahead = Token.MAPPEDBY;
                            }
							else 
                            {
								lookahead = Token.LITERAL;
								nextTokenValue = parsedString;
							}
							break;

						case "private":
						case "protected":
						case "public":
							lookahead = Token.MODIFIER;
							nextTokenValue = parsedString;
							break;

						case "interface":
							lookahead = Token.INTERFACE;
							break;

						default:
							lookahead = Token.LITERAL;
							nextTokenValue = parsedString;
							break;
					}
					break;
			}
		}

		public Token GetNextToken() 
        {
			Token nextToken = Lookahead;
			TokenValue = nextTokenValue;
			BuildNextToken();
			return nextToken;
		}

		public Token Lookahead 
        {
			get 
            {
				if (lookahead == null)
				{
                    BuildNextToken();
                }
				return (Token)lookahead;
			}
		}
	}
}