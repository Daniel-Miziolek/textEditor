using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TextEditor.Models;

namespace TextEditor.Controls
{
    public class TextEditorControl : FrameworkElement
    {
        private readonly TextDocument _document;
        private readonly Typeface _typeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private readonly double _fontSize = 20;
        private readonly Brush _background = new SolidColorBrush(Color.FromRgb(46, 46, 51));
        private readonly Brush _textBrush = new SolidColorBrush(Color.FromRgb(213, 213, 219));
        private readonly Brush _cursorBrush = new SolidColorBrush(Color.FromRgb(213, 213, 219));
        private readonly Brush _selectionBrush = new SolidColorBrush(Color.FromRgb(75, 105, 160));

        private double _horizontalOffset;
        private double _verticalOffset;

        public TextEditorControl()
        {
            _document = new TextDocument();

            Focusable = true;
            Cursor = Cursors.IBeam;
            FocusVisualStyle = null;

            Loaded += (_, _) =>
            {
                Focus();
                InvalidateVisual();
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(_background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            DrawSelection(dc);
            DrawText(dc);
            DrawCursor(dc);
        }

        private FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, _fontSize, _textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private void DrawText(DrawingContext dc)
        {
            double lineHeight = _fontSize;
            double x = 10 - _horizontalOffset;

            for (int line = 0; line < _document.LineCount; line++)
            {
                FormattedText formattedText = CreateFormattedText(_document.GetLine(line));
                double y = 10 + line * lineHeight - _verticalOffset;

                dc.DrawText(formattedText, new Point(x, y));
            }
        }

        private void DrawCursor(DrawingContext dc)
        {
            FormattedText formattedText = CreateFormattedText(_document.GetTextBeforeCursorOnCurrentLine());

            double cursorX = 10 - _horizontalOffset + formattedText.WidthIncludingTrailingWhitespace;
            double cursorY = 10 + _document.CurrentLine * _fontSize - _verticalOffset;

            dc.DrawLine(new Pen(_cursorBrush, 2), new Point(cursorX, cursorY), new Point(cursorX, cursorY + _fontSize));
        }

        private void DrawSelection(DrawingContext dc)
        {
            if (!_document.HasSelection)
                return;

            int globalPosition = 0;
            double lineHeight = _fontSize;

            for (int line = 0; line < _document.LineCount; line++)
            {
                string lineText = _document.GetLine(line);

                int lineStart = globalPosition;
                int lineEnd = lineStart + lineText.Length;

                int selectionStart = Math.Max(_document.SelectionStart, lineStart);
                int selectionEnd = Math.Min(_document.SelectionEnd, lineEnd);

                if (selectionStart < selectionEnd)
                {
                    int prefixLength = selectionStart - lineStart;
                    int selectedLength = selectionEnd - selectionStart;

                    string prefix = lineText.Substring(0, prefixLength);
                    string selectedText = lineText.Substring(prefixLength, selectedLength);

                    FormattedText prefixFormatted = CreateFormattedText(prefix);
                    FormattedText selectedFormatted = CreateFormattedText(selectedText);

                    double x = 10 - _horizontalOffset + prefixFormatted.WidthIncludingTrailingWhitespace;
                    double y = 10 + line * lineHeight - _verticalOffset;
                    double width = Math.Max(selectedFormatted.WidthIncludingTrailingWhitespace, 1);

                    dc.DrawRectangle(_selectionBrush, null, new Rect(x, y, width, lineHeight));
                }

                globalPosition = lineEnd;

                if (line < _document.LineCount - 1)
                    globalPosition++;
            }
        }

        private void EnsureCursorVisible()
        {
            string textBeforeCursor = _document.ToString().Substring(0, _document.CursorPosition);
            FormattedText fullText = CreateFormattedText(textBeforeCursor);

            double cursorX = 10 + fullText.WidthIncludingTrailingWhitespace;

            double leftMargin = 10;
            double rightMargin = 10;

            double visibleRight = _horizontalOffset + ActualWidth - rightMargin;

            if (cursorX > visibleRight)
                _horizontalOffset = cursorX - ActualWidth + rightMargin;

            double visibleLeft = _horizontalOffset + leftMargin;

            if (cursorX < visibleLeft)
                _horizontalOffset = cursorX - leftMargin;

            _horizontalOffset = Math.Max(0, _horizontalOffset);

            double cursorY = 10 + _document.CurrentLine * _fontSize;

            double topMargin = 10;
            double bottomMargin = 10;

            double visibleBottom = _verticalOffset + ActualHeight - bottomMargin;

            if (cursorY + _fontSize > visibleBottom)
                _verticalOffset = cursorY + _fontSize - ActualHeight + bottomMargin;

            double visibleTop = _verticalOffset + topMargin;

            if (cursorY < visibleTop)
                _verticalOffset = cursorY - topMargin;

            _verticalOffset = Math.Max(0, _verticalOffset);

            double contentHeight = 20 + _document.LineCount * _fontSize;
            double maxOffset = Math.Max(0, contentHeight - ActualHeight);

            _verticalOffset = Math.Min(_verticalOffset, maxOffset);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (!string.IsNullOrEmpty(e.Text))
            {
                _document.Insert(e.Text);

                EnsureCursorVisible();
                InvalidateVisual();
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            bool isNavigationKey =
                e.Key == Key.Left ||
                e.Key == Key.Right ||
                e.Key == Key.Up ||
                e.Key == Key.Down ||
                e.Key == Key.Home ||
                e.Key == Key.End;

            if (shift && isNavigationKey)
                _document.StartSelection();

            switch (e.Key)
            {
                case Key.Left:
                    if (shift)
                        _document.MoveToLeft();
                    else
                        _document.MoveToLeftAndClearSelection();
                    break;

                case Key.Right:
                    if (shift)
                        _document.MoveToRight();
                    else
                        _document.MoveToRightAndClearSelection();
                    break;

                case Key.Up:
                    _document.MoveToPreviousLine();
                    if (!shift)
                        _document.ClearSelection();
                    break;

                case Key.Down:
                    _document.MoveToNextLine();
                    if (!shift)
                        _document.ClearSelection();
                    break;

                case Key.Home:
                    _document.MoveToStart();
                    if (!shift)
                        _document.ClearSelection();
                    break;

                case Key.End:
                    _document.MoveToEnd();
                    if (!shift)
                        _document.ClearSelection();
                    break;

                case Key.Back:
                    _document.Backspace();
                    break;

                case Key.Delete:
                    _document.Delete();
                    break;

                case Key.Enter:
                    _document.Insert('\n');
                    break;

                case Key.Tab:
                    _document.Insert('\t');
                    break;

                case Key.A:
                    if (ctrl)
                    {
                        _document.SelectAll();
                        EnsureCursorVisible();
                        InvalidateVisual();
                        e.Handled = true;
                    }
                    return;

                default:
                    return;
            }

            if (shift && isNavigationKey)
                _document.UpdateSelection();

            EnsureCursorVisible();
            InvalidateVisual();

            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();

            e.Handled = true;
        }
    }
}