using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ag.WPF.UpDown
{
    /// <summary>
    /// Represents custom control with button spinners that allows incrementing and decrementing numeric values by using the spinner buttons and keyboard up/down arrows.
    /// </summary>
    #region Named parts
    [TemplatePart(Name = ElementText, Type = typeof(TextBox))]
    [TemplatePart(Name = ElementButtonUp, Type = typeof(RepeatButton))]
    [TemplatePart(Name = ElementButtonDown, Type = typeof(RepeatButton))]
    #endregion
    public sealed class UpDown : Control
    {
#nullable disable
        private enum CurrentKey
        {
            None,
            Number,
            Delete,
            Back,
            Decimal
        }

        private struct CurrentPosition
        {
            public CurrentKey Key;
            public int Offset;
            public bool Exclude;
        }

        #region Constants
        private const string ElementText = "PART_Text";
        private const string ElementButtonUp = "PART_Up";
        private const string ElementButtonDown = "PART_Down";
        #endregion

        #region Elements
        private TextBox _textBox;
        private RepeatButton _upButton;
        private RepeatButton _downButton;
        #endregion

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(UpDown),
                new FrameworkPropertyMetadata(0m, OnValueChanged, ConstraintValue));
        /// <summary>
        /// The identifier of the <see cref="MaxValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(decimal), typeof(UpDown),
                new FrameworkPropertyMetadata(decimal.MaxValue, OnMaxValueChanged, CoerceMaximum));
        /// <summary>
        /// The identifier of the <see cref="MinValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(decimal), typeof(UpDown),
                new FrameworkPropertyMetadata(decimal.MinValue, OnMinValueChanged));
        /// <summary>
        /// The identifier of the <see cref="Step"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(decimal), typeof(UpDown),
                new FrameworkPropertyMetadata(1m, OnStepChanged, CoerceStep));
        /// <summary>
        /// The identifier of the <see cref="NegativeForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NegativeForegroundProperty = DependencyProperty.Register(nameof(NegativeForeground), typeof(SolidColorBrush), typeof(UpDown),
                new FrameworkPropertyMetadata(Brushes.Red, OnNegativeForegroundChanged));
        /// <summary>
        /// The identifier of the <see cref="DecimalPlaces"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(nameof(DecimalPlaces), typeof(uint), typeof(UpDown),
                new FrameworkPropertyMetadata((uint)0, OnDecimalPlacesChanged));
        /// <summary>
        /// The identifier of the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(UpDown),
                new FrameworkPropertyMetadata(true, OnIsReadOnlyChanged));
        /// <summary>
        /// The identifier of the <see cref="UseGroupSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseGroupSeparatorProperty = DependencyProperty.Register(nameof(UseGroupSeparator), typeof(bool), typeof(UpDown),
                new FrameworkPropertyMetadata(true, OnUseGroupSeparatorChanged));
        /// <summary>
        /// The identifier of the <see cref="ShowUpDown"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowUpDownProperty = DependencyProperty.Register(nameof(ShowUpDown), typeof(bool), typeof(UpDown),
            new FrameworkPropertyMetadata(true, OnShowUpDownChanged));
        /// <summary>
        /// The identifier of the <see cref="ShowUpDown"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowNullValueProperty = DependencyProperty.Register(nameof(AllowNullValue), typeof(bool), typeof(UpDown),
            new FrameworkPropertyMetadata(false, OnAllowNullValueChanged));
        /// <summary>
        /// The identifier of the <see cref="ShowTrailingZeros"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTrailingZerosProperty = DependencyProperty.Register(nameof(ShowTrailingZeros), typeof(bool), typeof(UpDown),
                new FrameworkPropertyMetadata(true, OnShowTrailingZerosChanged));
        #endregion

        private CurrentPosition _position;
        private bool _gotFocus;

        static UpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UpDown), new FrameworkPropertyMetadata(typeof(UpDown)));
        }

        #region Public dependency properties handlers
        /// <summary>
        /// Gets or sets the value that indicates whether trailing zeroes in decimal part of UpDown should be shown.
        /// </summary>
        public bool ShowTrailingZeros
        {
            get => (bool)GetValue(ShowTrailingZerosProperty);
            set => SetValue(ShowTrailingZerosProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether UpDown can show empty text field.
        /// </summary>
        public bool AllowNullValue
        {
            get => (bool)GetValue(AllowNullValueProperty);
            set => SetValue(AllowNullValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether up and down buttons are visible.
        /// </summary>
        public bool ShowUpDown
        {
            get => (bool)GetValue(ShowUpDownProperty);
            set => SetValue(ShowUpDownProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether group separator is used for number formatting.
        /// </summary>
        public bool UseGroupSeparator
        {
            get => (bool)GetValue(UseGroupSeparatorProperty);
            set => SetValue(UseGroupSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates whether UpDown is in read-only state.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the value that indicates the count of decimal digits shown at UpDown.
        /// </summary>
        public uint DecimalPlaces
        {
            get => (uint)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }
        /// <summary>
        /// Gets or sets the Brush to apply to the text contents of UpDown when control's value is negative.
        /// </summary>
        public SolidColorBrush NegativeForeground
        {
            get => (SolidColorBrush)GetValue(NegativeForegroundProperty);
            set => SetValue(NegativeForegroundProperty, value);
        }
        /// <summary>
        /// Gets or sets the value to increment or decrement UpDown when the up or down buttons are clicked.
        /// </summary>
        public decimal Step
        {
            get => (decimal)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }
        /// <summary>
        /// Gets or sets the minimum allowed value of UpDown.
        /// </summary>
        public decimal MinValue
        {
            get => (decimal)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }
        /// <summary>
        /// Gets or sets the maximum allowed value of UpDown.
        /// </summary>
        public decimal MaxValue
        {
            get => (decimal)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }
        /// <summary>
        /// Gets or sets the value of UpDown.
        /// </summary>
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        #endregion

        #region Routed events
        /// <summary>
        /// Occurs when the <see cref="IsReadOnly"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsReadOnlyChanged
        {
            add => AddHandler(IsReadOnlyChangedEvent, value);
            remove => RemoveHandler(IsReadOnlyChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="IsReadOnlyChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent IsReadOnlyChangedEvent = EventManager.RegisterRoutedEvent("IsReadOnlyChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="UseGroupSeparator"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> UseGroupSeparatorChanged
        {
            add => AddHandler(UseGroupSeparatorChangedEvent, value);
            remove => RemoveHandler(UseGroupSeparatorChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="UseGroupSeparatorChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent UseGroupSeparatorChangedEvent = EventManager.RegisterRoutedEvent("UseGroupSeparatorChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="ShowUpDown"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> ShowUpDownChanged
        {
            add => AddHandler(ShowUpDownChangedEvent, value);
            remove => RemoveHandler(ShowUpDownChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ShowUpDownChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ShowUpDownChangedEvent = EventManager.RegisterRoutedEvent("ShowUpDownChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="AllowNullValue"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> AllowNullValueChanged
        {
            add => AddHandler(AllowNullValueChangedEvent, value);
            remove => RemoveHandler(AllowNullValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="AllowNullValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent AllowNullValueChangedEvent = EventManager.RegisterRoutedEvent("AllowNullValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="DecimalPlaces"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<uint> DecimalPlacesChanged
        {
            add => AddHandler(DecimalPlacesChangedEvent, value);
            remove => RemoveHandler(DecimalPlacesChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="DecimalPlacesChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DecimalPlacesChangedEvent = EventManager.RegisterRoutedEvent("DecimalPlacesChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<uint>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="NegativeForeground"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<SolidColorBrush> NegativeForegroundChanged
        {
            add => AddHandler(NegativeForegroundChangedEvent, value);
            remove => RemoveHandler(NegativeForegroundChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="NegativeForegroundChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent NegativeForegroundChangedEvent = EventManager.RegisterRoutedEvent("NegativeForegroundChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<SolidColorBrush>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="Step"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> StepChanged
        {
            add => AddHandler(StepChangedEvent, value);
            remove => RemoveHandler(StepChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="StepChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent StepChangedEvent = EventManager.RegisterRoutedEvent("StepChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="MinValue"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> MinValueChanged
        {
            add => AddHandler(MinValueChangedEvent, value);
            remove => RemoveHandler(MinValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="MinValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent MinValueChangedEvent = EventManager.RegisterRoutedEvent("MinValueChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="MaxValueChanged"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> MaxValueChanged
        {
            add => AddHandler(MaxValueChangedEvent, value);
            remove => RemoveHandler(MaxValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="MaxValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent MaxValueChangedEvent = EventManager.RegisterRoutedEvent("MaxValueChanged",
            RoutingStrategy.Direct, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="Value"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ValueChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(UpDown));

        /// <summary>
        /// Occurs when the <see cref="ShowTrailingZeros"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> ShowTrailingZerosChanged
        {
            add => AddHandler(ShowTrailingZerosChangedEvent, value);
            remove => RemoveHandler(ShowTrailingZerosChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="ShowTrailingZerosChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ShowTrailingZerosChangedEvent = EventManager.RegisterRoutedEvent("ShowTrailingZerosChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(UpDown));

        #endregion

        #region Callback procedures
        /// <summary>
        /// Invoked just before the <see cref="ShowTrailingZerosChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnShowTrailingZerosChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = ShowTrailingZerosChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnShowTrailingZerosChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown box) return;
            box.OnShowTrailingZerosChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="IsReadOnlyChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = IsReadOnlyChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnIsReadOnlyChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="ShowUpDownChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnShowUpDownChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = ShowUpDownChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnShowUpDownChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnShowUpDownChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="AllowNullValueChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnAllowNullValueChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = AllowNullValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnAllowNullValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnAllowNullValueChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="UseGroupSeparatorChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnUseGroupSeparatorChanged(bool oldValue, bool newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue)
            {
                RoutedEvent = UseGroupSeparatorChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnUseGroupSeparatorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnUseGroupSeparatorChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="NegativeForegroundChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old foreground</param>
        /// <param name="newValue">New foreground</param>
        private void OnNegativeForegroundChanged(SolidColorBrush oldValue, SolidColorBrush newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<SolidColorBrush>(oldValue, newValue)
            {
                RoutedEvent = NegativeForegroundChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnNegativeForegroundChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnNegativeForegroundChanged((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue);
        }
        /// <summary>
        /// Invoked just before the <see cref="DecimalPlacesChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old decimal digits count</param>
        /// <param name="newValue">New decimal digits count</param>
        private void OnDecimalPlacesChanged(uint oldValue, uint newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<uint>(oldValue, newValue)
            {
                RoutedEvent = DecimalPlacesChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnDecimalPlacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnDecimalPlacesChanged((uint)e.OldValue, (uint)e.NewValue);
        }

        /// <summary>
        /// Invoked just before the <see cref="StepChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old step</param>
        /// <param name="newValue">New step</param>
        private void OnStepChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = StepChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnStepChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnStepChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        private static object CoerceStep(DependencyObject d, object value)
        {
            if (d is not UpDown upd) return value;
            var step = Convert.ToDecimal(value);
            step = step < 0 ? Math.Abs(step) : step;
            var fraction = step - decimal.Truncate(step);
            var arr =
                fraction.ToString(CultureInfo.CurrentCulture)
                    .Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToCharArray());
            if (arr.Length == 2 && arr[1].Length > upd.DecimalPlaces)
                upd.DecimalPlaces = (uint)arr[1].Length;
            return step;
        }

        /// <summary>
        /// Invoked just before the <see cref="MinValueChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old min value</param>
        /// <param name="newValue">New min value</param>
        private void OnMinValueChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = MinValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnMinValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.CoerceValue(MaxValueProperty);
            upd.CoerceValue(ValueProperty);
            upd.OnMinValueChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        /// <summary>
        /// Invoked just before the <see cref="MaxValueChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old max value</param>
        /// <param name="newValue">New max value</param>
        private void OnMaxValueChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = MaxValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnMaxValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.CoerceValue(ValueProperty);
            upd.OnMaxValueChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        private static object CoerceMaximum(DependencyObject d, object value)
        {
            var max = Convert.ToDecimal(value);
            if (d is not UpDown upd) return value;
            return max < upd.MinValue ? upd.MinValue : value;
        }

        /// <summary>
        /// Invoked just before the <see cref="ValueChanged"/> event is raised on UpDown
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnValueChanged(decimal oldValue, decimal newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown upd) return;
            upd.OnValueChanged(Convert.ToDecimal(e.OldValue), Convert.ToDecimal(e.NewValue));
        }

        private static object ConstraintValue(DependencyObject d, object value)
        {
            var newValue = Convert.ToDecimal(value);
            if (d is not UpDown upd) return value;
            if (newValue < upd.MinValue) return upd.MinValue;
            return newValue > upd.MaxValue ? upd.MaxValue : value;
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_textBox != null)
            {
                _textBox.GotFocus -= TextBox_GotFocus;
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.PreviewMouseRightButtonUp -= TextBox_PreviewMouseRightButtonUp;
                _textBox.TextChanged -= TextBox_TextChanged;
                _textBox.PreviewTextInput -= _textBox_PreviewTextInput;
                _textBox.LostFocus -= _textBox_LostFocus;
                _textBox.PreviewMouseLeftButtonDown -= _textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Clear();
            }
            _textBox = GetTemplateChild(ElementText) as TextBox;
            if (_textBox != null)
            {
                _textBox.GotFocus += TextBox_GotFocus;
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                _textBox.TextChanged += TextBox_TextChanged;
                _textBox.PreviewTextInput += _textBox_PreviewTextInput;
                _textBox.LostFocus += _textBox_LostFocus;
                _textBox.PreviewMouseLeftButtonDown += _textBox_PreviewMouseLeftButtonDown;
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, pasteCommandBinding));
                _textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, cutCommandBinding));
            }

            if (_downButton != null)
            {
                _downButton.Click -= DownButton_Click;
            }
            _downButton = GetTemplateChild(ElementButtonDown) as RepeatButton;
            if (_downButton != null)
            {
                _downButton.Click += DownButton_Click;
            }

            if (_upButton != null)
            {
                _upButton.Click -= UpButton_Click;
            }
            _upButton = GetTemplateChild(ElementButtonUp) as RepeatButton;
            if (_upButton != null)
            {
                _upButton.Click += UpButton_Click;
            }
        }
        #endregion

        #region Private event handlers
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            AddStep(true);
            if (!_textBox.IsFocused)
                _textBox.Focus();
            else
                _textBox.SelectAll();
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            AddStep(false);
            if (!_textBox.IsFocused)
                _textBox.Focus();
            else
                _textBox.SelectAll();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = true;
            _textBox.SelectAll();
        }

        private void _textBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                _textBox.SelectAll();
        }

        private void _textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _gotFocus = false;
            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
            {
                Value = null;
            }
        }

        private void _textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                _gotFocus = false;
                if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator)
                {
                    e.Handled = true;
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                {
                    if (DecimalPlaces == 0)
                    {
                        e.Handled = true;
                        return;
                    }

                    if (ShowTrailingZeros)
                    {
                        if (_textBox.Text != CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                        {
                            _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                            e.Handled = true;
                        }
                        else
                        {
                            _position.Key = CurrentKey.Decimal;
                        }
                    }
                    else
                    {
                        if (_textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) == -1)
                        {
                            _position.Key = CurrentKey.Decimal;
                        }
                        else
                        {
                            _textBox.CaretIndex = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + 1;
                            e.Handled = true;
                        }
                    }
                    return;
                }
                else if (e.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign)
                {
                    if (_textBox.SelectionLength == _textBox.Text.Length)
                    {
                        return;
                    }
                    if (_textBox.CaretIndex > 0)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else if (!e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9"))
                {
                    e.Handled = true;
                    return;
                }

                if (ShowTrailingZeros && e.Text.In("0", "1", "2", "3", "4", "5", "6", "7", "8", "9")
                    && _textBox.Text.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    && _textBox.CaretIndex == _textBox.Text.Length)
                {
                    e.Handled = true;
                    return;
                }
                else
                {
                    _position.Key = CurrentKey.Number;
                }
            }
            finally
            {
                if (!e.Handled)
                {
                    setPositionOffset();
                }
            }
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!ShowTrailingZeros)
            {
                if (_gotFocus)
                {
                    _textBox.SelectAll();
                    _gotFocus = false;
                }
                return;
            }

            if (_position.Exclude)
                return;
            if (_position.Key.In(CurrentKey.Number, CurrentKey.Back, CurrentKey.Decimal))
            {
                if (_textBox.Text.Length >= _position.Offset)
                {
                    _textBox.CaretIndex = _textBox.Text.Length - _position.Offset;
                }
            }
        }

        private void TextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _gotFocus = false;
            _position.Key = CurrentKey.None;
            _position.Offset = 0;
            _position.Exclude = false;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (e.Key != Key.Home && e.Key != Key.End)
                    e.Handled = true;
                return;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!e.Key.In(Key.Home, Key.End, Key.A, Key.C, Key.V, Key.X, Key.Z, Key.Y))
                    e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.Right:
                case Key.Home:
                case Key.End:
                    break;
                case Key.Up:
                    AddStep(true);
                    _textBox.SelectAll();
                    e.Handled = true;
                    break;
                case Key.Down:
                    AddStep(false);
                    _textBox.SelectAll();
                    e.Handled = true;
                    break;
                case Key.Delete:
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 0 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if ((DecimalPlaces > 0
                        && _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                                StringComparison.Ordinal))
                                || _textBox.CaretIndex == _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator,
                                StringComparison.Ordinal))
                    {
                        _textBox.CaretIndex++;
                        e.Handled = true;
                        break;
                    }
                    break;
                case Key.Back:
                    _position.Key = CurrentKey.Back;
                    if ((_textBox.SelectionLength == _textBox.Text.Length) || (_textBox.CaretIndex == 1 && _textBox.Text.Length == 1))
                    {
                        Value = null;
                        e.Handled = true;
                        break;
                    }
                    if (DecimalPlaces > 0
                        && _textBox.CaretIndex ==
                        _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                            StringComparison.Ordinal) + 1)
                    {
                        _textBox.CaretIndex--;
                        e.Handled = true;
                        break;
                    }
                    setPositionOffset();
                    break;
            }
        }
        #endregion

        #region Private procedures

        private void setPositionOffset()
        {
            if (!ShowTrailingZeros) return;
            if ((_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key != CurrentKey.Decimal) || _textBox.Text.Length == _textBox.SelectionLength || Value == null)
            {
                _position.Exclude = true;
            }

            if (_textBox.Text == CultureInfo.CurrentCulture.NumberFormat.NegativeSign && _position.Key == CurrentKey.Decimal)
            {
                if (DecimalPlaces > 0)
                {
                    _position.Offset = (int)DecimalPlaces;
                    return;
                }
            }

            var sepPos = _textBox.Text.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            _position.Offset = _textBox.Text.Length == _textBox.SelectionLength
                ? _textBox.Text.Length - 1
                : sepPos == -1
                    ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                    : _textBox.CaretIndex <= sepPos
                        ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength)
                        : _position.Key == CurrentKey.Number
                            ? _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) - 1
                            : _textBox.Text.Length - (_textBox.CaretIndex + _textBox.SelectionLength) + 1;
        }

        private void AddStep(bool plus)
        {
            if (plus)
            {
                if (Value == null)
                    Value = Step;
                else if (Value + Step <= MaxValue)
                    Value += Step;
            }
            else
            {
                if (Value == null)
                    Value = -Step;
                else if (Value - Step >= MinValue)
                    Value -= Step;
            }
        }

        private void cutCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            Clipboard.SetText(_textBox.SelectedText);
            if (_textBox.SelectionLength != _textBox.Text.Length)
                _textBox.Text = _textBox.Text.Substring(0, _textBox.SelectionStart) + _textBox.Text.Substring(_textBox.SelectionStart + _textBox.SelectionLength);
            else
                Value = null;
        }

        private void pasteCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            _position.Offset = 0;
            _position.Exclude = false;
            _position.Key = CurrentKey.None;

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!decimal.TryParse(text, out _))
                {
                    e.Handled = true;
                }
                else
                {
                    _position.Key = CurrentKey.Number;
                    setPositionOffset();
                    if (_textBox.SelectionLength > 0)
                        _textBox.SelectedText = text;
                    else
                        _textBox.Text = _textBox.Text.Insert(_textBox.CaretIndex, text);
                }
            }
            else
            {
                e.Handled = true;
            }
        }
        #endregion
    }

    internal static class Extensions
    {
        internal static bool In<T>(this T obj, params T[] values) => values.Contains(obj);
    }

    /// <summary>
    /// 
    /// </summary>
    public class UpDownForegroundConverter : IMultiValueConverter
    {
        /// <summary>
        /// Determines UpDown foreground.
        /// </summary>
        /// <param name="values">Array consists of current UpDown value, regular foreground brush and negative foreground brush</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Brush depended on current value sign.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not decimal decimalValue || values[1] is not Brush foregroundBrush || values[2] is not Brush negativeBrush) return null;
            return decimalValue >= 0 ? foregroundBrush : negativeBrush;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">Not used.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Not used.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts decimal value to string.
    /// </summary>
    public class UpDownTextToValueConverter : IMultiValueConverter
    {
        private const decimal EPSILON = 0.0000000000000000000000000001m;
        private string _textValue;

        private string getRealFractionString(decimal value, CultureInfo culture)
        {
            var arr = value.ToString().Split(culture.NumberFormat.NumberDecimalSeparator[0]);
            if (arr.Length == 2)
                return arr[1];
            return null;
        }

        private object[] getDecimalFromString(string stringValue)
        {
            if (double.TryParse(stringValue, out double doubleValue))
            {
                if (doubleValue <= (double)decimal.MaxValue && doubleValue >= (double)decimal.MinValue)
                    return new object[] { decimal.Parse(stringValue, NumberStyles.Any) };
                else if (doubleValue > (double)decimal.MaxValue)
                    return new object[] { decimal.MaxValue };
                else
                    return new object[] { decimal.MinValue };
            }
            return null;
        }

        /// <summary>
        /// Converts decimal value to string.
        /// </summary>
        /// <param name="values">Array consists of current UpDown value, decimal places and separator using flag.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Formatted string.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not decimal decimalValue || values[1] is not uint decimalPlaces || values[2] is not bool useSeparator) return "";

            var addMinus = false;
            var showTrailing = true;
            var isFocused = false;
            if (values.Length > 3 && values[3] is bool bl && !bl)
                showTrailing = false;
            if (values.Length > 4 && values[4] is bool fc)
                isFocused = fc;

            if (decimalValue == EPSILON)
            {
                var text = _textValue;
                if (!showTrailing)
                {
                    var arr = text.Split(culture.NumberFormat.NumberDecimalSeparator[0]);
                    if (arr.Length == 2 && !string.IsNullOrEmpty(arr[1]) && arr[1].Length >= decimalPlaces)
                    {
                        text = $"{arr[0]}{culture.NumberFormat.NumberDecimalSeparator}{arr[1].TrimEnd('0')}";
                    }
                    return text;
                }
                else
                {
                    if (text == culture.NumberFormat.NegativeSign)
                    {
                        return text;
                    }
                    else
                    {
                        addMinus = true;
                        decimalValue = 0;
                    }
                }
            }
            else if (decimalValue == -EPSILON)
                return null;

            var partInt = decimal.Truncate(decimalValue);
            var partFraction =
                Math.Abs(decimal.Truncate((decimalValue - partInt) * (int)Math.Pow(10.0, decimalPlaces)));
            var formatInt = useSeparator ? "#" + culture.NumberFormat.NumberGroupSeparator + "##0" : "##0";
            var formatFraction = new string('0', (int)decimalPlaces);
            var stringInt = partInt.ToString(formatInt);
            var stringFraction = partFraction.ToString(formatFraction);
            if (!showTrailing && stringFraction.EndsWith("0"))
            {
                var realDecimalString = getRealFractionString(decimalValue, culture);
                if (realDecimalString == null || realDecimalString.Length >= decimalPlaces)
                {
                    stringFraction = stringFraction.TrimEnd('0');
                }
                else
                {
                    stringFraction = realDecimalString;
                }
            }
            if ((decimalValue < 0 && partInt == 0) || addMinus)
                stringInt = $"{CultureInfo.CurrentCulture.NumberFormat.NegativeSign}{stringInt}";
            var result = decimalPlaces > 0
                ? string.IsNullOrEmpty(stringFraction) && !isFocused ? stringInt : $"{stringInt}{culture.NumberFormat.NumberDecimalSeparator}{stringFraction}"
                : stringInt;
            return result;
        }

        /// <summary>
        /// Converts string to decimal.
        /// </summary>
        /// <param name="value">String.</param>
        /// <param name="targetTypes">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Decimal.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            _textValue = null;
            if (value is not string stringValue) return null;
            if (!string.IsNullOrEmpty(stringValue))
                stringValue = stringValue.Replace(culture.NumberFormat.NumberGroupSeparator, "");
            else
                return null;
            object[] result;
            if (stringValue != culture.NumberFormat.NegativeSign)
            {
                if (stringValue == $"{culture.NumberFormat.NegativeSign}{culture.NumberFormat.NumberDecimalSeparator}")
                {
                    result = new object[] { -EPSILON };
                }
                else if (stringValue == culture.NumberFormat.NumberDecimalSeparator)
                {
                    result = new object[] { -EPSILON };
                }
                else if (stringValue == $"{culture.NumberFormat.NegativeSign}0")
                {
                    _textValue = stringValue;
                    result = new object[] { EPSILON };
                }
                else if (stringValue.StartsWith($"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}"))
                {
                    if (stringValue == $"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}")
                    {
                        _textValue = stringValue;
                        result = new object[] { EPSILON };
                    }
                    else
                    {
                        var arr = stringValue.Split(culture.NumberFormat.NumberDecimalSeparator[0]);
                        if (arr.Length == 2 && arr[1].All(c => c == '0'))
                        {
                            _textValue = $"{culture.NumberFormat.NegativeSign}0{culture.NumberFormat.NumberDecimalSeparator}{arr[1]}";
                            result = new object[] { EPSILON };
                        }
                        else
                        {
                            result = getDecimalFromString(stringValue);
                        }
                    }
                }
                else
                {
                    result = getDecimalFromString(stringValue);
                }
            }
            else
            {
                _textValue = stringValue;
                result = new object[] { EPSILON };
            }
            return result;
        }
#nullable restore
    }
}
