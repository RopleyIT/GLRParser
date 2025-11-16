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

namespace Parsing;

/// <summary>
/// Common interface exposed by all guard functions
/// referenced within the parser table. This is used
/// at runtime, unlike the BoolExpr family of classes
/// that are intended for managing expressions while
/// building the parser.
/// </summary>

public interface IGuardEvaluator
{
    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM object within whose
    /// context the guard function should be evaluated</param>
    /// <param name="arg">The argument to the guard function</param>
    /// <returns>The returned truth value from the guard</returns>

    bool Value(object context, object arg);

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM object within whose
    /// context the guard function should be evaluated</param>
    /// <returns>The returned truth value from the guard</returns>

    bool Value(object context);

    /// <summary>
    /// Perform late binding of the guard evaluator so that
    /// the parser tables point at the correct guard
    /// evaluation methods within the derived parser class
    /// </summary>
    /// <param name="parserClass">The type of the derived
    /// parser class being used by this grammar</param>
    /// <param name="hasParam">True if the guard function
    /// will be passed an argument to help it decide
    /// on the result, false to receive no parameter.</param>

    void Bind(Type parserClass, bool hasParam);

    /// <summary>
    /// Return the guard as a string
    /// </summary>
    /// <returns>The guard expression as a string</returns>

    string AsString();
}

/// <summary>
/// Runtime class that evaluates a guard function
/// within the derived parser class
/// </summary>

public class LeafEvaluator : IGuardEvaluator
{
    /// <summary>
    /// The name of the guard function within the parser
    /// class. This is bound at run-time using a compiled
    /// Expression tree so that performance is maintained.
    /// </summary>

    public string MethodName
    {
        get;
        private set;
    }

    /// <summary>
    /// The delegate that points at the guard function
    /// </summary>

    public GuardProxy GuardFunction
    {
        get;
        set;
    }

    /// <summary>
    /// The delegate that points at the parameterless
    /// guard function as used by finite state machines
    /// </summary>

    public FSMGuardProxy FSMGuardFunction
    {
        get;
        set;
    }

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <param name="arg">The argument to the guard function</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context, object arg) => GuardFunction(context, arg);

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context) => FSMGuardFunction(context);

    /// <summary>
    /// Return the guard as a string
    /// </summary>
    /// <returns>The guard expression as a string</returns>

    public string AsString() => MethodName;

    /// <summary>
    /// Initialise the leaf guard function as an open instance
    /// delegate, using reflection to find the method from the class
    /// </summary>
    /// <param name="methodName">The name of the guard method</param>

    public LeafEvaluator(string methodName)
    {
        GuardFunction = null;
        MethodName = methodName;
    }

    /// <summary>
    /// Default constructor, used when the GuardFunction is
    /// filled in subsequently using a property assignment
    /// </summary>

    public LeafEvaluator() => GuardFunction = null;

    /// <summary>
    /// Perform late binding of the guard evaluator so that
    /// the parser tables point at the correct guard
    /// evaluation methods within the derived parser class
    /// </summary>
    /// <param name="parserClass">The type of the derived
    /// parser class being used by this grammar</param>
    /// <param name="hasParam">True if the guard function
    /// will be passed an argument to help it decide
    /// on the result, false to receive no parameter.</param>

    public void Bind(Type parserClass, bool hasParam)
    {
        if (hasParam)
            GuardFunction = GuardAndActionBinder
                .CreateGuardProxy(parserClass, MethodName);
        else
            FSMGuardFunction = GuardAndActionBinder
                .CreateFSMGuardProxy(parserClass, MethodName);
    }
}

/// <summary>
/// Evaluates the logical complement of the evaluator
/// assigned to its Arg property, at runtime
/// </summary>

public class NotEvaluator : IGuardEvaluator
{
    public IGuardEvaluator Arg
    {
        get;
        set;
    }

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <param name="arg">The argument to the guard function</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context, object arg) => !Arg.Value(context, arg);

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context) => !Arg.Value(context);

    /// <summary>
    /// Perform late binding of the guard evaluator so that
    /// the parser tables point at the correct guard
    /// evaluation methods within the derived parser class
    /// </summary>
    /// <param name="parserClass">The type of the derived
    /// parser class being used by this grammar</param>
    /// <param name="hasParam">True if the guard function
    /// will be passed an argument to help it decide
    /// on the result, false to receive no parameter.</param>

    public void Bind(Type parserClass, bool hasParam) =>
        // Since wwe don't have a method name to bind,
        // we just delegate the operation down the
        // expression tree.

        Arg.Bind(parserClass, hasParam);

    /// <summary>
    /// Return the guard as a string
    /// </summary>
    /// <returns>The guard expression as a string</returns>

    public string AsString() => "!(" + Arg.AsString() + ")";
}

/// <summary>
/// Evaluates the boolean AND operation on its
/// Left and Right evaluator arguments. Note that
/// this implements C# style sequence point behaviour
/// meaning that if the left operand is false, the
/// right operand isn't even evaluated. This class
/// is intended for use at runtime within a parser.
/// </summary>

public class AndEvaluator : IGuardEvaluator
{
    public IGuardEvaluator Left
    {
        get;
        set;
    }

    public IGuardEvaluator Right
    {
        get;
        set;
    }

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <param name="arg">The argument to the guard function</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context, object arg) => Left.Value(context, arg) && Right.Value(context, arg);

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context) => Left.Value(context) && Right.Value(context);

    /// <summary>
    /// Perform late binding of the guard evaluator so that
    /// the parser tables point at the correct guard
    /// evaluation methods within the derived parser class
    /// </summary>
    /// <param name="parserClass">The type of the derived
    /// parser class being used by this grammar</param>
    /// <param name="hasParam">True if the guard function
    /// will be passed an argument to help it decide
    /// on the result, false to receive no parameter.</param>

    public void Bind(Type parserClass, bool hasParam)
    {
        // Since wwe don't have a method name to bind,
        // we just delegate the operation down the
        // expression tree.

        Left.Bind(parserClass, hasParam);
        Right.Bind(parserClass, hasParam);
    }

    /// <summary>
    /// Return the guard as a string
    /// </summary>
    /// <returns>The guard expression as a string</returns>

    public string AsString() => "(" + Left.AsString() + " & " + Right.AsString() + ")";
}

/// <summary>
/// Evaluates the boolean OR operation on its
/// Left and Right evaluator arguments. Note that
/// this implements C# style sequence point behaviour
/// meaning that if the left operand is true, the
/// right operand isn't even evaluated. This class
/// is intended for use at runtime in a parser.
/// </summary>

public class OrEvaluator : IGuardEvaluator
{
    public IGuardEvaluator Left
    {
        get;
        set;
    }

    public IGuardEvaluator Right
    {
        get;
        set;
    }

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <param name="arg">The argument to the guard function</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context, object arg) => Left.Value(context, arg) || Right.Value(context, arg);

    /// <summary>
    ///  Evaluate the guard function
    /// </summary>
    /// <param name="context">The parser or FSM within whose context
    /// the guard function should be evaluated</param>
    /// <returns>The returned truth value from the guard</returns>

    public bool Value(object context) => Left.Value(context) || Right.Value(context);

    /// <summary>
    /// Perform late binding of the guard evaluator so that
    /// the parser tables point at the correct guard
    /// evaluation methods within the derived parser class
    /// </summary>
    /// <param name="parserClass">The type of the derived
    /// parser class being used by this grammar</param>
    /// <param name="hasParam">True if the guard function
    /// will be passed an argument to help it decide
    /// on the result, false to receive no parameter.</param>

    public void Bind(Type parserClass, bool hasParam)
    {
        // Since we don't have a method name to bind,
        // we just delegate the operation down the
        // expression tree.

        Left.Bind(parserClass, hasParam);
        Right.Bind(parserClass, hasParam);
    }

    /// <summary>
    /// Return the guard as a string
    /// </summary>
    /// <returns>The guard expression as a string</returns>

    public string AsString() => "(" + Left.AsString() + " | " + Right.AsString() + ")";
}
