using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace MahApps.Metro.Controls
{
    /// <summary>
    ///     Represents a base-class for time picking.
    /// </summary>
    [TemplatePart(Name = ElementButton, Type = typeof(Button))]
    [TemplatePart(Name = ElementHourHand, Type = typeof(UIElement))]
    [TemplatePart(Name = ElementHourPicker, Type = typeof(Selector))]
    [TemplatePart(Name = ElementMinuteHand, Type = typeof(UIElement))]
    [TemplatePart(Name = ElementSecondHand, Type = typeof(UIElement))]
    [TemplatePart(Name = ElementSecondPicker, Type = typeof(Selector))]
    [TemplatePart(Name = ElementMinutePicker, Type = typeof(Selector))]
    [TemplatePart(Name = ElementAmPmSwitcher, Type = typeof(Selector))]
    [TemplatePart(Name = ElementTextBox, Type = typeof(DatePickerTextBox))]
    [TemplatePart(Name = ElementPopup, Type = typeof(Popup))]
    [DefaultEvent("SelectedDateTimeChanged")]
    public abstract class TimePickerBase : Control
    {
        private const string ElementAmPmSwitcher = "PART_AmPmSwitcher";
        private const string ElementButton = "PART_Button";
        private const string ElementHourHand = "PART_HourHand";
        private const string ElementHourPicker = "PART_HourPicker";
        private const string ElementMinuteHand = "PART_MinuteHand";
        private const string ElementMinutePicker = "PART_MinutePicker";
        private const string ElementPopup = "PART_Popup";
        private const string ElementSecondHand = "PART_SecondHand";
        private const string ElementSecondPicker = "PART_SecondPicker";
        private const string ElementTextBox = "PART_TextBox";

        private Selector _ampmSwitcher;
        private Button _dropDownButton;
        private bool _deactivateRangeBaseEvent;
        private bool _deactivateTextChangedEvent;
        private bool _textInputChanged;
        private UIElement _hourHand;
        public Selector _hourInput;
        private UIElement _minuteHand;
        private Selector _minuteInput;
        protected Popup _popUp;
        private bool _disablePopupReopen;
        private UIElement _secondHand;
        private Selector _secondInput;
        protected DatePickerTextBox _textBox;
        protected DateTime? _originalSelectedDateTime;

        public static readonly DependencyProperty SourceHoursProperty = DependencyProperty.Register(
          "SourceHours",
          typeof(IEnumerable<int>),
          typeof(TimePickerBase),
          new FrameworkPropertyMetadata(Enumerable.Range(0, 24), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceSourceHours));

        public static readonly DependencyProperty SourceMinutesProperty = DependencyProperty.Register(
            "SourceMinutes",
            typeof(IEnumerable<int>),
            typeof(TimePickerBase),
            new FrameworkPropertyMetadata(Enumerable.Range(0, 60), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceSource60));

        public static readonly DependencyProperty SourceSecondsProperty = DependencyProperty.Register(
            "SourceSeconds",
            typeof(IEnumerable<int>),
            typeof(TimePickerBase),
            new FrameworkPropertyMetadata(Enumerable.Range(0, 60), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, CoerceSource60));

        public static readonly DependencyProperty IsDropDownOpenProperty
            = DatePicker.IsDropDownOpenProperty.AddOwner(typeof(TimePickerBase),
                                                         new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged, OnCoerceIsDropDownOpen));

        private static object OnCoerceIsDropDownOpen(DependencyObject d, object baseValue)
        {
            if (d is TimePickerBase tp && !tp.IsEnabled)
            {
                return false;
            }

            return baseValue;
        }

        /// <summary>
        /// IsDropDownOpenProperty property changed handler.
        /// </summary>
        /// <param name="d">DatePicker that changed its IsDropDownOpen.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs.</param>
        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TimePickerBase tp))
            {
                return;
            }

            bool newValue = (bool)e.NewValue;
            if (tp._popUp != null && tp._popUp.IsOpen != newValue)
            {
                tp._popUp.IsOpen = newValue;
                if (newValue)
                {
                    tp._originalSelectedDateTime = tp.SelectedDateTime;
                    
                    tp.FocusElementAfterIsDropDownOpenChanged();
                }
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePickerBase tp)
            {
                tp.CoerceValue(IsDropDownOpenProperty);
            }
        }

        /// <summary>
        /// This method is invoked when the <see cref="IsDropDownOpenProperty"/> changes.
        /// </summary>
        protected virtual void FocusElementAfterIsDropDownOpenChanged()
        {
            // noting here
        }

        public static readonly DependencyProperty IsClockVisibleProperty = DependencyProperty.Register(
            "IsClockVisible",
            typeof(bool),
            typeof(TimePickerBase),
            new PropertyMetadata(true));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            "IsReadOnly",
            typeof(bool),
            typeof(TimePickerBase),
            new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty HandVisibilityProperty = DependencyProperty.Register(
            "HandVisibility",
            typeof(TimePartVisibility),
            typeof(TimePickerBase),
            new PropertyMetadata(TimePartVisibility.All, OnHandVisibilityChanged));

        public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
            "Culture",
            typeof(CultureInfo),
            typeof(TimePickerBase),
            new PropertyMetadata(null, OnCultureChanged));

        public static readonly DependencyProperty PickerVisibilityProperty = DependencyProperty.Register(
            "PickerVisibility",
            typeof(TimePartVisibility),
            typeof(TimePickerBase),
            new PropertyMetadata(TimePartVisibility.All, OnPickerVisibilityChanged));

        public static readonly RoutedEvent SelectedDateTimeChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectedDateTimeChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<DateTime?>),
            typeof(TimePickerBase));

        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register(
            "SelectedDateTime",
            typeof(DateTime?),
            typeof(TimePickerBase),
            new FrameworkPropertyMetadata(default(DateTime?), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

        public static readonly DependencyProperty SelectedTimeFormatProperty = DependencyProperty.Register(
            nameof(SelectedTimeFormat),
            typeof(TimePickerFormat),
            typeof(TimePickerBase),
            new PropertyMetadata(TimePickerFormat.Long, OnSelectedTimeFormatChanged));

        public static readonly DependencyProperty HoursItemStringFormatProperty = DependencyProperty.Register(nameof(HoursItemStringFormat), typeof(string), typeof(TimePickerBase), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty MinutesItemStringFormatProperty = DependencyProperty.Register(nameof(MinutesItemStringFormat), typeof(string), typeof(TimePickerBase), new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SecondsItemStringFormatProperty = DependencyProperty.Register(nameof(SecondsItemStringFormat), typeof(string), typeof(TimePickerBase), new FrameworkPropertyMetadata(null));

        #region Do not change order of fields inside this region

        /// <summary>
        /// This readonly dependency property is to control whether to show the date-picker (in case of <see cref="DateTimePicker"/>) or hide it (in case of <see cref="TimePicker"/>.
        /// </summary>
        private static readonly DependencyPropertyKey IsDatePickerVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
          "IsDatePickerVisible", typeof(bool), typeof(TimePickerBase), new PropertyMetadata(true));

        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess",Justification = "Otherwise we have \"Static member initializer refers to static member below or in other type part\" and thus resulting in having \"null\" as value")]
        public static readonly DependencyProperty IsDatePickerVisibleProperty = IsDatePickerVisiblePropertyKey.DependencyProperty;

        #endregion

        /// <summary>
        /// This list contains values from 0 to 55 with an interval of 5. It can be used to bind to <see cref="SourceMinutes"/> and <see cref="SourceSeconds"/>.
        /// </summary>
        /// <example>
        /// <code>&lt;MahApps:TimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf5}" /&gt;</code>
        /// <code>&lt;MahApps:DateTimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf5}" /&gt;</code>
        /// </example>
        /// <returns>
        /// Returns a list containing {0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55}.
        /// </returns>
        public static readonly IEnumerable<int> IntervalOf5 = CreateValueList(5);

        /// <summary>
        /// This list contains values from 0 to 50 with an interval of 10. It can be used to bind to <see cref="SourceMinutes"/> and <see cref="SourceSeconds"/>.
        /// </summary>
        /// <example>
        /// <code>&lt;MahApps:TimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf10}" /&gt;</code>
        /// <code>&lt;MahApps:DateTimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf10}" /&gt;</code>
        /// </example>
        /// <returns>
        /// Returns a list containing {0, 10, 20, 30, 40, 50}.
        /// </returns>
        public static readonly IEnumerable<int> IntervalOf10 = CreateValueList(10);

        /// <summary>
        /// This list contains values from 0 to 45 with an interval of 15. It can be used to bind to <see cref="SourceMinutes"/> and <see cref="SourceSeconds"/>.
        /// </summary>
        /// <example>
        /// <code>&lt;MahApps:TimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf15}" /&gt;</code>
        /// <code>&lt;MahApps:DateTimePicker SourceSeconds="{x:Static MahApps:TimePickerBase.IntervalOf15}" /&gt;</code>
        /// </example>
        /// <returns>
        /// Returns a list containing {0, 15, 30, 45}.
        /// </returns>
        public static readonly IEnumerable<int> IntervalOf15 = CreateValueList(15);

        static TimePickerBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(typeof(TimePickerBase)));
            EventManager.RegisterClassHandler(typeof(TimePickerBase), UIElement.GotFocusEvent, new RoutedEventHandler(OnGotFocus));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(VerticalAlignment.Center));
            LanguageProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(OnLanguageChanged));
            IsEnabledProperty.OverrideMetadata(typeof(TimePickerBase), new UIPropertyMetadata(OnIsEnabledChanged));
        }

        protected TimePickerBase()
        {
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, this.OutsideCapturedElementHandler);
        }

        /// <summary>
        ///     Occurs when the <see cref="SelectedDateTime" /> property is changed.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<DateTime?> SelectedDateTimeChanged
        {
            add { AddHandler(SelectedDateTimeChangedEvent, value); }
            remove { RemoveHandler(SelectedDateTimeChangedEvent, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating the culture to be used in string formatting operations.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(null)]
        public CultureInfo Culture
        {
            get { return (CultureInfo)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating the visibility of the clock hands in the user interface (UI).
        /// </summary>
        /// <returns>
        ///     The visibility definition of the clock hands. The default is <see cref="TimePartVisibility.All" />.
        /// </returns>
        [Category("Appearance")]
        [DefaultValue(TimePartVisibility.All)]
        public TimePartVisibility HandVisibility
        {
            get { return (TimePartVisibility)GetValue(HandVisibilityProperty); }
            set { SetValue(HandVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the date can be selected or not. This property is read-only.
        /// </summary>
        public bool IsDatePickerVisible
        {
            get { return (bool)GetValue(IsDatePickerVisibleProperty); }
            protected set { SetValue(IsDatePickerVisiblePropertyKey, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the clock of this control is visible in the user interface (UI). This is a
        ///     dependency property.
        /// </summary>
        /// <remarks>
        ///     If this value is set to false then <see cref="Orientation" /> is set to
        ///     <see cref="System.Windows.Controls.Orientation.Vertical" />
        /// </remarks>
        /// <returns>
        ///     true if the clock is visible; otherwise, false. The default value is true.
        /// </returns>
        [Category("Appearance")]
        public bool IsClockVisible
        {
            get { return (bool)GetValue(IsClockVisibleProperty); }
            set { SetValue(IsClockVisibleProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the drop-down for a <see cref="TimePickerBase"/> box is currently
        ///         open.
        /// </summary>
        /// <returns>true if the drop-down is open; otherwise, false. The default is false.</returns>
        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the contents of the <see cref="TimePickerBase" /> are not editable.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="TimePickerBase" /> is read-only; otherwise, false. The default is false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a value indicating the visibility of the selectable date-time-parts in the user interface (UI).
        /// </summary>
        /// <returns>
        ///     visibility definition of the selectable date-time-parts. The default is <see cref="TimePartVisibility.All" />.
        /// </returns>
        [Category("Appearance")]
        [DefaultValue(TimePartVisibility.All)]
        public TimePartVisibility PickerVisibility
        {
            get { return (TimePartVisibility)GetValue(PickerVisibilityProperty); }
            set { SetValue(PickerVisibilityProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the currently selected date and time.
        /// </summary>
        /// <returns>
        ///     The date and time which is currently selected. The default is null.
        /// </returns>
        public DateTime? SelectedDateTime
        {
            get { return (DateTime?)GetValue(SelectedDateTimeProperty); }
            set { SetValue(SelectedDateTimeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the format that is used to display the selected time.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(TimePickerFormat.Long)]
        public TimePickerFormat SelectedTimeFormat
        {
            get { return (TimePickerFormat)GetValue(SelectedTimeFormatProperty); }
            set { SetValue(SelectedTimeFormatProperty, value); }
        }

        public string HoursItemStringFormat
        {
            get { return (string)GetValue(HoursItemStringFormatProperty); }
            set { SetValue(HoursItemStringFormatProperty, value); }
        }

        public string MinutesItemStringFormat
        {
            get { return (string)GetValue(MinutesItemStringFormatProperty); }
            set { SetValue(MinutesItemStringFormatProperty, value); }
        }

        public string SecondsItemStringFormat
        {
            get { return (string)GetValue(SecondsItemStringFormatProperty); }
            set { SetValue(SecondsItemStringFormatProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a collection used to generate the content for selecting the hours.
        /// </summary>
        /// <returns>
        ///     A collection that is used to generate the content for selecting the hours. The default is a list of interger from 0
        ///     to 23 if <see cref="IsMilitaryTime" /> is false or a list of interger from
        ///     1 to 12 otherwise..
        /// </returns>
        [Category("Common")]
        public IEnumerable<int> SourceHours
        {
            get { return (IEnumerable<int>)GetValue(SourceHoursProperty); }
            set { SetValue(SourceHoursProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a collection used to generate the content for selecting the minutes.
        /// </summary>
        /// <returns>
        ///     A collection that is used to generate the content for selecting the minutes. The default is a list of int from
        ///     0 to 59.
        /// </returns>
        [Category("Common")]
        public IEnumerable<int> SourceMinutes
        {
            get { return (IEnumerable<int>)GetValue(SourceMinutesProperty); }
            set { SetValue(SourceMinutesProperty, value); }
        }

        /// <summary>
        ///     Gets or sets a collection used to generate the content for selecting the seconds.
        /// </summary>
        /// <returns>
        ///     A collection that is used to generate the content for selecting the minutes. The default is a list of int from
        ///     0 to 59.
        /// </returns>
        [Category("Common")]
        public IEnumerable<int> SourceSeconds
        {
            get { return (IEnumerable<int>)GetValue(SourceSecondsProperty); }
            set { SetValue(SourceSecondsProperty, value); }
        }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="DateTimeFormatInfo.AMDesignator" /> that is specified by the
        ///     <see cref="CultureInfo" />
        ///     set by the <see cref="Culture" /> (<see cref="FrameworkElement.Language" /> if null) has not a value.
        /// </summary>
        public bool IsMilitaryTime
        {
            get
            {
                var dateTimeFormat = this.SpecificCultureInfo.DateTimeFormat;
                return !string.IsNullOrEmpty(dateTimeFormat.AMDesignator) && (dateTimeFormat.ShortTimePattern.Contains("h") || dateTimeFormat.LongTimePattern.Contains("h"));
            }
        }

        protected CultureInfo SpecificCultureInfo
        {
            get { return Culture ?? Language.GetSpecificCulture(); }
        }

        /// <summary>
        ///     When overridden in a derived class, is invoked whenever application code or internal processes call
        ///     <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            UnSubscribeEvents();

            base.OnApplyTemplate();

            this._popUp = GetTemplateChild(ElementPopup) as Popup;

            this._dropDownButton = GetTemplateChild(ElementButton) as Button;
            _hourInput = GetTemplateChild(ElementHourPicker) as Selector;
            _minuteInput = GetTemplateChild(ElementMinutePicker) as Selector;
            _secondInput = GetTemplateChild(ElementSecondPicker) as Selector;
            _hourHand = GetTemplateChild(ElementHourHand) as FrameworkElement;
            _ampmSwitcher = GetTemplateChild(ElementAmPmSwitcher) as Selector;
            _minuteHand = GetTemplateChild(ElementMinuteHand) as FrameworkElement;
            _secondHand = GetTemplateChild(ElementSecondHand) as FrameworkElement;
            _textBox = GetTemplateChild(ElementTextBox) as DatePickerTextBox;

            SetHandVisibility(HandVisibility);
            SetPickerVisibility(PickerVisibility);

            SetHourPartValues(SelectedDateTime.GetValueOrDefault().TimeOfDay);
            WriteValueToTextBox();

            SetDefaultTimeOfDayValues();
            SubscribeEvents();
            ApplyCulture();
        }

        protected virtual void ApplyCulture()
        {
            _deactivateRangeBaseEvent = true;

            if (_ampmSwitcher != null)
            {
                _ampmSwitcher.Items.Clear();
                if (!string.IsNullOrEmpty(SpecificCultureInfo.DateTimeFormat.AMDesignator))
                {
                    _ampmSwitcher.Items.Add(SpecificCultureInfo.DateTimeFormat.AMDesignator);
                }

                if (!string.IsNullOrEmpty(SpecificCultureInfo.DateTimeFormat.PMDesignator))
                {
                    _ampmSwitcher.Items.Add(SpecificCultureInfo.DateTimeFormat.PMDesignator);
                }
            }

            SetAmPmVisibility();

            CoerceValue(SourceHoursProperty);

            if (SelectedDateTime.HasValue)
            {
                SetHourPartValues(SelectedDateTime.Value.TimeOfDay);
            }

            SetDefaultTimeOfDayValues();

            _deactivateRangeBaseEvent = false;

            WriteValueToTextBox();
        }

        protected Binding GetBinding(DependencyProperty property, BindingMode bindingMode = BindingMode.Default)
        {
            return new Binding(property.Name) { Source = this, Mode = bindingMode };
        }
        
        protected virtual string GetValueForTextBox()
        {
            var format = SelectedTimeFormat == TimePickerFormat.Long ? string.Intern(SpecificCultureInfo.DateTimeFormat.LongTimePattern) : string.Intern(SpecificCultureInfo.DateTimeFormat.ShortTimePattern);
            var valueForTextBox = SelectedDateTime?.ToString(string.Intern(format), SpecificCultureInfo);
            return valueForTextBox;
        }

        protected virtual void ClockSelectedTimeChanged()
        {
            var time = this.GetSelectedTimeFromGUI() ?? TimeSpan.Zero;
            var date = SelectedDateTime ?? DateTime.Today;

            this.SetCurrentValue(SelectedDateTimeProperty, date.Date + time);
        }

        protected void RaiseSelectedDateTimeChangedEvent(DateTime? oldValue, DateTime? newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<DateTime?>(oldValue, newValue) { RoutedEvent = SelectedDateTimeChangedEvent };
            this.RaiseEvent(args);
        }

        private static void OnSelectedTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePickerBase tp)
            {
                tp.WriteValueToTextBox();
            }
        }

        protected void SetDefaultTimeOfDayValues()
        {
            SetDefaultTimeOfDayValue(_hourInput);
            SetDefaultTimeOfDayValue(_minuteInput);
            SetDefaultTimeOfDayValue(_secondInput);
            SetDefaultTimeOfDayValue(_ampmSwitcher);
        }

        private void SubscribeEvents()
        {
            if (_popUp != null)
            {
                _popUp.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PopUp_PreviewMouseLeftButtonDown));
                _popUp.Opened += PopUp_Opened;
                _popUp.Closed += PopUp_Closed;

                if (this.IsDropDownOpen)
                {
                    this._popUp.IsOpen = true;
                }
            }
            
            this.SubscribeTimePickerEvents(_hourInput, _minuteInput, _secondInput, _ampmSwitcher);

            if (this._dropDownButton != null)
            {
                this._dropDownButton.Click += this.OnDropDownButtonClicked;
                _dropDownButton.AddHandler(MouseLeaveEvent, new MouseEventHandler(DropDownButton_MouseLeave), true);
            }
            
            if (_textBox != null)
            {
                _textBox.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown), true);
                _textBox.TextChanged += this.TextBox_TextChanged;
                _textBox.LostFocus += this.TextBox_LostFocus;
            }
        }

        private void UnSubscribeEvents()
        {
            if (_popUp != null)
            {
                _popUp.RemoveHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PopUp_PreviewMouseLeftButtonDown));
                _popUp.Opened -= PopUp_Opened;
                _popUp.Closed -= PopUp_Closed;
            }

            this.UnsubscribeTimePickerEvents(_hourInput, _minuteInput, _secondInput, _ampmSwitcher);

            if (this._dropDownButton != null)
            {
                this._dropDownButton.Click -= this.OnDropDownButtonClicked;
                _dropDownButton.RemoveHandler(MouseLeaveEvent, new MouseEventHandler(DropDownButton_MouseLeave));
            }

            if (_textBox != null)
            {
                _textBox.RemoveHandler(TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
                _textBox.TextChanged -= this.TextBox_TextChanged;
                _textBox.LostFocus -= this.TextBox_LostFocus;
            }
        }

        private void OutsideCapturedElementHandler(object sender, MouseButtonEventArgs e)
        {
            if (this.IsDropDownOpen)
            {
                if (!(this._dropDownButton?.InputHitTest(e.GetPosition(this._dropDownButton)) is null))
                {
                    return;
                }

                this.SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void PopUp_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Popup popup && !popup.StaysOpen)
            {
                if (this._dropDownButton?.InputHitTest(e.GetPosition(this._dropDownButton)) != null)
                {
                    // This popup is being closed by a mouse press on the drop down button
                    // The following mouse release will cause the closed popup to immediately reopen.
                    // Raise a flag to block reopeneing the popup
                    this._disablePopupReopen = true;
                }
            }
        }

        private void PopUp_Opened(object sender, EventArgs e)
        {
            if (!this.IsDropDownOpen)
            {
                this.SetCurrentValue(IsDropDownOpenProperty, true);
            }

            this.OnPopUpOpened();

            // this.OnCalendarOpened(new RoutedEventArgs());
        }

        protected virtual void OnPopUpOpened()
        {
            // nothing here
        }

        private void PopUp_Closed(object sender, EventArgs e)
        {
            if (this.IsDropDownOpen)
            {
                this.SetCurrentValue(IsDropDownOpenProperty, false);
            }

            this.OnPopUpClosed();

            // OnCalendarClosed(new RoutedEventArgs());
        }

        protected virtual void OnPopUpClosed()
        {
            // nothing here
        }

        protected virtual void WriteValueToTextBox()
        {
            if (_textBox != null)
            {
                _deactivateTextChangedEvent = true;
                _textBox.Text = GetValueForTextBox();
                _deactivateTextChangedEvent = false;
            }
        }

        private static IList<int> CreateValueList(int interval)
        {
            return Enumerable.Repeat(interval, 60 / interval)
                             .Select((value, index) => value * index)
                             .ToList();
        }

        private static object CoerceSource60(DependencyObject d, object basevalue)
        {
            var list = basevalue as IEnumerable<int>;
            if (list != null)
            {
                return list.Where(i => i >= 0 && i < 60);
            }

            return Enumerable.Empty<int>();
        }

        private static object CoerceSourceHours(DependencyObject d, object basevalue)
        {
            var timePickerBase = d as TimePickerBase;
            var hourList = basevalue as IEnumerable<int>;
            if (timePickerBase != null && hourList != null)
            {
                if (timePickerBase.IsMilitaryTime)
                {
                    return hourList.Where(i => i > 0 && i <= 12).OrderBy(i => i, new AmPmComparer());
                }
                return hourList.Where(i => i >= 0 && i < 24);
            }
            return Enumerable.Empty<int>();
        }

        private void TimePickerPreviewKeyDown(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is Selector))
            {
                return;
            }

            var selector = sender as Selector;
            var keyEventArgs = (KeyEventArgs)e;

            Debug.Assert(selector != null);
            Debug.Assert(keyEventArgs != null);

            if (keyEventArgs.Key == Key.Escape || keyEventArgs.Key == Key.Enter || keyEventArgs.Key == Key.Space)
            {
                this.SetCurrentValue(IsDropDownOpenProperty, false);
                if (keyEventArgs.Key == Key.Escape)
                {
                    this.SetCurrentValue(SelectedDateTimeProperty, this._originalSelectedDateTime);
                }
            }
        }

        private void TimePickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_deactivateRangeBaseEvent)
            {
                return;
            }

            this.ClockSelectedTimeChanged();
        }

        private static void OnCultureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timePartPickerBase = (TimePickerBase)d;

            var info = e.NewValue as CultureInfo;
            timePartPickerBase.Language = info != null ? XmlLanguage.GetLanguage(info.IetfLanguageTag) : XmlLanguage.Empty;

            timePartPickerBase.ApplyCulture();
        }

        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timePartPickerBase = (TimePickerBase)d;

            timePartPickerBase.Language = e.NewValue as XmlLanguage ?? XmlLanguage.Empty;

            timePartPickerBase.ApplyCulture();
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            TimePickerBase picker = (TimePickerBase)sender;
            if (!e.Handled && picker.Focusable)
            {
                if (Equals(e.OriginalSource, picker))
                {
                    // MoveFocus takes a TraversalRequest as its argument.
                    var request = new TraversalRequest((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
                    // Gets the element with keyboard focus.
                    var elementWithFocus = Keyboard.FocusedElement as UIElement;
                    // Change keyboard focus.
                    if (elementWithFocus != null)
                    {
                        elementWithFocus.MoveFocus(request);
                    }
                    else
                    {
                        picker.Focus();
                    }

                    e.Handled = true;
                }
                else if (picker._textBox != null && Equals(e.OriginalSource, picker._textBox))
                {
                    picker._textBox.SelectAll();
                    e.Handled = true;
                }
            }
        }

        private static void OnHandVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimePickerBase)d).SetHandVisibility((TimePartVisibility)e.NewValue);
        }

        private static void OnPickerVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimePickerBase)d).SetPickerVisibility((TimePartVisibility)e.NewValue);
        }

        private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timePartPickerBase = (TimePickerBase)d;

            if (timePartPickerBase._deactivateRangeBaseEvent)
            {
                return;
            }

            timePartPickerBase.OnSelectedDateTimeChanged(e.OldValue as DateTime?, e.NewValue as DateTime?);

            timePartPickerBase.WriteValueToTextBox();

            timePartPickerBase.RaiseSelectedDateTimeChangedEvent(e.OldValue as DateTime?, e.NewValue as DateTime?);
        }

        protected virtual void OnSelectedDateTimeChanged(DateTime? oldValue, DateTime? newValue)
        {
            this.SetHourPartValues(newValue.GetValueOrDefault().TimeOfDay);
        }

        private static void SetVisibility(UIElement partHours, UIElement partMinutes, UIElement partSeconds, TimePartVisibility visibility)
        {
            if (partHours != null)
            {
                partHours.Visibility = visibility.HasFlag(TimePartVisibility.Hour) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (partMinutes != null)
            {
                partMinutes.Visibility = visibility.HasFlag(TimePartVisibility.Minute) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (partSeconds != null)
            {
                partSeconds.Visibility = visibility.HasFlag(TimePartVisibility.Second) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static bool IsValueSelected(Selector selector)
        {
            return selector != null && selector.SelectedItem != null;
        }

        private static void SetDefaultTimeOfDayValue(Selector selector)
        {
            if (selector != null)
            {
                if (selector.SelectedValue == null)
                {
                    selector.SelectedIndex = 0;
                }
            }
        }

        protected TimeSpan? GetSelectedTimeFromGUI()
        {
            {
                if (IsValueSelected(_hourInput) &&
                    IsValueSelected(_minuteInput) &&
                    IsValueSelected(_secondInput))
                {
                    var hours = (int)_hourInput.SelectedItem;
                    var minutes = (int)_minuteInput.SelectedItem;
                    var seconds = (int)_secondInput.SelectedItem;

                    hours += GetAmPmOffset(hours);

                    return new TimeSpan(hours, minutes, seconds);
                }

                return null;
            }
        }

        /// <summary>
        ///     Gets the offset from the selected <paramref name="currentHour" /> to use it in <see cref="TimeSpan" /> as hour
        ///     parameter.
        /// </summary>
        /// <param name="currentHour">The current hour.</param>
        /// <returns>
        ///     An integer representing the offset to add to the hour that is selected in the hour-picker for setting the correct
        ///     <see cref="DateTime.TimeOfDay" />. The offset is determined as follows:
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Condition</term><description>Offset</description>
        ///         </listheader>
        ///         <item>
        ///             <term><see cref="IsMilitaryTime" /> is false</term><description>0</description>
        ///         </item>
        ///         <item>
        ///             <term>Selected hour is between 1 AM and 11 AM</term><description>0</description>
        ///         </item>
        ///         <item>
        ///             <term>Selected hour is 12 AM</term><description>-12h</description>
        ///         </item>
        ///         <item>
        ///             <term>Selected hour is between 12 PM and 11 PM</term><description>+12h</description>
        ///         </item>
        ///     </list>
        /// </returns>
        private int GetAmPmOffset(int currentHour)
        {
            if (IsMilitaryTime)
            {
                if (currentHour == 12)
                {
                    if (Equals(_ampmSwitcher.SelectedItem, SpecificCultureInfo.DateTimeFormat.AMDesignator))
                    {
                        return -12;
                    }
                }
                else if (Equals(_ampmSwitcher.SelectedItem, SpecificCultureInfo.DateTimeFormat.PMDesignator))
                {
                    return 12;
                }
            }

            return 0;
        }

        private void OnDropDownButtonClicked(object sender, RoutedEventArgs e)
        {
            TogglePopUp();
        }

        private void DropDownButton_MouseLeave(object sender, MouseEventArgs e)
        {
            this._disablePopupReopen = false;
        }

        private void TogglePopUp()
        {
            if (this.IsDropDownOpen)
            {
                this.SetCurrentValue(IsDropDownOpenProperty, false);
            }
            else
            {
                if (this._disablePopupReopen)
                {
                    this._disablePopupReopen = false;
                }
                else
                {
                    SetSelectedDateTime();
                    this.SetCurrentValue(IsDropDownOpenProperty, true);
                }
            }
        }

        private void SetAmPmVisibility()
        {
            if (_ampmSwitcher != null)
            {
                if (!PickerVisibility.HasFlag(TimePartVisibility.Hour))
                {
                    _ampmSwitcher.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _ampmSwitcher.Visibility = IsMilitaryTime ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void SetHandVisibility(TimePartVisibility visibility)
        {
            SetVisibility(_hourHand, _minuteHand, _secondHand, visibility);
        }

        private void SetHourPartValues(TimeSpan timeOfDay)
        {
            if (this._deactivateRangeBaseEvent)
            {
                return;
            }

            _deactivateRangeBaseEvent = true;
            if (_hourInput != null)
            {
                if (IsMilitaryTime)
                {
                    _ampmSwitcher.SelectedValue = timeOfDay.Hours < 12 ? SpecificCultureInfo.DateTimeFormat.AMDesignator : SpecificCultureInfo.DateTimeFormat.PMDesignator;
                    if (timeOfDay.Hours == 0 || timeOfDay.Hours == 12)
                    {
                        _hourInput.SelectedValue = 12;
                    }
                    else
                    {
                        _hourInput.SelectedValue = timeOfDay.Hours % 12;
                    }
                }
                else
                {
                    _hourInput.SelectedValue = timeOfDay.Hours;
                }
            }

            if (_minuteInput != null)
            {
                _minuteInput.SelectedValue = timeOfDay.Minutes;
            }

            if (_secondInput != null)
            {
                _secondInput.SelectedValue = timeOfDay.Seconds;
            }

            _deactivateRangeBaseEvent = false;
        }

        private void SetPickerVisibility(TimePartVisibility visibility)
        {
            SetVisibility(_hourInput, _minuteInput, _secondInput, visibility);
            SetAmPmVisibility();
        }

        private void UnsubscribeTimePickerEvents(params Selector[] selectors)
        {
            foreach (var selector in selectors.Where(i => i != null))
            {
                selector.PreviewKeyDown -= this.TimePickerPreviewKeyDown;
                selector.SelectionChanged -= this.TimePickerSelectionChanged;
            }
        }

        private void SubscribeTimePickerEvents(params Selector[] selectors)
        {
            foreach (var selector in selectors.Where(i => i != null))
            {
                selector.PreviewKeyDown += this.TimePickerPreviewKeyDown;
                selector.SelectionChanged += this.TimePickerSelectionChanged;
            }
        }

        private bool ProcessKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.System:
                {
                    switch (e.SystemKey)
                    {
                        case Key.Down:
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                            {
                                TogglePopUp();
                                return true;
                            }

                            break;
                        }
                    }

                    break;
                }

                case Key.Enter:
                {
                    SetSelectedDateTime();
                    return true;
                }
            }

            return false;
        }

        protected abstract void SetSelectedDateTime();

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_textInputChanged)
            {
                _textInputChanged = false;

                SetSelectedDateTime();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = ProcessKey(e) || e.Handled;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_deactivateTextChangedEvent)
            {
                _textInputChanged = true;
            }
        }
    }
}