using Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SentenceParser;

/// <summary>
/// An example of a sentence tokeniser that breaks
/// sentences up into individual words. The vocabulary
/// for this sample tokeniser is very limited, being
/// taken from the following words, plus their plurals,
/// and their present and perfect tenses if verbs.
/// NOUNS: cat, dog, rabbit, human
/// VERBS: lick, defend, like, reveal
/// ADJECTIVES: hairy, pink, little, cold
/// ADVERBS: noisily, lovingly, morosely, quickly
/// </summary>

public class SentenceTokeniser : IEnumerable<IToken>
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

    private readonly string input;
    private int nextCharPos;

    /// <summary>
    /// Create an instance of the input grammar tokeniser
    /// </summary>
    /// <param name="sr">The input stream whence the grammar symbols will be read. Note
    /// that the Tokeniser does not close the stream, though it does consume it
    /// entirely, moving the read position to the end of stream.</param>
    /// <param name="terminals">Lookup table of names and integer values
    /// for the terminal tokens</param>

    public SentenceTokeniser(TextReader sr, TwoWayMap<string, int> terminals)
    {
        // Capture the reference to the token name/value lookup table

        if (terminals != null)
            tokenValue = terminals;
        else
            throw new ArgumentNullException(nameof(terminals), "Token name/value map null");

        // Make sure there is a valid input stream for source tokens

        if (sr == null)
            throw new ArgumentException("Tokeniser must be constructed with a readable input stream");

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
    /// Return the next input token to the caller
    /// </summary>
    /// <returns>The next token parsed</returns>

    private IToken NextToken()
    {
        bool parsingEmptyLines = true;
        do
        {
            // Skip over leading white space and
            // trailing comments, up to and including
            // the end of line. Adjust the line number
            // appropriately if an end of line is
            // encountered rather than EOF.

            EatPattern(@"[\t ]*");
            if (!string.IsNullOrEmpty(EatPattern(@"\r?\n")))
                lineNumber++;
            else
                parsingEmptyLines = false;
        } while (parsingEmptyLines);

        // Check for end of input

        if (EndOfInput())
            return new ParserToken(tokenValue["EOF"], string.Empty, lineNumber);

        IToken found;
        if ((found = TryToken("the", "THE")) != null)
            return found;

        if ((found = TryToken("an?", "A")) != null)
            return found;

        if ((found = TryToken("(cats?)|(dogs?)|(rabbits?)|(humans?)", "NOUN")) != null)
            return found;

        if ((found = TryToken("(hairy)|(pink)|(little)|(cold)", "ADJECTIVE")) != null)
            return found;

        if ((found = TryToken("(lick(s|ed)?)|(defend(s|ed)?)|(like(s|ed)?)|(reveal(s|ed)?)", "VERB")) != null)
            return found;

        if ((found = TryToken("(noisily)|(lovingly)|(morosely)|(quickly)", "ADVERB")) != null)
            return found;

        if ((found = TryToken("\\.", "PERIOD")) != null)
            return found;

        return TryToken(".*$", "UNKNOWN");
    }

    /// <summary>
    /// Are we at the end of the input stream?
    /// </summary>
    /// <returns>Whether we are at the end of the input stream</returns>

    private bool EndOfInput() => nextCharPos >= input.Length;

    /// <summary>
    /// Wrap up the test for a text pattern and the creation of
    /// the specified token type if the pattern has been found
    /// </summary>
    /// <param name="pattern">Regular expression pattern to look for</param>
    /// <param name="tokenType">The type of token to generate</param>
    /// <returns>A parser token if matched, or null if not</returns>

    public IToken TryToken(string pattern, string tokenType)
    {
        string tokenString = EatPattern(pattern);
        if (!string.IsNullOrEmpty(tokenString))
            return new ParserToken(tokenValue[tokenType], tokenString, lineNumber);
        else
            return null;
    }

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
        return EatPattern(new Regex(pat, RegexOptions.Multiline | RegexOptions.IgnoreCase));
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

    //private bool EatChar(char c)
    //{
    //    if (input[nextCharPos] == c)
    //    {
    //        nextCharPos++;
    //        return true;
    //    }
    //    else
    //        return false;
    //}

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
}
