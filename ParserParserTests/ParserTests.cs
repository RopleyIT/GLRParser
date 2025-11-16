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

using BooleanLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParserGenerator;
using Parsing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ParserParserTests;

[TestClass]
public class ParserTests
{
    private readonly string jcGrammar = @"
events
{
    ENTRY,
    PVAL,
    IVAL,
    BUS,
    TRAM,
    EXIT,
    TERMINAL
}

guards
{
    WithinMJT
    {
        // Sample inline guard function

        return $token - 8 > 0;
    },

    ValidOSIPair,
    WithinOSITime,
    AllTapsInSamePaidArea,
    TapAtContinuableStation,
    WithinCET,
    SamePaidAreaAsFirstTap,
    WithinSST,
    ContinuedExitTapsWithinSSCET,
    ValidOSIExit,
    InTramPaidArea,
    PreviousJourneyWasTram,
    WithinTramMJT,
    WithinSSET
}

grammar(JourneyList)
{
    StartingTap :
        ENTRY

    |	PVAL

    |	IVAL
    ;

    StartingTapOfJourney:
        ENTRY[!WithinMJT]

    |	PVAL[!WithinMJT]

    |	IVAL[!WithinMJT]
    ;

    StartingReentryTap:
        ENTRY[ValidOSIPair & WithinOSITime]

    |	PVAL[ValidOSIPair & WithinOSITime]

    |	IVAL[ValidOSIPair & WithinOSITime]
    ;

    StartingTapNotReentry:
        ENTRY[!(ValidOSIPair & WithinOSITime)]

    |	PVAL[!(ValidOSIPair & WithinOSITime)]

    |	IVAL[!(ValidOSIPair & WithinOSITime)]
    ;

    InProgressJourney :
        StartingTap
        {
            // Rules 1, 2, 3
        }

    |	InProgressJourney StartingTapOfJourney
        {
            // Part of rule 12 & 14
            // Unlink most recent OSI if any
            // Set final state to Unfinished
            // Save current journey
            // Append tap to a new journey

            int t = (int)$0;
            $$ = (int)$1 + t;
        }

    |	InProgressJourney IVAL
        {
            // Rule 15
            // Append tap to current journey
            // Update list of interchange points
        }

    |	InProgressJourney PVAL[(AllTapsInSamePaidArea & TapAtContinuableStation) & WithinCET]
        {
            // Rule 20
            // Append tap to current journey
        }

    |	InProgressJourney PVAL[(SamePaidAreaAsFirstTap & !WithinSST) & !(TapAtContinuableStation & AllTapsInSamePaidArea)]
        {
            // Rule 23
            // Set final state to Unfinished
            // Save current journey
            // Append tap to new journey
        }

    |	InProgressJourney ENTRY
        {
            // RUle 26
            // Set final state to Unfinished
            // Save current journey
            // Append tap to new journey
        }

    |	ContinuedJourney StartingTapOfJourney
        {
            // Part of rule 29
            // Set final state to Complete
            // Save current journey
            // Append tap to a new journey
        }

    |	ContinuedJourney PVAL[!ContinuedExitTapsWithinSSCET]
        {
            // Rule 30
            // Set final state to Complete
            // Save current journey
            // Append PVAL to a new journey
        }

    |	ContinuedJourney ENTRY
        {
            // Rule 35
            // Set final state to Complete
            // Save current journey
            // Append entry tap to a new journey
        }

    |	BeyondOSIExit StartingReentryTap
        {
            // Rules 40, 41, 42
            // Append tap to journey
            // Mark as first tap of current leg
            // Reset final state to Unfinished
            // Update list of interchange points
        }

    |	BeyondOSIExit StartingTapNotReentry
        {
            // Rules 43, 44, 45
            // Save current journey
            // Append starting tap to a new journey
        }
    ;

    BeyondOSIExit :
        InProgressJourney EXIT[ValidOSIExit]
        {
            // Rule 19 where EXIT at valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
        }

    |	InProgressJourney PVAL[ValidOSIExit]
        {
            // Rule 25 where PVAL at valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
        }

    |	ContinuedJourney EXIT[ValidOSIExit]
        {
            // Rule 33 where EXIT at valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
        }

    |	ContinuedJourney PVAL[ValidOSIExit & !TapAtContinuableStation]
        {
            // Rule 34 where PVAL at valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
        }
    ;

    ContinuedJourney :
        InProgressJourney PVAL[TapAtContinuableStation & !AllTapsInSamePaidArea]
        {
            // Rule 24
            // Append tap to current journey
        }

    |	ContinuedJourney PVAL[TapAtContinuableStation]
        {
            // Rule 31
            // Append tap to current journey
        }

    |	ContinuedJourney IVAL
        {
            // Rule 36
            // Append tap to current journey
            // Update interchange points
        }
    ;

    Journey :

        EXIT[(InTramPaidArea & PreviousJourneyWasTram) & WithinTramMJT]
        {
            // Rule 4
            // Append EXIT tap to a new journey (Current behaviour)
            // Drop this single EXIT tap from the journey
            // If it contains taps (which it doesn't), save the completed journey
        }

    |	EXIT
        {
            // Rule 5
            // Append exit tap to a new journey
            // Set final state to Unstarted
            // Save the Unstarted journey
        }

    |	BUS
        {
            // Rule 6
            // Append bus tap to a new journey
            // Set final state to BusResolution
            // Save the completed journey
        }

    |	TRAM
        {
            // Rule 7
            // Append tram tap to a new journey
            // Set final state to Tramresolution
            // Save the completed journey
        }

    |	InProgressJourney TRAM[(InTramPaidArea & !SamePaidAreaAsFirstTap) & WithinMJT]
        {
            // Rule 10
            // Append synthetic exit tap to current journey
            // Set final state to Complete
            // Save current journey
            // Append the TRAM tap to a new journey
            // Set final state to TramResolution
            // Save tram journey
        }

    |	InProgressJourney BUS
        {
            // Rules 8, 12 & 14
            // Unlink most recent OSI if any
            // Set final state to Unfinished
            // Save unfinished journey
            // Append bus tap to a new journey
            // Set final state to BusResolution
            // Save the bus journey
        }

    |	InProgressJourney TRAM[(InTramPaidArea & SamePaidAreaAsFirstTap) & WithinSST]
        {
            // Rule 9
            // Replace taps in current journey with just the tram tap
            // Set final state to TramResolution
            // Save the completed tram journey
        }

    |	InProgressJourney TRAM
        {
            // Rule 11, 12 & 14
            // Unlink most recent OSI if any
            // Set final state to Unfinished
            // Save unfinished journey
            // Append tram tap to a new journey
            // Set final state to TramResolution
            // Save the tram journey
        }

    |	InProgressJourney TERMINAL
        {
            // Rule 13
            // Unlink most recent OSI if any
            // Set final state to Unfinished
            // Save the unfinished journey
        }

    |	InProgressJourney EXIT[!WithinMJT]
        {
            // Part of rule 12 & 14
            // Unlink most recent OSI if any
            // Set final state to Unfinished
            // Save the unfinished journey
            // Append exit tap into a new journey
            // Set final state to Unstarted
            // Save the unstarted journey
        }

    |	InProgressJourney EXIT[SamePaidAreaAsFirstTap & WithinSSET]
        {
            // Rule 16
            // Append tap to current journey
            // Set final state to SameStationExit
            // Save completed journey
        }

    |	InProgressJourney EXIT[(SamePaidAreaAsFirstTap & WithinSST) & !WithinSSET]
        {
            // Rule 17
            // Append tap to current journey
            // Set final state to HereToHere
            // Save completed journey
        }

    |	InProgressJourney EXIT[SamePaidAreaAsFirstTap & !WithinSST]
        {
            // Rule 18
            // Set final state to Unfinished
            // Save current journey
            // Append EXIT tap to new journey
            // Set final state to Unstarted
            // Save current journey
        }

    |	InProgressJourney EXIT[!ValidOSIExit]
        {
            // Rule 19 when not an OSI exit
            // Append tap to current journey
            // Set final state to Complete
            // Save completed journey
        }

    |	InProgressJourney PVAL[(TapAtContinuableStation & SamePaidAreaAsFirstTap) & (WithinSST & !WithinCET)]
        {
            // Rule 21
            // Append tap to current journey
            // Set final state to HereToHere
            // Save completed journey
        }

    |	InProgressJourney PVAL[!TapAtContinuableStation & (SamePaidAreaAsFirstTap & WithinSST)]
        {
            // Rule 22
            // Append tap to current journey
            // Set final state to HereToHere
            // Save completed journey
        }

    |	InProgressJourney PVAL[!ValidOSIExit]
        {
            // Rule 25
            // Append tap to current journey
            // Set final state to Complete
            // Save completed journey
        }

    |	ContinuedJourney TERMINAL
        {
            // Part of rule 29
            // Set final state to Complete
            // Save current journey
        }

    |	ContinuedJourney BUS
        {
            // Rule 27
            // Set final state to Complete
            // Save current journey
            // Append bus tap to new journey
            // Set final state to BusResolution
            // Save current journey
        }

    |	ContinuedJourney TRAM
        {
            // Rule 28
            // Set final state to Complete
            // Save current journey
            // Append tram tap to new journey
            // Set final state to TramResolution
            // Save current journey
        }

    |	ContinuedJourney EXIT[!WithinMJT]
        {
            // part of Rule 29
            // Set final state to Complete
            // Save current journey
            // Append exit tap to new journey
            // Set final state to Unstarted
            // Save unstarted journey
        }

    |	ContinuedJourney EXIT[!ContinuedExitTapsWithinSSCET]
        {
            // Rule 32
            // If not already marked Unstarted, set final state to Complete
            // Save current journey
            // Append EXIT tap to new journey
            // Set final state to Unstarted
            // Save unstarted journey
        }

    |	ContinuedJourney EXIT[!ValidOSIExit]
        {
            // Rule 33 when not a valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
            // Save completed journey
        }

    |	ContinuedJourney PVAL[!TapAtContinuableStation & !ValidOSIExit]
        {
            // Rule 34 when not a valid OSI exit
            // Append tap to current journey
            // Set final state to Complete
            // Save completed journey
        }

    |	BeyondOSIExit BUS
        {
            // Rule 37
            // Save current journey
            // Append bus tap to a new journey
            // Set final state to BusResolution
            // Save completed bus journey
        }

    |	BeyondOSIExit TRAM
        {
            // Rule 38
            // Save current journey
            // Append tram tap to a new journey
            // Set final state to TramResolution
            // Save completed tram journey
        }

    |	BeyondOSIExit TERMINAL
        {
            // Part of rule 39
            // Save current journey
        }

    |	BeyondOSIExit EXIT
        {
            // Rule 46
            // Save current journey
            // Append exit tap to a new journey
            // Set final state to Unstarted
            // Save unstarted journey
        }
    ;

    JourneyList: JourneyList Journey

    |
    ;
}
";
    [TestMethod]
    public void TestJCGrammar()
    {
        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringWriter parserOutput = new();
        StringBuilder sb = new();
        p.DebugStream = new StringWriter(sb);
        // p.DebugStream = new DebugWriter();
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(jcGrammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(8, p.ConstructedGrammar.Terminals);
        Assert.HasCount(14, p.ConstructedGrammar.Guards);
        Assert.HasCount(9, p.ConstructedGrammar.Nonterminals);
        string validationResult = p.ConstructedGrammar.CreateItemSetsAndGotoEntries(false);
        Assert.AreEqual(string.Empty, validationResult);
        _ = p.ConstructedGrammar.EstablishShiftReduceOrdersForItemSets();
        //Assert.AreEqual(string.Empty, validationResult);
        GrammarOutput parserWriter = new(parserOutput);
        parserWriter.RenderStateTables(p.ConstructedGrammar, false, false, false);
        //string firsts = p.ConstructedGrammar.RenderFirstSets();
        //string itemSets = p.ConstructedGrammar.RenderItemSetsAndGotos();
        Assert.HasCount(65, p.ConstructedGrammar.ItemSets);
        Assert.IsGreaterThan(0, parserOutput.ToString().Length);
    }

    private readonly string parserGrammarText =
        "options\r\n" +
        "{\r\n" +
        "    using System.Diagnostics,\r\n" +
        "    using ParserGenerator,\r\n" +
        "    namespace LRParser\r\n" +
        "}\r\n" +
        "events\r\n" +
        "{\r\n" +
        "    UNKNOWN,\r\n" +
        "    LBRACE,\r\n" +
        "    RBRACE,\r\n" +
        "    LPAREN,\r\n" +
        "    RPAREN,\r\n" +
        "    COMMA,\r\n" +
        "    COLON,\r\n" +
        "    OR,\r\n" +
        "    SEMI,\r\n" +
        "    LBRACK,\r\n" +
        "    RBRACK,\r\n" +
        "    AND,\r\n" +
        "    NOT,\r\n" +
        "    TOKENS,\r\n" +
        "    CONDITIONS,\r\n" +
        "    GRAMMAR,\r\n" +
        "    IDENTIFIER,\r\n" +
        "    TOKEN,\r\n" +
        "    CONDITION,\r\n" +
        "    CODE,\r\n" +
        "    OPTIONS,\r\n" +
        "    USING,\r\n" +
        "    NAMESPACE\r\n" +
        "}\r\n" +
        "guards\r\n" +
        "{\r\n" +
        "    NONE\r\n" +
        "}\r\n" +
        "grammar(CompleteGrammar)\r\n" +
        "{\r\n" +
        "    CompleteGrammar :\r\n" +
        "        ParserOptions Events Guards Rules\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    ParserOptions :\r\n" +
        "        OPTIONS LBRACE OptionList RBRACE\r\n" +
        "    |\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OptionList :\r\n" +
        "        OptionList COMMA ParserOption\r\n" +
        "    |   ParserOption\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    ParserOption :\r\n" +
        "        USING IDENTIFIER\r\n" +
        "        {\r\n" +
        "            ParserInstance.AddUsing($1.ToString());\r\n" +
        "        }\r\n" +
        "    |   NAMESPACE IDENTIFIER\r\n" +
        "        {\r\n" +
        "            ParserInstance.NameSpace = $1.ToString();\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    Events :\r\n" +
        "        TOKENS LBRACE TokenList RBRACE\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    TokenList :\r\n" +
        "        IDENTIFIER\r\n" +
        "        {\r\n" +
        "            $$ = ParserInstance.CheckAndInsertToken($0.ToString());\r\n" +
        "        }\r\n" +
        "    |   TokenList COMMA IDENTIFIER\r\n" +
        "        {\r\n" +
        "            $$ = ParserInstance.CheckAndInsertToken($2.ToString());\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    Guards :\r\n" +
        "        CONDITIONS LBRACE ConditionList RBRACE\r\n" +
        "    |\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    ConditionList :\r\n" +
        "        IDENTIFIER\r\n" +
        "        {\r\n" +
        "            $$ = ParserInstance.CheckAndInsertGuardToken($0.ToString());\r\n" +
        "        }\r\n" +
        "    |   ConditionList COMMA IDENTIFIER\r\n" +
        "        {\r\n" +
        "            $$ = ParserInstance.CheckAndInsertGuardToken($2.ToString());\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    Rules : \r\n" +
        "        GRAMMAR LPAREN IDENTIFIER RPAREN LBRACE RuleList RBRACE\r\n" +
        "        {\r\n" +
        "            if(!ParserInstance.ConstructedGrammar.SetStartingNonterminal($2.ToString()))\r\n" +
        "                $$ = new ParserError \r\n" +
        "                {\r\n" +
        "                    Fatal = true,\r\n" +
        "                    Message = \"No grammar entry symbol found called \" + $2.ToString()\r\n" +
        "                };\r\n" +
        "            else\r\n" +
        "                $$ = null;\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    RuleList :\r\n" +
        "        RuleList Rule\r\n" +
        "    |   Rule\r\n" +
        "    ; \r\n" +
        "\r\n" +
        "    Rule :\r\n" +
        "        IDENTIFIER COLON ProductionList SEMI\r\n" +
        "        {\r\n" +
        "            // Build the list of production right hand sides in\r\n" +
        "            // reverse order. Note that the use of a List with\r\n" +
        "            // insertion at the front is not very efficient here.\r\n" +
        "            // Might be better to use another collection and\r\n" +
        "            // convert it to a List when the rule has been\r\n" +
        "            // parsed completely.\r\n" +
        "\r\n" +
        "            List<GrammarProduction> gpl = $2 as List<GrammarProduction>;\r\n" +
        "\r\n" +
        "            // Check for a terminal token name being reused as\r\n" +
        "            // a non-terminal token to the left of a grammar rule\r\n" +
        "\r\n" +
        "            GrammarToken gt = ParserInstance.ConstructedGrammar.Terminals\r\n" +
        "                .FirstOrDefault(t => t.Text == $0.ToString());\r\n" +
        "            if (gt != null)\r\n" +
        "                $$ = new ParserError\r\n" +
        "                {\r\n" +
        "                    Fatal = true,\r\n" +
        "                    Message = gt.Text + \" already used as a terminal token name\"\r\n" +
        "                };\r\n" +
        "            else\r\n" +
        "            {\r\n" +
        "                gt = ParserInstance.ConstructedGrammar.Nonterminals\r\n" +
        "                    .FirstOrDefault(t => t.Text == $0.ToString());\r\n" +
        "                if (gt == null)\r\n" +
        "                {\r\n" +
        "                    gt = new GrammarToken\r\n" +
        "                        (ParserInstance.ConstructedGrammar.Nonterminals.Count + 16385, \r\n" +
        "                        ParserGenerator.TokenType.Nonterminal, $0.ToString());\r\n" +
        "                    ParserInstance.ConstructedGrammar.Nonterminals.Add(gt);\r\n" +
        "                }\r\n" +
        "                // Build the non-terminal rule name onto the\r\n" +
        "                // front of each production in this group. Note that\r\n" +
        "                // the positioning of the setting of the ProductionNumber\r\n" +
        "                // ensures that the value 0 is reserved for later\r\n" +
        "                // (used for the extended grammar start symbol _Start).\r\n" +
        "\r\n" +
        "                foreach (GrammarProduction gp in gpl)\r\n" +
        "                {\r\n" +
        "                    gp.LHS = gt;\r\n" +
        "                    ParserInstance.ConstructedGrammar.Productions.Add(gp);\r\n" +
        "                    gp.ProductionNumber = ParserInstance.ConstructedGrammar.Productions.Count;\r\n" +
        "                }\r\n" +
        "                $$ = null;\r\n" +
        "            }\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    ProductionList :\r\n" +
        "        ProductionList OR Production\r\n" +
        "        {\r\n" +
        "            // Build the list of production right hand sides\r\n" +
        "\r\n" +
        "            List<GrammarProduction> gpl = $0 as List<GrammarProduction>;\r\n" +
        "            gpl.Add($2 as GrammarProduction);\r\n" +
        "            $$ = gpl;\r\n" +
        "        }\r\n" +
        "    |   Production\r\n" +
        "        {\r\n" +
        "            // Create the new empty list that will be filled with\r\n" +
        "            // productions in reverse order as rules are reduced\r\n" +
        "\r\n" +
        "            List<GrammarProduction> gpl = new List<GrammarProduction>();\r\n" +
        "            gpl.Add($0 as GrammarProduction);\r\n" +
        "            $$ = gpl;\r\n" +
        "        }\r\n" +
        "    ; \r\n" +
        "\r\n" +
        "    Production :\r\n" +
        "        ElementList OptCode\r\n" +
        "        {\r\n" +
        "            // The index number for the production is corrected\r\n" +
        "            // later when it gets inserted into the list of Productions\r\n" +
        "\r\n" +
        "            $$ = new GrammarProduction\r\n" +
        "                (0, $0 as List<GrammarElement>, $1 as GrammarToken);\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OptCode : \r\n" +
        "        CODE\r\n" +
        "        {\r\n" +
        "            $$ = new GrammarToken\r\n" +
        "                (0, ParserGenerator.TokenType.Code, $0.ToString());\r\n" +
        "        }\r\n" +
        "    |   \r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    ElementList :\r\n" +
        "        ElementList Element\r\n" +
        "        {\r\n" +
        "            // Build the list of rule elements in reverse order.\r\n" +
        "            // Note that the use of a List with insertion at the\r\n" +
        "            // front is not very efficient here. Might be better\r\n" +
        "            // to use another collection and convert it to a List\r\n" +
        "            // when the rule has been parsed completely.\r\n" +
        "\r\n" +
        "            List<GrammarElement> gel = $0 as List<GrammarElement>;\r\n" +
        "            gel.Add($1 as GrammarElement);\r\n" +
        "            $$ = gel;\r\n" +
        "        }\r\n" +
        "    |   \r\n" +
        "        {\r\n" +
        "            // Create the empty list that will be filled with each\r\n" +
        "            // grammar rule element on completion of each rule\r\n" +
        "\r\n" +
        "            $$ = new List<GrammarElement>();\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    Element :\r\n" +
        "        IDENTIFIER OptGuard\r\n" +
        "        {\r\n" +
        "            // Look up the indentifier from the lists of\r\n" +
        "            // terminal and non-terminal tokens\r\n" +
        "\r\n" +
        "            GrammarToken tok = ParserInstance.ConstructedGrammar.Terminals\r\n" +
        "                .FirstOrDefault(t => t.Text == $0.ToString());\r\n" +
        "            if (tok == null)\r\n" +
        "                tok = ParserInstance.ConstructedGrammar.Nonterminals\r\n" +
        "                    .FirstOrDefault(t => t.Text == $0.ToString());\r\n" +
        "            if (tok == null)\r\n" +
        "            {\r\n" +
        "                tok = new GrammarToken\r\n" +
        "                    (ParserInstance.ConstructedGrammar.Nonterminals.Count + 16385, \r\n" +
        "                    ParserGenerator.TokenType.Nonterminal, $0.ToString());\r\n" +
        "                ParserInstance.ConstructedGrammar.Nonterminals.Add(tok);\r\n" +
        "            }\r\n" +
        "\r\n" +
        "            // Create the rule element\r\n" +
        "\r\n" +
        "            $$ = new GrammarElement(tok, $1 as BoolExpr);\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OptGuard :\r\n" +
        "        LBRACK CondExpr RBRACK\r\n" +
        "        {\r\n" +
        "            // The element between the square brackets\r\n" +
        "            // is the conditional expression\r\n" +
        "\r\n" +
        "            $$ = $1;\r\n" +
        "        }\r\n" +
        "    |   \r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    CondExpr :\r\n" +
        "        OrableExpr OptOr\r\n" +
        "        {\r\n" +
        "            if ($1 != null)\r\n" +
        "                $$ = new BinExpr(false, $0 as BoolExpr, $1 as BoolExpr);\r\n" +
        "            else\r\n" +
        "                $$ = $0;\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OptOr : \r\n" +
        "        OR OrableExpr\r\n" +
        "        {\r\n" +
        "            // Return the right operand from the OR\r\n" +
        "            // operation to the parent rule\r\n" +
        "\r\n" +
        "            $$ = $1;\r\n" +
        "        }\r\n" +
        "    |   \r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OrableExpr :\r\n" +
        "        AndableExpr OptAnd\r\n" +
        "        {\r\n" +
        "            if ($1 != null)\r\n" +
        "                $$ = new BinExpr(true, $0 as BoolExpr, $1 as BoolExpr);\r\n" +
        "            else\r\n" +
        "                $$ = $0;\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    OptAnd :\r\n" +
        "        AND AndableExpr\r\n" +
        "        {\r\n" +
        "            // Return the right operand from the AND\r\n" +
        "            // operation to the parent rule\r\n" +
        "            \r\n" +
        "            $$ = $1;\r\n" +
        "        }\r\n" +
        "    |   \r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    AndableExpr :\r\n" +
        "        RootExpr\r\n" +
        "        {\r\n" +
        "            // Pass the root expression back up the rule tree\r\n" +
        "\r\n" +
        "            $$ = $0;\r\n" +
        "        }\r\n" +
        "    |   NOT RootExpr\r\n" +
        "        {\r\n" +
        "            // Prepend the root expression with a NOT\r\n" +
        "            // operator node, and return that to the\r\n" +
        "            // parent rule\r\n" +
        "            \r\n" +
        "            $$ = new NotExpr($1 as BoolExpr);\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "\r\n" +
        "    RootExpr :\r\n" +
        "        IDENTIFIER\r\n" +
        "        {\r\n" +
        "            // Look up the identifier among the list of guards\r\n" +
        "\r\n" +
        "            GrammarToken gt = ParserInstance.ConstructedGrammar.Guards\r\n" +
        "                .FirstOrDefault(t => t.Text == $0.ToString());\r\n" +
        "\r\n" +
        "            if (gt == null)\r\n" +
        "                $$ = new ParserError\r\n" +
        "                {\r\n" +
        "                    Fatal = true,\r\n" +
        "                    Message = $0.ToString() + \" is not a valid guard condition\"\r\n" +
        "                };\r\n" +
        "            else\r\n" +
        "                $$ = new LeafExpr(gt);\r\n" +
        "        }\r\n" +
        "    |   LPAREN CondExpr RPAREN\r\n" +
        "        {\r\n" +
        "            // The second argument should already be the\r\n" +
        "            // correct BoolExpr to return to the parent rule\r\n" +
        "\r\n" +
        "            $$ = $1;\r\n" +
        "        }\r\n" +
        "    ;\r\n" +
        "}\r\n";

    [TestMethod]
    public void TestLRParserGrammar()
    {
        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringWriter parserOutput = new();
        StringBuilder sb = new();
        p.DebugStream = new StringWriter(sb);
        // p.DebugStream = new DebugWriter();
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(parserGrammarText), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(24, p.ConstructedGrammar.Terminals);
        Assert.HasCount(1, p.ConstructedGrammar.Guards);
        Assert.HasCount(23, p.ConstructedGrammar.Nonterminals);
        string validationResult = p.ConstructedGrammar.CreateItemSetsAndGotoEntries(false);
        Assert.AreEqual(string.Empty, validationResult);
        p.ConstructedGrammar.EstablishShiftReduceOrdersForItemSets();
        GrammarOutput parserWriter = new(parserOutput);
        parserWriter.RenderStateTables(p.ConstructedGrammar, false, false, false);
        //string firsts = p.ConstructedGrammar.RenderFirstSets();
        //string itemSets = p.ConstructedGrammar.RenderItemSetsAndGotos();
        Assert.HasCount(87, p.ConstructedGrammar.ItemSets);
        Assert.IsGreaterThan(0, parserOutput.ToString().Length);
    }

    [TestMethod]
    public void TestParser()
    {
        string inputGrammar = @"
                events
                {
                    id,
                    PLUS
                }
                guards
                {
                    NONE
                }
                grammar(E)
                {
                    E: T PLUS E 
                        { 
                           // T_PLUS_E_Action 
                        }
                    |  T;
                    T : id
                        {
                            // id_Action
                        };
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringWriter parserOutput = new();

        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(inputGrammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(3, p.ConstructedGrammar.Terminals);
        Assert.HasCount(1, p.ConstructedGrammar.Guards);
        Assert.HasCount(2, p.ConstructedGrammar.Nonterminals);
        string validationResult = p
            .ConstructedGrammar.CreateItemSetsAndGotoEntries(false);
        Assert.AreEqual(string.Empty, validationResult);
        validationResult = p.ConstructedGrammar
            .EstablishShiftReduceOrdersForItemSets();
        Assert.AreEqual(string.Empty, validationResult);
        GrammarOutput parserWriter = new(parserOutput);
        parserWriter.RenderStateTables(p.ConstructedGrammar, false, false, false);
        //string firsts = p.ConstructedGrammar.RenderFirstSets();
        //string itemSets = p.ConstructedGrammar.RenderItemSetsAndGotos();
        Assert.HasCount(7, p.ConstructedGrammar.ItemSets);
    }

    [TestMethod]
    public void TestOptionalMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                grammar(root)
                {
                    root: item? BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(0, p.ConstructedGrammar.Guards);
        Assert.HasCount(3, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(5, p.ConstructedGrammar.Productions);
        Assert.AreEqual("zeroOrOne_item", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(1, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("zeroOrOne_item", p.ConstructedGrammar.Productions[1].LHS.Text);
        Assert.HasCount(0, p.ConstructedGrammar.Productions[1].RHS);
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[2].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[2].RHS);
        Assert.AreEqual("zeroOrOne_item", p.ConstructedGrammar.Productions[2].RHS[0].Token.Text);
    }

    [TestMethod]
    public void TestZeroToManyMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                grammar(root)
                {
                    root: item* BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(0, p.ConstructedGrammar.Guards);
        Assert.HasCount(3, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(5, p.ConstructedGrammar.Productions);
        Assert.AreEqual("zeroToMany_item", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("zeroToMany_item", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[1].Token.Text);
        Assert.AreEqual("zeroToMany_item", p.ConstructedGrammar.Productions[1].LHS.Text);
        Assert.HasCount(0, p.ConstructedGrammar.Productions[1].RHS);
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[2].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[2].RHS);
        Assert.AreEqual("zeroToMany_item", p.ConstructedGrammar.Productions[2].RHS[0].Token.Text);
    }

    [TestMethod]
    public void TestOneToManyMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                grammar(root)
                {
                    root: item+ BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(0, p.ConstructedGrammar.Guards);
        Assert.HasCount(3, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(5, p.ConstructedGrammar.Productions);
        Assert.AreEqual("oneToMany_item", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("oneToMany_item", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[1].Token.Text);
        Assert.AreEqual("oneToMany_item", p.ConstructedGrammar.Productions[1].LHS.Text);
        Assert.HasCount(1, p.ConstructedGrammar.Productions[1].RHS);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[1].RHS[0].Token.Text);
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[2].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[2].RHS);
        Assert.AreEqual("oneToMany_item", p.ConstructedGrammar.Productions[2].RHS[0].Token.Text);
    }

    [TestMethod]
    public void TestOptionalGuardedMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                guards
                {
                    FLAT,
                    SHARP
                }
                grammar(root)
                {
                    root: item[FLAT]? BELL
                    |     item?[SHARP] BELL
                    |     item[FLAT]?[SHARP] BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(2, p.ConstructedGrammar.Guards);
        Assert.HasCount(4, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(9, p.ConstructedGrammar.Productions);
        Assert.AreEqual("zeroOrOne_item_FLAT", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(1, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("FLAT", p.ConstructedGrammar.Productions[0].RHS[0].Guard.ToString());
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[6].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[6].RHS);
        Assert.AreEqual("zeroOrOne_item_FLAT", p.ConstructedGrammar.Productions[6].RHS[0].Token.Text);
        Assert.AreEqual("SHARP", p.ConstructedGrammar.Productions[6].RHS[0].Guard.ToString());
        Assert.AreEqual("BELL", p.ConstructedGrammar.Productions[6].RHS[1].Token.Text);
    }

    [TestMethod]
    public void TestZeroToManyGuardedMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                guards
                {
                    FLAT,
                    SHARP
                }
                grammar(root)
                {
                    root: item[FLAT]* BELL
                    |     item*[SHARP] BELL
                    |     item[FLAT]*[SHARP] BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(2, p.ConstructedGrammar.Guards);
        Assert.HasCount(4, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(9, p.ConstructedGrammar.Productions);
        Assert.AreEqual("zeroToMany_item_FLAT", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("zeroToMany_item_FLAT", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[1].Token.Text);
        Assert.AreEqual("FLAT", p.ConstructedGrammar.Productions[0].RHS[1].Guard.ToString());
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[6].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[6].RHS);
        Assert.AreEqual("zeroToMany_item_FLAT", p.ConstructedGrammar.Productions[6].RHS[0].Token.Text);
        Assert.AreEqual("SHARP", p.ConstructedGrammar.Productions[6].RHS[0].Guard.ToString());
        Assert.AreEqual("BELL", p.ConstructedGrammar.Productions[6].RHS[1].Token.Text);
        Assert.IsNull(p.ConstructedGrammar.Productions[6].RHS[1].Guard);
    }

    [TestMethod]
    public void TestOneToManyGuardedMultiplicity()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG,
                    BELL
                }
                guards
                {
                    FLAT,
                    SHARP
                }
                grammar(root)
                {
                    root: item[FLAT]+ BELL
                    |     item+[SHARP] BELL
                    |     item[FLAT]+[SHARP] BELL;
                    item: DING | DONG;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(4, p.ConstructedGrammar.Terminals);
        Assert.HasCount(2, p.ConstructedGrammar.Guards);
        Assert.HasCount(4, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(9, p.ConstructedGrammar.Productions);
        Assert.AreEqual("oneToMany_item_FLAT", p.ConstructedGrammar.Productions[0].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[0].RHS);
        Assert.AreEqual("oneToMany_item_FLAT", p.ConstructedGrammar.Productions[0].RHS[0].Token.Text);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[0].RHS[1].Token.Text);
        Assert.AreEqual("FLAT", p.ConstructedGrammar.Productions[0].RHS[1].Guard.ToString());
        Assert.HasCount(1, p.ConstructedGrammar.Productions[1].RHS);
        Assert.AreEqual("item", p.ConstructedGrammar.Productions[1].RHS[0].Token.Text);
        Assert.AreEqual("FLAT", p.ConstructedGrammar.Productions[1].RHS[0].Guard.ToString());
        Assert.AreEqual("root", p.ConstructedGrammar.Productions[6].LHS.Text);
        Assert.HasCount(2, p.ConstructedGrammar.Productions[6].RHS);
        Assert.AreEqual("oneToMany_item_FLAT", p.ConstructedGrammar.Productions[6].RHS[0].Token.Text);
        Assert.AreEqual("SHARP", p.ConstructedGrammar.Productions[6].RHS[0].Guard.ToString());
        Assert.AreEqual("BELL", p.ConstructedGrammar.Productions[6].RHS[1].Token.Text);
        Assert.IsNull(p.ConstructedGrammar.Productions[6].RHS[1].Guard);
    }

    [TestMethod]
    public void TestMiniGrammar()
    {
        string grammar = @"
                events
                {
                    DING,
                    DONG
                }
                guards
                {
                    BIG,
                    LOUD,
                    WET,
                    WARM,
                    SLIPPERY
                }
                grammar(root)
                {
                    root: DING root
                    |     DONG[!BIG & ((LOUD & WET & WARM) | SLIPPERY)] root
                    | ;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(3, p.ConstructedGrammar.Terminals);
        Assert.HasCount(5, p.ConstructedGrammar.Guards);
        Assert.HasCount(1, p.ConstructedGrammar.Nonterminals);
    }

    [TestMethod]
    public void TestTypedGrammar()
    {
        string grammar = @"
                events
                {
                    DING <int> = 2,
                    DONG <List<Dictionary<int,char>>> = 4
                }
                guards
                {
                    BIG,
                    LOUD,
                    WET,
                    WARM,
                    SLIPPERY
                }
                grammar(root)
                {
                    root<List<int[]>>: DING root
                    |     DONG[!BIG & ((LOUD & WET & WARM) | SLIPPERY)] root
                    | 
                    ;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(3, p.ConstructedGrammar.Terminals);
        Assert.AreEqual("List<Dictionary<int,char>>", p.ConstructedGrammar.Terminals[2].ValueType);
        Assert.HasCount(5, p.ConstructedGrammar.Guards);
        Assert.HasCount(1, p.ConstructedGrammar.Nonterminals);
        Assert.AreEqual("List<int[]>", p.ConstructedGrammar.Nonterminals[0].ValueType);
    }

    [TestMethod]
    public void TestInvalidArgCount()
    {
        string grammar = @"
                events
                {
                    DING <int> = 2,
                    DONG <List<Dictionary<int,char>>> = 4
                }
                guards
                {
                    BIG,
                    LOUD,
                    WET,
                    WARM,
                    SLIPPERY
                }
                grammar(root)
                {
                    root<List<int[]>>: DING root
                    |     DONG[!BIG & ((LOUD & WET & WARM) | SLIPPERY)] root
                          {
                              $$ = $2;
                          }
                    | 
                    ;
                }";

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        p.ErrorLevel = Parsing.MessageLevel.WARN;
        StringBuilder se = new();
        //p.TraceLog = new StringWriter(sb);
        p.ErrStream = new StringWriter(se);
        bool result = p.Parse(new Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
        Assert.IsFalse(result);
        Assert.Contains("$2 outside valid range ($0 to $1)", se.ToString());
    }

    [TestMethod]
    public void TestAsIdentifier()
    {
        LeafIndexProvider lip = new();
        LeafExpr left = new("left", lip);
        Assert.AreEqual("left", left.AsIdentifier(lip));
        LeafExpr right = new("right", lip);
        NotExpr notRight = new(right);
        Assert.AreEqual("right_1", notRight.AsIdentifier(lip));
        AndExpr andExpr = new(left, notRight);
        Assert.AreEqual("left_right_2", andExpr.AsIdentifier(lip));
        LeafExpr mid = new("middle", lip);
        OrExpr bigExpr = new(mid, andExpr);
        Assert.AreEqual("left_right_middle_F2", bigExpr.AsIdentifier(lip));
    }

    /*   [TestMethod]
       public void TestGuardedNonterminals()
       {
           string grammar = @"
               events
               {
                   V, W, X, Y, Z
               }
               guards
               {
                   C1
               }
               grammar(topRule)
               {
                   topRule: Z w {action 0}
                   | w {action 1};
                   w: x y z[C1] V {action 2}
                   | x y z[C1] x {action 3};
                   y: Z Y {action 4}
                   | W {action 5};
                   z: Z y {action 6}
                   | y W {action 7}
                   | {action 8};
                   x: X Y {action 9}
                   | V {action 10};
               }";

           LRParser p = ParserFactory<LRParser>.CreateInstance();
           StringBuilder sb = new StringBuilder();
           //p.TraceLog = new StringWriter(sb);
           p.DebugStream = new StringWriter(sb);
           bool result = p.Parse(new Parsing.Tokeniser(new StringReader(grammar), p.ParserTable.Tokens));
           Assert.IsTrue(result);
           Assert.AreEqual(6, p.ConstructedGrammar.Terminals.Count);
           Assert.AreEqual(1, p.ConstructedGrammar.Guards.Count);
           Assert.AreEqual(5, p.ConstructedGrammar.Nonterminals.Count);
           string validationResult = p
               .ConstructedGrammar.CreateItemSetsAndGotoEntries(false);
           Assert.AreEqual(19, p.ConstructedGrammar.Productions.Count);
           Assert.AreEqual(string.Empty, validationResult);
       }*/

    [TestMethod]
    public void TestGuardPriorities()
    {
        string grammar = @"
                options
                {
                    using Parsing,
                    namespace ParserParserTests,
                    parserclass GPParser
                }
                events
                {
                    EV, EW
                }
                guards
                {
                    C, D, E
                }
                grammar(topRule)
                {
                    topRule: w EW[D&E]
                    | EV[D]
                    | w EW[C&D];
                w: EV[C|D]
                | EV[D|E];
                }";

        ParserFactory<GPParser>.InitializeFromGrammar
            (
                grammar,
                false,
                false,
                true
            );

        GPParser gpp = ParserFactory<GPParser>.CreateInstance();

        Assert.IsNotNull(gpp);
    }

    private readonly string fsmGrammar =
        @"
            options
            {
                using System,
                namespace ParserParserTests,
                fsmclass TestFSM
            }
            events
            {
                TICK,
                TOCK,
                STOPALARM,
                END
            }
            guards
            {
                AlarmTimeReached,
                AtQuarterHour
            }
            fsm(AwaitingTick)
            {
                AwaitingTock :
                    entry
                    { Advance(); }
                    TOCK AwaitingTick
                ;

                AwaitingTick :
                    TICK[!AtQuarterHour & AlarmTimeReached] AwaitingTock
                         { AlarmOn(); }
                |   TICK[AtQuarterHour & !AlarmTimeReached] AwaitingTock
                         { Chime(); }
                |	TICK[AtQuarterHour & AlarmTimeReached] AwaitingTock
                         { Chime(); AlarmOn(); }
                |   TICK AwaitingTock
                |   STOPALARM AwaitingTick
                         { AlarmOff(); }
                |   END
                ;
            }
            ";

    [TestMethod]
    public void ParseSimpleFSM()
    {

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(fsmGrammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(5, p.ConstructedGrammar.Terminals); // Includes ERR token
        Assert.HasCount(2, p.ConstructedGrammar.Guards);
        Assert.HasCount(2, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(2, p.ConstructedGrammar.MachineStates);
    }

    [TestMethod]
    public void CreateFSMSource()
    {
        StringBuilder src = new();
        StringWriter srcStream = new(src);
        FSMOutput fsmGenerator = new(srcStream);
        string err = fsmGenerator.BuildSource(fsmGrammar, false, out List<string> extRefs);
        Assert.IsTrue(string.IsNullOrEmpty(err));
        Assert.IsNotNull(extRefs);
        Assert.IsEmpty(extRefs);
    }

    [TestMethod]
    public void CreateSimpleFSM()
    {
        FSMFactory<TestFSM>.InitializeFromGrammar(fsmGrammar, false);
        FSM fsm = FSMFactory<TestFSM>.CreateInstance();
        Assert.IsNotNull(fsm);
        Assert.HasCount(2, fsm.FSMTable.States);
        Assert.AreEqual("AwaitingTick", fsm.FSMTable.States[0].Name);
        Assert.HasCount(6, fsm.FSMTable.States[0].Transitions);
        Assert.AreEqual("(AtQuarterHour & !(AlarmTimeReached))", fsm.FSMTable.States[0].Transitions[1].Condition.AsString());
        Assert.AreEqual("TICK", fsm.FSMTable.Tokens[fsm.FSMTable.States[0].Transitions[1].InputToken]);
    }

    [TestMethod]
    public void RunSimpleFSM()
    {
        FSMFactory<TestFSM>.InitializeFromGrammar(fsmGrammar, false);
        TestFSM fsm = FSMFactory<TestFSM>.CreateInstance();
        Assert.IsNotNull(fsm);
        Assert.IsTrue(fsm.Run(fsm));
        Assert.AreEqual(6, fsm.Minutes);
        Assert.AreEqual(1, fsm.ChimeCount);
        Assert.IsTrue(fsm.AlarmEnabled);
    }
    private readonly string tlGrammar =
        @"
                options
                {
                    namespace TrafficLightController,
                    fsmclass CrossingController
                }

                events
                {
                    TIMERTICK,
                    BUTTONPRESS
                }

                guards
                {
                    ButtonWasPressed
                }

                fsm(Green)
                {
                    // The AllRed state is used when both traffic and
                    // pedestrians are barred from crossing. This is
                    // the state the controller is in while waiting
                    // for slower pedestrians to complete their crossing.

                    AllRed: 
                        TIMERTICK RedAndYellow
                            { SetLightRedAndYellow(); SetRedAndYellowTimer(); }
                    |	BUTTONPRESS AllRed
                            { RecordButtonPressed(); }
                    ;

                    // This is an English traffic light! In England
                    // the lights go to both red and yellow lit up
                    // for a few seconds between being red and green.

                    RedAndYellow: 
                        TIMERTICK TimedGreen
                            { SetLightGreen(); SetGreenTimer(); }
                    |	BUTTONPRESS RedAndYellow
                            { RecordButtonPressed(); }
                    ;

                    // Once the lights have gone green, there is a
                    // minimum interval they must remain green before
                    // the controller will respond to the next button press.

                    TimedGreen: 
                        TIMERTICK[ButtonWasPressed] Yellow
                            { SetLightYellow(); SetYellowTimer(); }
                    |	BUTTONPRESS TimedGreen
                            { RecordButtonPressed(); }
                    |	TIMERTICK[!ButtonWasPressed] Green
                    ;

                    // After the minimum interval for the lights on green
                    // has expired, the lights will stay on green indefinitely
                    // unless a pedestrian presses the request to cross button.

                    Green: 
                        BUTTONPRESS Yellow
                            { SetLightYellow(); RecordButtonPressed(); SetYellowTimer(); }
                    ;

                    // The yellow light is displayed for a few seconds between
                    // green and red to tell traffic to stop at the light if
                    // it is safe for them to do so.

                    Yellow: 
                        BUTTONPRESS Yellow
                            { RecordButtonPressed(); }
                    |	TIMERTICK RedWalk
                            { SetLightRed(); SetLightWalk(); ClearButtonPress(); SetWalkTimer(); }
                    ;

                    // Once the lights have gone to red, we allow
                    // pedestrians to cross the road.

                    RedWalk: BUTTONPRESS RedWalk
                    |	TIMERTICK AllRed
                            { SetLightDontWalk(); SetAllRedTimer(); }
                    ;
                }            
            ";

    [TestMethod]
    public void ParseTrafficLightFSM()
    {

        LRParser p = ParserFactory<LRParser>.CreateInstance();
        StringBuilder sb = new();
        //p.TraceLog = new StringWriter(sb);
        p.DebugStream = new StringWriter(sb);
        bool result = p.Parse(new Parsing.Tokeniser(new StringReader(tlGrammar), p.ParserTable.Tokens));
        Assert.IsTrue(result);
        Assert.HasCount(3, p.ConstructedGrammar.Terminals); // Includes ERR token
        Assert.HasCount(1, p.ConstructedGrammar.Guards);
        Assert.HasCount(6, p.ConstructedGrammar.Nonterminals);
        Assert.HasCount(6, p.ConstructedGrammar.MachineStates);
    }

    [TestMethod]
    public void TestGrammarItemSort()
    {
        LeafIndexProvider lip = new();
        GrammarToken t1 = new(1, TokenType.Terminal, "TokenType1");
        GrammarToken t2 = new(2, TokenType.Terminal, "TokenType2");
        BoolExpr LAndNotR = new AndExpr(new LeafExpr("L", lip), new NotExpr(new LeafExpr("R", lip)));
        BoolExpr NotLAndR = new AndExpr(new LeafExpr("R", lip), new NotExpr(new LeafExpr("L", lip)));
        BoolExpr L = new LeafExpr("L", lip);

        GrammarItemSet gis = new([]);
        List<GrammarItemSet> target =
        [
            gis
        ];
        gis.Shifts.Add(new GrammarElement(t1, null), target);
        gis.Shifts.Add(new GrammarElement(t2, L), target);
        gis.Shifts.Add(new GrammarElement(t1, L), target);
        gis.Shifts.Add(new GrammarElement(t2, LAndNotR), target);
        gis.Shifts.Add(new GrammarElement(t1, NotLAndR), target);
        gis.Shifts.Add(new GrammarElement(t1, LAndNotR), target);
        gis.Shifts.Add(new GrammarElement(t2, null), target);
        gis.ComputeShiftReduceOrder();
        Assert.IsTrue(gis.IntersectingPairs != null
            && gis.IntersectingPairs.Count == 0);
        Assert.AreEqual(t1, gis.TransitionsInOrder[0].Element.Token);
        Assert.AreEqual(NotLAndR, gis.TransitionsInOrder[0].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[1].Element.Token);
        Assert.AreEqual(LAndNotR, gis.TransitionsInOrder[1].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[2].Element.Token);
        Assert.AreEqual(L, gis.TransitionsInOrder[2].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[3].Element.Token);
        Assert.IsNull(gis.TransitionsInOrder[3].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[4].Element.Token);
        Assert.AreEqual(LAndNotR, gis.TransitionsInOrder[4].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[5].Element.Token);
        Assert.AreEqual(L, gis.TransitionsInOrder[5].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[6].Element.Token);
        Assert.IsNull(gis.TransitionsInOrder[6].Element.Guard);
    }

    [TestMethod]
    public void TestGrammarItemIntersection()
    {
        LeafIndexProvider lip = new();
        GrammarToken t1 = new(1, TokenType.Terminal, "TokenType1");
        GrammarToken t2 = new(2, TokenType.Terminal, "TokenType2");
        BoolExpr LOrNotR = new OrExpr(new LeafExpr("L", lip), new NotExpr(new LeafExpr("R", lip)));
        BoolExpr NotLOrR = new OrExpr(new LeafExpr("R", lip), new NotExpr(new LeafExpr("L", lip)));
        BoolExpr L = new LeafExpr("L", lip);

        GrammarItemSet gis = new([]);
        List<GrammarItemSet> target =
        [
            gis
        ];
        gis.Shifts.Add(new GrammarElement(t1, null), target);
        gis.Shifts.Add(new GrammarElement(t2, L), target);
        gis.Shifts.Add(new GrammarElement(t1, L), target);
        gis.Shifts.Add(new GrammarElement(t2, LOrNotR), target);
        gis.Shifts.Add(new GrammarElement(t1, NotLOrR), target);
        gis.Shifts.Add(new GrammarElement(t1, LOrNotR), target);
        gis.Shifts.Add(new GrammarElement(t2, null), target);
        gis.ComputeShiftReduceOrder();
        Assert.IsTrue(gis.IntersectingPairs != null
            && gis.IntersectingPairs.Count == 2);
        Assert.AreEqual(t1, gis.IntersectingPairs[0].Key.Element.Token);
        Assert.AreEqual(L, gis.IntersectingPairs[0].Key.Element.Guard);
        Assert.AreEqual(t1, gis.IntersectingPairs[0].Value.Element.Token);
        Assert.AreEqual(NotLOrR, gis.IntersectingPairs[0].Value.Element.Guard);
        Assert.AreEqual(t1, gis.IntersectingPairs[1].Key.Element.Token);
        Assert.AreEqual(NotLOrR, gis.IntersectingPairs[1].Key.Element.Guard);
        Assert.AreEqual(t1, gis.IntersectingPairs[1].Value.Element.Token);
        Assert.AreEqual(LOrNotR, gis.IntersectingPairs[1].Value.Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[0].Element.Token);
        Assert.AreEqual(L, gis.TransitionsInOrder[0].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[1].Element.Token);
        Assert.AreEqual(NotLOrR, gis.TransitionsInOrder[1].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[2].Element.Token);
        Assert.AreEqual(LOrNotR, gis.TransitionsInOrder[2].Element.Guard);
        Assert.AreEqual(t1, gis.TransitionsInOrder[3].Element.Token);
        Assert.IsNull(gis.TransitionsInOrder[3].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[4].Element.Token);
        Assert.AreEqual(L, gis.TransitionsInOrder[4].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[5].Element.Token);
        Assert.AreEqual(LOrNotR, gis.TransitionsInOrder[5].Element.Guard);
        Assert.AreEqual(t2, gis.TransitionsInOrder[6].Element.Token);
        Assert.IsNull(gis.TransitionsInOrder[6].Element.Guard);
    }
}

public class TestFSM : FSM, IEnumerable<Parsing.IToken>
{
    public int Minutes
    {
        get;
        private set;
    }

    public int ChimeCount
    {
        get;
        private set;
    }

    public bool AlarmEnabled;

    public bool AlarmTimeReached() => Minutes % 60 == 0;

    public bool AtQuarterHour() => Minutes % 15 == 0;

    public void Advance() => Minutes++;

    public void AlarmOn() => AlarmEnabled = true;

    public void Chime() => ChimeCount++;

    public void AlarmOff() => AlarmEnabled = false;

    public IEnumerator<Parsing.IToken> GetEnumerator()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return new ParserToken(FSMTable.Tokens["TICK"]);
            yield return new ParserToken(FSMTable.Tokens["TOCK"]);
        }
        yield return new ParserToken(FSMTable.Tokens["END"]);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < 6; i++)
        {
            yield return new ParserToken(FSMTable.Tokens["TICK"]);
            yield return new ParserToken(FSMTable.Tokens["TOCK"]);
        }
        yield return new ParserToken(FSMTable.Tokens["END"]);
    }
}

public class DebugWriter : TextWriter
{
    public override void Write(string format, params object[] arg) => Debug.Write(string.Format(format, arg));

    public override void WriteLine(string value) => Debug.Write(string.Format("{0}\r\n", value));

    public override Encoding Encoding => Encoding.Default;
}

public class GPParser : Parser
{
    public static bool C(object _) => true;

    public static bool D(object _) => true;

    public static bool E(object _) => true;
}
