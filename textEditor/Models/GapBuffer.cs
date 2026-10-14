using System;

namespace TextEditor.Models
{
    public class GapBuffer
    {
        private char[] _buffer;
        private int _gapStart;
        private int _gapEnd;

        public int Length => _buffer.Length - (_gapEnd - _gapStart);

        public GapBuffer(int gapSize = 128)
        {
            gapSize = Math.Max(1, gapSize);

            _buffer = new char[gapSize];
            _gapStart = 0;
            _gapEnd = gapSize;
        }

        public void MoveGapTo(int pos)
        {
            if (pos < 0 || pos > Length)
                throw new ArgumentOutOfRangeException(nameof(pos));

            if (pos == _gapStart)
                return;

            if (pos < _gapStart)
            {
                int count = _gapStart - pos;

                Array.Copy(_buffer, pos, _buffer, _gapEnd - count, count);

                _gapStart = pos;
                _gapEnd -= count;
            }
            else
            {
                int count = pos - _gapStart;

                Array.Copy(_buffer, _gapEnd, _buffer, _gapStart, count);

                _gapStart += count;
                _gapEnd += count;
            }
        }

        private void EnsureCapacity(int extra)
        {
            int gapSize = _gapEnd - _gapStart;

            if (gapSize >= extra)
                return;

            int oldCapacity = _buffer.Length;
            int newCapacity = Math.Max(oldCapacity * 2, oldCapacity + extra - gapSize);

            char[] newBuffer = new char[newCapacity];

            int tailLength = oldCapacity - _gapEnd;
            int newGapEnd = newCapacity - tailLength;

            Array.Copy(_buffer, 0, newBuffer, 0, _gapStart);
            Array.Copy(_buffer, _gapEnd, newBuffer, newGapEnd, tailLength);

            _buffer = newBuffer;
            _gapEnd = newGapEnd;
        }

        public void Insert(int pos, char c)
        {
            MoveGapTo(pos);
            EnsureCapacity(1);

            _buffer[_gapStart] = c;
            _gapStart++;
        }

        public void Delete(int pos, int count)
        {
            if (pos < 0 || count < 0 || pos + count > Length)
                throw new ArgumentOutOfRangeException();

            if (count == 0)
                return;

            MoveGapTo(pos + count);
            _gapStart -= count;
        }

        public char CharAt(int pos)
        {
            if (pos < 0 || pos >= Length)
                throw new ArgumentOutOfRangeException(nameof(pos));

            return pos < _gapStart ? _buffer[pos] : _buffer[pos + (_gapEnd - _gapStart)];
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(Length);

            for (int i = 0; i < _gapStart; i++)
                sb.Append(_buffer[i]);

            for (int i = _gapEnd; i < _buffer.Length; i++)
                sb.Append(_buffer[i]);

            return sb.ToString();
        }
    }
}