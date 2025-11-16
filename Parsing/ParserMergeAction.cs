using System;

namespace Parsing;

/// <summary>
/// A run-time representation of a merge handler
/// as used in GLR parsing
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="methodName">The name of the method
/// within the parser class that will be bound to.</param>

public class ParserMergeAction(string methodName)
{

    /// <summary>
    /// The name of the merge function within the parser
    /// class. This is bound at run-time using a compiled
    /// Expression tree so that performance is maintained.
    /// </summary>

    public string MethodName
    {
        get;
        private set;
    } = methodName;

    /// <summary>
    /// The delegate to the actual method to be called
    /// </summary>

    public MergeActionProxy Function
    {
        get;
        private set;
    } = null;

    /// <summary>
    /// Implement run-time binding of the inline merge functions,
    /// converting the method name and parser class type into an
    /// open instance delegate that calls the action method in
    /// the derived parser class.
    /// </summary>
    /// <param name="parserClass">The type of derived
    /// parser class</param>

    public void Bind(Type parserClass) =>
        // Attach a new action proxy for this inline action

        Function = GuardAndActionBinder
            .CreateMergeActionProxy(parserClass, MethodName);
}
