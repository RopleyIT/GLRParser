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

using System.Collections.Generic;

namespace Parsing;

/// <summary>
/// General purpose two-way dictionary, in which
/// the lookups of key from value are also efficient.
/// </summary>
/// <typeparam name="TK">Key type</typeparam>
/// <typeparam name="TV">value type associated with key type</typeparam>

public class TwoWayMap<TK, TV>
{
    private readonly Dictionary<TK, TV> forward;
    private readonly Dictionary<TV, TK> reverse;

    /// <summary>
    /// Default constructor
    /// </summary>

    public TwoWayMap()
    {
        forward = [];
        reverse = [];
    }

    /// <summary>
    /// Non-exception lookup of value given a key
    /// </summary>
    /// <param name="k">Key to search on</param>
    /// <param name="v">here to put the returned value</param>
    /// <returns>True if key found</returns>

    public bool TryGetByKey(TK k, out TV v) => forward.TryGetValue(k, out v);

    /// <summary>
    /// Non-exception lookup of key given a value
    /// </summary>
    /// <param name="v">Value to index on</param>
    /// <param name="k">Key associated with the value</param>
    /// <returns>True if found</returns>

    public bool TryGetByValue(TV v, out TK k) => reverse.TryGetValue(v, out k);

    /// <summary>
    /// Add a key-value pair to the map
    /// </summary>
    /// <param name="k">Key to add</param>
    /// <param name="v">Value associated with the key</param>

    public void Add(TK k, TV v)
    {
        forward.Add(k, v);
        reverse.Add(v, k);
    }

    /// <summary>
    /// Indexer on key
    /// </summary>
    /// <param name="k">Key to lookup</param>
    /// <returns>Value associated with the ley</returns>

    public TV this[TK k]
    {
        get => forward[k];
        set
        {
            forward[k] = value;
            reverse[value] = k;
        }
    }

    /// <summary>
    /// Indexer on value
    /// </summary>
    /// <param name="v">Value to lookup</param>
    /// <returns>The key associated with the value</returns>

    public TK this[TV v]
    {
        get => reverse[v];
        set
        {
            reverse[v] = value;
            forward[value] = v;
        }
    }

    /// <summary>
    /// Number of items in the map
    /// </summary>

    public int Count => forward.Count;
}
