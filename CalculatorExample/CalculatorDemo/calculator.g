options
{
    using Parsing,
    namespace CalculatorDemo,
	parserclass Calculator
}

tokens
{
    LPAREN,
    RPAREN,
    PLUS,
    MINUS,
    TIMES,
    DIVIDE,
    POWER,
    NUMBER <string>,
    PERIOD,
    EXPONENT
}

grammar(result)
{
	result:
		addExpression
		{
			// Result is a property of the
			// Calculator parser class,
			// used to extract the result
			// of the calculation after the
			// parser has run.

			Result = $0.ToString();
		}
	;

	leafExpression <double> :
		number
		{
			$$ = $0;
		}

	|	LPAREN addExpression RPAREN
		{
			$$ = $1;
		}

	|	MINUS leafExpression
		{
			$$ = -$1;
		}
	;

	powExpression <double> :
	    powExpression POWER leafExpression
		{
			$$ = Math.Pow($0, $2);
		}

	|	leafExpression
		{
			$$ = $0;
		}
	;

    mulExpression <double> :
        mulExpression TIMES powExpression
        {
            $$ = $0 * $2;
        }

    |   mulExpression DIVIDE powExpression
        {
			$$ = $0 / $2;
        }

	|   powExpression
		{
			$$ = $0;
		}
	;

	addExpression <double> :
        addExpression PLUS mulExpression
        {
            $$ = $0 + $2;
        }

    |   addExpression MINUS mulExpression
        {
            $$ = $0 - $2;
        }

	|	mulExpression
		{
			$$ = $0;
		}
    ;

    number <double> :
        NUMBER exponent
        {
            $$ = double.Parse($0) * Math.Pow(10.0, $1);
		}

    |   NUMBER mantissa exponent
        {
            $$ = (double.Parse($0) + $1) 
				* Math.Pow(10.0, $2);
        }

	|	mantissa exponent
		{
            $$ = $0 * Math.Pow(10.0, $1);
        }
    ;

	mantissa <double> : 
        PERIOD NUMBER
		{
			int digitCount = $1.Length;
            double mantissa = double.Parse($1);
            while(--digitCount >= 0)
                mantissa *= 0.1;
			$$ = mantissa;
		}
	;

    exponent <double> :
        EXPONENT NUMBER
        {
            $$ = double.Parse($1);
        }

    |   EXPONENT MINUS NUMBER
        {
            $$ = - double.Parse($2);
        }

    |
        {
            $$ = 0.0;
        }
    ;
}