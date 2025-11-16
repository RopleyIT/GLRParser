using BooleanLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParserGenerator;

namespace ParserParserTests;

[TestClass, System.Runtime.InteropServices.GuidAttribute("3F35F0FD-52F1-4498-96D7-931FDB8024D7")]
public class GrammarTests
{
    private static BoolExpr SetupFourVarXor(LeafIndexProvider bef, char baseChar)
    {
        BoolExpr left = new LeafExpr(baseChar.ToString(), bef);
        BoolExpr right = new LeafExpr(((char)(baseChar + 1)).ToString(), bef);
        BoolExpr l = new AndExpr(new NotExpr(left), right);
        BoolExpr r = new AndExpr(left, new NotExpr(right));
        BoolExpr lroot = new OrExpr(l, r);
        left = new LeafExpr(((char)(baseChar + 2)).ToString(), bef);
        right = new LeafExpr(((char)(baseChar + 3)).ToString(), bef);
        l = new AndExpr(new NotExpr(left), right);
        r = new AndExpr(left, new NotExpr(right));
        BoolExpr rroot = new OrExpr(l, r);
        BoolExpr root = new OrExpr
        (
            new AndExpr(new NotExpr(lroot), rroot),
            new AndExpr(lroot, new NotExpr(rroot))
        );
        return root;
    }

    [TestMethod]
    public void CompareElements()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        BoolExpr rbe = SetupFourVarXor(bef, 'A');
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarToken rgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsTrue(le == re);
        Assert.IsTrue(re == le);
        Assert.IsTrue(le.Equals(re));
        Assert.IsTrue(re.Equals(le));
    }

    [TestMethod]
    public void CompareElementsOneNull()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = null;
        Assert.IsFalse(le == re);
        Assert.IsFalse(re == le);
        Assert.IsFalse(le.Equals(re));
    }

    [TestMethod]
    public void CompareElementsBothNull()
    {
        GrammarElement le = null;
        GrammarElement re = null;
        Assert.IsTrue(le == re);
        Assert.IsTrue(re == le);
    }

    [TestMethod]
    public void CompareElementsDiffBE()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        BoolExpr rbe = SetupFourVarXor(bef, 'B');
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarToken rgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsFalse(le == re);
        Assert.IsFalse(re == le);
        Assert.IsFalse(le.Equals(re));
        Assert.IsFalse(re.Equals(le));
    }

    [TestMethod]
    public void CompareElementsOneNullBE()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        BoolExpr rbe = null;
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarToken rgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsFalse(le == re);
        Assert.IsFalse(re == le);
        Assert.IsFalse(le.Equals(re));
        Assert.IsFalse(re.Equals(le));
    }
    [TestMethod]
    public void CompareElementsBothNullBE()
    {
        _ = new LeafIndexProvider();
        BoolExpr lbe = null;
        BoolExpr rbe = null;
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarToken rgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsTrue(le == re);
        Assert.IsTrue(re == le);
        Assert.IsTrue(le.Equals(re));
        Assert.IsTrue(re.Equals(le));
    }

    [TestMethod]
    public void CompareElementsOneNullTok()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        BoolExpr rbe = SetupFourVarXor(bef, 'A');
        GrammarToken lgt = new(3, TokenType.Terminal, "NAME", "string");
        GrammarToken rgt = null;
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsFalse(le == re);
        Assert.IsFalse(re == le);
        Assert.IsFalse(le.Equals(re));
        Assert.IsFalse(re.Equals(le));
    }

    [TestMethod]
    public void CompareElementsBothNullTok()
    {
        LeafIndexProvider bef = new();
        BoolExpr lbe = SetupFourVarXor(bef, 'A');
        BoolExpr rbe = SetupFourVarXor(bef, 'A');
        GrammarToken lgt = null;
        GrammarToken rgt = null;
        GrammarElement le = new(lgt, lbe);
        GrammarElement re = new(rgt, rbe);
        Assert.IsTrue(le == re);
        Assert.IsTrue(re == le);
        Assert.IsTrue(le.Equals(re));
        Assert.IsTrue(re.Equals(le));
    }
}
