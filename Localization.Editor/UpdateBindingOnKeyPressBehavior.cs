using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Localization.Editor
{
    public class UpdateBindingOnKeyPressBehavior : Behavior<UIElement>
    {
        private DependencyProperty _targetProperty;

        public string TargetDependencyPropertyName { get; set; }
        public Key Key { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            _targetProperty = (DependencyProperty)AssociatedObject
                    .GetType()
                    .GetFields(BindingFlags.Static | BindingFlags.Public)
                    .First(f => f.Name.Equals(TargetDependencyPropertyName, StringComparison.Ordinal))
                    .GetValue(AssociatedObject)!;

            AssociatedObject.PreviewKeyDown += this.AssociatedObject_PreviewKeyDown;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreviewKeyDown -= this.AssociatedObject_PreviewKeyDown;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key) return;

            (BindingOperations.GetBindingExpression(AssociatedObject, _targetProperty)).UpdateSource();
        }
    }
    public class EditorTextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewKeyDown += this.AssociatedObject_PreviewKeyDown;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreviewKeyDown -= this.AssociatedObject_PreviewKeyDown;
        }

        private int[] GetLineStartPositions(int startPos = 0, int? stopPos = null, bool includeBeforeStartPos = false)
        {
            List<int> l = new();

            if (startPos == 0) l.Add(startPos);
            else if (includeBeforeStartPos)
            {
                for (int i = startPos; i >= 0; --i)
                {
                    if (i == 0)
                    {
                        l.Add(0);
                        break;
                    }
                    switch (AssociatedObject.Text[i])
                    {
                    case '\n':
                        l.Add(i + 1);
                        break;
                    }
                }
            }

            for (int i = startPos, i_max = stopPos ?? AssociatedObject.Text.Length; i < i_max; ++i)
            {
                switch (AssociatedObject.Text[i])
                {
                case '\n':
                    l.Add(i + 1);
                    break;
                }
            }

            return l.ToArray();
        }
        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.Tab:
                {
                    if (AssociatedObject.SelectionLength > 0 && AssociatedObject.SelectedText.Contains('\n'))
                    {
                        string result = AssociatedObject.Text;
                        bool isShiftDown = e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift);

                        var lineStarts = GetLineStartPositions(AssociatedObject.SelectionStart, AssociatedObject.SelectionStart + AssociatedObject.SelectionLength, true);
                        for (int i = lineStarts.Length - 1; i >= 0; --i)
                        {
                            if (!isShiftDown)
                                result = result.Insert(lineStarts[i], "\t");
                            else if (result[lineStarts[i]] == '\t')
                                result = result.Remove(lineStarts[i], 1);
                        }

                        int diff = result.Length - AssociatedObject.Text.Length;
                        if (diff != 0)
                        {
                            AssociatedObject.LockCurrentUndoUnit();
                            var selectionStart = AssociatedObject.SelectionStart;
                            var selectionLength = AssociatedObject.SelectionLength;
                            AssociatedObject.Text = result;
                            AssociatedObject.SelectionStart = selectionStart + diff;
                            AssociatedObject.SelectionLength = selectionLength + diff;
                            e.Handled = true;
                        }
                    }
                    break;
                }
            }
        }
    }
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            return true;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class ChainConverter : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (IValueConverter converter in this)
            {
                value = converter.Convert(value, targetType, parameter, culture);
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (IValueConverter converter in this.AsEnumerable().Reverse())
            {
                value = converter.ConvertBack(value, targetType, parameter, culture);
            }
            return value;
        }
    }
}
