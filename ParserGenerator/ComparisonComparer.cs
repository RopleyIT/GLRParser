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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ParserGenerator
{
    /// <summary>
    /// Wrapper class hosting a Comparison(Of T) inside
    /// an IComparer(Of T), since the IEnumerable(Of T)
    /// interface's OrderBy extension method expects
    /// only an IComparer(Of T) as its argument.
    /// </summary>
    /// <typeparam name="T">The type of the items 
    /// being ordered</typeparam>
    /// <remarks>
    /// Constructor. Wraps a Comparison(Of T)
    /// so that it can be exposed as an
    /// IComparer(Of T)
    /// </remarks>
    /// <param name="cmp">The comparison object
    /// to be wrapped</param>

    internal class ComparisonComparer<T>(Comparison<T> cmp) : IComparer<T>, IComparer
    {
        private readonly Comparison<T> comparison = cmp;

        /// <summary>
        /// Implementation of the IComparer(Of T)
        /// </summary>
        /// <param name="x">Left item to be compared</param>
        /// <param name="y">Right item to be compared</param>
        /// <returns>negative if the items are already
        /// in the correct order, zero of the items are
        /// equal valued and could be placed either way
        /// round, positive if the items should be
        /// reversed to be in order.</returns>

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }

        /// <summary>
        /// Implementation of the IComparer
        /// weakly typed interface
        /// </summary>
        /// <param name="x">Left item to be compared</param>
        /// <param name="y">Right item to be compared</param>
        /// <returns>negative if the items are already
        /// in the correct order, zero of the items are
        /// equal valued and could be placed either way
        /// round, positive if the items should be
        /// reversed to be in order.</returns>

        public int Compare(object x, object y)
        {
            return comparison((T)x, (T)y);
        }
    }

    /// <summary>
    /// Extension methods to support deferred ordering of elements
    /// </summary>

    public static class OrderByExtensions
    {
        /// <summary>
        /// Extends the OrderBy method overloads of the IEnumerable(Of T)
        /// interface so that it can use a comparison object as well as the
        /// already implemented IComparer(Of T)
        /// </summary>
        /// <typeparam name="T">The type of the items to be sorted</typeparam>
        /// <param name="source">The enumerable to be sorted</param>
        /// <param name="cmp">The comparison delegate to be used in sorting</param>
        /// <returns>The enumerable with the sorting algorithm applied</returns>

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> cmp)
        {
            return source.OrderBy(t => t, new ComparisonComparer<T>(cmp));
        }
    }
}
