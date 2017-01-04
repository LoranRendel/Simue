using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simue
{
    public enum TokenType:byte
    {
        Empty = 0,
        Unknown,
        DegreeLiteral,
        AccidentalLiteral,
        Divider,
        MinusSign,
        PlusSign,
        WhiteSpace,
        TempLiteral,
        TranspositionMarker,
        Number,
        TripletMarker,
        RepeatOperator,
        ProlongOperator,
        DotOperator,
        TimeUnit,
        End,
        Start,
        PauseLiteral,
        AliasReference,
        OpenBracket,
        CloseBracket,
        AliasDeclaration
    }
    public enum ExpressionType:byte
    {
        Undefined = 0,
        Tempo,
        Note,
        Operator,
        Sound
    }
    public enum CharGroup
    {
        Letter,
        Digit,
        Other,
        Undefined,
        WhiteSpace
    }
    public struct Token
    {
        public TokenType Type
        {
            get { return _type; }
        }
        public string Value
        {
            get
            {
                if (_type == TokenType.Empty)
                    return string.Empty;
                return _value;
            }
        }
        public int Index
        {
            get { return _indexInString; }
        }
        private string _value;
        private TokenType _type;
        private int _indexInString;
        public Token(TokenType type, int indexInString, string value)
        {
            _type = type;
            _indexInString = indexInString;
            if (type == TokenType.Empty && value != string.Empty)
                throw new ArgumentException("You cannot create an empty token with a value. When you create an empty token, use string.Empty or \"\" as the value argument.", "value");
            _value = value;
        }
        public static Token Empty
        {
            get { return new Token(); }
        }
    }
    public struct Note
    {
        public double Frequency { get; set; }
        public double Duration { get; set; }
        public Note(double frequncy, double duration)
        {
            Frequency = frequncy;
            Duration = duration;
        }
    }
    public class Song
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Text { get; set; }
        public Note[] Notes { get; set; }
        public Double Length { get { return GetSongLength(this.Notes); } }
        public Song(){ }
        public Song(string name, string author, Note[] notes)
        {
            this.Name = name;
            this.Author = author;
            this.Notes = notes;
        }
        private Double GetSongLength(Note[] notes)
        {
            try
            {
                if (notes != null)
                    return notes.Select(note => note.Duration).Aggregate((total, next) => total + next);
            }
            catch
            {
                return 0d;
            }
            return 0d;
        }
    }

    public class CompilationResult
    {
        public List<Token> Errors { get; set; }
        public Song Song { get; set; }
    }
}