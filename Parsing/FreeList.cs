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


namespace Parsing
{
    /// <summary>
    /// Nodes that can be managed by the FreeList
    /// class must support the following mechanism
    /// for taking them from or putting them onto
    /// a linked list free list. This is a way of
    /// reusing the object references that would
    /// otherwise be used in the stack data
    /// structure as free list links.
    /// </summary>

    public interface IFreeable<T>
    {
        /// <summary>
        /// The reference to a next node in a list
        /// </summary>
        /// <returns>Reference to the new
        /// head of the freelist</returns>

        T NextFree
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Implement a free list for holding second hand object
    /// references for objects of type T. This is useful
    /// where these objects are frequently allocated or
    /// deallocated, to avoid the overhead of garbage
    /// collection for those objects.
    /// </summary>
    /// <typeparam name="T">The type of object to create
    /// a free list for. Note that the objects to
    /// be managed must be reference types, and
    /// must support a default constructor. They must
    /// also expose the IFreeable interface so that
    /// they can be placed into a linked list.</typeparam>

    public class FreeList<T> where T : class, IFreeable<T>, new()
    {
        /// <summary>
        /// The default amount by which the free list grows
        /// each time the list is exhausted.
        /// </summary>

        private const int DefaultGrowth = 32;

        // Internal fields for the free list manager

        private T freeList;
        private readonly int growth;
        private readonly object mutex;

        /// <summary>
        /// Constructor. Sets an initial expected capacity
        /// for the free list, and arranges that if that
        /// capacity is exceeded, the storage grows linearly
        /// in multiples of that capacity.
        /// </summary>
        /// <param name="growsBy">The
        /// incremental capacity of the free list</param>

        public FreeList(int growsBy)
        {
            // Create the multithreading lock

            mutex = new object();

            // Tune the granularity by which
            // the free list is repopulated

            if (growsBy <= 0)
                growth = DefaultGrowth;
            else
                growth = growsBy;

            // Create an initial allocation of
            // free objects ready for use.

            lock (mutex)
            {
                freeList = null;
                PopulateFreeList();
            }
        }

        /// <summary>
        /// Default constructor. Sets the amount the
        /// freelist grows by to the default value.
        /// </summary>

        public FreeList()
            : this(DefaultGrowth)
        {
        }

        /// <summary>
        /// Put ready-constructed objects into the stack
        /// up to the specified initial capacity
        /// </summary>

        private void PopulateFreeList()
        {
            for (int i = 0; i < growth; i++)
            {
                T t = new T
                {
                    NextFree = freeList
                };
                freeList = t;
            }
        }

        /// <summary>
        /// Obtain an object from the free list
        /// to use in the application
        /// </summary>
        /// <returns>An object to use within the
        /// application</returns>

        public T Alloc()
        {
            lock (mutex)
            {
                if (freeList == null)
                    PopulateFreeList();
                T head = freeList;
                freeList = freeList.NextFree;
                return head;
            }
        }

        /// <summary>
        /// Place an object that is no longer
        /// needed back into the free list
        /// </summary>
        /// <param name="t">The object that is no
        /// longer needed</param>

        public void Free(T t)
        {
            lock (mutex)
            {
                t.NextFree = freeList;
                freeList = t;
            }
        }

        /// <summary>
        /// If the stack capacity becomes ridiculously
        /// high, this will ensure that any spare
        /// capacity is discarded, along with the
        /// objects that are referenced by that
        /// spare capacity.
        /// </summary>

        public void Truncate()
        {
            lock (mutex)
            {
                freeList = null;
            }
        }
    }
}
