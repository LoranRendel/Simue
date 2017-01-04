using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simue;
using System.Collections.Generic;
using System.Linq;

namespace SimueTests
{
    [TestClass]
    public class SimueComplierTests
    {
        string _sample_1 = "temp120 c4-4";

        private string _sample_2 =
            "TEMP220 EB5-8 D5-8 D5-4 EB5-8 D5-8 D5-4 EB5-8 d5-8 d5-4 bb5-4 P-4 BB5-8 A5-8 G5-4 G5-8 F5-8 EB5-4 EB5-8 D5-8 C5-4 C5-4 P-4 D5-8 C5-8 C5-4 D5-8 C5-8 C5-4 D5-8 C5-8 C5-4 A5-4 P-4 A5-8 G5-8 F#5-4 F#5-8 EB5-8 D5-4 D5-8 C5-8 BB4-4 BB4-4 P-4 BB5-8 A5-8 A5-4 C6-4 F#5-4 A5-4 G5-4 D5-4 P-4 BB5-8 A5-8 A5-4 C6-4 F#5-4 A5-4 G5-4 BB5-4 A5-8 G5-8 F5-8 EB5-8 D5-1 C#5-1 D5-2";

        private string _sample_3 =
            "temp150 eb5-8^3 partOne[ab5-8 eb5-8 cb6-8 eb5-8] partOne partOne bb5-8 eb5-8 ab5-8 eb5-8 ab5-8 eb5-8^4 cb5-8^4 ab4-8^4 d4-8^4 eb4-8^4 bb5-8^3 partTwo[bb5-8 fb5-8 db6-8 fb5-8] partTwo partTwo cb6-8 fb5-8 bb5-8 fb5-8 db6-8 bb5-8^4 g5-8^3 g5-8 fb5-8^5 eb5-8 db5-8 bb4-8 cb5-8^4 eb5-8^3 partThree[eb5-8 cb5-8 ab5-8 cb5-8] partThree gb5-8 db5-8 fb5-8 db5-8 eb5-8 ab4-8 db5-8  ab4-8 partFour[db5-8 bb4-8 gb5-8 bb4-8] partFour fb5-8 cb5-8 eb5-8 cb5-8 db5-8 gb4-8 cb5-8 gb4-8 cb5-8 ab4-8 db5-8 ab4-8 bb4-8 ab4-8 cb5-8 ab4-8 gb5-8 db5-8 fb5-8 db5-8 eb5-8 ab4-8 db5-8 ab4-8 partFive[eb5-8 gb4-8 cb5-8 gb4-8] partFive partSix[db5-8 gb4-8 bb4-8 gb4-8] partSix eb5-8 ab4-8 ab5-8 eb5-8 a5-8 eb5-8 ab5-8 eb5-8 gb5-8 db5-8 fb5-8 db5-8 eb5-8 ab4-8 db5-8 ab4-8 db5-8 bb4-8 fb5-8 bb4-8 ab5-8 bb4-8 gb5-8 bb4-8 fb5-8 cb5-8 eb5-8 cb5-8 db5-8 gb4-8 cb5-8 gb4-8 cb5-8 ab4-8 db5-8 ab4-8 bb4-8 ab4-8 cb5-8 ab4-8 gb5-8 db5-8 fb5-8 db5-8 eb5-8 ab4-8 db5-8 ab4-8 partSeven[eb5-8 gb4-8 cb5-8 gb4-8] partSeven partEight[db5-8 gb4-8 bb4-8 gb4-8]  partEight partnine[cb5-8 cb4-8 gb4-8 cb4-8] partnine bb4-8 bb3-8 g4-8 bb3-8 bb4-8";

        [TestMethod]
        public void TestTokenize()
        {

            SimueCompiler compiler = new SimueCompiler();
            bool exceptions = false;
            List<Token> tokens = null;
            try
            {
                tokens = compiler.Tokenize(_sample_1);
            }
            catch
            {
                exceptions = true;
            }
            Assert.IsFalse(exceptions, "The method threw an exception.");
            Assert.IsNotNull(tokens, "The method did not return any results.");
            if (tokens != null)
            {
                List<Token> expectedResult = new List<Token>();
                expectedResult.Add(new Token(TokenType.Start, 0, string.Empty));
                expectedResult.Add(new Token(TokenType.TempLiteral, 0, "temp"));
                expectedResult.Add(new Token(TokenType.Number, 4, "120"));
                expectedResult.Add(new Token(TokenType.WhiteSpace, 7, " "));
                expectedResult.Add(new Token(TokenType.DegreeLiteral, 8, "c"));
                expectedResult.Add(new Token(TokenType.Number, 9, "4"));
                expectedResult.Add(new Token(TokenType.Divider, 10, "-"));
                expectedResult.Add(new Token(TokenType.Number, 11, "4"));
                expectedResult.Add(new Token(TokenType.End, _sample_1.Length, string.Empty));
                int i = expectedResult.Except(tokens).Count();
                Assert.AreEqual<int>(0, i, "The generated results are erroneous.");
            }
        }

        [TestMethod]
        public void TestParse()
        {
            SimueCompiler compiler = new SimueCompiler();
            try
            {
                var result = compiler.Parse(compiler.Tokenize(_sample_1));
                Assert.AreEqual(0, result.Errors.Count, $"{nameof(_sample_1)} was compiled with errors.");
                result = compiler.Parse(compiler.Tokenize(_sample_2));
                Assert.AreEqual(0, result.Errors.Count, $"{nameof(_sample_2)} was compiled with errors.");
                result = compiler.Parse(compiler.Tokenize(_sample_3));
                Assert.AreEqual(0, result.Errors.Count, $"{nameof(_sample_3)} was compiled with errors.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"An exception was thrown during compilation. {ex.Message}");
            }
            
        }
    }
}

