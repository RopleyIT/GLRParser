// This source code is based on code written for Ropley Information
// Technology Ltd. (RIT), and is offered for public use without warranty.
// You are entitled to edit or extend this code for your own purposes,
// but use of any unmodified parts of this code does not grant
// the user exclusive rights or ownership of that unmodified code. 
// While every effort has been made to deliver quality software, 
// there is no guarantee that this product offered for public use
// is without defects. The software is provided "as is," and you 
// use the software at your own risk. No warranties are made as to 
// performance, merchantability, fitness for a particular purpose, 
// nor are any other warranties expressed or implied. No oral or 
// written communication from or information provided by RIT 
// shall create a warranty. Under no circumstances shall RIT
// be liable for direct, indirect, special, incidental, or 
// consequential damages resulting from the use, misuse, or 
// inability to use this software, even if RIT has been
// advised of the possibility of such damages. Downloading
// opening or using this file in any way will constitute your 
// agreement to these terms and conditions. Do not use this 
// software if you do not agree to these terms.

using ParserGenerator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Parsing;

/// <summary>
/// Tokenise the input text for the parser grammar
/// </summary>

public class Tokeniser : IEnumerable<IToken>
{
    // The line number within the input stream

    private int lineNumber;

    // Holds the list of tokens, which are pulled from the
    // input stream as soon as the tokeniser is constructed

    private readonly List<IToken> tokenStream;

    // Reference to the lookup table of token names against their
    // integer token values. This is usually constructed from the
    // input grammar and held by the parser.

    private readonly TwoWayMap<string, int> tokenValue;

    /// <summary>
    /// Create an instance of the input grammar tokeniser
    /// </summary>
    /// <param name="sr">The input stream whence the grammar symbols will be read. Note
    /// that the Tokeniser does not close the stream, though it does consume it
    /// entirely, moving the read position to the end of stream.</param>
    /// <param name="terminals">Lookup table of names and integer values
    /// for the terminal tokens</param>

    public Tokeniser(TextReader sr, TwoWayMap<string, int> terminals)
    {
        // Capture the reference to the token name/value lookup table

        if (terminals != null)
            tokenValue = terminals;
        else
            throw new ArgumentNullException(nameof(terminals), "Token name/value map null");

        // Make sure there is a valid input stream for source tokens

        if (sr == null)
            throw new ArgumentException("Tokeniser must be constructed with a readable input stream");

        // Initialise the single character token table

        singleCharTokens =
        [
            new (tokenValue["LBRACE"], "{", 0),
            new (tokenValue["RBRACE"], "}", 0),
            new (tokenValue["LPAREN"], "(", 0),
            new (tokenValue["RPAREN"], ")", 0),
            new (tokenValue["COMMA"], ",", 0),
            new (tokenValue["COLON"], ":", 0),
            new (tokenValue["OR"], "|", 0),
            new (tokenValue["SEMI"], ";", 0),
            new (tokenValue["LBRACK"], "[", 0),
            new (tokenValue["RBRACK"], "]", 0),
            new (tokenValue["AND"], "&", 0),
            new (tokenValue["NOT"], "!", 0),
            new (tokenValue["EQUALS"], "=", 0),
            new (tokenValue["LT"], "<", 0)
        ];

        // Read the entire grammar input string

        input = sr.ReadToEnd();
        nextCharPos = 0;
        lineNumber = 1;

        // Build a complete list of input tokens before parsing

        tokenStream = [];
        IToken tok;
        while ((tok = NextToken()).Type != tokenValue["EOF"])
            tokenStream.Add(tok);

        // Make sure the end of input token appears
        // at the end of the list of tokens

        tokenStream.Add(tok);

        // Release the input string object

        input = null;
    }

    /// <summary>
    /// Return a strongly-typed enumerator over the
    /// set of tokens in the input stream
    /// </summary>
    /// <returns>The token stream enumerator</returns>

    public IEnumerator<IToken> GetEnumerator() => tokenStream.GetEnumerator();

    /// <summary>
    /// Return an enumerator over the
    /// set of tokens in the input stream
    /// </summary>
    /// <returns>The token stream enumerator</returns>

    IEnumerator System.Collections.IEnumerable.GetEnumerator() => tokenStream.GetEnumerator();

    /// <summary>
    /// Return the next input token to the caller
    /// </summary>
    /// <returns>The next token parsed</returns>

    private IToken NextToken()
    {
        // Strip out trailing comments and new lines.
        // Do this repeatedly to skip over blank
        // lines and any lines containing only
        // comments.

        bool parsingEmptyLines = true;
        do
        {
            // Skip over leading white space and
            // trailing comments, up to and including
            // the end of line. Adjust the line number
            // appropriately if an end of line is
            // encountered rather than EOF.

            EatPattern(@"[\t ]*(//.*$)?");
            if (!string.IsNullOrEmpty(EatPattern(@"\r?\n")))
                lineNumber++;
            else
                parsingEmptyLines = false;
        } while (parsingEmptyLines);

        // Check for end of input

        if (EndOfInput())
            return new ParserToken(tokenValue["EOF"], string.Empty, lineNumber);

        // Look for section delimiter keywords

        IToken foundToken;
        if ((foundToken = TryToken("options", "OPTIONS")) != null)
            return foundToken;

        if ((foundToken = TryToken("using", "USING")) != null)
            return foundToken;

        if ((foundToken = TryToken("namespace", "NAMESPACE")) != null)
            return foundToken;

        if ((foundToken = TryToken("parserclass", "PARSERCLASS")) != null)
            return foundToken;

        if ((foundToken = TryToken("fsmclass", "FSMCLASS")) != null)
            return foundToken;

        if ((foundToken = TryToken("(reference)|(assemblyref)", "ASSEMBLYREF")) != null)
            return foundToken;

        if ((foundToken = TryToken("(tokens)|(events)", "TOKENS")) != null)
            return foundToken;

        if ((foundToken = TryToken("(conditions)|(guards)", "CONDITIONS")) != null)
            return foundToken;

        if ((foundToken = TryToken("(grammar)|(rules)", "GRAMMAR")) != null)
            return foundToken;

        // Support for direct FSM description

        if ((foundToken = TryToken("(fsm)|(statemachine)", "FSM")) != null)
            return foundToken;

        if ((foundToken = TryToken("(entry)|(onentry)", "ONENTRY")) != null)
            return foundToken;

        if ((foundToken = TryToken("(exit)|(onexit)", "ONEXIT")) != null)
            return foundToken;

        // Support for GLR parser merge resolution

        if ((foundToken = TryToken("merge", "MERGE")) != null)
            return foundToken;

        // Non-keyword tokens

        if ((foundToken = TryToken(@"\d+", "INTEGER")) != null)
        {
            // Found a string of decimal digits that are
            // not preceded by a dollar symbol. This is treated
            // as an integer token. Hence the conversion of the
            // digit string to a single integer value below.

            foundToken.Value = int.Parse(foundToken.Value.ToString());
            return foundToken;
        }

        if ((foundToken = TryToken(@"[\?\+\*]", "MULTIPLICITY")) != null)
        {
            // Found one of the three characters that represent
            // multiplicity on an input terminal or non-terminal.

            switch (foundToken.Value.ToString())
            {
                case "?":
                    foundToken.Value = Multiplicity.ZeroOrOne;
                    break;
                case "+":
                    foundToken.Value = Multiplicity.OneToMany;
                    break;
                case "*":
                    foundToken.Value = Multiplicity.ZeroToMany;
                    break;
            }
            return foundToken;
        }

        // Look for single character tokens. Handle special cases
        // such as nested braces once token recognised.

        IToken t = EatSingleCharToken();
        if (t != null)
        {
            // Deal with nested code segments

            if (t.Type == tokenValue["LBRACE"])
            {
                if (withinBraces)
                    t = new ParserToken(tokenValue["CODE"], ParseCodeBlock(), lineNumber);
                else
                    withinBraces = true;
            }
            else if (t.Type == tokenValue["RBRACE"])
                withinBraces = false;
            else if (t.Type == tokenValue["LT"])
                t = new ParserToken(tokenValue["TOKENTYPE"], ParseTypeString(), lineNumber);

            // Return the single character token

            return t;
        }

        // Now look for the identifier token

        if ((foundToken = TryToken(@"[a-zA-Z][\.a-zA-Z0-9]*", "IDENTIFIER")) != null)
            return foundToken;

        // Default error handling throws away rest of line

        return TryToken(".*$", "UNKNOWN");
    }

    /// <summary>
    /// Wrap up the test for a text pattern and the creation of
    /// the specified token type if the pattern has been found
    /// </summary>
    /// <param name="pattern">Regular expression pattern to look for</param>
    /// <param name="tokenType">The type of token to generate</param>
    /// <returns>A parser token if matched, or null if not</returns>

    private IToken TryToken(string pattern, string tokenType)
    {
        string tokenString = EatPattern(pattern);
        if (!string.IsNullOrEmpty(tokenString))
            return new ParserToken(tokenValue[tokenType], tokenString, lineNumber);
        else
            return null;
    }

    // The input grammar, which is read as a single
    // string at the beginning of parsing, so that
    // regular expression objects can be used to
    // navigate along it.

    private readonly string input;

    // Character position of next input character
    // within the string variable above.

    private int nextCharPos;

    // A data type appears between '<' and '>' characters
    // occasionally in the grammar, and has to be
    // recognised as a type string. This is further
    // complicated by the fact that these characters are
    // used within a type to represent generic types.

    private string ParseTypeString()
    {
        // Where the type string will be captured

        StringBuilder sb = new();

        // Counts the number of seen less-than characters that have
        // yet to be matched with a greater-than. When this
        // drops to zero, we have captured the type.

        int nestingDepth = 1;

        // Read the input stream, parsing the code
        // block until the ending curly brace is found

        while (nestingDepth > 0)
        {
            // Pull the next input character from the input grammar stream

            int c = NextChar();
            switch (c)
            {
                // End of input stream unexpected during a code block. Let
                // later code handle this when it attempts to read the
                // input stream again.

                case -1:
                    nestingDepth = 0;
                    break;

                // A less-than character increases the nesting depth.
                // Similarly for greater-than characters, the 
                // nesting depth is decremented.


                case '<':
                    nestingDepth++;
                    break;

                case '>':
                    nestingDepth--;
                    break;
            }

            // Provided we have not reached end of input, the character
            // should be appended to the end of the code block

            if (c != -1 && nestingDepth > 0)
                sb.Append((char)c);
        }

        // Now pass back the completed code fragment to the caller

        return sb.ToString();
    }

    /// <summary>
    /// State flag used while parsing a code block in the grammar
    /// </summary>

    private enum CodeBlockState
    {
        InCode,             // Not in comments or strings
        InString,           // Between double quote characters
        EscapeInString,     // Backslash seen while inside a string
        InChar,             // Between single quotes
        EscapeInChar,       // Backslash seen while inside a char
        SlashSeen,          // An opening forward slash has been encountered
        InMultiLineComment, // Within /* .. */ comment
        ClosingStarSeen,    // Asterisk seen while inside /* .. */ comment
        InOneLineComment,   // Between // and end of line
    };

    /// <summary>
    /// Assuming the left curly bracket has already been recognised,
    /// eat the input block of code up to and including the corresponding
    /// closing curly bracket. A code block is assumed to be anything that
    /// begins and ends with a curly brace, and that is nested already
    /// one level inside outermost curly braces. Thus the outermost pair
    /// of curly braces are treated as part of the grammar, while curly
    /// braces nested deeper than this become part of a code block.
    /// Note that C# language semantics are used when consuming the
    /// code block. This means that curly braces within // or /*.. */
    /// comments are ignored, as are curly braces inside string or
    /// character literals, as far as matching curly brace pairs
    /// is concerned.
    /// </summary>
    /// <returns>The code block, complete with opening curly bracket added back to it</returns>

    private string ParseCodeBlock()
    {
        // Where the code block string will be captured

        StringBuilder sb = new();

        // State variable used while parsing the code fragment

        CodeBlockState cbs = CodeBlockState.InCode;

        // Counts the number of seen left curly braces that have
        // yet to be matched with a right curly brace. When this
        // drops to zero, we have captured the code block.

        int nestingDepth = 1;

        // Read the input stream, parsing the code
        // block until the ending curly brace is found

        while (nestingDepth > 0)
        {
            // Pull the next input character from the input grammar stream

            int c = NextChar();
            switch (c)
            {
                // End of input stream unexpected during a code block. Let
                // later code handle this when it attempts to read the
                // input stream again.

                case -1:
                    nestingDepth = 0;
                    break;

                // A left curly brace increases the nesting depth, but
                // only if it is not in a comment, string or character
                // literal. Similarly for right curly braces, but the 
                // nesting depth is decremented.


                case '{':
                case '}':
                    if (cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InCode;
                    if (cbs == CodeBlockState.InCode)
                    {
                        if (c == '{')
                            nestingDepth++;
                        else
                            nestingDepth--;
                    }
                    else if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    break;

                // The slash character has several meanings and thus can cause a
                // number of state changes. It can begin either a single or multi-
                // line comment. It can end a multi-line comment. In other
                // situations it should be just treated as a non-special character.

                case '/':
                    if (cbs == CodeBlockState.InCode)
                        cbs = CodeBlockState.SlashSeen;
                    else if (cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InOneLineComment;
                    else if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InCode;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    break;

                // Handle the asterisk used within opening and closing
                // constructs for multi-line comments

                case '*':
                    if (cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.InMultiLineComment || cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.ClosingStarSeen;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    break;

                // Newline character ends a one line comment

                case '\n':

                    if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs != CodeBlockState.InMultiLineComment)
                        cbs = CodeBlockState.InCode;
                    lineNumber++;
                    break;

                // Opening and closing double quote on a string

                case '\"':
                    if (cbs == CodeBlockState.InCode
                        || cbs == CodeBlockState.EscapeInString
                        || cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InString;
                    else if (cbs == CodeBlockState.InString)
                        cbs = CodeBlockState.InCode;
                    else if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    break;

                // Opening and closing single quote for a character

                case '\'':
                    if (cbs == CodeBlockState.InCode
                        || cbs == CodeBlockState.EscapeInChar
                        || cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InChar;
                    else if (cbs == CodeBlockState.InChar)
                        cbs = CodeBlockState.InCode;
                    else if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    break;

                // Escapes in strings and characters

                case '\\':
                    if (cbs == CodeBlockState.InChar)
                        cbs = CodeBlockState.EscapeInChar;
                    else if (cbs == CodeBlockState.InString)
                        cbs = CodeBlockState.EscapeInString;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    else if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InCode;
                    break;

                // All remaining characters

                default:
                    if (cbs == CodeBlockState.ClosingStarSeen)
                        cbs = CodeBlockState.InMultiLineComment;
                    else if (cbs == CodeBlockState.SlashSeen)
                        cbs = CodeBlockState.InCode;
                    else if (cbs == CodeBlockState.EscapeInChar)
                        cbs = CodeBlockState.InChar;
                    else if (cbs == CodeBlockState.EscapeInString)
                        cbs = CodeBlockState.InString;
                    break;
            }

            // Provided we have not reached end of input, the character
            // should be appended to the end of the code block

            if (c != -1 && nestingDepth > 0)
                sb.Append((char)c);
        }

        // Now pass back the completed code fragment to the caller

        return sb.ToString();
    }

    /// <summary>
    /// Read the input stream
    /// </summary>
    /// <returns>The next character from the input stream,
    /// or -1 if fallen off the end of the stream</returns>

    private int NextChar()
    {
        if (EndOfInput())
            return -1;
        else
            return input[nextCharPos++];
    }

    /// <summary>
    /// Are we at the end of the input stream?
    /// </summary>
    /// <returns>Whether we are at the end of the input stream</returns>

    private bool EndOfInput() => nextCharPos >= input.Length;

    /// <summary>
    /// Given a regular expression pattern, if
    /// the head of the input stream matches this
    /// pattern, return the match, adjusting the
    /// front of the stream. If no match is found,
    /// the input stream remains unadjusted.
    /// </summary>
    /// <param name="pat">The string regular expresssion to look for.</param>
    /// <returns>The matched sequence, or empty if no match at 
    /// the head of the string.</returns>

    private string EatPattern(string pat)
    {
        if (!pat.StartsWith("\\G"))
            pat = "\\G(" + pat + ")";
        return EatPattern(new Regex(pat, RegexOptions.Multiline));
    }

    /// <summary>
    /// Given a regular expression pattern, if
    /// the head of the input stream matches this
    /// pattern, return the match, adjusting the
    /// front of the stream. If no match is found,
    /// the input stream remains unadjusted.
    /// </summary>
    /// <param name="re">The regular expression to look for.</param>
    /// <returns>The matched sequence, or empty if no match at 
    /// the head of the string.</returns>

    private string EatPattern(Regex re)
    {
        Match m = re.Match(input, nextCharPos);
        if (m.Success)
        {
            nextCharPos += m.Length;
            return m.Value;
        }
        else
            return string.Empty;
    }

    /// <summary>
    /// Input consumer for single characters, that
    /// consequently runs much faster than regular
    /// expression matching.
    /// </summary>
    /// <param name="c">The character to look for next</param>
    /// <returns>True if found, after advancing input
    /// pointer. False if not found, without altering
    /// input stream.</returns>

    private bool EatChar(char c)
    {
        if (input[nextCharPos] == c)
        {
            nextCharPos++;
            return true;
        }
        else
            return false;
    }

    // Whether we are in braces

    private bool withinBraces;

    // The list of possible single character tokens
    // that the grammar parser recognises

    private readonly ParserToken[] singleCharTokens;

    /// <summary>
    /// Look through a list of single character tokens that are
    /// valid for the parser grammar. If one is found, return
    /// the corresponding token to the caller.
    /// </summary>
    /// <returns>The token corresponding to the single character
    /// symbol at the front of the input stream. The symbol is
    /// consumed. If no single character token is found, return
    /// a null reference.</returns>

    private IToken EatSingleCharToken()
    {
        foreach (IToken t in singleCharTokens)
        {
            if (EatChar(t.Value.ToString()[0]))
                return new ParserToken(t.Type, t.Value, lineNumber);
        }
        return null;
    }
}
