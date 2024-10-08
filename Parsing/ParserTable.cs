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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Parsing
{
    /// <summary>
    /// The definition of the grammar as generated by
    /// the parser generator. These tables are used
    /// by the Parser class to parse the input.
    /// </summary>

    public class ParserTable
    {
        /// <summary>
        /// At run-time, convert all the guard and action method names
        /// into proxy functions that call the real guard and action
        /// functions with those names in their derived parser class.
        /// </summary>
        /// <param name="parserType">The type of the parser derived class</param>

        public void Bind(Type parserType)
        {
            // Bind all the guard functions using a compiled
            // expression proxy. This has the benefits of
            // late binding, with the performance (nearly)
            // of compile time binding.

            foreach (ParserState state in States)
                foreach (ParserStateColumn col in state.TerminalColumns)
                    col.Condition?.Bind(parserType, true);

            // Bind all the action functions using a compiled
            // expression proxy.

            foreach (ParserProduction pp in Productions)
                pp.InlineAction?.Bind(parserType);

            // Bind any merge actions held by the parser table

            if (MergeActions != null)
                foreach (ParserMergeAction pma in MergeActions.Values)
                    pma.Bind(parserType);

            // Mark the parser tables as bound

            Bound = true;
        }

        /// <summary>
        /// Delegate to a method containing the code:
        /// return new MyParserNamespace.AutoGenerated.MyParser_AutoGenerated();
        /// where the programmer-written parser class was called
        /// MyParserNamespace.MyParser.
        /// </summary>

        private Delegate parserDefaultConstructor = null;

        /// <summary>
        /// Initialize the factory delegate for creating instances of the parser class
        /// </summary>
        /// <param name="autoGenParserType">The autogenerated parser class</param>

        public void InitializeParserConstructor(Type autoGenParserType)
        {
            // Argument validation

            ArgumentNullException.ThrowIfNull(autoGenParserType);

            // Create dynamic IL code to invoke the constructor
            // for the found parser class type, then cache it
            // into the delegate field reserved for that purpose.

            LambdaExpression newLambda = Expression.Lambda(Expression.New(autoGenParserType));
            parserDefaultConstructor = newLambda.Compile();
        }

        /// <summary>
        /// Create an instance of the parser whose
        /// class is associated with this ParserTable
        /// </summary>
        /// <typeparam name="T">The type of the parser class to create. Note
        /// that this should be either the programmer's parser class type
        /// as derived from Parsing.Parser, or could be just Parsing.Parser
        /// or Parsing.IParser if we don't need the actual parser type.</typeparam>
        /// <returns>An instance of the parser with the specified class</returns>

        public T CreateParser<T>() where T : IParser, new()
        {
            // Before this factory method is called, we need to bind the
            // constructor for the auto-generated derived parser class so
            // that subsequent calls to this method execute very quickly.
            // To do this, call InitializeParserConstructor before calling
            // this CreateParser<T> method.

            if (parserDefaultConstructor == null)
                throw new InvalidOperationException
                    ("InitializeParserConstructor should be called before CreateParser<T>");

            // Invoke the constructor for the auto-generated parser derived class,
            // but return it as a reference to the programmer-written parser class
            // the programmer wrote. There is no reason for a developer to want
            // direct access to the autogenerated methods of the derived parser
            // class. Note that the parser table is also injected here.

            T newParser = ((Func<T>)parserDefaultConstructor)();
            newParser.ParserTable = this;
            return newParser;
        }

        /// <summary>
        /// Indicates whether the run-time binding operation
        /// has taken place on the parser tables. This run-time
        /// binding compiles the proxies that link the actions
        /// and guards in the parser tables with the equivalent
        /// methods in the derived parser class.
        /// </summary>

        public bool Bound
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the parser expects to run using
        /// yacc/bison style error recovery, where states are
        /// popped from the stack until an error token is
        /// shiftable. At this point input tokens are discarded
        /// until a token matches the valid token after the
        /// error token. If set to false, the first encountered
        /// error causes the parser to abort and return false.
        /// </summary>

        public bool ErrorRecoveryEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup table for names of tokens as used
        /// in the grammar, since we are using
        /// inbounded integers as the token types
        /// rather than an enum, so that the tokens
        /// can be set at runtime.
        /// </summary>

        public TwoWayMap<string, int> Tokens
        {
            get;
            set;
        }

        /// <summary>
        /// The list of productions that are used to
        /// reduce expanded productions to just their
        /// left-hand token when completely recognised.
        /// </summary>

        public ParserProduction[] Productions
        {
            get;
            set;
        }

        /// <summary>
        /// The list of item sets that represent the states
        /// we pass through as we parse a user's input tokens
        /// </summary>

        public ParserState[] States
        {
            get;
            set;
        }

        /// <summary>
        /// The set of compound guard
        /// expressions used by the parser
        /// </summary>

        public IGuardEvaluator[] Guards
        {
            get;
            set;
        }

        /// <summary>
        /// The lookup table of merge functions to be
        /// called when reducing to a particular rule LHS
        /// </summary>

        public Dictionary<int, ParserMergeAction> MergeActions
        {
            get;
            set;
        }

        /// <summary>
        /// Find the production that is the 'ruleNumth'
        /// production reducing to the non terminal
        /// token with identifier 'lhs'.
        /// </summary>
        /// <param name="lhs">The name of the token at the left of the rule</param>
        /// <param name="ruleNum">The index into the list of tokens that have this
        /// name at the left of the rule</param>
        /// <returns>The corresponding ParserProduction object. Can return
        /// null is either argument is invalid.</returns>

        public ParserProduction FindByRuleName(string lhs, int ruleNum)
        {
            // Don't tolerate a search when there are no productions
            // in the parser table, or if the rule name is non-existent.

            if (Productions == null
                || string.IsNullOrEmpty(lhs)
                || ruleNum < 0)
                return null;

            // Find the unique integer allocated to this non-terminal token.
            // If the name is not found, just return null to the caller.

            if (!Tokens.TryGetByKey(lhs, out int token))
                return null;

            // Find the parser production at the specified index. Return
            // null if the index is out of range for the set of productions
            // that have this left hand side token name.

            return Productions.FirstOrDefault
                (p => p.NonterminalType == token && p.RuleNumber == ruleNum);
        }
    }
}