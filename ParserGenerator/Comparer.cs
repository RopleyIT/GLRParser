// This source code is based on code written for Ropley Information
// Technology Ltd. (RIT), is and offered for public use without warranty.
// You are entitled to edit or extend this code for your own purposes,
// but use of any unmodified parts of this code does not grant
// the user exclusive rights or ownership of that unmodified code. 
// While every effort has been made to deliver quality software, 
// there is no guarantee that this product offered for public use
// is without defects. The software is provided “as is," and you 
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

namespace ParserGenerator
{
    /// <summary>
    /// Simplified comparer class that allows a
    /// Func to be used as an IEqualityComparer
    /// </summary>
    /// <typeparam name="T">The type of the objects
    /// being compared for equality</typeparam>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="cmpDelegate">The delegate or
    /// lambda expression being used as the comparer</param>

    public class Comparer<T>(Func<T, T, bool> cmpDelegate) : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> comparer = cmpDelegate
            ?? throw new ArgumentNullException
                ("cmpDelegate", "Need a comparer delegate");

        /// <summary>
        /// Implementation of the equality operation. Merely
        /// uses the delegate passed in the constructor.
        /// </summary>
        /// <param name="x">Left item to be compared</param>
        /// <param name="y">Right item to be compared</param>
        /// <returns>True if deemed equal</returns>

        public bool Equals(T x, T y)
        {
            return comparer(x, y);
        }

        /// <summary>
        /// Hard-wired algorithm for rendering the hash
        /// code for each object being compared
        /// </summary>
        /// <param name="obj">The item whose hash
        /// value is needed</param>
        /// <returns>The item's hash value</returns>

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
