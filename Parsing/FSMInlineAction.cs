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

namespace Parsing
{
    /// <summary>
    /// Represents one function to be called in a list of functions
    /// attached to a state transition.
    /// </summary>

    public class FSMInlineAction
    {
        /// <summary>
        /// Next action function in the linked list of functions.
        /// Null is used to terminate the list
        /// </summary>

        public FSMInlineAction Next
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the action function within the FSM
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

        public FSMActionProxy Function
        {
            get;
            private set;
        }

        /// <summary>
        /// Render object in a human readable form
        /// </summary>
        /// <returns>A readable version of the inline action</returns>

        public override string ToString() => $"{MethodName}()";

        /// <summary>
        /// Construct an inline action list of function descriptions
        /// </summary>
        /// <param name="methodNames">The list of names of action
        /// methods within the late-bound class</param>

        public FSMInlineAction(IEnumerable<string> methodNames)
        {
            if (methodNames == null || methodNames.Count() <= 0)
                throw new ArgumentException("A non-empty list of method names must be provided");

            if (methodNames.Count() > 1)
                Next = new FSMInlineAction(methodNames.Skip(1));
            else
                Next = null;
            Function = null;
            MethodName = methodNames.First();
        }

        /// <summary>
        /// Construct an inline action list of function descriptions,
        /// allowing multiple string arguments to constructor
        /// </summary>
        /// <param name="methodNames">Array or list of names of action
        /// functions in FSM class that will become an action list.</param>

        public FSMInlineAction(params string[] methodNames)
            : this(methodNames.AsEnumerable())
        {
        }

        /// <summary>
        /// Implement run-time binding of the inline action functions,
        /// converting the method name and FSM class type into an
        /// open instance delegate that calls the action method in
        /// the derived FSM class.
        /// </summary>
        /// <param name="fsmDerivedClass">The type of derived
        /// FSM class</param>

        public void Bind(Type fsmDerivedClass)
        {
            // Attach a new action proxy for this inline action

            Function = GuardAndActionBinder
                .CreateFSMActionProxy(fsmDerivedClass, MethodName);

            // Walk the list of inline actions, binding each

            if (Next != null)
                Next.Bind(fsmDerivedClass);
        }
    }
}
