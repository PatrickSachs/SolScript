using System;

namespace SolScript.Parser
{
    /// <summary>
    ///     Event arguments used whenever the parser encounters a token in the SolScript source file.
    /// </summary>
    public class SolParserTokenEventArgs
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" /></exception>
        public SolParserTokenEventArgs(SolTokenType token, string text, int start, int length)
        {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            Token = token;
            Text = text;
            Start = start;
            Length = length;
        }

        /// <summary>
        ///     The end index of the token.
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        ///     The length of the token.
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     The start index in the source file text.
        /// </summary>
        public int Start { get; }

        /// <summary>
        ///     The text of the token.
        /// </summary>
        public string Text { get; }

        /// <summary>
        ///     Which token are we talking about?
        /// </summary>
        public SolTokenType Token { get; }
    }
}