using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor.Models
{
    class GapBuffer
    {
        public char[] _buffer;
        public int _gapStart;
        public int _gapEnd;

        public int Length => _buffer.Length - (_gapEnd - _gapStart);


        public GapBuffer(int gapSize = 128)
        {
            _buffer = new char[gapSize];
            _gapStart = 0;
            _gapEnd = gapSize;
        }

        public void MoveGapTo(int pos)
        {
            if (pos == _gapStart) return;

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
            if (_gapEnd - _gapStart >= extra)
                return;

            int oldLength = _buffer.Length;
            int newCapacity = Math.Max(oldLength * 2, oldLength + extra);
            char[] newBuffer = new char[newCapacity];

            int tailLength = oldLength - _gapEnd;
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
            MoveGapTo(pos + count);
            _gapStart -= count;
        }

        public char CharAt(int pos)
        {
            return pos < _gapStart ? _buffer[pos] : _buffer[pos + (_gapEnd - _gapStart)];
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(Length);
            for (int i = 0; i < _gapStart; i++) sb.Append(_buffer[i]);
            for (int i = _gapEnd; i < _buffer.Length; i++) sb.Append(_buffer[i]);
            return sb.ToString();
        }
    }
}
