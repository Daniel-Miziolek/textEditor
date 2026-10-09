using System.Text;
using TextEditor.Models;

class TextDocument
{
    public GapBuffer _buffer { get; }

    public int CursorPosition { get; private set; }

    public int Length => _buffer.Length;
    public int CurrentLine
    {
        get
        {
            int line = 0;

            for (int i = 0;  i < CursorPosition;  i++)
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
        _buffer = new GapBuffer(Math.Max(128, text.Length));

        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < text.Length; i++)
                _buffer.Insert(i, text[i]);
        }

        CursorPosition = _buffer.Length;
    }

    public void Insert(char c)
    {
        _buffer.Insert(CursorPosition, c);
        CursorPosition++;
    }

    public void Insert(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (char c in text)
            Insert(c);
    }

    public void Backspace()
    {
        if (CursorPosition <= 0)
            return;

        _buffer.Delete(CursorPosition - 1, 1);
        CursorPosition--;
    }

    public void Delete()
    {
        if (CursorPosition >= Length)
            return;

        _buffer.Delete(CursorPosition, 1);
    }

    public void MoveToLeft()
    {
        if (CursorPosition > 0)
            CursorPosition--;
    }

    public void MoveToRight()
    {
        if (CursorPosition < Length)
            CursorPosition++;
    }


    public void MoveToPreviousLine()
    {
        int currentColumn = CurrentColumn;

        if (CurrentLine == 0)
            return;

        int previousLineStart = CursorPosition - CurrentColumn - 1;

        while (previousLineStart > 0 && _buffer.CharAt(previousLineStart - 1) != '\n')
        {
            previousLineStart--;
        }

        int previousLineEnd = previousLineStart;

        while (previousLineEnd < Length && _buffer.CharAt(previousLineEnd) != '\n')
        {
            previousLineEnd++;
        }

        CursorPosition = Math.Min(previousLineStart + currentColumn, previousLineEnd);
    }

    public void MoveToNextLine()
    {
        int currentColumn = CurrentColumn;

        int currentLineEnd = CursorPosition;

        while (currentLineEnd < Length && _buffer.CharAt(currentLineEnd) != '\n')
        {
            currentLineEnd++;
        }

        if (currentLineEnd >= Length)
            return;

        int nextLineStart = currentLineEnd + 1;

        int nextLineEnd = nextLineStart;

        while (nextLineEnd < Length && _buffer.CharAt(nextLineEnd) != '\n')
        {
            nextLineEnd++;
        }

        CursorPosition = Math.Min(nextLineStart + currentColumn, nextLineEnd);
    }

    public void MoveToStart()
    {
        CursorPosition = 0;
    }

    public void MoveToEnd()
    {
        CursorPosition = Length;
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
            else
            {
                if (currentLine == lineNumber)
                    sb.Append(c);
            }
        }

        if (currentLine == lineNumber)
            return sb.ToString();

        return string.Empty;
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
        {
            sb.Append(_buffer.CharAt(i));
        }

        return sb.ToString();
    }
}