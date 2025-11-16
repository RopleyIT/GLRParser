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
using System.IO;
using System.Text;

namespace ParserParserTests;

/// <summary>
/// Summary description for InlineTests
/// </summary>
[TestClass]
public class InlineTests(TestContext context)
{
    /// <summary>
    /// Gets or sets the test context which provides
    /// information about and functionality for the current test run.
    /// </summary>

    public TestContext TestContext { get; set; } = context;

    #region Additional test attributes
    //
    // You can use the following additional attributes as you write your tests:
    //
    // Use ClassInitialize to run code before running the first test in the class
    // [ClassInitialize()]
    // public static void MyClassInitialize(TestContext testContext) { }
    //
    // Use ClassCleanup to run code after all tests in a class have run
    // [ClassCleanup()]
    // public static void MyClassCleanup() { }
    //
    // Use TestInitialize to run code before running each test 
    // [TestInitialize()]
    // public void MyTestInitialize() { }
    //
    // Use TestCleanup to run code after each test has run
    // [TestCleanup()]
    // public void MyTestCleanup() { }
    //
    #endregion

    private readonly string testGrammar = @"
            options
            {
                using Parsing,
                using ParserGenerator, 
                using BooleanLib,
                namespace ParserParserTests,
                parserclass InlineTestParser
            }
            events
            {
                UNKNOWN = 5,
                NOUN = 10,
                VERB = 15,
                ADJECTIVE = 20,
                ADVERB = 25,
                THE,
                A,
                PERIOD
            }
            guards
            {
                PluralNoun,
                SingularVerb,
                PluralVerb,
                Past
            }
            grammar(para)
            {
                para: 
                    sentence
                |   para sentence
                ;

                sentence:
                    presentSentence 
                    { BumpPresentTense(); }
                |   pastSentence 
                    { BumpPastTense(); }
                ;

                presentSentence:
                    nounPhrase[!PluralNoun] verb[!Past & SingularVerb] nounPhrase PERIOD
                    { BumpSingular(); }
                |   nounPhrase[PluralNoun] verb[!Past & PluralVerb] nounPhrase PERIOD
                    { BumpPlural(); }
                ;

                pastSentence:
                    nounPhrase[!PluralNoun] verb[Past & SingularVerb] nounPhrase PERIOD
                    { BumpSingular(); }
                |   nounPhrase[PluralNoun] verb[Past & PluralVerb] nounPhrase PERIOD
                    { BumpPlural(); }
                ;

                nounPhrase:
                    THE adjectives NOUN
                |   A adjectives NOUN
                |   adjectives NOUN[PluralNoun]
                ;

                adjectives:
                    adjectives ADJECTIVE
                    { AppendAdjective($1.ToString()); BumpAdjectives(); }
                |
                ;

                verb:
                    adverbs VERB
                ;

                adverbs:
                    ADVERB
                |
                ;
            }
        ";

    public string multiplicityGrammar = @"
            options
            {
                using Parsing,
                using ParserGenerator, 
                using BooleanLib,
                namespace ParserParserTests,
                parserclass InlineTestParser
            }
            events
            {
                UNKNOWN = 5,
                NOUN<string> = 10,
                VERB<string> = 15,
                ADJECTIVE<string> = 20,
                ADVERB<string> = 25,
                THE,
                A,
                PERIOD
            }
            guards
            {
                PluralNoun,
                SingularVerb,
                PluralVerb,
                Past
            }
            grammar(para)
            {
                para: sentence +
                ;

                sentence:
                    presentSentence 
                    { BumpPresentTense(); }
                |   pastSentence 
                    { BumpPastTense(); }
                ;

                presentSentence:
                    nounPhrase[!PluralNoun] verb[!Past & SingularVerb] nounPhrase PERIOD
                    { BumpSingular(); }
                |   nounPhrase[PluralNoun] verb[!Past & PluralVerb] nounPhrase PERIOD
                    { BumpPlural(); }
                ;

                pastSentence:
                    nounPhrase[!PluralNoun] verb[Past & SingularVerb] nounPhrase PERIOD
                    { BumpSingular(); }
                |   nounPhrase[PluralNoun] verb[Past & PluralVerb] nounPhrase PERIOD
                    { BumpPlural(); }
                ;

                nounPhrase:
                    THE adjectives NOUN
                |   A adjectives NOUN
                |   adjectives NOUN[PluralNoun]
                ;

                adjectives: 
                    ADJECTIVE *
                    { 
                        IList<string> adjectives = $0;
                        AppendAdjectiveList(adjectives); BumpAdjectiveList(adjectives.Count); 
                    }
                ;

                verb: 
                    ADVERB ?  VERB
                    {
                        $$ = $1;
                    }
                ;
            }
        ";

    [TestMethod]
    public void TestSentenceOffline()
    {
        StringBuilder tableString = new();
        TextWriter tabOut = new StringWriter(tableString);
        StringBuilder debugString = new();
        TextWriter dbgOut = new StringWriter(debugString);
        StringBuilder parserString = new();
        TextWriter parserOut = new StringWriter(parserString);
        string err =
            ParserFactory.CreateOfflineParser
            (
                testGrammar,
                parserOut,
                tabOut,
                dbgOut,
                true,
                false,
                false,
                out _
            );
        Assert.StartsWith("Parser created with warnings:", err);
    }

    [TestMethod]
    public void TestMultiplicityInlineParser()
    {
        StringBuilder tableString = new();
        TextWriter tabOut = new StringWriter(tableString);
        StringBuilder srcString = new();
        TextWriter srcOut = new StringWriter(srcString);
        StringBuilder errString = new();

        ParserFactory<InlineTestParser>.InitializeFromGrammar
        (
            new StringReader(multiplicityGrammar),
            tabOut,
            false,
            false,
            false,
            srcOut
        );
        InlineTestParser itp = ParserFactory<InlineTestParser>.CreateInstance();

        Assert.AreEqual(0, errString.Length);
        Assert.IsNotNull(itp);

        // Grammar based on the words: cat dog rabbit human,
        // a the hairy pink little cold lick defend like reveal,
        // noisily, lovingly, quickly, morosely.

        string grammarInput =
            "the cold rabbit reveals a human. the hairy pink dogs lovingly licked the cat.";

        // Create the input tokeniser, connecting it to the
        // list of terminal token values from the parser

        TestTokeniser tt = new(new StringReader(grammarInput), itp.Tokens);

        // Now execute the parse

        bool result = itp.Parse(tt);
        Assert.IsTrue(result);
        Assert.AreEqual(3, itp.AdjectivesCount);
        Assert.AreEqual(1, itp.PastCount);
        Assert.AreEqual(1, itp.PresentCount);
        Assert.AreEqual(1, itp.SingularCount);
        Assert.AreEqual(1, itp.PluralCount);
        Assert.AreEqual("pink", itp.AdjectiveList[2]);

        // Now perform the clone test

        InlineTestParser clone = ParserFactory<InlineTestParser>.CreateInstance();
        tt = new TestTokeniser(new StringReader(grammarInput), itp.Tokens);

        // Now execute the parse using the clone

        result = clone.Parse(tt);
        Assert.IsTrue(result);
        Assert.AreEqual(3, clone.AdjectivesCount);
        Assert.AreEqual(1, clone.PastCount);
        Assert.AreEqual(1, itp.PresentCount);
        Assert.AreEqual(1, clone.SingularCount);
        Assert.AreEqual(1, clone.PluralCount);
        Assert.AreEqual("pink", clone.AdjectiveList[2]);

        // Validate the allocation of token values

        Assert.AreEqual(25, clone.ParserTable.Tokens["ADVERB"]);
        Assert.AreEqual(16388, clone.ParserTable.Tokens["THE"]);
    }

    [TestMethod]
    public void TestCloneInlineParser()
    {
        StringBuilder tableString = new();
        TextWriter tabOut = new StringWriter(tableString);
        StringBuilder srcString = new();
        TextWriter srcOut = new StringWriter(srcString);

        ParserFactory<InlineTestParser>.InitializeFromGrammar
        (
            new StringReader(testGrammar),
            tabOut,
            false,
            false,
            false,
            srcOut
        );

        InlineTestParser itp = ParserFactory<InlineTestParser>.CreateInstance();

        Assert.AreEqual(0, ParserFactory<InlineTestParser>.CompilerErrors.Length);
        Assert.IsNotNull(itp);

        // Grammar based on the words: cat dog rabbit human,
        // a the hairy pink little cold lick defend like reveal,
        // noisily, lovingly, quickly, morosely.

        string grammarInput =
            "the cold rabbit reveals a human.";

        // Create the input tokeniser, connecting it to the
        // list of terminal token values from the parser

        TestTokeniser tt = new(new StringReader(grammarInput), itp.Tokens);

        // Now execute the parse

        bool result = itp.Parse(tt);
        Assert.IsTrue(result);
        Assert.AreEqual(1, itp.AdjectivesCount);
        Assert.AreEqual(0, itp.PastCount);
        Assert.AreEqual(1, itp.SingularCount);
        Assert.AreEqual(0, itp.PluralCount);
        Assert.AreEqual("cold", itp.AdjectiveList[0]);

        // Now perform the clone test

        InlineTestParser clone = ParserFactory<InlineTestParser>.CreateInstance();
        tt = new TestTokeniser(new StringReader(grammarInput), clone.Tokens);

        // Now execute the parse using the clone

        result = clone.Parse(tt);
        Assert.IsTrue(result);
        Assert.AreEqual(1, clone.AdjectivesCount);
        Assert.AreEqual(0, clone.PastCount);
        Assert.AreEqual(1, clone.SingularCount);
        Assert.AreEqual(0, clone.PluralCount);
        Assert.AreEqual("cold", clone.AdjectiveList[0]);

        // Validate the allocation of token values

        Assert.AreEqual(25, clone.ParserTable.Tokens["ADVERB"]);
        Assert.AreEqual(16388, clone.ParserTable.Tokens["THE"]);
    }

    [TestMethod]
    public void TestPluralInlineParser()
    {
        StringBuilder tableString = new();
        TextWriter tabOut = new StringWriter(tableString);
        StringBuilder srcString = new();
        TextWriter srcOut = new StringWriter(srcString);

        ParserFactory<InlineTestParser>.InitializeFromGrammar
        (
            new StringReader(testGrammar),
            tabOut,
            false,
            false,
            false,
            srcOut
        );
        InlineTestParser itp = ParserFactory<InlineTestParser>.CreateInstance();

        Assert.AreEqual(0, ParserFactory<InlineTestParser>.CompilerErrors.Length);
        Assert.IsNotNull(itp);

        // Grammar based on the words: cat dog rabbit human,
        // a the hairy pink little cold lick defend like reveal,
        // noisily, lovingly, quickly, morosely.

        string grammarInput =
            "the little cats quickly defended the hairy pink dog.";

        // Create the input tokeniser, connecting it to the
        // list of terminal token values from the parser

        TestTokeniser tt = new(new StringReader(grammarInput), itp.Tokens);

        // Now execute the parse

        bool result = itp.Parse(tt);
        Assert.IsTrue(result);
        Assert.AreEqual(3, itp.AdjectivesCount);
        Assert.AreEqual(1, itp.PastCount);
        Assert.AreEqual(0, itp.SingularCount);
        Assert.AreEqual(1, itp.PluralCount);
        Assert.AreEqual("pink", itp.AdjectiveList[2]);
    }
}
