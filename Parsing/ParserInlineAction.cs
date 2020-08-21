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

namespace Parsing
{
    /// <summary>
    /// Represents one function to be called in a list of functions
    /// attached to a rule reduction. Also identifies which tokens
    /// that are arguments to the production should be passed
    /// </summary>

    public class ParserInlineAction
    {
        /// <summary>
        /// The name of the action function within the parser
        /// class. This is bound at run-time using a compiled
        /// Expression tree so that performance is maintained.
        /// </summary>

        public string MethodName
        {
            get;
            private set;
        }

        /// <summary>
        /// The delegate to the actual method to be called
        /// </summary>

        public ActionProxy Function
        {
            get;
            private set;
        }

        /// <summary>
        /// Render object in a human readable form
        /// </summary>
        /// <returns>A readable version of the inline action</returns>

        public override string ToString() => MethodName + "(object[] args)";

        /// <summary>
        /// Constructor for a single inline action
        /// </summary>
        /// <param name="methodName">The name of the action method
        /// within a class that will later be defined when Bind
        /// is invoked</param>

        public ParserInlineAction(string method)
        {
            Function = null;
            MethodName = method;
        }

        /// <summary>
        /// Implement run-time binding of the inline action functions,
        /// converting the method name and parser class type into an
        /// open instance delegate that calls the action method in
        /// the derived parser class.
        /// </summary>
        /// <param name="parserClass">The type of derived
        /// parser class</param>

        public void Bind(Type parserClass) =>
            // Attach a new action proxy for this inline action

            Function = GuardAndActionBinder
                .CreateParserActionProxy(parserClass, MethodName);
    }
}
