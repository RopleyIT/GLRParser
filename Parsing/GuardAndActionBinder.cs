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
using System.Reflection;

namespace Parsing
{
    /// <summary>
    /// Type of methods that are used to compute guard conditions
    /// on behalf of the parser.
    /// </summary>
    /// <param name="target">The parser instance</param>
    /// <param name="argument">An optional object argument</param>
    /// <returns>True if the guard condition is satisfied</returns>

    public delegate bool GuardProxy(object target, object argument);

    /// <summary>
    /// Type of methods that are used to compute guard conditions
    /// on behalf of the parser.
    /// </summary>
    /// <param name="target">The parser instance</param>
    /// <returns>True if the guard condition is satisfied</returns>

    public delegate bool FSMGuardProxy(object target);

    /// <summary>
    /// Type of methods that are used to execute reduction actions
    /// on behalf of the parser.
    /// </summary>
    /// <param name="target">The parser instance</param>
    /// <param name="arguments">The list of values of tokens from
    /// the rule elements that are being reduced. Index zero
    /// is used for the returned value that will replace
    /// the value in the single reduced non-terminal token.</param>

    public delegate void ActionProxy(object target, object[] arguments);
    /// <summary>
    /// Type of methods that are used to execute
    /// transition actions within the state machine.
    /// </summary>
    /// <param name="target">The FSM instance</param>
    /// <param name="argument">An optional object argument</param>

    public delegate void FSMActionProxy(object target);

    /// <summary>
    /// Type of methods used to choose between two different
    /// reductions that are valid at some point in a generalised
    /// parse using the GLR parser.
    /// </summary>
    /// <param name="left">The earlier candidate reduction</param>
    /// <param name="right">The new colliding candidate reduction</param>
    /// <returns>The reduction selected as the correct reduction
    /// to apply at this point in the parse. The other reduction will be
    /// removed from the set of valid parses. If null is returned,
    /// both candidates are kept alive as the parse proceeds. If
    /// not eliminated in some parent reduction, this will mean
    /// there will be multiple valid parses at the end of grammar
    /// parsing.</returns>

    public delegate void MergeActionProxy
        (object target, NonterminalToken[] args);

    /// <summary>
    /// Suite of methods used to look up a method in a class, and
    /// create an open instance delegate to the method. An open instance
    /// delegate is one in which the Target object has not been set,
    /// unlike normal delegates. The target object is instead
    /// specified when the member method is called. Hence this
    /// kind of delegate behaves like a pointer to an instance
    /// method inside a class, for which the instance is supplied
    /// at the point the method is called.
    /// </summary>

    public static class GuardAndActionBinder
    {
        /// <summary>
        /// Given the reflection data for a method within a class that has
        /// the correct signature to be a guard function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the parser class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>
        /// <returns>An open instance delegate to the named proxy</returns>

        public static GuardProxy CreateGuardProxy(Type containerClassType, string methodName) => CreateProxy<GuardProxy>(containerClassType, methodName, typeof(object), null, typeof(bool));

        /// <summary>
        /// Given the reflection data for a method within a class that has
        /// the correct signature to be a guard function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the parser class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>
        /// <returns>An open instance delegate to the named proxy</returns>

        public static FSMGuardProxy CreateFSMGuardProxy(Type containerClassType, string methodName) => CreateProxy<FSMGuardProxy>(containerClassType, methodName, null, null, typeof(bool));

        /// <summary>
        /// Given the reflection data for a method within a class that has
        /// the correct signature to be an action function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the parser class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>
        /// <returns>An open instance delegate to the named proxy</returns>

        public static ActionProxy CreateParserActionProxy(Type containerClassType, string methodName) => CreateProxy<ActionProxy>(containerClassType, methodName, typeof(object[]), null, null);

        /// <summary>
        /// Given the reflection data for a method within a class that has
        /// the correct signature to be an FSM action function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the FSM class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>
        /// <returns>An open instance delegate to the named proxy</returns>

        public static FSMActionProxy CreateFSMActionProxy(Type containerClassType, string methodName) => CreateProxy<FSMActionProxy>(containerClassType, methodName, null, null, null);

        /// <summary>
        /// Given the reflection data for a method within a parser class that has
        /// the correct signature to be merge action function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the parser class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>

        public static MergeActionProxy CreateMergeActionProxy(Type containerClassType, string methodName) => CreateProxy<MergeActionProxy>
            (
                containerClassType,         // The type of the parser class containing the merge method
                methodName,                 // The name of the merge action method
                typeof(NonterminalToken[]), // First NonterminalToken argument
                null,                       // Second NonterminalToken argument
                null                        // Void return type
            );

        /// <summary>
        /// Thin wrapper around the Type.GetMethod() function
        /// </summary>
        /// <param name="parserClassType">The parser class from which we wish to
        /// look up a guard or action method's MethodInfo</param>
        /// <param name="methodName">The name of the method we seek</param>
        /// <returns>The method info for the named method. Failure
        /// causes an exception to be thrown.</returns>

        private static MethodInfo GetParserMethodInfo(Type parserClassType, string methodName)
        {
            // Reflect the method data for the named method from
            // this class, handling any errors along the way

            MethodInfo methInfo = parserClassType.GetMethod
                (methodName, BindingFlags.Instance 
                    | BindingFlags.Static 
                    | BindingFlags.Public 
                    | BindingFlags.FlattenHierarchy);
            if (methInfo == null)
                throw new ArgumentException
                    ("Method " + methodName + " not found in class " + parserClassType.Name);
            else
                return methInfo;
        }

        /// <summary>
        /// Given the reflection data for a method within a class that has
        /// the correct signature to be a guard or action function, create an open
        /// instance delegate proxy that can be used to call that method
        /// on a late-bound instance without the penalty of using 
        /// reflection on each successive method call.
        /// </summary>
        /// <param name="containerClassType">The type object for the class
        /// in which the method is found</param>
        /// <param name="methodName">The name of the method in the class that we wish
        /// the delegate to refer to</param>
        /// <param name="firstArgType">The data type of the first permitted argument to the function.
        /// Set to null if the proxy will not receive an argument.</param>
        /// <param name="secondArgType">The data type of the second permitted argument to the function.
        /// Set to null if the proxy will not receive an argument.</param>
        /// <param name="retType">The return type for the function, or null if void</param>
        /// <returns>An open instance delegate to the named proxy</returns>

        private static TProxy CreateProxy<TProxy>
            (Type containerClassType, string methodName, Type firstArgType, Type secondArgType, Type retType)
        {
            if(firstArgType == null && secondArgType != null)
                throw new ArgumentException("First argType cannot be null if second is not");

            MethodInfo method = GetParserMethodInfo(containerClassType, methodName);

            // Build the expression for 'this' if an instance method. The
            // class of the instance variable must match the class containing
            // the method to be invoked, and the single object parameter is
            // passed. This is analogous to writing source code:
            // ((ParserClassType)parserInstance).Guard(argument);
            // If the method is a static method, we set up a dummy
            // instance parameter for the lambda expression, but set the
            // this reference for the static method to null.

            ParameterExpression instanceParameter 
                = Expression.Parameter(typeof(object), "target");
            UnaryExpression thisParameter = null;
            if (!method.IsStatic)
            {
                thisParameter = Expression.Convert
                (
                    instanceParameter,
                    method.DeclaringType
                );
            }

            // Build parameter list for the new proxy method

            List<ParameterExpression> parms = [instanceParameter];
            if (firstArgType != null)
                parms.Add(Expression.Parameter(firstArgType, "argument"));
            if (secondArgType != null)
                parms.Add(Expression.Parameter(secondArgType, "argument2"));

            // Create a call to the method for which we are a proxy, 

            Expression call = Expression.Call(thisParameter, method, parms.Skip(1));

            // If the proxy is to a function that returns a value,
            // we need to cast the return data to the expected type.

            if (retType != null)
                call = Expression.Convert
                (
                    call,
                    retType
                );

            // Now instantiate the delegate to the proxy method, ensuring that
            // the return type from the named method is cast to the expected
            // boolean type

            Expression<TProxy> lambda = Expression.Lambda<TProxy>(call, parms);

            // Convert the expression tree into IL code so that once it
            // has been executed once, it will be executed rapidly

            return lambda.Compile();
        }
    }
}