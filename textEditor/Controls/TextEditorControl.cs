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

        private readonly Typeface _typeface =
            new Typeface(
                new FontFamily("Consolas"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

        private double _fontSize = 20;

        private readonly Brush _background =
            new SolidColorBrush(Color.FromRgb(46, 46, 51));

        private readonly Brush _textBrush =
            new SolidColorBrush(Color.FromRgb(213, 213, 219));

        private readonly Brush _cursorBrush =
            new SolidColorBrush(Color.FromRgb(213, 213, 219));

        private double _horizontalOffset = 0;

        public TextEditorControl()
        {
            _document = new TextDocument();

            Focusable = true;
            Loaded += (_, _) =>
            {
                Focus();
                InvalidateVisual();
            };
            Cursor = Cursors.IBeam;
            FocusVisualStyle = null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(
                _background,
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            DrawText(dc);

            DrawCursor(dc);
        }

        private void DrawText(DrawingContext dc)
        {
            double lineHeight = _fontSize;
            double x = 10 - _horizontalOffset;

            for (int line = 0; line < _document.LineCount; line++)
            {
                string lineText = _document.GetLine(line);

                var formattedText = new FormattedText(
                    lineText,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize,
                    _textBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double y = 10 + line * lineHeight;

                dc.DrawText(
                    formattedText,
                    new Point(x, y));
            }
        }

        private void DrawCursor(DrawingContext dc)
        {
            string textBeforeCursor =
                _document.GetTextBeforeCursorOnCurrentLine();

            var formattedText = new FormattedText(
                textBeforeCursor,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                _fontSize,
                _textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double lineHeight = _fontSize;

            double cursorX = 10 - _horizontalOffset + formattedText.WidthIncludingTrailingWhitespace;

            double cursorY = 10 + _document.CurrentLine * lineHeight;

            double cursorHeight = _fontSize;

            dc.DrawLine(
                new Pen(_cursorBrush, 2),
                new Point(cursorX, cursorY),
                new Point(cursorX, cursorY + cursorHeight));
        }

        private void EnsureCursorVisible()
        {
            int cursorPosition = _document.CursorPosition;

            string textBeforeCursor = "";

            for (int i = 0; i < cursorPosition; i++)
            {
                textBeforeCursor += _document.CharAt(i);
            }

            var formattedText = new FormattedText(
                textBeforeCursor,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                _fontSize,
                _textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double cursorX = 10 + formattedText.WidthIncludingTrailingWhitespace;

            double leftMargin = 10;
            double rightMargin = 10;

            double visibleRight = _horizontalOffset + ActualWidth - rightMargin;

            if (cursorX > visibleRight)
            {
                _horizontalOffset = cursorX - ActualWidth + rightMargin;
            }

            double visibleLeft = _horizontalOffset + leftMargin;

            if (cursorX < visibleLeft)
            {
                _horizontalOffset =
                    cursorX - leftMargin;
            }

            if (_horizontalOffset < 0)
            {
                _horizontalOffset = 0;
            }
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

            switch (e.Key)
            {
                case Key.Left:
                    _document.MoveToLeft();
                    EnsureCursorVisible();
                    break;

                case Key.Right:
                    _document.MoveToRight();
                    EnsureCursorVisible();
                    break;

                case Key.Home:
                    _document.MoveToStart();
                    EnsureCursorVisible();
                    break;

                case Key.End:
                    _document.MoveToEnd();
                    EnsureCursorVisible();
                    break;

                case Key.Back:
                    _document.Backspace();
                    EnsureCursorVisible();
                    break;

                case Key.Delete:
                    _document.Delete();
                    EnsureCursorVisible();
                    break;

                case Key.Enter:
                    _document.NextLine();
                    EnsureCursorVisible();
                    break;

                case Key.Tab:
                    _document.Insert('\t');
                    EnsureCursorVisible();
                    break;

                default:
                    return;
            }

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