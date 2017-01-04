using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Simue
{
    public class SimueCompiler
    {
        const string TRANSPOSITION_MARKER = "t";
        const string REPEAT_OPERATOR = "^";
        const string PROLONG_OPERATOR = "x";
        const string DIVIDER = "-";
        const string DOT_OPERATOR = ".";
        string[] acceptableOpsNdivs = new string[] { TRANSPOSITION_MARKER, REPEAT_OPERATOR, PROLONG_OPERATOR, DIVIDER, DOT_OPERATOR };
        
        string _degrees = "cdefgabh";
        string _accidentals = "b#";
        string _timeUnits = "hmsms";
        const string TEMP_LITERAL = "temp";
        public bool SUCCESS = true;
        Dictionary<string, int> degreeSemitones = null;
        public SimueCompiler()
        {
            degreeSemitones = new Dictionary<string, int>();
            degreeSemitones.Add("C", 0);
            degreeSemitones.Add("D", 2);
            degreeSemitones.Add("E", 4);
            degreeSemitones.Add("F", 5);
            degreeSemitones.Add("G", 7);
            degreeSemitones.Add("A", 9);
            degreeSemitones.Add("B", 11);
            degreeSemitones.Add("H", 11);
        }
        #region Tokenizer
        private string _input = string.Empty;
        public List<Token> Tokenize(string input)
        {
            _input = input;
            input = input.ToLower();
            List<Token> tokens = new List<Token>();
            CharGroup currentCharaterType = CharGroup.Undefined;
            ExpressionType tokenizerContext = ExpressionType.Undefined;
            int index = 0;
            string rawExpression = string.Empty;
            tokens.Add(new Token(TokenType.Start, 0, string.Empty));
            for (int i = 0; i < input.Length; i++)
            {
                var t = GetCharGroup(input[i]);
                if (currentCharaterType != t)
                {
                    ProcessRawExpression(rawExpression, currentCharaterType, index, tokens, ref tokenizerContext);
                    rawExpression = string.Empty;
                    currentCharaterType = t;
                    index = i;
                }
                rawExpression += input[i];
            }
            ProcessRawExpression(rawExpression, currentCharaterType, index, tokens, ref tokenizerContext);
            tokens.Add(new Token(TokenType.End, input.Length, string.Empty));
            return tokens;
        }
        private void ProcessRawExpression(string expression, CharGroup charsType, int index, List<Token> tokens, ref ExpressionType tokenizerContext)
        {
            switch (charsType)
            {
                case CharGroup.Letter:
                    if (expression == TEMP_LITERAL)
                    {
                        tokenizerContext = ExpressionType.Tempo;
                        tokens.Add(new Token(TokenType.TempLiteral, index, expression));
                        return;
                    }
                    if (expression.Length == 2)
                    {
                        if (tokenizerContext == ExpressionType.Sound && expression == "ms")
                        {
                            tokens.Add(new Token(TokenType.TimeUnit, index, expression));
                            return;
                        }
                        //Detect aliasID
                        if (tokenizerContext == ExpressionType.Undefined)
                        {
                            if (!_degrees.Contains(expression[0]) || !_accidentals.Contains(expression[1]))
                            {
                                if (_input.Skip(index + 2).Take(1).FirstOrDefault() == '[')
                                    tokens.Add(new Token(TokenType.AliasDeclaration, index, expression));
                                else
                                    tokens.Add(new Token(TokenType.AliasReference, index, expression));
                                return;
                            }
                        }
                        ProcessRawExpression(expression[0].ToString(), CharGroup.Letter, index, tokens, ref tokenizerContext);
                        ProcessRawExpression(expression[1].ToString(), CharGroup.Letter, index + 1, tokens, ref tokenizerContext);
                        return;
                    }
                    if (expression.Length == 1)
                    {
                        //Detect the transposition marker
                        if (tokenizerContext == ExpressionType.Tempo && expression == TRANSPOSITION_MARKER)
                        {
                            tokens.Add(new Token(TokenType.TranspositionMarker, index, expression));
                            tokenizerContext = ExpressionType.Operator;
                            return;
                        }
                        //Detect a note
                        if (tokenizerContext == ExpressionType.Undefined && _degrees.Contains(expression))
                        {
                            tokens.Add(new Token(TokenType.DegreeLiteral, index, expression));
                            tokenizerContext = ExpressionType.Note;
                            return;
                        }
                        //Detrect pause
                        if (tokenizerContext == ExpressionType.Undefined && expression == "p")
                        {
                            tokens.Add(new Token(TokenType.PauseLiteral, index, expression));
                            tokenizerContext = ExpressionType.Note;
                            return;
                        }
                        //Detect an accidential sign
                        if (tokenizerContext == ExpressionType.Note && _accidentals.Contains(expression))
                        {
                            tokens.Add(new Token(TokenType.AccidentalLiteral, index, expression));
                            return;
                        }
                        //Detect the triplet sign
                        if (tokenizerContext == ExpressionType.Note && expression == TRANSPOSITION_MARKER)
                        {
                            tokens.Add(new Token(TokenType.TripletMarker, index, expression));
                            return;
                        }
                        if ((tokenizerContext == ExpressionType.Note || tokenizerContext == ExpressionType.Sound) && expression == PROLONG_OPERATOR)
                        {
                            tokens.Add(new Token(TokenType.ProlongOperator, index, expression));
                            return;
                        }
                        if (tokenizerContext == ExpressionType.Sound && _timeUnits.Contains(expression))
                        {
                            tokens.Add(new Token(TokenType.TimeUnit, index, expression));
                            return;
                        }
                        if (tokenizerContext == ExpressionType.Undefined && _input.Skip(index + expression.Length).Take(1).FirstOrDefault() == '[' && !_degrees.Contains(expression))
                        {
                            tokens.Add(new Token(TokenType.AliasDeclaration, index, expression));
                            return;
                        }
                        if (tokenizerContext == ExpressionType.Undefined && !_degrees.Contains(expression))
                        {
                            tokens.Add(new Token(TokenType.AliasReference, index, expression));
                            return;
                        }
                        tokens.Add(new Token(TokenType.Unknown, index, expression));
                        return;
                    }
                    if (tokenizerContext == ExpressionType.Undefined && _input.Skip(index + expression.Length).Take(1).FirstOrDefault() == '[')
                    {
                        tokens.Add(new Token(TokenType.AliasDeclaration, index, expression));
                        return;
                    }
                    tokens.Add(new Token(TokenType.AliasReference, index, expression));
                    return;
                case CharGroup.Digit:
                    tokens.Add(new Token(TokenType.Number, index, expression));
                    if (tokenizerContext == ExpressionType.Undefined)
                        tokenizerContext = ExpressionType.Sound;
                    break;
                case CharGroup.Other:
                    if (expression.Length > 1 && expression.Count(ch => ch == '.') != expression.Length)
                    {
                        for (int i = 0; i < expression.Length; i++)
                            ProcessRawExpression(expression[i].ToString(), CharGroup.Other, index + i, tokens, ref tokenizerContext);
                        return;
                    }
                    if (expression == REPEAT_OPERATOR)
                    {
                        tokens.Add(new Token(TokenType.RepeatOperator, index, expression));
                        return;
                    }
                    if ((tokenizerContext == ExpressionType.Note || tokenizerContext == ExpressionType.Sound) && expression == DIVIDER)
                    {
                        tokens.Add(new Token(TokenType.Divider, index, expression));
                        return;
                    }
                    if (tokenizerContext == ExpressionType.Operator && expression == DIVIDER)
                    {
                        tokens.Add(new Token(TokenType.MinusSign, index, expression));
                        return;
                    }
                    if (expression == "[")
                    {
                        tokens.Add(new Token(TokenType.OpenBracket, index, expression));
                        return;
                    }
                    if (expression == "]")
                    {
                        tokens.Add(new Token(TokenType.CloseBracket, index, expression));
                        return;
                    }
                    if (_accidentals.Contains(expression))
                    {
                        tokens.Add(new Token(TokenType.AccidentalLiteral, index, expression));
                        return;
                    }
                    if (expression.Count(ch => ch == '.') == expression.Length && (tokenizerContext == ExpressionType.Note || tokenizerContext == ExpressionType.Sound))
                    {
                        tokens.Add(new Token(TokenType.DotOperator, index, expression));
                        return;
                    }


                    tokens.Add(new Token(TokenType.Unknown, index, expression));
                    break;
                case CharGroup.WhiteSpace:
                    tokens.Add(new Token(TokenType.WhiteSpace, index, expression));
                    tokenizerContext = ExpressionType.Undefined;
                    break;
                case CharGroup.Undefined:
                    return;
            }


        }
        private CharGroup GetCharGroup(char ch)
        {
            if (char.IsDigit(ch))
                return CharGroup.Digit;
            if (char.IsLetter(ch))
                return CharGroup.Letter;
            if (char.IsWhiteSpace(ch))
                return CharGroup.WhiteSpace;
            return CharGroup.Other;
        }
        #endregion
        #region Parser
        Token currToken = new Token();
        Token acceptedToken = new Token();
        int CTi = -1;
        private void NextToken(List<Token> tokens)
        {
            CTi++;
            if (CTi < tokens.Count)
                currToken = tokens[CTi];

        }
        private bool Accept(List<Token> tokens, TokenType token)
        {
            if (token != currToken.Type) return false;
            acceptedToken = currToken;
            NextToken(tokens);
            return true;
        }
        private bool Expect(List<Token> tokens, List<Token> erroneousTokens, TokenType token)
        {
            if (Accept(tokens, token))
                return true;
            erroneousTokens.Add(currToken);
            return false;
        }
        public CompilationResult Parse(List<Token> tokens)
        {
            List<Token> erroneousTokens = new List<Token>();
            CompilationResult result = new CompilationResult();
            result.Song = new Song();
            result.Errors = erroneousTokens;
            List<Note> notes = new List<Note>();
            List<Tuple<int, string, Note[]>> aliases = new List<Tuple<int, string, Note[]>>();
            int aliasLevel = 0;
          
            double temp = 100;
            double transpositionHalfTones = 0;
            NextToken(tokens);
            Expect(tokens, erroneousTokens, TokenType.Start);
            if (Accept(tokens, TokenType.TempLiteral))
            {
                if (Expect(tokens, erroneousTokens, TokenType.Number))
                {
                    //MAX TEMP - 10000
                    if (double.TryParse(acceptedToken.Value, out temp))
                    {
                        if (temp > 10000)
                        {
                           erroneousTokens.Add(acceptedToken);
                            return result;
                        }
                    }
                    else
                    {
                        erroneousTokens.Add(acceptedToken);
                        return result;
                    }
                    if (Accept(tokens, TokenType.TranspositionMarker))
                    {
                        bool negative = Accept(tokens, TokenType.MinusSign);
                        if (Expect(tokens, erroneousTokens, TokenType.Number))
                        {
                            if (double.TryParse(acceptedToken.Value, out transpositionHalfTones))
                            {
                                if (transpositionHalfTones > 300)
                                {
                                    erroneousTokens.Add(acceptedToken);
                                    return result;
                                }
                            }
                            else
                            {
                                erroneousTokens.Add(acceptedToken);
                                return result;
                            }
                            if (negative)
                                transpositionHalfTones *= -1;
                        }

                    }
                }
                while (Accept(tokens, TokenType.WhiteSpace))
                {
                    Expression(tokens, erroneousTokens, notes, aliases, ref aliasLevel, ref transpositionHalfTones, ref temp);
                }
            }
            else
            {
                do { Expression(tokens, erroneousTokens, notes, aliases, ref aliasLevel, ref transpositionHalfTones, ref temp); }
                while (Accept(tokens, TokenType.WhiteSpace));
            }
            Expect(tokens, erroneousTokens, TokenType.End);
            CTi = -1;
            currToken = new Token();
            result.Song.Notes = notes.ToArray();
            return result;
        }
        private bool Note(List<Token> tokens, List<Token> erroneousTokens, List<Note> notes, ref double transpositionHalfTones, ref double temp)
        {
            double whole = 60000 / (temp / 4d);
            double denominator = 4d;
            double duration = whole / denominator;
            double frequency = 0d;
            double haftones = -1;
            double accidential = 0;
            double octave = 4;
            if (Accept(tokens, TokenType.DegreeLiteral) || Accept(tokens, TokenType.PauseLiteral))
            {
                bool pause = acceptedToken.Type == TokenType.PauseLiteral;
                if (!pause)
                    haftones = degreeSemitones[acceptedToken.Value.ToUpper()];


                if (Accept(tokens, TokenType.AccidentalLiteral))
                {
                    if (acceptedToken.Value == "#")
                        accidential = 1;
                    else
                        accidential = -1;

                }
                if (Accept(tokens, TokenType.Number))
                {
                    if (double.TryParse(acceptedToken.Value, out octave))
                    {
                        if (octave > 8 || octave < 0)
                        {
                            erroneousTokens.Add(acceptedToken);
                            return false;
                        }
                    }
                    else
                    {
                        erroneousTokens.Add(acceptedToken);
                        return false;
                    }
                }
                if (Accept(tokens, TokenType.Divider))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        if (double.TryParse(acceptedToken.Value, out denominator))
                        {
                            if (denominator > 256)
                            {
                                erroneousTokens.Add(acceptedToken);
                                return false;
                            }
                        }
                        else
                        {
                            erroneousTokens.Add(acceptedToken);
                            return false;
                        }
                    }
                }
                duration = whole / denominator;

                frequency = 16.3516 * Math.Pow(2, (octave * 12 + (haftones + accidential + transpositionHalfTones)) / 12);
                if (pause)
                    frequency = 0;
                if (Accept(tokens, TokenType.DotOperator))
                {
                    duration = DotOperator(duration, acceptedToken.Value.Length);
                }
                if (Accept(tokens, TokenType.RepeatOperator))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        int repeatCount = 0;
                        if (int.TryParse(acceptedToken.Value, out repeatCount))
                        {
                            if (repeatCount > 256)
                            {
                                erroneousTokens.Add(acceptedToken);
                                return false;
                            }
                        }
                        else
                        {
                            erroneousTokens.Add(acceptedToken);
                            return false;

                        }
                        for (int i = 0; i < repeatCount - 1; i++)
                        {
                            notes.Add(new Note
                            {
                                Frequency = frequency,
                                Duration = duration
                            });
                        }
                    }
                }
                if (Accept(tokens, TokenType.ProlongOperator))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        int repeatCount = 1;
                        if (int.TryParse(acceptedToken.Value, out repeatCount))
                        {
                            duration *= repeatCount;
                        }
                        else
                        {
                            erroneousTokens.Add(acceptedToken);
                            return false;
                        }

                    }
                }
                if (Accept(tokens, TokenType.TripletMarker))
                {
                    duration = duration * 2d / 3d;
                }
                notes.Add(new Note
                {
                    Frequency = frequency,
                    Duration = duration
                });
                return true;
            }
            return false;

        }
        private bool Sound(List<Token> tokens, List<Token> erroneousTokens, List<Note> notes, ref double temp)
        {
            double frequency = 0;
            double duration1 = 60000 / temp;
            double duration2 = 0;
            double duration3 = 0;
            double duration4 = 0;
            if (Accept(tokens, TokenType.Number))
            {
                if (double.TryParse(acceptedToken.Value, out frequency))
                {
                    if (frequency > 20000)
                    {
                        erroneousTokens.Add(acceptedToken);
                        return false;
                    }
                }
                if (Accept(tokens, TokenType.Divider))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        double.TryParse(acceptedToken.Value, out duration1);
                        if (Accept(tokens, TokenType.TimeUnit))
                        {
                            switch (acceptedToken.Value)
                            {
                                case "s":
                                    duration1 *= 1000;
                                    break;
                                case "h":
                                    duration1 *= 60 * 60 * 1000;
                                    break;
                                case "ms":
                                    break;
                                case "m":
                                    duration1 *= 60 * 1000;
                                    break;

                            }
                            if (Accept(tokens, TokenType.Number))
                            {
                                double.TryParse(acceptedToken.Value, out duration2);
                                if (Expect(tokens, erroneousTokens, TokenType.TimeUnit))
                                {
                                    switch (acceptedToken.Value)
                                    {
                                        case "s":
                                            duration2 *= 1000;
                                            break;
                                        case "h":
                                            duration2 *= 60 * 60 * 1000;
                                            break;
                                        case "ms":
                                            break;
                                        case "m":
                                            duration2 *= 60 * 1000;
                                            break;

                                    }
                                    if (Accept(tokens, TokenType.Number))
                                    {
                                        double.TryParse(acceptedToken.Value, out duration3);
                                        if (Expect(tokens, erroneousTokens, TokenType.TimeUnit))
                                        {
                                            switch (acceptedToken.Value)
                                            {
                                                case "s":
                                                    duration3 *= 1000;
                                                    break;
                                                case "h":
                                                    duration3 *= 60 * 60 * 1000;
                                                    break;
                                                case "ms":
                                                    break;
                                                case "m":
                                                    duration3 *= 60 * 1000;
                                                    break;

                                            }
                                            if (Accept(tokens, TokenType.Number))
                                            {
                                                double.TryParse(acceptedToken.Value, out duration4);
                                                if (Expect(tokens, erroneousTokens, TokenType.TimeUnit))
                                                {
                                                    switch (acceptedToken.Value)
                                                    {
                                                        case "s":
                                                            duration4 *= 1000;
                                                            break;
                                                        case "h":
                                                            duration4 *= 60 * 60 * 1000;
                                                            break;
                                                        case "ms":
                                                            break;
                                                        case "m":
                                                            duration4 *= 60 * 1000;
                                                            break;

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                double duration = duration1 + duration2 + duration3 + duration4;
                if (Accept(tokens, TokenType.DotOperator))
                {
                    duration = DotOperator(duration, acceptedToken.Value.Length);
                }
                if (Accept(tokens, TokenType.RepeatOperator))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        int repeatCount = 0;
                        if (int.TryParse(acceptedToken.Value, out repeatCount))
                        {
                            if (repeatCount > 256)
                            {
                                erroneousTokens.Add(acceptedToken);
                                return false;
                            }
                        }
                        else
                        {
                            erroneousTokens.Add(acceptedToken);
                            return false;
                        }
                        for (int i = 0; i < repeatCount - 1; i++)
                        {
                            notes.Add(new Note
                            {
                                Frequency = frequency,
                                Duration = duration
                            });
                        }
                    }
                }
                if (Accept(tokens, TokenType.ProlongOperator))
                {
                    if (Expect(tokens, erroneousTokens, TokenType.Number))
                    {
                        int repeatCount = 1;
                        int.TryParse(acceptedToken.Value, out repeatCount);
                        duration *= repeatCount;
                    }
                }
                notes.Add(new Note
                {
                    Frequency = frequency,
                    Duration = duration
                });
                return true;
            }
            return false;
        }
        private bool Alias(List<Token> tokens, List<Token> erroneousTokens, List<Tuple<int, string, Note[]>> aliases, List<Note> notes, ref int aliasLevel, ref double transpositionHalfTones, ref double temp)
        {
            int start = notes.Count;
            if (Accept(tokens, TokenType.AliasDeclaration))
            {
                string aliasName = acceptedToken.Value;
                if (Expect(tokens, erroneousTokens, TokenType.OpenBracket))
                {
                    aliasLevel++;
                    if (Expression(tokens, erroneousTokens, notes, aliases, ref aliasLevel, ref transpositionHalfTones, ref temp))
                    {
                        while (Accept(tokens, TokenType.WhiteSpace))
                        {
                            Expression(tokens, erroneousTokens, notes, aliases, ref aliasLevel, ref transpositionHalfTones, ref temp);
                        }
                        if (Expect(tokens, erroneousTokens, TokenType.CloseBracket))
                        {
                            //Enter alias
                            aliases.Add(new Tuple<int, string, Note[]>(aliasLevel, aliasName, notes.Skip(start).ToArray()));
                            //Leave alias;
                            aliasLevel--;
                            return true;
                        }

                    }
                    else
                    {
                        erroneousTokens.Add(acceptedToken);
                        return false;
                    }
                }

            }
            return false;
        }
        private double DotOperator(double duration, int dots)
        {
            return duration * (2d - 1 / Math.Pow(2, dots));

        }
        private bool Expression(List<Token> tokens, List<Token> erroneousTokens, List<Note> notes, List<Tuple<int, string, Note[]>> aliases, ref int aliasLevel, ref double transpositionHalfTones, ref double temp)
        {
            bool note = Note(tokens, erroneousTokens, notes, ref transpositionHalfTones, ref temp);
            bool sound = Sound(tokens, erroneousTokens, notes, ref temp);
            bool alias = Alias(tokens, erroneousTokens, aliases, notes, ref aliasLevel, ref transpositionHalfTones, ref temp);
            if (Accept(tokens, TokenType.AliasReference))
            {
                int level = aliasLevel;
                var aliasContent = aliases.Where(A => (A.Item1 == level + 1 && A.Item2 == acceptedToken.Value));
                if (aliasContent.Count() == 1)
                {
                    notes.AddRange(aliasContent.FirstOrDefault().Item3);
                    return true;
                }
                else
                {
                    erroneousTokens.Add(acceptedToken);
                    return false;
                }
            }
            return note | sound | alias;
        }
        #endregion
    }
}