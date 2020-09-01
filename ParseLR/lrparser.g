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

    options 
    { 
        using Parsing,
        using ParserGenerator, 
        using BooleanLib,
        namespace Parsing,
        parserclass LRParser
    } 

    events 
    { 
        UNKNOWN, 
        LBRACE, 
        RBRACE, 
        LPAREN, 
        RPAREN, 
        COMMA, 
        COLON, 
        OR, 
        SEMI, 
        LBRACK, 
        RBRACK, 
        AND, 
        NOT, 
        TOKENS, 
        CONDITIONS, 
        GRAMMAR, 
        IDENTIFIER<string>, 
        CODE<string>, 
        OPTIONS, 
        USING, 
        NAMESPACE,
        PARSERCLASS,
        EQUALS,
        INTEGER<int>,
        FSM,
        ONENTRY,
        ONEXIT,
        FSMCLASS,
        MERGE,
        MULTIPLICITY<Multiplicity>,
        ASSEMBLYREF,
        TOKENTYPE<string>,
        LT
    } 

    grammar(CompleteGrammar) 
    { 
        // Actions below added for lists of inline actions

        CompleteGrammar : 
            ParserOptions? Events Guards Rules 
        ; 
     
        ParserOptions : 
            OPTIONS LBRACE OptionList RBRACE 
        ; 
     
        OptionList : 
            OptionList COMMA ParserOption 
        |   ParserOption 
        |	error
            {
                Error("Badly formed parser option");
            }
        ; 
     
        ParserOption : 
            USING IDENTIFIER 
            { 
                AddUsing($1);
            } 
        |   NAMESPACE IDENTIFIER 
            { 
                Namespace = $1;
            } 
        |	PARSERCLASS IDENTIFIER
            {
                ParserClass = $1;
            }
        |	FSMCLASS IDENTIFIER
            {
                FSMClass = $1;
            }
        |   ASSEMBLYREF IDENTIFIER
            {
                AddAssemblyReference($1);
            }
        ; 
     
        Events : 
            TOKENS LBRACE IdentWithOptAssignList RBRACE 
            {
                foreach(Tuple<string, string, int> token in $2)
                {
                    if(!CheckAndInsertToken(token.Item1, token.Item2, token.Item3))
                        break;
                }
            }
        |	TOKENS error RBRACE
            {
                Error
                (
                    "Token/event list should be a comma separated list of " +
                    "identifiers or identifier=value pairs"
                );
            }
        ;

        IdentWithOptAssignList<List<Tuple<string, string, int>>> :
            IdentWithOptAssignList COMMA IdentWithOptAssign
            {
                $0.Add($2);
                $$ = $0;
            }
        
        |	IdentWithOptAssign
            {
                $$ = new List<Tuple<string, string, int>> { $0 };
            }
        ;

        IdentWithOptAssign<Tuple<string, string, int>>:
            IDENTIFIER TOKENTYPE? AssignedValue?
            {
                $$ = 
                    new Tuple<string, string, int>
                    (
                        $0, 
                        $1.Value, 
                        $2.HasValue 
							? $2.Value 
							: Grammar.UnassignedTokenValue
                    );
            }
        ;
     
		AssignedValue<int> :
			EQUALS INTEGER
			{
				$$ = $1;
			}
		;

        Guards : 
            CONDITIONS LBRACE GuardList RBRACE 
            {
                foreach(GrammarGuardOrAction guard in $2)
                {
                    if(!CheckAndInsertGuardToken(guard))
                        break;
                }
            }
        |	CONDITIONS error RBRACE
            {
                Error("List of guards must be guard function names separated by commas");
            }
        | 
        ; 

        IdentifierList<List<string>> :
            IdentifierList COMMA IDENTIFIER
            {
                $0.Add($2);
                $$ = $0;
            }
        
        |	IDENTIFIER
            {
                $$ = new List<string> { $0 };
            }
        ;

        GuardList<List<GrammarGuardOrAction>> :
            GuardList COMMA GuardInstance
            {
                $0.Add($2);
                $$ = $0;
            }
        
        |	GuardInstance
            {
                $$ = new List<GrammarGuardOrAction> { $0 };
            }
        ;

        GuardInstance<GrammarGuardOrAction> :
            IDENTIFIER TOKENTYPE? CODE?
            {
                $$ = new GrammarGuardOrAction($0, $2.Value, $1.Value);
            }
        ;

        Rules :  
            GRAMMAR LPAREN IDENTIFIER RPAREN LBRACE Rule+ RBRACE 
            { 
                if(!ConstructedGrammar.SetStartingNonterminal($2))
                    Error("No grammar entry symbol found called " + $2); 
            }

        // Support for simple finite state machines

        |	FSM LPAREN IDENTIFIER RPAREN LBRACE State + RBRACE
            {
                string result = ConstructedGrammar.ValidateStates($2);
                if(!string.IsNullOrEmpty(result))
                    Error(result);
            }
        ;
     
		RuleNameAndType<GrammarToken> :
			IDENTIFIER TOKENTYPE? COLON
			{
                $$ = ConstructedGrammar.Terminals
                    .FirstOrDefault(t => t.Text == $0);
                if ($$ != null)
                    Error($$.Text + " already used as a terminal token name");
                else
                {
                    $$ = ConstructedGrammar.Nonterminals
                        .FirstOrDefault(t => t.Text == $0);
                    if ($$ == null)
                    {
                        $$ = new GrammarToken
                            (ConstructedGrammar.Nonterminals.Count + Grammar.MinNonterminalValue + 1,
                            ParserGenerator.TokenType.Nonterminal, $0, $1.Value);
                        ConstructedGrammar.Nonterminals.Add($$);
                    }
                    else if($1.HasValue 
                        && !string.IsNullOrEmpty($$.ValueType) 
                        && $1.Value != $$.ValueType)
                    {
                        Error("Non-terminal " + $$.Text + " has conflicting Value types");
                    }
                    else if(string.IsNullOrEmpty($$.ValueType))
                        $$.ValueType = $1.Value;
				}
				CurrentRuleLHS = $$;
			}
		;

        Rule :
            RuleNameAndType ProductionList Merge? SEMI
            {
                // Capture the merge handler if it exists

                if($2.HasValue)
                {
                    if(ConstructedGrammar.MergeActions.ContainsKey($0.Text))
                    {
                        // Generate error to indicate that there has
                        // already been a merge handler defined for
                        // this non-terminal token
                                        
                        Error("Multiple merge actions defined for non-terminal " + $0.Text);
                    }
                    else
                        ConstructedGrammar.MergeActions.Add($0.Text, $2.Value);
					string errMsg = $2.Value.ValidateCodeArguments(2, "merge on " + $0.Text);
					if(!string.IsNullOrEmpty(errMsg))
						Error(errMsg);
                }
            }
        |	error SEMI
        ;

        ProductionList :
            ProductionList OR Production
        |   Production
        ;

        Production :
            Element * CODE ?
            {
                // The index number for the production is corrected
                // later when it gets inserted into the list of Productions

                GrammarProduction gp = new GrammarProduction
                        (ConstructedGrammar, 0, $0, $1.Value);
				AppendGrammarProduction(CurrentRuleLHS, gp);
				string errMsg = gp.ValidateCodeArguments();
				if(!string.IsNullOrEmpty(errMsg))
					Error(errMsg);
			}
        ; 

        Element<GrammarElement> : 
            IDENTIFIER Guard? Multiplier?
            {
				Multiplicity elementMultiplicity = Multiplicity.ExactlyOne;
				BoolExpr multiplicityGuard = default(BoolExpr);
				
				if($2.HasValue)
				{
					elementMultiplicity = $2.Value.Item1;
					multiplicityGuard = $2.Value.Item2;
				}

                // Deal with the special error identifier

                if($0 == "error" && OutputParserSupportsErrorRecovery)
                    $0 = "ERR";

                // Manage syntax errors for the 'error' keyword

                if($0 == "ERR" 
                    && (!OutputParserSupportsErrorRecovery 
                        || elementMultiplicity != Multiplicity.ExactlyOne 
                        || $1.HasValue))
                    Error("Keyword 'error' cannot be followed by '?', '+', '*', or a guard condition");
                else
                    // Use a method in the LR parser to construct the
                    // additional multiplicity rules, and to replace
                    // the identifier and its multiplicity symbol with
                    // the corresponding element.

                    $$ = ManageMultiplicityProductions
                        ($0, $1.Value, elementMultiplicity, multiplicityGuard);
            }
        ; 
     
        Guard<BoolExpr> : 
            LBRACK CondExpr RBRACK 
            { 
                // The element between the square brackets 
                // is the conditional expression 
     
                $$ = $1; 
            } 
        ; 

        Multiplier<Tuple<Multiplicity, BoolExpr>> :
            MULTIPLICITY Guard?
            {
                $$ = new Tuple<Multiplicity, BoolExpr>($0, $1.Value);
            }
        ;
     
        CondExpr<BoolExpr> : 
            CondExpr OR OrableExpr 
            { 
                $$ = new OrExpr($0, $2); 
            } 
        |	OrableExpr
            {
                $$ = $0;
            }
        ; 
     
        OrableExpr<BoolExpr> : 
            OrableExpr AND AndableExpr
            { 
                $$ = new AndExpr($0, $2); 
            } 
        |	AndableExpr
            {
                $$ = $0;
            }
        ; 
     
        AndableExpr<BoolExpr> : 
            RootExpr 
            { 
                // Pass the root expression back up the rule tree 
     
                $$ = $0; 
            } 
        |   NOT RootExpr 
            { 
                // Prepend the root expression with a NOT 
                // operator node, and return that to the 
                // parent rule 
                 
                $$ = new NotExpr($1); 
            } 
        ; 
     
        RootExpr<BoolExpr> : 
            IDENTIFIER 
            { 
                // Look up the identifier among the list of guards 
     
                GrammarToken gt = ConstructedGrammar.Guards 
                    .FirstOrDefault(t => t.Text == $0); 
     
                if (gt == null) 
                    Error($0 + " is not a valid guard condition"); 
                else 
                    $$ = new LeafExpr(gt.Text, 
                        ConstructedGrammar.BooleanExpressionCache.IndexProvider); 
            } 
        |   LPAREN CondExpr RPAREN 
            { 
                // The second argument should already be the 
                // correct BoolExpr to return to the parent rule 
     
                $$ = $1; 
            } 
        ; 

        State :
            IDENTIFIER COLON OnEntry ? OnExit ? TransitionList SEMI
            {
                // First process the state identifier
                // and store it in the list of non-terminals

                GrammarToken gt = ConstructedGrammar.Nonterminals
                    .FirstOrDefault(t => t.Text == $0);
                if (gt == null)
                {
                    gt = new GrammarToken
                        (ConstructedGrammar.Nonterminals.Count + Grammar.MinNonterminalValue + 1,
                        ParserGenerator.TokenType.Nonterminal, $0);
                    ConstructedGrammar.Nonterminals.Add(gt);
                }

                // Populate the state with the transition list. If there are
                // multiple state sections with the same state name, they are
                // treated as if they were contiguous.

                State s = ConstructedGrammar.MachineStates.FirstOrDefault(t => t.StateName.Text == $0);
                if(s == null)
                {
	                s = new State(gt, $2.Value, $3.Value);
                    ConstructedGrammar.MachineStates.Add(s);
                }
                s.Transitions.AddRange($4);
            }
        |	error SEMI
        ;

        OnEntry<GrammarGuardOrAction> :
            ONENTRY TransitionAction
            {
                $$ = $1;
            }
        ;

        OnExit<GrammarGuardOrAction> :
            ONEXIT TransitionAction
            {
                $$ = $1;
            }
        ;

        TransitionList<List<StateTransition>> :
            TransitionList OR Transition
            {
                $0.Add($2);
                $$ = $0;
            }

        |	Transition
            {
                $$ = new List<StateTransition> { $0 };
            }
        ;

        Transition<StateTransition> :
            Element IDENTIFIER TransitionAction?
            {
                // Find the identifier used as the target state
                // and store it in the list of non-terminals

                GrammarToken gt = ConstructedGrammar.Nonterminals
                    .FirstOrDefault(t => t.Text == $1);
                if (gt == null)
                {
                    gt = new GrammarToken
                        (ConstructedGrammar.Nonterminals.Count + Grammar.MinNonterminalValue + 1,
                        ParserGenerator.TokenType.Nonterminal, $1);
                    ConstructedGrammar.Nonterminals.Add(gt);
                }

                // Now create a state transition

                $$ = new StateTransition
                {
                    TransitionEventAndGuard = $0,
                    TargetState = gt,
                    Code = $2.Value
                };
            }

        |	Element TransitionAction?
            {
                $$ = new StateTransition
                {
                    TransitionEventAndGuard = $0,
                    TargetState = null,
                    Code = $1.Value
                };
            }
        ;

        TransitionAction<GrammarGuardOrAction> :
            CODE
            {
                $$ = new GrammarGuardOrAction(string.Empty, $0);
            }
        ;

        Merge<GrammarGuardOrAction> :
            MERGE CODE
            {
                $$ = new GrammarGuardOrAction(string.Empty, $1);
            }
        ;	
    }