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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLRParserTests
{
    [TestClass]
    public class GLRTests
    {
        private static readonly string EEiGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiConflictedParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }
            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                ;
            }
        ";

        [TestMethod]
        public void TestEEiForConflicts()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            Assert.ThrowsExactly<ArgumentException>(() => ParserFactory<EEiConflictedParser>.InitializeFromGrammar
            (
                new StringReader(EEiGrammar),
                tabOut,
                false,
                false,
                false,
                srcOut
            ));
        }

        private static readonly string EEiGLRGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiGLRParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }
            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                ;
            }
        ";

        [TestMethod]
        public void TestEEiGLRParserForNoConflicts()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                new StringReader(EEiGLRGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );
            Assert.AreEqual(0, ParserFactory<EEiGLRParser>.CompilerErrors.Length);
            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();
            Assert.IsNotNull(itp);
        }

        [TestMethod]
        public void TestEEiGLRParse()
        {
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                EEiGLRGrammar,
                false,
                false,
                true
            );
            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();

            bool result = itp.Parse(itp);
            Assert.IsTrue(result);
            Assert.AreEqual(2, itp.ParserResults.Length);

            // Test the first parser result

            NonterminalToken ntt = itp.ParserResults[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // Leftmost E from E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);

            // Test the second parser result

            ntt = itp.ParserResults[1] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // 2nd E from E PLUS E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);
        }

        private static readonly string EEiGLRRefGrammar = @"
            options
            {
                assemblyref System.Net.Http.dll,
                using System.Net.Http,
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiGLRParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }
            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                    {
                        HttpClient http = new HttpClient();
                    }
                ;
            }
        ";

        [TestMethod]
        public void TestEEiGLRParserForReferences()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                new StringReader(EEiGLRRefGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );

            Assert.AreEqual(0, ParserFactory<EEiGLRParser>.CompilerErrors.Length);
            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();
            Assert.IsNotNull(itp);
        }

        private static readonly string MergingTimesEEiGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiGLRParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }

            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                merge
                {
                    if($0.Children[1].Type == ParserTable.Tokens[" + "\"TIMES\"" + @"])
                        $$ = $0;
                    else if($1.Children[1].Type == ParserTable.Tokens[" + "\"TIMES\"" + @"])
                        $$ = $1;
                    else
                        $$ = null;
                }       
                ;
            }
        ";

        [TestMethod]
        public void TestEEiGLRMergeTimes()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                new StringReader(MergingTimesEEiGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );

            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();

            bool result = itp.Parse(itp);
            Assert.IsTrue(result);
            Assert.AreEqual(1, itp.ParserResults.Length);

            // Test the parser result

            NonterminalToken ntt = itp.ParserResults[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // Leftmost E from E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);
        }

        private static readonly string MergingPlusEEiGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiGLRParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }

            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                merge
                {
                    if($0.Children[1].Type == ParserTable.Tokens[" + "\"PLUS\"" + @"])
                        $$ = $0;
                    else if($1.Children[1].Type == ParserTable.Tokens[" + "\"PLUS\"" + @"])
                        $$ = $1;
                    else
                        $$ = null;
                }       
                ;
            }
        ";

        [TestMethod]
        public void TestEEiGLRMergePlus()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                new StringReader(MergingPlusEEiGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );
            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();

            bool result = itp.Parse(itp);
            Assert.IsTrue(result);
            Assert.AreEqual(1, itp.ParserResults.Length);

            // Test the parser result

            NonterminalToken ntt = itp.ParserResults[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // 2nd E from E PLUS E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);
        }

        private static readonly string MergingNeitherEEiGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass EEiGLRParser
            }
            events
            {
                i,
                PLUS,
                TIMES
            }

            grammar(E)
            {
                E:  i
                |   E PLUS E
                |   E TIMES E
                merge
                {
                    $$ = null;
                }
                ;
            }
        ";

        [TestMethod]
        public void TestEEiGLRMergeNeither()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<EEiGLRParser>.InitializeFromGrammar
            (
                new StringReader(MergingNeitherEEiGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );

            EEiGLRParser itp = ParserFactory<EEiGLRParser>.CreateInstance();
            bool result = itp.Parse(itp);
            Assert.IsTrue(result);
            Assert.AreEqual(2, itp.ParserResults.Length);

            // Test the first parser result

            NonterminalToken ntt = itp.ParserResults[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[0] as NonterminalToken;
            Assert.IsNotNull(ntt); // Leftmost E from E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);

            // Test the second parser result

            ntt = itp.ParserResults[1] as NonterminalToken;
            Assert.IsNotNull(ntt); // E from _Start: SOF E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["PLUS"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // 2nd E from E PLUS E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(3, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["TIMES"], ntt.Children[1].Type);
            ntt = ntt.Children[2] as NonterminalToken;
            Assert.IsNotNull(ntt); // Rightmost E of E TIMES E
            Assert.AreEqual(itp.Tokens["E"], ntt.Type);
            Assert.AreEqual(1, ntt.Children.Length);
            Assert.AreEqual(itp.Tokens["i"], ntt.Children[0].Type);
        }

        private static readonly string offlineSource =
            "options\r\n" +
            "{\r\n" +
            "    namespace GLRCalculator,\r\n" +
            "    parserclass CalcParser\r\n" +
            "}\r\n" +
            "\r\n" +
            "events\r\n" +
            "{\r\n" +
            "     i,\r\n" +
            "    PLUS,\r\n" +
            "    TIMES\r\n" +
            "}\r\n" +
            "\r\n" +
            "grammar(E)\r\n" +
            "{\r\n" +
            "    E:  i\r\n" +
            "    {\r\n" +
            "        $$ = $0;\r\n" +
            "    }\r\n" +
            "\r\n" +
            "|   E PLUS E\r\n" +
            "    {\r\n" +
            "        $$ = (int)$0 + (int)$1;\r\n" +
            "    }\r\n" +
            "\r\n" +
            "|   E TIMES E\r\n" +
            "    {\r\n" +
            "        $$ = (int)$0 * (int)$1;\r\n" +
            "    }\r\n" +
            "\r\n" +
            "merge\r\n" +
            "    {\r\n" +
            "        if ($0.Children[1].Type == ParserTable.Tokens[\"TIMES\"])\r\n" +
            "            $$ = $0;\r\n" +
            "        else if ($1.Children[1].Type == ParserTable.Tokens[\"TIMES\"])\r\n" +
            "            $$ = $1;\r\n" +
            "    }\r\n" +
            "    ;\r\n" +
            "}\r\n";

        [TestMethod]
        public void TestOfflineGLRParserGeneration()
        {
            StringBuilder output = new();
            using StringReader src = new(offlineSource);
            using StringWriter dst = new(output);
            string creationResult = ParserFactory.CreateOfflineParser
                (src, dst, null, null, false, false, true, out List<string> extRefs);

            Assert.IsTrue(string.IsNullOrEmpty(creationResult));
        }

        private readonly string multiplicityGrammar = @"
            options
            {
                using Parsing,
                namespace GLRParserTests,
                parserclass InlineGLRTestParser
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
                PluralNoun
                {
                    return $token.ToString().EndsWith(" + "\"s\"" + @");
                },
                SingularVerb
                {
                    return $token.ToString().EndsWith(" + "\"s\"" + @")
                        || $token.ToString().EndsWith(" + "\"ed\"" + @");
                },
                PluralVerb
                {
                    return !($token.ToString().EndsWith(" + "\"s\"" + @"));
                },
                Past
                {
                    return $token.ToString().EndsWith(" + "\"ed\"" + @");
                }
            }
            grammar(para)
            {
                para: sentence+
                ;

                sentence:
                    presentSentence 
                    {
                        PresentCount++;
                    }
                |   pastSentence 
                    {
                        PastCount++;
                    }
                ;

                presentSentence:
                    nounPhrase[!PluralNoun] verb[!Past & SingularVerb] nounPhrase PERIOD
                    {
                        SingularCount++;
                    }
                |   nounPhrase[PluralNoun] verb[!Past & PluralVerb] nounPhrase PERIOD
                    {
                        PluralCount++;
                    }
                ;

                pastSentence:
                    nounPhrase[!PluralNoun] verb[Past & SingularVerb] nounPhrase PERIOD
                    {
                        SingularCount++;
                    }
                |   nounPhrase[PluralNoun] verb[Past & PluralVerb] nounPhrase PERIOD
                    {
                        PluralCount++;
                    }
                ;

                nounPhrase:
                    THE adjectives NOUN
                |   A adjectives NOUN
                |   adjectives NOUN[PluralNoun]
                ;

                adjectives: ADJECTIVE*
                {
                    IList<string> adjectives = AsList<string>($0);
                    foreach (string s in adjectives)
                        AdjectiveList.Add(s);
                    AdjectivesCount += adjectives.Count;
                }
                ;

                verb: ADVERB?  VERB
                {
                    $$ = $1;
                }
                ;
            }
        ";

        [TestMethod]
        public void TestGLRMultiplicityInlineParser()
        {
            StringBuilder tableString = new();
            StringBuilder srcString = new();
            using TextWriter tabOut = new StringWriter(tableString);
            using TextWriter srcOut = new StringWriter(srcString);
            ParserFactory<InlineGLRTestParser>.InitializeFromGrammar
            (
                new StringReader(multiplicityGrammar),
                tabOut,
                false,
                false,
                true,
                srcOut
            );

            Assert.AreEqual(0, ParserFactory<InlineGLRTestParser>.CompilerErrors.Length);
            InlineGLRTestParser itp = ParserFactory<InlineGLRTestParser>.CreateInstance();
            Assert.IsNotNull(itp);
            //srcString = new StringBuilder();
            //srcOut = new StringWriter(srcString);
            itp.DebugStream = srcOut;

            // Grammar based on the words: cat dog rabbit human,
            // a the hairy pink little cold lick defend like reveal,
            // noisily, lovingly, quickly, morosely.

            string grammarInput =
                "the cold rabbit reveals a human. the hairy pink dogs lovingly licked the cat.";

            // Create the input tokeniser, connecting it to the
            // list of terminal token values from the parser

            GLRTestTokeniser tt = new(new StringReader(grammarInput), itp.Tokens);

            // Now execute the parse

            bool result = itp.Parse(tt);
            Assert.IsTrue(result);
            Assert.AreEqual(1, itp.ParserResults.Length);

            // Must evaluate the top node of the parser tree
            // to force the action functions to be executed

            Assert.IsNull(itp.ParserResults[0].Value);
            Assert.AreEqual(3, itp.AdjectivesCount);
            Assert.AreEqual(1, itp.PastCount);
            Assert.AreEqual(1, itp.SingularCount);
            Assert.AreEqual(1, itp.PluralCount);
            Assert.AreEqual("cold", itp.AdjectiveList[2]);

            // Now perform the clone test

            InlineGLRTestParser clone = ParserFactory<InlineGLRTestParser>.CreateInstance();
            tt = new GLRTestTokeniser(new StringReader(grammarInput), clone.Tokens);

            // Now execute the parse using the clone

            result = clone.Parse(tt);
            Assert.IsTrue(result);
            Assert.AreEqual(1, clone.ParserResults.Length);

            // Must evaluate the top node of the parser tree
            // to force the action functions to be executed

            Assert.IsNull(clone.ParserResults[0].Value);
            Assert.AreEqual(3, clone.AdjectivesCount);
            Assert.AreEqual(1, clone.PastCount);
            Assert.AreEqual(1, clone.SingularCount);
            Assert.AreEqual(1, clone.PluralCount);
            Assert.AreEqual("cold", clone.AdjectiveList[2]);

            // Validate the allocation of token values

            Assert.AreEqual(25, clone.ParserTable.Tokens["ADVERB"]);
            Assert.AreEqual(16388, clone.ParserTable.Tokens["THE"]);
        }
    }

    public class EEiConflictedParser : Parser
    {
        // Empty placeholder class for testing parser failure.
        // When building a regular LR(1) parser from the EEi
        // grammar, conflicts should be identified while doing
        // the parser generation.
    }

    public class EEiGLRParser : GeneralisedParser, IEnumerable<IToken>
    {
        // Empty placeholder class for ensuring conflicts are
        // ignored when building the GLR parser.

        // We also embed the test's input tokeniser here

        public IEnumerator<IToken> GetEnumerator()
        {
            yield return new ParserToken(ParserTable.Tokens["i"]);
            yield return new ParserToken(ParserTable.Tokens["PLUS"]);
            yield return new ParserToken(ParserTable.Tokens["i"]);
            yield return new ParserToken(ParserTable.Tokens["TIMES"]);
            yield return new ParserToken(ParserTable.Tokens["i"]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return new ParserToken(ParserTable.Tokens["i"]);
            yield return new ParserToken(ParserTable.Tokens["PLUS"]);
            yield return new ParserToken(ParserTable.Tokens["i"]);
            yield return new ParserToken(ParserTable.Tokens["TIMES"]);
            yield return new ParserToken(ParserTable.Tokens["i"]);
        }

        // Sample merge functions for the parser

        public void EIsEOpEPrefersPlus(NonterminalToken[] alternatives)
        {
            if (alternatives == null || alternatives.Length != 3)
                throw new ArgumentException("Merge function called with invalid args");

            if (alternatives[1].Children[1].Type == ParserTable.Tokens["PLUS"])
                alternatives[0] = alternatives[1];
            else if (alternatives[2].Children[1].Type == ParserTable.Tokens["PLUS"])
                alternatives[0] = alternatives[2];
            else
                alternatives[0] = null;
        }

        public void EIsEOpEPrefersTimes(NonterminalToken[] alternatives)
        {
            if (alternatives == null || alternatives.Length != 3)
                throw new ArgumentException("Merge function called with invalid args");

            if (alternatives[1].Children[1].Type == ParserTable.Tokens["TIMES"])
                alternatives[0] = alternatives[1];
            else if (alternatives[2].Children[1].Type == ParserTable.Tokens["TIMES"])
                alternatives[0] = alternatives[2];
            else
                alternatives[0] = null;
        }

        public static void EIsEOpEPrefersNeither(NonterminalToken[] alternatives)
        {
            if (alternatives == null || alternatives.Length != 3)
                throw new ArgumentException("Merge function called with invalid args");
            alternatives[0] = null;
        }
    }
}
