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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parsing;
using System.Collections.Generic;
using System.IO;

namespace ParserParserTests;

[TestClass]
public class TokeniserTests
{
    private TwoWayMap<string, int> TokenNames;

    [TestInitialize]
    public void SetupTokens()
    {
        TokenNames = new TwoWayMap<string, int>();

        // Terminal token names and values

        TokenNames.Add("EOF", 0);
        TokenNames.Add("SOF", 1);
        TokenNames.Add("UNKNOWN", 2);
        TokenNames.Add("LBRACE", 3);
        TokenNames.Add("RBRACE", 4);
        TokenNames.Add("LPAREN", 5);
        TokenNames.Add("RPAREN", 6);
        TokenNames.Add("COMMA", 7);
        TokenNames.Add("COLON", 8);
        TokenNames.Add("OR", 9);
        TokenNames.Add("SEMI", 10);
        TokenNames.Add("LBRACK", 11);
        TokenNames.Add("RBRACK", 12);
        TokenNames.Add("AND", 13);
        TokenNames.Add("NOT", 14);
        TokenNames.Add("TOKENS", 15);
        TokenNames.Add("CONDITIONS", 16);
        TokenNames.Add("GRAMMAR", 17);
        TokenNames.Add("IDENTIFIER", 18);
        TokenNames.Add("CODE", 21);
        TokenNames.Add("OPTIONS", 23);
        TokenNames.Add("USING", 24);
        TokenNames.Add("NAMESPACE", 25);
        TokenNames.Add("ACTIONS", 26);
        TokenNames.Add("EQUALS", 27);
        TokenNames.Add("QUESTION", 28);
        TokenNames.Add("STAR", 29);
        TokenNames.Add("PLUS", 30);
        TokenNames.Add("TOKENTYPE", 31);
        TokenNames.Add("LT", 32);
    }

    [TestMethod]
    public void TestParseCodeBlockStrings()
    {
        string testData = "{{{\"{\\\"{\"'{''\\'''{'}}}}AfterString";
        TextReader tr = new StringReader(testData);
        Tokeniser tk = new(tr, TokenNames);

        IEnumerator<IToken> tokIter = tk.GetEnumerator();
        tokIter.MoveNext();
        IToken t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual("{\"{\\\"{\"'{''\\'''{'}", t.Value);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
    }

    [TestMethod]
    public void TestParseStringFormatStrings()
    {
        string testData =
            "{{\"\\t\"}}";

        TextReader tr = new StringReader(testData);
        Tokeniser tk = new(tr, TokenNames);

        IEnumerator<IToken> tokIter = tk.GetEnumerator();
        tokIter.MoveNext();
        IToken t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["CODE"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
    }

    [TestMethod]
    public void TestParseCodeBlockComments()
    {
        string testData = "{{{//{\r\n}/*{\r\n{**/}}AfterString";
        TextReader tr = new StringReader(testData);
        Tokeniser tk = new(tr, TokenNames);
        IEnumerator<IToken> tokIter = tk.GetEnumerator();
        tokIter.MoveNext();
        IToken t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual("{//{\r\n}/*{\r\n{**/", t.Value);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
    }

    [TestMethod]
    public void TestCommentStripping()
    {
        string testData = @"
                // A comment

                fred
                {
                    // Indented comment
                    // Spaced comment
                }
                ";
        TextReader tr = new StringReader(testData);
        Tokeniser tk = new(tr, TokenNames);
        IEnumerator<IToken> tokIter = tk.GetEnumerator();
        tokIter.MoveNext();
        IToken t = tokIter.Current;
        Assert.AreEqual(TokenNames["IDENTIFIER"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
    }

    [TestMethod]
    public void TestTokeniser()
    {
        string testData = "\r\nevents\r\n{\tFRED,\tJ0e\r\n}"
            + "\r\nrules{topRule : FRED {\r\ncode;\r\n}\r\n;}";
        TextReader tr = new StringReader(testData);
        Tokeniser tk = new(tr, TokenNames);
        IEnumerator<IToken> tokIter = tk.GetEnumerator();
        tokIter.MoveNext();
        IToken t = tokIter.Current;
        Assert.AreEqual(TokenNames["TOKENS"], t.Type);
        Assert.AreEqual("2", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        Assert.AreEqual("3", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["IDENTIFIER"], t.Type);
        Assert.AreEqual("FRED", t.Value);
        Assert.AreEqual("3", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["COMMA"], t.Type);
        Assert.AreEqual("3", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["IDENTIFIER"], t.Type);
        Assert.AreEqual("J0e", t.Value);
        Assert.AreEqual("3", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
        Assert.AreEqual("4", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["GRAMMAR"], t.Type);
        Assert.AreEqual("5", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["LBRACE"], t.Type);
        Assert.AreEqual("5", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["IDENTIFIER"], t.Type);
        Assert.AreEqual("topRule", t.Value);
        Assert.AreEqual("5", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["COLON"], t.Type);
        Assert.AreEqual("5", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["IDENTIFIER"], t.Type);
        Assert.AreEqual("FRED", t.Value);
        Assert.AreEqual("5", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["CODE"], t.Type);
        Assert.AreEqual("\r\ncode;\r\n", t.Value);
        Assert.AreEqual("7", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["SEMI"], t.Type);
        Assert.AreEqual("8", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["RBRACE"], t.Type);
        Assert.AreEqual("8", t.Position);
        tokIter.MoveNext();
        t = tokIter.Current;
        Assert.AreEqual(TokenNames["EOF"], t.Type);
    }
}
