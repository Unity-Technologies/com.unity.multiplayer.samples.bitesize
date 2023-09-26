using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
{
    /// <summary>
    /// Shows text in the form of a speech bubble
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_BackgroundSprite;
        [SerializeField] TMP_Text m_MessageLabel;
        [SerializeField] Vector2 m_Padding;
        [SerializeField] int m_MaxCharactersPerLine = 20;
        [SerializeField] string m_DefaultMessage;
        [SerializeField] bool m_SetupOnStart;

        void Start()
        {
            if (m_SetupOnStart)
            {
                Setup(m_DefaultMessage);
            }
        }

        /// <summary>
        /// Shows some text in the bubble
        /// </summary>
        /// <param name="text"></param>
        public void Setup(string text)
        {
            gameObject.SetActive(true);
            m_MessageLabel.SetText(WordWrap(text, m_MaxCharactersPerLine));
            m_MessageLabel.ForceMeshUpdate();
            Vector2 textSize = m_MessageLabel.GetRenderedValues(false);
            m_BackgroundSprite.size = textSize + m_Padding;
        }

        /// <summary>
        /// Word wraps the given text to fit within the specified width.
        /// </summary>
        /// <param name="text">Text to be word wrapped</param>
        /// <param name="width">Width, in characters, to which the text
        /// should be word wrapped</param>
        /// <returns>The modified text</returns>
        /// <remarks>Based on: https://www.codeproject.com/Articles/51488/Implementing-Word-Wrap-in-C </remarks>
        static string WordWrap(string text, int width)
        {
            if (width < 1)
            {
                return text;
            }

            int position;
            int next;
            var sb = new StringBuilder();
            // Parse each line of text
            for (position = 0; position < text.Length; position = next)
            {
                int lineEndingIndex = text.IndexOf(Environment.NewLine, position);
                if (lineEndingIndex == -1)
                {
                    next = lineEndingIndex = text.Length;
                }
                else
                {
                    next = lineEndingIndex + Environment.NewLine.Length;
                }

                // Copy this line of text, breaking into smaller lines as needed
                if (lineEndingIndex > position)
                {
                    do
                    {
                        int lineLentgh = lineEndingIndex - position;
                        if (lineLentgh > width)
                        {
                            lineLentgh = BreakLine(text, position, width);
                        }
                        sb.Append(text, position, lineLentgh);
                        sb.Append(Environment.NewLine);

                        // Trim whitespace following break
                        position += lineLentgh;
                        while (position < lineEndingIndex && Char.IsWhiteSpace(text[position]))
                        {
                            position++;
                        }
                    } while (lineEndingIndex > position);
                }
                else
                {
                    sb.Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        static int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            int i = max;
            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
            {
                i--;
            }

            // If no whitespace found, break at maximum length
            if (i < 0)
            {
                return max;
            }

            // Find start of whitespace
            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
            {
                i--;
            }

            // Return length of text before whitespace
            return i + 1;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
