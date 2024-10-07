using ParserGenerator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Parsing
{
    /// <summary>
    /// Used to place suitable type casts on
    /// the $0 .. $N parameters to action code
    /// </summary>

    public class DollarParameterReplacer
    {
        private readonly GrammarProduction production;
        private readonly StringBuilder declarations;
        private readonly List<int> argIndexes;
        private readonly bool forceWeakTyping;

        /// <summary>
        /// Strongly typed local variables captured
        /// during regular expression matching
        /// </summary>

        public string Declarations => declarations.ToString();

        /// <summary>
        /// Constructor. Takes the grammar value types for
        /// each terminal and non terminal in the production,
        /// and uses them to form casts on the $N parameters.
        /// </summary>
        /// <param name="gp">The grammar production from which
        /// to fetch the terminal and non-terminal token
        /// value types for each element.</param>
        /// <param name="forceWeak">Set to true
        /// to prevent the creation of strongly-typed
        /// local variables. This is used for methods that
        /// might be shared between rules, such as the
        /// multiplicity methods, for example.</param>

        public DollarParameterReplacer(GrammarProduction gp, bool forceWeak)
        {
            production = gp ?? throw new ArgumentException
                    ("Null grammar production passed into DollarParameterReplacer");
            declarations = new StringBuilder();
            argIndexes = [0];
            forceWeakTyping = forceWeak;

            // Set up the declaration for the return slot args[0]

            if (!forceWeakTyping && !string.IsNullOrEmpty(gp.LHS.ValueType))
            {
                declarations.Append
                    ($"            {gp.LHS.ValueType} arg0 = default({gp.LHS.ValueType});\r\n");
            }
        }

        /// <summary>
        /// For the substitutable parameter $N return
        /// the string "((argType)args[N+1])"
        /// </summary>
        /// <param name="m">The regular expression match</param>
        /// <returns>The replacement string to be substituted</returns>

        public string ReplacementDollarParameter(Match m)
        {
            if (m == null)
                throw new ArgumentException
                    ("ReplacementDollarParameter needs a non-null Match parameter");
            if (int.TryParse(m.Value.AsSpan(1), out int dollarParamValue)
                && dollarParamValue >= 0
                && dollarParamValue < production.RHS.Count)
            {
                GrammarToken tok = production.RHS[dollarParamValue].Token;
                Multiplicity multiplicity = tok.Multiplicity;
                string baseType = tok.BaseType;
                string valueType = tok.ValueType;

                // Deal with untyped tokens by simple argument array indexing

                if (forceWeakTyping || string.IsNullOrEmpty(valueType))
                    return $"(args[{dollarParamValue + 1}])";

                // This is a typed token. We shall extract a declaration
                // for the top of the action function that casts to the correct
                // type, then just substitute the variable name.

                if (!argIndexes.Contains(dollarParamValue + 1))
                {
                    switch (multiplicity)
                    {
                        default: // Correct for Multiplicity.ExactlyOne
                            declarations.Append
                                ($"            {valueType} arg{dollarParamValue + 1} = " +
                                    $"({valueType})args[{dollarParamValue + 1}];\r\n");
                            break;

                        case Multiplicity.ZeroOrOne:
                            string strBaseType = string.IsNullOrEmpty(baseType) ?
                                    "object" : baseType;
                            declarations.Append
                            (
                                $"            {valueType} arg{dollarParamValue + 1} = " +
                                $"AsOptional<{strBaseType}>(args[{dollarParamValue + 1}]);\r\n"
                            );
                            break;

                        case Multiplicity.OneToMany:
                        case Multiplicity.ZeroToMany:
                            strBaseType = string.IsNullOrEmpty(baseType) ?
                                    "object" : baseType;
                            declarations.Append
                            (
                                $"            {valueType} arg{dollarParamValue + 1} = " +
                                $"AsList<{strBaseType}>(args[{dollarParamValue + 1}]);\r\n"
                            );
                            break;
                    }
                }
                argIndexes.Add(dollarParamValue + 1);
                return $"arg{dollarParamValue + 1}";
            }
            else
                return m.Value; // Don't substitute if out of range
        }
    }
}
