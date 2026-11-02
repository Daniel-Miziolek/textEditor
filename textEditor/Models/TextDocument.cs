using System;
using System.Collections.Generic;
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
        private readonly List<int> _lineStarts = new() { 0 };

        public bool HasSelection => SelectionStart >= 0 && SelectionEnd > SelectionStart;

        public int LineCount => _lineStarts.Count;

        public int CurrentLine => LineIndexAt(CursorPosition);

        public int CurrentColumn => CursorPosition - _lineStarts[CurrentLine];

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
                InsertChar(_buffer.Length, c);

            CursorPosition = _buffer.Length;
        }

        public void Insert(char c)
        {
            ReplaceSelectionIfNeeded();
            InsertChar(CursorPosition, c);
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
                InsertChar(CursorPosition, c);
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

            DeleteRange(CursorPosition - 1, 1);
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

            DeleteRange(CursorPosition, 1);

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

            DeleteRange(start, count);
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

            int targetLine = CurrentLine + direction;
            if (targetLine < 0 || targetLine >= LineCount)
                return;

            int targetStart = _lineStarts[targetLine];
            int targetEnd = GetLineEnd(targetLine);

            CursorPosition = Math.Min(targetStart + _preferredColumn, targetEnd);
        }

        public void MoveToStart()
        {
            CursorPosition = _lineStarts[CurrentLine];
            _preferredColumn = -1;
        }

        public void MoveToEnd()
        {
            CursorPosition = GetLineEnd(CurrentLine);
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
            if (lineNumber < 0 || lineNumber >= _lineStarts.Count)
                return string.Empty;

            int start = _lineStarts[lineNumber];
            int end = GetLineEnd(lineNumber);
            var sb = new StringBuilder(end - start);

            for (int i = start; i < end; i++)
                sb.Append(_buffer.CharAt(i));

            return sb.ToString();
        }

        public int GetLineStart(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= _lineStarts.Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            return _lineStarts[lineNumber];
        }

        public string GetTextBeforeCursorOnCurrentLine()
        {
            int lineStart = _lineStarts[CurrentLine];
            var sb = new StringBuilder(CursorPosition - lineStart);

            for (int i = lineStart; i < CursorPosition; i++)
                sb.Append(_buffer.CharAt(i));

            return sb.ToString();
        }

        private void InsertChar(int pos, char c)
        {
            _buffer.Insert(pos, c);

            for (int i = _lineStarts.Count - 1; i >= 1; i--)
            {
                if (_lineStarts[i] > pos)
                    _lineStarts[i]++;
                else
                    break;
            }

            if (c == '\n')
                _lineStarts.Insert(LineIndexAt(pos) + 1, pos + 1);
        }

        private void DeleteRange(int pos, int count)
        {
            if (count <= 0)
                return;

            int end = pos + count;

            for (int i = _lineStarts.Count - 1; i >= 1; i--)
            {
                int start = _lineStarts[i];

                if (start > end)
                    _lineStarts[i] = start - count;
                else if (start > pos)
                    _lineStarts.RemoveAt(i);
                else
                    break;
            }

            _buffer.Delete(pos, count);
        }

        private int LineIndexAt(int position)
        {
            int lo = 0;
            int hi = _lineStarts.Count - 1;
            int result = 0;

            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;

                if (_lineStarts[mid] <= position)
                {
                    result = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return result;
        }

        private int GetLineEnd(int lineNumber)
        {
            return lineNumber + 1 < _lineStarts.Count
                ? _lineStarts[lineNumber + 1] - 1
                : Length;
        }
    }
}
