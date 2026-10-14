using System;
using System.Text;
using TextEditor.Models;

namespace TextEditor
{
    public class TextDocument
    {
        public GapBuffer _buffer { get; }

        public int CursorPosition { get; private set; }

        public int Length => _buffer.Length;

        public int SelectionStart { get; private set; } = -1;
        public int SelectionEnd { get; private set; } = -1;

        private int _selectionAnchor = -1;
        private int _preferredColumn = -1;

        public bool HasSelection => SelectionStart >= 0 && SelectionEnd > SelectionStart;

        public int CurrentLine
        {
            get
            {
                int line = 0;

                for (int i = 0; i < CursorPosition; i++)
                {
                    if (_buffer.CharAt(i) == '\n')
                        line++;
                }

                return line;
            }
        }

        public int CurrentColumn
        {
            get
            {
                for (int i = CursorPosition - 1; i >= 0; i--)
                {
                    if (_buffer.CharAt(i) == '\n')
                        return CursorPosition - i - 1;
                }

                return CursorPosition;
            }
        }

        public TextDocument()
        {
            _buffer = new GapBuffer();
            CursorPosition = 0;
        }

        public TextDocument(string text)
        {
            text ??= string.Empty;

            _buffer = new GapBuffer(Math.Max(128, text.Length));

            foreach (char c in text)
                _buffer.Insert(_buffer.Length, c);

            CursorPosition = _buffer.Length;
        }

        public void Insert(char c)
        {
            ReplaceSelectionIfNeeded();

            _buffer.Insert(CursorPosition, c);
            CursorPosition++;

            _preferredColumn = -1;
        }

        public void Insert(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            ReplaceSelectionIfNeeded();

            foreach (char c in text)
            {
                _buffer.Insert(CursorPosition, c);
                CursorPosition++;
            }

            _preferredColumn = -1;
        }

        private void ReplaceSelectionIfNeeded()
        {
            if (HasSelection)
                DeleteSelection();
            else
                ClearSelection();
        }

        public void Backspace()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition <= 0)
                return;

            _buffer.Delete(CursorPosition - 1, 1);
            CursorPosition--;

            _preferredColumn = -1;
        }

        public void Delete()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition >= Length)
                return;

            _buffer.Delete(CursorPosition, 1);

            _preferredColumn = -1;
        }

        public void DeleteSelection()
        {
            if (!HasSelection)
            {
                ClearSelection();
                return;
            }

            int start = SelectionStart;
            int count = SelectionEnd - SelectionStart;

            _buffer.Delete(start, count);
            CursorPosition = start;

            ClearSelection();
            _preferredColumn = -1;
        }

        public void MoveToLeft()
        {
            if (CursorPosition > 0)
                CursorPosition--;

            _preferredColumn = -1;
        }

        public void MoveToRight()
        {
            if (CursorPosition < Length)
                CursorPosition++;

            _preferredColumn = -1;
        }

        public void MoveToLeftAndClearSelection()
        {
            if (HasSelection)
                CursorPosition = SelectionStart;
            else
                MoveToLeft();

            ClearSelection();
            _preferredColumn = -1;
        }

        public void MoveToRightAndClearSelection()
        {
            if (HasSelection)
                CursorPosition = SelectionEnd;
            else
                MoveToRight();

            ClearSelection();
            _preferredColumn = -1;
        }

        public void StartSelection()
        {
            if (_selectionAnchor < 0)
                _selectionAnchor = CursorPosition;

            UpdateSelection();
        }

        public void UpdateSelection()
        {
            if (_selectionAnchor < 0)
                return;

            SelectionStart = Math.Min(_selectionAnchor, CursorPosition);
            SelectionEnd = Math.Max(_selectionAnchor, CursorPosition);
        }

        public void ClearSelection()
        {
            SelectionStart = -1;
            SelectionEnd = -1;
            _selectionAnchor = -1;
        }

        public void MoveToPreviousLine()
        {
            MoveVertical(-1);
        }

        public void MoveToNextLine()
        {
            MoveVertical(1);
        }

        public void SelectAll()
        {
            _selectionAnchor = 0;
            SelectionStart = 0;
            SelectionEnd = Length;
            CursorPosition = Length;
        }

        private void MoveVertical(int direction)
        {
            if (_preferredColumn < 0)
                _preferredColumn = CurrentColumn;

            int lineStart = CursorPosition - CurrentColumn;
            int targetStart;

            if (direction < 0)
            {
                if (CurrentLine == 0)
                    return;

                targetStart = lineStart - 1;

                while (targetStart > 0 && _buffer.CharAt(targetStart - 1) != '\n')
                    targetStart--;
            }
            else
            {
                int currentEnd = lineStart;

                while (currentEnd < Length && _buffer.CharAt(currentEnd) != '\n')
                    currentEnd++;

                if (currentEnd >= Length)
                    return;

                targetStart = currentEnd + 1;
            }

            int targetEnd = targetStart;

            while (targetEnd < Length && _buffer.CharAt(targetEnd) != '\n')
                targetEnd++;

            CursorPosition = Math.Min(targetStart + _preferredColumn, targetEnd);
        }

        public void MoveToStart()
        {
            int position = CursorPosition;

            while (position > 0 && _buffer.CharAt(position - 1) != '\n')
                position--;

            CursorPosition = position;
            _preferredColumn = -1;
        }

        public void MoveToEnd()
        {
            int position = CursorPosition;

            while (position < Length && _buffer.CharAt(position) != '\n')
                position++;

            CursorPosition = position;
            _preferredColumn = -1;
        }

        public void MoveToDocumentStart()
        {
            CursorPosition = 0;
            _preferredColumn = -1;
        }

        public void MoveToDocumentEnd()
        {
            CursorPosition = Length;
            _preferredColumn = -1;
        }

        public void NextLine()
        {
            Insert('\n');
        }

        public char CharAt(int pos)
        {
            return _buffer.CharAt(pos);
        }

        public override string ToString()
        {
            return _buffer.ToString();
        }

        public string GetLine(int lineNumber)
        {
            int currentLine = 0;
            var sb = new StringBuilder();

            for (int i = 0; i < Length; i++)
            {
                char c = _buffer.CharAt(i);

                if (c == '\n')
                {
                    if (currentLine == lineNumber)
                        return sb.ToString();

                    currentLine++;
                    sb.Clear();
                }
                else if (currentLine == lineNumber)
                {
                    sb.Append(c);
                }
            }

            return currentLine == lineNumber ? sb.ToString() : string.Empty;
        }

        public int LineCount
        {
            get
            {
                int count = 1;

                for (int i = 0; i < Length; i++)
                {
                    if (_buffer.CharAt(i) == '\n')
                        count++;
                }

                return count;
            }
        }

        public string GetTextBeforeCursorOnCurrentLine()
        {
            int lineStart = CursorPosition - CurrentColumn;
            var sb = new StringBuilder();

            for (int i = lineStart; i < CursorPosition; i++)
                sb.Append(_buffer.CharAt(i));

            return sb.ToString();
        }
    }
}