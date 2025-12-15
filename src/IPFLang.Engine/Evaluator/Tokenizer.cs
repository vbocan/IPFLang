using System.Globalization;
using System.Text;

namespace IPFLang.Evaluator
{
    public class Tokenizer
    {
        public Tokenizer(TextReader reader)
        {
            _reader = reader;
            NextChar();
            NextToken();
        }

        TextReader _reader;
        char _currentChar;
        Token _currentToken;
        decimal _number;
        string _identifier = string.Empty;
        string _currency = string.Empty;

        public Token Token
        {
            get { return _currentToken; }
        }

        public decimal Number
        {
            get { return _number; }
        }

        public string Identifier
        {
            get { return _identifier; }
        }

        public string Currency
        {
            get { return _currency; }
        }

        // Read the next character from the input strem
        // and store it in _currentChar, or load '\0' if EOF
        void NextChar()
        {
            int ch = _reader.Read();
            _currentChar = ch < 0 ? '\0' : (char)ch;
        }

        // Read the next token from the input stream
        public void NextToken()
        {
            // Skip whitespace
            while (char.IsWhiteSpace(_currentChar))
            {
                NextChar();
            }

            // Special characters
            switch (_currentChar)
            {
                case '\0':
                    _currentToken = Token.EOF;
                    return;

                case '+':
                    NextChar();
                    _currentToken = Token.Add;
                    return;

                case '-':
                    NextChar();
                    _currentToken = Token.Subtract;
                    return;

                case '*':
                    NextChar();
                    _currentToken = Token.Multiply;
                    return;

                case '/':
                    NextChar();
                    _currentToken = Token.Divide;
                    return;

                case '(':
                    NextChar();
                    _currentToken = Token.OpenParens;
                    return;

                case ')':
                    NextChar();
                    _currentToken = Token.CloseParens;
                    return;

                case ',':
                    NextChar();
                    _currentToken = Token.Comma;
                    return;
            }

            // Number or CurrencyLiteral?
            if (char.IsDigit(_currentChar) || _currentChar == '.')
            {
                // Capture digits/decimal point
                var sb = new StringBuilder();
                bool haveDecimalPoint = false;
                while (char.IsDigit(_currentChar) || (!haveDecimalPoint && _currentChar == '.'))
                {
                    sb.Append(_currentChar);
                    haveDecimalPoint = _currentChar == '.';
                    NextChar();
                }

                // Parse the number
                _number = decimal.Parse(sb.ToString(), CultureInfo.InvariantCulture);

                // Check for currency suffix: <CUR>
                if (_currentChar == '<')
                {
                    NextChar(); // skip '<'
                    var currSb = new StringBuilder();
                    while (char.IsLetter(_currentChar))
                    {
                        currSb.Append(_currentChar);
                        NextChar();
                    }
                    if (_currentChar == '>')
                    {
                        NextChar(); // skip '>'
                        _currency = currSb.ToString().ToUpperInvariant();
                        _currentToken = Token.CurrencyLiteral;
                        return;
                    }
                    // Invalid currency syntax - treat as error
                    throw new SyntaxException($"Invalid currency literal: expected '>' after currency code");
                }

                _currency = string.Empty;
                _currentToken = Token.Number;
                return;
            }

            // Identifier - starts with letter or underscore
            if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                var sb = new StringBuilder();

                // Accept letter, digit or underscore
                while (char.IsLetterOrDigit(_currentChar) || _currentChar == '_' || _currentChar == '!')
                {
                    sb.Append(_currentChar);
                    NextChar();
                }

                // Setup token
                _identifier = sb.ToString();
                _currentToken = Token.Identifier;
                return;
            }
        }
    }
}
