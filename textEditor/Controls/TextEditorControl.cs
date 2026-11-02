using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TextEditor.Controls
{
    public class TextEditorControl : FrameworkElement
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(
                nameof(Document),
                typeof(TextDocument),
                typeof(TextEditorControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDocumentChanged));

        private readonly Typeface _typeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private readonly double _fontSize = 20;
        private readonly Brush _background = new SolidColorBrush(Color.FromRgb(46, 46, 51));
        private readonly Brush _textBrush = new SolidColorBrush(Color.FromRgb(213, 213, 219));
        private readonly Brush _cursorBrush = new SolidColorBrush(Color.FromRgb(213, 213, 219));
        private readonly Brush _selectionBrush = new SolidColorBrush(Color.FromRgb(75, 105, 160));

        private double _horizontalOffset;
        private double _verticalOffset;

        public TextDocument? Document
        {
            get => (TextDocument?)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public TextEditorControl()
        {
            Document = new TextDocument();

            Focusable = true;
            Cursor = Cursors.IBeam;
            FocusVisualStyle = null;

            Loaded += (_, _) =>
            {
                Focus();
                InvalidateVisual();
            };
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TextEditorControl)d;
            control._horizontalOffset = 0;
            control._verticalOffset = 0;
            control.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(_background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Document is null)
                return;

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
            var document = Document;
            if (document is null)
                return;

            double lineHeight = _fontSize;
            double x = 10 - _horizontalOffset;

            for (int line = 0; line < document.LineCount; line++)
            {
                FormattedText formattedText = CreateFormattedText(document.GetLine(line));
                double y = 10 + line * lineHeight - _verticalOffset;

                dc.DrawText(formattedText, new Point(x, y));
            }
        }

        private void DrawCursor(DrawingContext dc)
        {
            var document = Document;
            if (document is null)
                return;

            FormattedText formattedText = CreateFormattedText(document.GetTextBeforeCursorOnCurrentLine());

            double cursorX = 10 - _horizontalOffset + formattedText.WidthIncludingTrailingWhitespace;
            double cursorY = 10 + document.CurrentLine * _fontSize - _verticalOffset;

            dc.DrawLine(new Pen(_cursorBrush, 2), new Point(cursorX, cursorY), new Point(cursorX, cursorY + _fontSize));
        }

        private void DrawSelection(DrawingContext dc)
        {
            var document = Document;
            if (document is null || !document.HasSelection)
                return;

            double lineHeight = _fontSize;

            for (int line = 0; line < document.LineCount; line++)
            {
                string lineText = document.GetLine(line);

                int lineStart = document.GetLineStart(line);
                int lineEnd = lineStart + lineText.Length;

                int selectionStart = Math.Max(document.SelectionStart, lineStart);
                int selectionEnd = Math.Min(document.SelectionEnd, lineEnd);

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
            }
        }

        private void EnsureCursorVisible()
        {
            var document = Document;
            if (document is null)
                return;

            FormattedText textBeforeCursor = CreateFormattedText(document.GetTextBeforeCursorOnCurrentLine());
            double cursorX = 10 + textBeforeCursor.WidthIncludingTrailingWhitespace;

            double leftMargin = 10;
            double rightMargin = 10;

            double visibleRight = _horizontalOffset + ActualWidth - rightMargin;

            if (cursorX > visibleRight)
                _horizontalOffset = cursorX - ActualWidth + rightMargin;

            double visibleLeft = _horizontalOffset + leftMargin;

            if (cursorX < visibleLeft)
                _horizontalOffset = cursorX - leftMargin;

            _horizontalOffset = Math.Max(0, _horizontalOffset);

            double cursorY = 10 + document.CurrentLine * _fontSize;

            double topMargin = 10;
            double bottomMargin = 10;

            double visibleBottom = _verticalOffset + ActualHeight - bottomMargin;

            if (cursorY + _fontSize > visibleBottom)
                _verticalOffset = cursorY + _fontSize - ActualHeight + bottomMargin;

            double visibleTop = _verticalOffset + topMargin;

            if (cursorY < visibleTop)
                _verticalOffset = cursorY - topMargin;

            _verticalOffset = Math.Max(0, _verticalOffset);

            double contentHeight = 20 + document.LineCount * _fontSize;
            double maxOffset = Math.Max(0, contentHeight - ActualHeight);

            _verticalOffset = Math.Min(_verticalOffset, maxOffset);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (Document is null)
                return;

            if (!string.IsNullOrEmpty(e.Text))
            {
                Document.Insert(e.Text);

                EnsureCursorVisible();
                InvalidateVisual();
            }

            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (Document is null)
                return;

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
                Document.StartSelection();

            switch (e.Key)
            {
                case Key.Left:
                    if (shift)
                        Document.MoveToLeft();
                    else
                        Document.MoveToLeftAndClearSelection();
                    break;

                case Key.Right:
                    if (shift)
                        Document.MoveToRight();
                    else
                        Document.MoveToRightAndClearSelection();
                    break;

                case Key.Up:
                    Document.MoveToPreviousLine();
                    if (!shift)
                        Document.ClearSelection();
                    break;

                case Key.Down:
                    Document.MoveToNextLine();
                    if (!shift)
                        Document.ClearSelection();
                    break;

                case Key.Home:
                    Document.MoveToStart();
                    if (!shift)
                        Document.ClearSelection();
                    break;

                case Key.End:
                    Document.MoveToEnd();
                    if (!shift)
                        Document.ClearSelection();
                    break;

                case Key.Back:
                    Document.Backspace();
                    break;

                case Key.Delete:
                    Document.Delete();
                    break;

                case Key.Enter:
                    Document.Insert('\n');
                    break;

                case Key.Tab:
                    Document.Insert('\t');
                    break;

                case Key.A:
                    if (ctrl)
                    {
                        Document.SelectAll();
                        EnsureCursorVisible();
                        InvalidateVisual();
                        e.Handled = true;
                    }
                    return;

                default:
                    return;
            }

            if (shift && isNavigationKey)
                Document.UpdateSelection();

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