using ag.WPF.NumericBox;
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
    [TemplatePart(Name = ElementNum, Type = typeof(NumericBox.NumericBox))]
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
        private const string ElementNum = "PART_Num";
        private const string ElementButtonUp = "PART_Up";
        private const string ElementButtonDown = "PART_Down";
        #endregion

        #region Elements
        private NumericBox.NumericBox _numericBox;
        private RepeatButton _upButton;
        private RepeatButton _downButton;
        #endregion

        #region Dependency properties
        /// <summary>
        /// The identifier of the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?), typeof(UpDown),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, ConstraintValue));
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
        /// The identifier of the <see cref="ShowTrailingZeros"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTrailingZerosProperty = DependencyProperty.Register(nameof(ShowTrailingZeros), typeof(bool), typeof(UpDown),
                new FrameworkPropertyMetadata(true, OnShowTrailingZerosChanged));
        // <summary>
        /// The identifier of the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(UpDown),
                new FrameworkPropertyMetadata("", OnTextChanged));
        /// <summary>
        /// The identifier of the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(UpDown),
                new FrameworkPropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));
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
        /// <summary>
        /// Gets or sets the text of UpDown.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                if (_numericBox != null)
                {
                    _numericBox.Text = value;
                }
            }
        }
        /// <summary>
        /// Gets or sets the text alignment of NumericBox.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }
        #endregion

        #region Routed events
        /// <summary>
        /// Occurs when the <see cref="TextAlignment"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TextAlignment> TextAlignmentChanged
        {
            add => AddHandler(TextAlignmentChangedEvent, value);
            remove => RemoveHandler(TextAlignmentChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextAlignmentChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextAlignmentChangedEvent = EventManager.RegisterRoutedEvent("TextAlignmentChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<TextAlignment>), typeof(UpDown));

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
        /// Occurs when the <see cref="Text"/> property has been changed in some way.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<string> TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }
        /// <summary>
        /// Identifies the <see cref="TextChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent("TextChanged",
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<string>), typeof(UpDown));

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
        /// Invoked just before the <see cref="TextAlignmentChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextAlignmentChanged(TextAlignment oldValue, TextAlignment newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<TextAlignment>(oldValue, newValue)
            {
                RoutedEvent = TextAlignmentChangedEvent
            };
            RaiseEvent(e);
        }

        private static void OnTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown box) return;
            box.OnTextAlignmentChanged((TextAlignment)(e.OldValue), (TextAlignment)(e.NewValue));
        }

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
        /// Invoked just before the <see cref="TextChanged"/> event is raised on NumericBox
        /// </summary>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        private void OnTextChanged(string oldValue, string newValue)
        {
            var e = new RoutedPropertyChangedEventArgs<string>(oldValue, newValue)
            {
                RoutedEvent = TextChangedEvent
            };
            RaiseEvent(e);
        }
        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not UpDown box) return;
            box.OnTextChanged((string)e.OldValue, (string)e.NewValue);
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
            if (value is null) return value;
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
            if (_numericBox != null)
            {
                BindingOperations.ClearAllBindings(_numericBox);
                BindingOperations.ClearBinding(this, TextProperty);
                _numericBox.PreviewKeyDown -= _numericBox_PreviewKeyDown;
            }
            _numericBox = GetTemplateChild(ElementNum) as NumericBox.NumericBox;
            if (_numericBox != null)
            {
                _numericBox.SetBinding(NumericBox.NumericBox.DecimalPlacesProperty, new Binding(nameof(DecimalPlaces)) { Source = this });
                _numericBox.SetBinding(NumericBox.NumericBox.ValueProperty, new Binding(nameof(Value)) { Source = this, Mode = BindingMode.TwoWay });
                _numericBox.SetBinding(NumericBox.NumericBox.UseGroupSeparatorProperty, new Binding(nameof(UseGroupSeparator)) { Source = this });
                _numericBox.SetBinding(NumericBox.NumericBox.ShowTrailingZerosProperty, new Binding(nameof(ShowTrailingZeros)) { Source = this });
                _numericBox.SetBinding(NumericBox.NumericBox.IsReadOnlyProperty, new Binding(nameof(IsReadOnly)) { Source = this });
                _numericBox.SetBinding(NumericBox.NumericBox.NegativeForegroundProperty, new Binding(nameof(NegativeForeground)) { Source = this });
                _numericBox.SetBinding(TextProperty, new Binding(nameof(Text)) { Source = _numericBox, Mode = BindingMode.OneWay });
                _numericBox.SetBinding(NumericBox.NumericBox.TextAlignmentProperty, new Binding(nameof(TextAlignment)) { Source = this });
                _numericBox.PreviewKeyDown += _numericBox_PreviewKeyDown;
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

        /// <summary>
        /// Is invoked when the control gains focus.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            _numericBox?.Focus();
        }
        #endregion

        #region Private event handlers

        private void _numericBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                addStep(true);
            }
            else if (e.Key == Key.Down)
            {
                addStep(false);
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            addStep(true);
            if (!_numericBox.IsFocused)
                _numericBox.Focus();
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            addStep(false);
            if (!_numericBox.IsFocused)
                _numericBox.Focus();
        }

        #endregion

        #region Private procedures

        private void addStep(bool plus)
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
        #endregion
    }
}
