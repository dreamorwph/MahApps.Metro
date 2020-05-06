﻿using System;
using System.Windows;
using System.Windows.Threading;

namespace MahApps.Metro.Controls
{
    /// <summary>
    ///     Represents a control that allows the user to select a time.
    /// </summary>
    public class TimePicker : TimePickerBase
    {
        static TimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
        }

        public TimePicker()
        {
            IsDatePickerVisible = false;
        }

        /// <inheritdoc />
        protected override void FocusElementAfterIsDropDownOpenChanged()
        {
            if (this._hourInput is null)
            {
                return;
            }

            // When the popup is opened set focus to the hour input.
            // Do this asynchronously because the IsDropDownOpen could
            // have been set even before the template for the DatePicker is
            // applied. And this would mean that the visuals wouldn't be available yet.

            this.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate ()
                {
                    // setting the focus to the calendar will focus the correct date.
                    this._hourInput.Focus();
                });
        }

        protected override void SetSelectedDateTime()
        {
            if (this._textBox is null)
            {
                return;
            }

            if (TimeSpan.TryParse(_textBox.Text, SpecificCultureInfo, out var timeSpan))
            {
                this.SetCurrentValue(SelectedDateTimeProperty, this.SelectedDateTime.GetValueOrDefault().Date + timeSpan);
            }
            else
            {
                this.SetCurrentValue(SelectedDateTimeProperty, null);
                if (SelectedDateTime == null)
                {
                    // if already null, overwrite wrong data in textbox
                    WriteValueToTextBox();
                }
            }
        }
    }
}