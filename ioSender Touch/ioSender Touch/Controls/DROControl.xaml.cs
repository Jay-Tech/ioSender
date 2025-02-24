/*
 * DROControl.xaml.cs - part of CNC Controls library
 *
 * v0.42 / 2023-02-14 / Io Engineering (Terje Io)
 *
 */

/*

Copyright (c) 2018-2023, Io Engineering (Terje Io)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

· Redistributions of source code must retain the above copyright notice, this
list of conditions and the following disclaimer.

· Redistributions in binary form must reproduce the above copyright notice, this
list of conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.

· Neither the name of the copyright holder nor the names of its contributors may
be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;

namespace ioSenderTouch.Controls;

public partial class DROControl : UserControl
{
    private double orgpos;
    private bool hasFocus = false;
    private Brush background = null;
    private static bool keyboardMappingsOk = false;
    public new bool IsFocused { get { return hasFocus; } }
    public bool IsFocusable { get; set; }

    public void EnableFocus()
    {
        IsFocusable = true;
    }
    public string DisplayFormat { get; private set; }

    public delegate void DROEnabledChangedHandler(bool enabled);
    public event DROEnabledChangedHandler DROEnabledChanged;

    public DROControl()
    {
        InitializeComponent();
    }
    private void DRO_Loaded(object sender, RoutedEventArgs e)
    {
        if (!keyboardMappingsOk && DataContext is GrblViewModel)
        {
            KeypressHandler keyboard = (DataContext as GrblViewModel)?.Keyboard;

            keyboardMappingsOk = true;

            keyboard.AddHandler(Key.X, ModifierKeys.Control | ModifierKeys.Shift, ZeroX);
            keyboard.AddHandler(Key.Y, ModifierKeys.Control | ModifierKeys.Shift, ZeroY);
            keyboard.AddHandler(Key.Z, ModifierKeys.Control | ModifierKeys.Shift, ZeroZ);
            if (GrblInfo.AxisFlags.HasFlag(AxisFlags.A))
                keyboard.AddHandler(Key.A, ModifierKeys.Control | ModifierKeys.Shift, ZeroA);
            if (GrblInfo.AxisFlags.HasFlag(AxisFlags.B))
                keyboard.AddHandler(Key.B, ModifierKeys.Control | ModifierKeys.Shift, ZeroB);
            if (GrblInfo.AxisFlags.HasFlag(AxisFlags.C))
                keyboard.AddHandler(Key.C, ModifierKeys.Control | ModifierKeys.Shift, ZeroC);
            keyboard.AddHandler(Key.D0, ModifierKeys.Control | ModifierKeys.Shift, ZeroAxes);
        }


        foreach (var axis in UiExtension.FindVisualChildren<DROBaseControl>(this))
        {
            axis.Tag = AxisLetterToIndex(axis.Label);
            axis.btnZero.Content = $"0{axis.Label}";
            axis.btnHome.Tag = axis.Label;
            axis.btnHome.Content = $"H{axis.Label}";
            axis.txtReadout.GotFocus += txtReadout_GotFocus;
            axis.txtReadout.LostFocus += txtReadout_LostFocus;
            axis.txtReadout.PreviewKeyDown += txtReadout_PreviewKeyDown;
            axis.txtReadout.PreviewKeyUp += txtReadout_PreviewKeyUp;
            axis.btnZero.Click += btnZero_Click;
            axis.btnHome.Click += BtnHome_Click;
        }
    }
        
    public  int AxisLetterToIndex(string letter)
    {
        return AxisLetters.IndexOf(letter, StringComparison.Ordinal);
    }
    public  string AxisLetters { get; private set; } = "XYZAB";

    private void txtReadout_GotFocus(object sender, RoutedEventArgs e)
    {
        //if (IsFocusable && !(DataContext as GrblViewModel).IsJobRunning)
        //{
        //    (DataContext as GrblViewModel).SuspendPositionNotifications = true;

        //    orgpos = (DataContext as GrblViewModel).Position.Values[(int)((NumericTextBox)(sender)).Tag];

        //    background = (sender as NumericTextBox).Background;
        //    (sender as NumericTextBox).IsReadOnly = false;
        //    (sender as NumericTextBox).Background = Brushes.White;

        //    hasFocus = true;

        //    DROEnabledChanged?.Invoke(true);
        //}
    }

    void txtReadout_LostFocus(object sender, EventArgs e)
    {
        //((NumericTextBox)(sender)).IsReadOnly = true;

        //(DataContext as GrblViewModel).SuspendPositionNotifications = false;

        //if (hasFocus)
        //{
        //    (sender as NumericTextBox).Background = background;
        //    (DataContext as GrblViewModel).Position.Values[(int)(sender as NumericTextBox).Tag] = orgpos;
        //}

        //hasFocus = false;

        //DROEnabledChanged?.Invoke(false);
    }

    private void txtReadout_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!hasFocus)
            e.Handled = true;
    }

    private void txtReadout_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NumericTextBox axis = (NumericTextBox)sender;

            if (axis.Value != orgpos)
                AxisPositionChanged(GrblInfo.AxisIndexToLetter((int)axis.Tag), axis.Value);

            axis.IsReadOnly = true;

            DROEnabledChanged?.Invoke(false);
        }
    }

    void btnZero_Click(object sender, EventArgs e)
    {
        AxisPositionChanged(GrblInfo.AxisIndexToLetter((int)(sender as Button).Tag), 0.0d);
    }
        
    void btnZeroAll_Click(object sender, EventArgs e)
    {
        AxisPositionChanged("ALL", 0.0d);
    }

    private bool ZeroAxes(Key key)
    {
        AxisPositionChanged("ALL", 0d);

        return true;
    }

    private bool ZeroX(Key key)
    {
        AxisPositionChanged("X", 0d);

        return true;
    }

    private bool ZeroY(Key key)
    {
        AxisPositionChanged("Y", 0d);

        return true;
    }
    private bool ZeroZ(Key key)
    {
        AxisPositionChanged("Z", 0d);

        return true;
    }
    private bool ZeroA(Key key)
    {
        AxisPositionChanged(GrblInfo.AxisIndexToLetter(3), 0d);

        return true;
    }
    private bool ZeroB(Key key)
    {
        AxisPositionChanged(GrblInfo.AxisIndexToLetter(4), 0d);

        return true;
    }
    private bool ZeroC(Key key)
    {
        AxisPositionChanged(GrblInfo.AxisIndexToLetter(5), 0d);

        return true;
    }
    private void BtnHome_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            var axis = button.Tag.ToString();
            (DataContext as GrblViewModel)?.ExecuteCommand($"$H{axis}");
        }
    }

    void AxisPositionChanged(string axis, double position)
    {
        if (axis == "ALL")
        {
            var s = GrblInfo.AxisFlags.ToIndices().Aggregate("G90G10L20P0",
                (current, i) => current + GrblInfo.AxisIndexToLetter(i) + "{0}");
            (DataContext as GrblViewModel)?.ExecuteCommand(string.Format(s, position.ToInvariantString("F3")));
        }
        else
        {
            var comand = $"G10L20P0{axis}{position.ToInvariantString("F3")}";
            (DataContext as GrblViewModel)?.ExecuteCommand($"G10L20P0{axis}{position.ToInvariantString("F3")}");
        }
               
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        (DataContext as GrblViewModel)?.ExecuteCommand("$H");
            
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

}

public class UiExtension
{
    public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield return (T)Enumerable.Empty<T>();
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
            if (ithChild == null) continue;
            if (ithChild is T t) yield return t;
            foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
        }
    }

}