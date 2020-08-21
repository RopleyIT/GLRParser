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
using ParserGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parsing
{
    public class LRParser : Parser
    {
        /// <summary>
        /// Options to be passed to the code generator.
        /// These include the list of using statements,
        /// and the namespace chosen for the parser.
        /// </summary>

        public Dictionary<string, object> Options
        {
            get
            {
                if (ConstructedGrammar != null)
                    return ConstructedGrammar.Options;
                else
                    return null;
            }
        }

        /// <summary>
        /// Set true if the output parser should unravel
        /// its stack when errors are encountered on input
        /// </summary>

        public bool OutputParserSupportsErrorRecovery
        {
            get;
            set;
        }

        /// <summary>
        /// The reference to the output data
        /// structure where the grammar will be built
        /// </summary>

        public Grammar ConstructedGrammar
        {
            get;
            set;
        }

        /// <summary>
        /// The list of using statement namespaces
        /// that should appear at the top of the parser.
        /// By default the following list of namspaces
        /// are used: System; System.Collections.Generic;
        /// System.Linq; System.Text; System.IO;
        /// </summary>

        public List<string> Usings => Options["usings"] as List<string>;

        /// <summary>
        /// If it is not already in the list, include a
        /// using statement into the list of usings that
        /// will appear at the top of the parser source.
        /// </summary>
        /// <param name="ns">Namespace to include</param>

        public void AddUsing(string ns)
        {
            if (!Usings.Exists(s => s == ns))
                Usings.Add(ns);
        }

        /// <summary>
        /// The list of additional DLLs referenced from
        /// this parser class. Any calls to classes in
        /// assemblies outside the default list will need
        /// to be added to the options section of the
        /// grammar as assemblyref statements. This is
        /// equivalent to the 'Add reference' option
        /// in visual studio projects. The default list
        /// of assemblies automatically referenced contains
        /// all the assemblies in csc.rsp (the .NET C#
        /// compiler default list) plus the assembly
        /// containing the application's parser class.
        /// </summary>

        public List<string> AssemblyReferences => Options["assemblyrefs"] as List<string>;

        /// <summary>
        /// If it is not already in the list, include
        /// an assemblyref statement into the list of
        /// references that will appear at the top of
        /// the parser source.
        /// </summary>
        /// <param name="ns">Namespace to include</param>

        public void AddAssemblyReference(string ns)
        {
            if (!AssemblyReferences.Exists(s => s == ns))
                AssemblyReferences.Add(ns);
        }

        /// <summary>
        /// Default constructor
        /// </summary>

        public LRParser()
        {
            // Create the output grammar object that will
            // capture the data to build the state machine
            // from the input grammar rules. Include the
            // error token into the terminal token list
            // at the same time.

            ConstructedGrammar = new Grammar();
            ConstructedGrammar.Terminals.Add
            (
                new GrammarToken
                (
                    Grammar.ErrorTokenValue, TokenType.Terminal, "ERR"
                )
            );

            // Set up the Usings property within the
            // option dictionary collection

            Options.Add("usings", new List<string>());

            // Insert the default list of assemblies that
            // require using statements in the output parser

            Usings.Add("System");
            Usings.Add("System.Collections.Generic");
            Usings.Add("System.Linq");
            Usings.Add("System.Text");
            Usings.Add("System.IO");

            // Set up the NameSpace and ParserClass
            // properties, and give them initial values

            Options.Add("namespace", "Parsing");
            Options.Add("parserclass", "LRParser");

            // Add the collection of specific external
            // assemblies the inline parser will need to
            // reference for the inline code in the
            // grammar.

            Options.Add("assemblyrefs", new List<string>());
        }

        /// <summary>
        /// By default, the namespace used is
        /// LRParser. This can be overridden
        /// by assigning to this property. This
        /// happens as part of the options clause
        /// at the top of the grammar.
        /// </summary>

        public string Namespace
        {
            get => Options["namespace"].ToString();
            set => Options["namespace"] = value;
        }

        /// <summary>
        /// The name of the derived parser class
        /// that the developer fills with their own
        /// guard functions, action functions and other
        /// application specific logic. This class may also
        /// have the autogenerated parser actions and
        /// guards created by the parser. This is why
        /// the derived parser class must be declared
        /// as a partial class.
        /// </summary>

        public string ParserClass
        {
            get => Options["parserclass"].ToString();
            set => Options["parserclass"] = value;
        }

        /// <summary>
        /// The name of the derived parser class
        /// that the developer fills with their own
        /// guard functions, action functions and other
        /// application specific logic. This class may also
        /// have the autogenerated parser actions and
        /// guards created by the parser. This is why
        /// the derived parser class must be declared
        /// as a partial class.
        /// </summary>

        public string FSMClass
        {
            get => Options["fsmclass"].ToString();
            set => Options["fsmclass"] = value;
        }

        /// <summary>
        /// The grammar token currently being defined in a grammar rule
        /// </summary>

        public GrammarToken CurrentRuleLHS
        {
            get;
            set;
        }

        /// <summary>
        /// Look in the lists of terminal tokens, guard conditions
        /// and action names to see if the proposed identifier has
        /// already been used in one of those lists.
        /// </summary>
        /// <param name="tokenName">The proposed new identifier</param>
        /// <returns>A parser error object if the name has already
        /// been used. Null if not.</returns>

        private bool CheckNameNotAlreadyUsed(string tokenName)
        {
            // Check the identifier has not already
            // appeared in the guards, terminal
            // tokens, or actions lists.

            GrammarToken tok = ConstructedGrammar.Terminals
                .FirstOrDefault(t => t.Text == tokenName);
            if (tok != null)
            {
                Error(tokenName + " already declared as a terminal token");
                return false;
            }

            tok = ConstructedGrammar.Guards
                .FirstOrDefault(t => t.Text == tokenName);
            if (tok != null)
            {
                Error(tokenName + " already declared as a guard condition");
                return false;
            }
            return true;
        }

        /// <summary>
        ///  Performs a check to see if a guard token with this
        ///  name has already been declared. If not, adds this
        ///  name to the list of guard tokens understood by the 
        ///  parser.
        /// </summary>
        /// <param name="guard">The new guard function name
        /// and optional code body to add.</param>
        /// <returns>True if able to insert. False if
        /// name in use.</returns>

        public bool CheckAndInsertGuardToken(GrammarGuardOrAction guard)
        {
            if (guard == null)
                throw new ArgumentNullException(nameof(guard));

            // Make sure we are not reusing the identifier

            if (!CheckNameNotAlreadyUsed(guard.Name))
                return false;

            // Add the identifier to the list of action names supported,
            // and the guard function implementation (if present) to the
            // list of guard implementations.

            ConstructedGrammar.Guards.Add
                (new GrammarToken
                    (ConstructedGrammar.Guards.Count + Grammar.MinGuardValue,
                    TokenType.Guard, guard.Name));
            ConstructedGrammar.GuardBodies.Add(guard);
            return true;
        }

        /// <summary>
        /// Check to see if the terminal token name has already been
        /// used within the parser. If not, go ahead and use it.
        /// </summary>
        /// <param name="tokenName">The new token name to try
        /// and add</param>
        /// <param name="valueType">The data type for this token's
        /// Value property</param>
        /// <param name="value">The value to associate with
        /// this new token</param>
        /// <returns>True if successful. If false, the parser's Error
        /// function will have been called to flag the error.</returns>

        public bool CheckAndInsertToken(string tokenName, string valueType, int value)
        {
            // Select a unique value for the token

            if (value == Grammar.UnassignedTokenValue)
                value =
                    (from tok in ConstructedGrammar.Terminals
                     where tok.TokenNumber >= Grammar.MinAutoTokenValue
                     && tok.TokenNumber <= Grammar.MaxAutoTokenValue
                     select tok.TokenNumber)
                    .DefaultIfEmpty(Grammar.MinAutoTokenValue - 1)
                    .Max() + 1;
            else if (value < Grammar.MinUserTokenValue || value > Grammar.MaxUserTokenValue)
            {
                Error
                (
                    "User values for tokens must be between " +
                    Grammar.MinUserTokenValue + " and " +
                    Grammar.MaxUserTokenValue
                );
                return false;
            }

            // Make sure we are not reusing the identifier, or value

            if (!CheckNameNotAlreadyUsed(tokenName))
                return false;
            if (ConstructedGrammar.Terminals.Exists(tok => tok.TokenNumber == value))
            {
                Error("Value " + value + " already used for another token");
                return false;
            }

            // Add the identifier to the list of action names supported

            GrammarToken gt = new GrammarToken
            (
                value,
                ParserGenerator.TokenType.Terminal,
                tokenName,
                valueType
            );
            ConstructedGrammar.Terminals.Add(gt);
            return true;
        }

        /// <summary>
        /// Add a new grammar production to the end of the grammar's
        /// set of known productions. Includes setting the LHS token,
        /// and the rule indexes and production numbers automatically.
        /// </summary>
        /// <param name="lhsToken">The token this production reduces to</param>
        /// <param name="gp">The grammar production to be added</param>

        public void AppendGrammarProduction(GrammarToken lhsToken, GrammarProduction gp)
        {
            if (gp == null)
                throw new ArgumentException
                    ("Grammar production cannnot be null if it is to be appended");

            // Build the non-terminal rule name onto the
            // front of each production in this group. Note that
            // the positioning of the setting of the ProductionNumber
            // ensures that the value 0 is reserved for later
            // (used for the extended grammar start symbol _Start).

            int ruleIndex = ConstructedGrammar.Productions.Count(p => p.LHS == lhsToken);
            gp.LHS = lhsToken;
            gp.NonterminalRuleNumber = ruleIndex;
            ConstructedGrammar.Productions.Add(gp);
            gp.ProductionNumber = ConstructedGrammar.Productions.Count;
        }

        /// <summary>
        /// Set of prefixes to place on rule names
        /// to support multiplicity grammar rules.
        /// </summary>

        private static readonly string[] mulPrefixes =
        {
            "",
            "zeroOrOne_",
            "oneToMany_",
            "zeroToMany_"
        };

        /// <summary>
        /// For a given token accompanied by a multiplicity symbol in the grammar,
        /// automatically ensure that the corresponding additional grammar rules
        /// that implement the selected multiplicity have been added to the grammar.
        /// For example, the presence of 'myToken+' in a grammar rule will cause
        /// the 'myToken+' in the grammar to be replaced with 'oneOrMany_myToken',
        /// and the following additional rules to appear in the grammar:
        /// oneOrMany_myToken :
        ///     oneOrMany_myToken myToken = LatestNonterminalInList
        /// |   myToken = EarliestNonterminalInList
        /// ;
        /// </summary>
        /// <param name="tokStr">The name of the token that has
        /// a '?', '+' or '*' after it</param>
        /// <param name="tokGuard">The guard expression to the left
        /// of the multiplicity symbol. Sets a simple guard condition
        /// on the preceding terminal or non-terminal token.</param>
        /// <param name="m">The multiplicity character, expressed as an enum</param>
        /// <param name="mGuard">The guard expression on the multiplicity. This
        /// should be tested at the end of the list of items in the multiplicity.</param>
        /// <returns>The grammar element for the renamed multiplicity token</returns>

        public GrammarElement ManageMultiplicityProductions
            (string tokStr, BoolExpr tokGuard, Multiplicity m, BoolExpr mGuard)
        {
            // Is this the name of a terminal token?

            GrammarToken tok = ConstructedGrammar.Terminals
                .FirstOrDefault(t => t.Text == tokStr);

            // If not, see if it is a non-terminal token

            if (tok == null)
                tok = ConstructedGrammar.Nonterminals
                    .FirstOrDefault(t => t.Text == tokStr);

            // This token has never been seen before, so is
            // presumably to be treated as a new non-terminal 
            // token. This is because all terminals must be
            // declared in the 'tokens' section of the grammar
            // before they are ever used.

            if (tok == null)
            {
                tok = new GrammarToken
                    (ConstructedGrammar.Nonterminals.Count + Grammar.MinNonterminalValue + 1,
                    ParserGenerator.TokenType.Nonterminal, tokStr);
                ConstructedGrammar.Nonterminals.Add(tok);
            }

            // Create the shared version of the boolean expression

            tokGuard = ConstructedGrammar.BooleanExpressionCache
                .InternExpression(tokGuard);

            // Create the rule element 

            GrammarElement tokElement = new GrammarElement(tok, tokGuard);

            // Simple identifier-guard pairs represent non-multiplicity
            // elements, so we can just return at this point

            if (m == Multiplicity.ExactlyOne)
                return tokElement;

            // If we get here, we have a '?', '*' or '+'
            // after the token and optional guard condition.
            // Look up the multiplicity identifier from the 
            // lists of terminal and non-terminal tokens,
            // and auto-generate the name for the multiplicity
            // grammar rule left hand side.

            string mulTokStr = mulPrefixes[(int)m] + tokElement.AsIdentifier
                (ConstructedGrammar.BooleanExpressionCache.IndexProvider);

            // Has this token already been referred to?

            GrammarToken mulTok = ConstructedGrammar.Nonterminals
                .FirstOrDefault(t => t.Text == mulTokStr);

            // If not used in a previous rule, we have to create the
            // corresponding grammar productions to handle the
            // multiplicity using recursive rules.

            if (mulTok == null)
            {
                // Create the new multiplicity non-terminal

                mulTok = new GrammarToken
                    (ConstructedGrammar.Nonterminals.Count + Grammar.MinNonterminalValue + 1,
                    ParserGenerator.TokenType.Nonterminal, mulTokStr);
                mulTok.SetMultiplicity(m, tok);
                ConstructedGrammar.Nonterminals.Add(mulTok);

                // Now add the extra grammar productions
                // associated with this multiplicity rule

                if (m == Multiplicity.ZeroOrOne)
                {
                    // Insert rules: optional_tokenName : 
                    //                  tokenName = EarliestNonterminalInList[tokenGuard]
                    //               |  = EmptyList ;

                    AddSingleTokenRule(mulTok, tokElement);
                    AddEmptyRule(mulTok);
                }
                else if (m == Multiplicity.ZeroToMany)
                {
                    // Insert rules: zeroToMany_tokenName : 
                    //                  zeroToMany_tokenName tokenName[tokenGuard] = LatestNonterminalInList
                    //               | = EmptyList ;

                    AddRecursiveTokenRule(mulTok, tokElement);
                    AddEmptyRule(mulTok);
                }
                else if (m == Multiplicity.OneToMany)
                {
                    // Insert rules: oneToMany_tokenName : 
                    //                  oneToMany_tokenName tokenName[tokenGuard] = LatestNonterminalInList
                    //               |  tokenName[tokenGuard] = EarliestNonterminalInList ;

                    AddRecursiveTokenRule(mulTok, tokElement);
                    AddSingleTokenRule(mulTok, tokElement);
                }
            }

            // Create the rule element after creating a 
            // shared version of the boolean expression

            mGuard = ConstructedGrammar.BooleanExpressionCache
                .InternExpression(mGuard);
            return new GrammarElement(mulTok, mGuard);
        }

        // Shared instances of the three multiplicity actions

        private GrammarGuardOrAction ggaEarliestNonterminalInList = null;
        private GrammarGuardOrAction ggaLatestNonterminalInList = null;
        private GrammarGuardOrAction ggaEmptyList = null;

        /// <summary>
        /// Add a new empty, actionless grammar rule for the
        /// token lhsToken (lhsToken: /* empty */;)
        /// </summary>
        /// <param name="lhsToken">The token to appear
        /// as the LHS side of the production</param>

        private void AddEmptyRule(GrammarToken lhsToken)
        {
            // Make sure the empty list code has been
            // autogenerated the first time it is needed.

            if (ggaEmptyList == null)
            {
                string code =
                    "args[0] = new List<object>();\r\n";
                ggaEmptyList = new GrammarGuardOrAction("EmptyList", code);
            }

            // Create the empty rule production

            GrammarProduction p = new GrammarProduction
                (ConstructedGrammar, 0, new List<GrammarElement>(0), ggaEmptyList);
            AppendGrammarProduction(lhsToken, p);
        }

        /// <summary>
        /// Add a grammar rule to put the first element into
        /// a list of elements. The rule that is inserted is
        /// of the grammar form:
        /// lhsToken : rhsToken = EarliestNonterminalInList;
        /// </summary>
        /// <param name="lhsToken">The token to appear as the LHS of the rule</param>
        /// <param name="rhsElement">The possibly guarded element to appear 
        /// as the RHS of the rule</param>

        private void AddSingleTokenRule(GrammarToken lhsToken, GrammarElement rhsElement)
        {
            // Ensure the earliest token in list code has been initialised

            if (ggaEarliestNonterminalInList == null)
            {
                string code =
                    "List<object> argList = new List<object>();\r\n" +
                    "argList.Add(args[1]);\r\n" +
                    "args[0] = argList;\r\n";
                ggaEarliestNonterminalInList = new GrammarGuardOrAction
                    ("EarliestNonterminalInList", code);
            }

            // Create the first item in list production

            List<GrammarElement> gel = new List<GrammarElement> { rhsElement };
            GrammarProduction p = new GrammarProduction
                (ConstructedGrammar, 0, gel, ggaEarliestNonterminalInList);
            AppendGrammarProduction(lhsToken, p);
        }

        /// <summary>
        /// Add a grammar rule to implement lists of tokens. The rule
        /// that is inserted is of the form:
        /// lhsToken: lhsToken rhsToken = LatestNonterminalInList;
        /// </summary>
        /// <param name="lhsToken">The token to appear as the LHS of the rule</param>
        /// <param name="rhsElement">The possibly guarded element to appear 
        /// as the RHS of the rule</param>

        private void AddRecursiveTokenRule(GrammarToken lhsToken, GrammarElement rhsElement)
        {
            // Ensure the earliest token in list code has been initialised

            if (ggaLatestNonterminalInList == null)
            {
                string code =
                    "List<object> argList = args[1] as List<object>;\r\n" +
                    "argList.Add(args[2]);\r\n" +
                    "args[0] = argList;\r\n";
                ggaLatestNonterminalInList = new GrammarGuardOrAction
                    ("LatestNonterminalInList", code);
            }

            // Now create the production

            List<GrammarElement> gel = new List<GrammarElement>(2)
            {
                new GrammarElement(lhsToken, null),
                rhsElement
            };
            GrammarProduction p = new GrammarProduction
                (ConstructedGrammar, 0, gel, ggaLatestNonterminalInList);
            AppendGrammarProduction(lhsToken, p);
        }
    }
}
