using BooleanLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BooleanLibTest
{
    [TestClass]
    public class BoolExprTests
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
        public void TestManyFalsesAtBottomOfTruthTable()
        {
            LeafIndexProvider bef = new();
            LeafExpr wmjt = new("WMJT", bef);
            for (int i = 1; i < 11; i++)
                _ = new LeafExpr($"Unused{i}", bef);
            LeafExpr icjhth = new("ICJHTH", bef);
            BoolExpr expr = new AndExpr(new NotExpr(wmjt), icjhth);
            long minimisedLeaves = expr.MinimisedLeaves();
            Assert.AreEqual(0x801L, minimisedLeaves);
        }

        [TestMethod]
        public void TestLowComparison()
        {
            LeafIndexProvider bef = new();
            BoolExpr expr = SetupFourVarXor(bef, 'S');
            BoolExpr cmpx = new AndExpr(new LeafExpr("S", bef), expr);
            BooleanComparison cmpResult = cmpx.CompareExpressions(expr);
            Assert.AreEqual(BooleanComparison.LeftIsSubsetOfRight, cmpResult);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.RightIsSubsetOfLeft, cmpResult);
            cmpx = new NotExpr(expr);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Disjoint, cmpResult);
            cmpx = SetupFourVarXor(bef, 'S');
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Equal, cmpResult);
            BoolExpr wExpr = new LeafExpr("W", bef);
            wExpr = new OrExpr(wExpr, new LeafExpr("S", bef));
            cmpResult = expr.CompareExpressions(wExpr);
            Assert.AreEqual(BooleanComparison.Intersect, cmpResult);
        }

        [TestMethod]
        public void TestHighComparison()
        {
            LeafIndexProvider bef = new();
            BoolExpr expr;
            for (int i = 0; i < 6; i++)
                _ = new LeafExpr($"unused{i}", bef);
            expr = SetupFourVarXor(bef, 'S');
            BoolExpr cmpx = new AndExpr(new LeafExpr("S", bef), expr);
            BooleanComparison cmpResult = cmpx.CompareExpressions(expr);
            Assert.AreEqual(BooleanComparison.LeftIsSubsetOfRight, cmpResult);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.RightIsSubsetOfLeft, cmpResult);
            cmpx = new NotExpr(expr);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Disjoint, cmpResult);
            cmpx = SetupFourVarXor(bef, 'S');
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Equal, cmpResult);
            BoolExpr wExpr = new LeafExpr("W", bef);
            wExpr = new OrExpr(wExpr, new LeafExpr("S", bef));
            cmpResult = expr.CompareExpressions(wExpr);
            Assert.AreEqual(BooleanComparison.Intersect, cmpResult);
        }

        [TestMethod]
        public void TestMixedComparison()
        {
            LeafIndexProvider bef = new();
            BoolExpr expr;
            for (int i = 0; i < 4; i++)
                _ = new LeafExpr($"unused{i}", bef);
            expr = SetupFourVarXor(bef, 'S');
            BoolExpr cmpx = new AndExpr(new LeafExpr("S", bef), expr);
            BooleanComparison cmpResult = cmpx.CompareExpressions(expr);
            Assert.AreEqual(BooleanComparison.LeftIsSubsetOfRight, cmpResult);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.RightIsSubsetOfLeft, cmpResult);
            cmpx = new NotExpr(expr);
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Disjoint, cmpResult);
            cmpx = SetupFourVarXor(bef, 'S');
            cmpResult = expr.CompareExpressions(cmpx);
            Assert.AreEqual(BooleanComparison.Equal, cmpResult);
            BoolExpr wExpr = new LeafExpr("W", bef);
            wExpr = new OrExpr(wExpr, new LeafExpr("S", bef));
            cmpResult = expr.CompareExpressions(wExpr);
            Assert.AreEqual(BooleanComparison.Intersect, cmpResult);
        }

        [TestMethod]
        public void TestExpressionCreation()
        {
            LeafIndexProvider bef = new();
            BoolExpr expr = SetupFourVarXor(bef, 'S');

            // Check the leaves that have been identified

            Assert.AreEqual(0xFL, expr.Leaves);
            Assert.AreEqual(0x6996699669966996UL, expr.ResultBits(0));
        }

        [TestMethod]
        public void TestHighLeavesExpressionCreation()
        {
            LeafIndexProvider bef = new();
            for (int i = 0; i < 6; i++)
                _ = new LeafExpr($"Unused{i}", bef);
            BoolExpr expr = SetupFourVarXor(bef, 'W');

            // Check the leaves that have been identified

            Assert.AreEqual(0x3C0L, expr.Leaves);
            Assert.AreEqual(0UL, expr.ResultBits(0));
            Assert.AreEqual(~0UL, expr.ResultBits(1));
            Assert.AreEqual(~0UL, expr.ResultBits(2));
            Assert.AreEqual(0UL, expr.ResultBits(3));
            Assert.AreEqual(~0UL, expr.ResultBits(4));
            Assert.AreEqual(0UL, expr.ResultBits(5));
            Assert.AreEqual(0UL, expr.ResultBits(6));
            Assert.AreEqual(~0UL, expr.ResultBits(7));
            Assert.AreEqual(~0UL, expr.ResultBits(8));
            Assert.AreEqual(0UL, expr.ResultBits(9));
            Assert.AreEqual(0UL, expr.ResultBits(10));
            Assert.AreEqual(~0UL, expr.ResultBits(11));
            Assert.AreEqual(0UL, expr.ResultBits(12));
            Assert.AreEqual(~0UL, expr.ResultBits(13));
            Assert.AreEqual(~0UL, expr.ResultBits(14));
            Assert.AreEqual(0UL, expr.ResultBits(15));
        }

        [TestMethod]
        public void TestMixedLeavesExpressionCreation()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr rexpr = SetupFourVarXor(bef, 'W');
            OrExpr expr = new
            (
                new AndExpr(new NotExpr(lexpr), rexpr),
                new AndExpr(lexpr, new NotExpr(rexpr))
            );

            // Check the leaves that have been identified

            ulong pat1 = 0x6996966996696996L;
            ulong pat2 = ~pat1;
            Assert.AreEqual(0xFFL, expr.Leaves);
            Assert.AreEqual(pat1, expr.ResultBits(0));
            Assert.AreEqual(pat2, expr.ResultBits(1));
            Assert.AreEqual(pat2, expr.ResultBits(2));
            Assert.AreEqual(pat1, expr.ResultBits(3));
        }

        [TestMethod]
        public void TestHighLeafMinimisation()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr rexpr = SetupFourVarXor(bef, 'W');
            BoolExpr leftLeaf = new LeafExpr("A", bef);
            BoolExpr rightLeaf = new LeafExpr("B", bef);
            BoolExpr xorExpr = new OrExpr
            (
                new AndExpr(new NotExpr(lexpr), rexpr),
                new AndExpr(lexpr, new NotExpr(rexpr))
            );

            // Now create the expression (A|B)|(~A&~B)&xorExpr,
            // which should have the value xorExpr when minimised

            OrExpr orExpr = new(leftLeaf, rightLeaf);
            AndExpr andExpr = new
                (new NotExpr(leftLeaf), new NotExpr(rightLeaf));
            orExpr = new OrExpr(andExpr, orExpr);
            andExpr = new AndExpr(xorExpr, orExpr);

            // Check the leaves that have been identified

            ulong pat1 = 0x6996966996696996UL;
            ulong pat2 = ~pat1;
            Assert.AreEqual(0x3FFL, andExpr.Leaves);
            Assert.AreEqual(pat1, andExpr.ResultBits(0));
            Assert.AreEqual(pat2, andExpr.ResultBits(1));
            Assert.AreEqual(pat2, andExpr.ResultBits(2));
            Assert.AreEqual(pat1, andExpr.ResultBits(3));
            Assert.AreEqual(0xFF, andExpr.MinimisedLeaves());
        }

        [TestMethod]
        public void TestLowLeafMinimisation()
        {
            LeafIndexProvider bef = new();
            BoolExpr leftLeaf = new LeafExpr("A", bef);
            BoolExpr rightLeaf = new LeafExpr("B", bef);
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr rexpr = SetupFourVarXor(bef, 'W');
            BoolExpr xorExpr = new OrExpr
            (
                new AndExpr(new NotExpr(lexpr), rexpr),
                new AndExpr(lexpr, new NotExpr(rexpr))
            );

            // Now create the expression (A|B)|(~A&~B)&xorExpr,
            // which should have the value xorExpr when minimised

            OrExpr orExpr = new(leftLeaf, rightLeaf);
            AndExpr andExpr = new
                (new NotExpr(leftLeaf), new NotExpr(rightLeaf));
            orExpr = new OrExpr(andExpr, orExpr);
            andExpr = new AndExpr(xorExpr, orExpr);

            // Check the leaves that have been identified

            ulong pat1 = 0x0FF0F00FF00F0FF0L;
            ulong pat2 = ~pat1;
            Assert.AreEqual(0x3FFL, andExpr.Leaves);
            Assert.AreEqual(pat1, andExpr.ResultBits(0));
            Assert.AreEqual(pat2, andExpr.ResultBits(1));
            Assert.AreEqual(pat2, andExpr.ResultBits(2));
            Assert.AreEqual(pat1, andExpr.ResultBits(3));
            Assert.AreEqual(0x3FCL, andExpr.MinimisedLeaves());
        }

        [TestMethod]
        public void TestMixedLeafMinimisation()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr leftLeaf = new LeafExpr("A", bef);
            BoolExpr rexpr = SetupFourVarXor(bef, 'W');
            BoolExpr rightLeaf = new LeafExpr("B", bef);
            BoolExpr xorExpr = new OrExpr
            (
                new AndExpr(new NotExpr(lexpr), rexpr),
                new AndExpr(lexpr, new NotExpr(rexpr))
            );

            // Now create the expression (A|B)|(~A&~B)&xorExpr,
            // which should have the value xorExpr when minimised

            BoolExpr orExpr = new OrExpr(leftLeaf, rightLeaf);
            BoolExpr andExpr = new AndExpr
                (new NotExpr(leftLeaf), new NotExpr(rightLeaf));
            orExpr = new OrExpr(andExpr, orExpr);
            andExpr = new AndExpr(xorExpr, orExpr);

            // Check the leaves that have been identified

            Assert.AreEqual(0x3FFL, andExpr.Leaves);
            Assert.AreEqual(0x1EFL, andExpr.MinimisedLeaves());
        }

        [TestMethod]
        public void TestAsIdentifier()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            string identifier = lexpr.AsIdentifier(bef);
            Assert.AreEqual("S_T_U_V_6996", identifier);
        }

        [TestMethod]
        public void TestMinimisedIdentifier()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr leftLeaf = new LeafExpr("A", bef);
            BoolExpr rexpr = SetupFourVarXor(bef, 'W');
            BoolExpr rightLeaf = new LeafExpr("B", bef);
            BoolExpr xorExpr = new OrExpr
            (
                new AndExpr(new NotExpr(lexpr), rexpr),
                new AndExpr(lexpr, new NotExpr(rexpr))
            );

            // Now create the expression (A|B)|(~A&~B)&xorExpr,
            // which should have the value xorExpr when minimised

            OrExpr orExpr = new(leftLeaf, rightLeaf);
            AndExpr andExpr = new
                (new NotExpr(leftLeaf), new NotExpr(rightLeaf));
            orExpr = new OrExpr(andExpr, orExpr);
            andExpr = new AndExpr(xorExpr, orExpr);

            // Check the identifier uses only minimised leaves

            string identifier = andExpr.AsIdentifier(bef);
            Assert.AreEqual("S_T_U_V_W_X_Y_Z_6996966996696996966969966996966996696996699696696996966996696996", identifier);
        }

        [TestMethod]
        public void TestComparer()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            _ = new LeafExpr("A", bef);
            BoolExpr rexpr = SetupFourVarXor(bef, 'S');
            Assert.IsTrue(lexpr == rexpr);
            Assert.IsTrue(lexpr.Equals(rexpr));
            Assert.IsTrue(rexpr.Equals(lexpr));
        }

        [TestMethod]
        public void TestComparerOneNull()
        {
            LeafIndexProvider bef = new();
            BoolExpr lexpr = SetupFourVarXor(bef, 'S');
            BoolExpr rexpr = null;
            Assert.IsFalse(lexpr == rexpr);
            Assert.IsFalse(rexpr == lexpr);
            Assert.IsFalse(lexpr.Equals(rexpr));
        }

        [TestMethod]
        public void TestComparerBothNull()
        {
            BoolExpr lexpr = null;
            BoolExpr rexpr = null;
            Assert.IsTrue(lexpr == rexpr);
            Assert.IsTrue(rexpr == lexpr);
        }
    }
}
