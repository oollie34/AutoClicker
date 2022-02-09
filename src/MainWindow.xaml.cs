﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using AutoClicker.Bindings;
using AutoClicker.Classes;

namespace AutoClicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainBindings MainBindings { get; set; }
        private readonly Dictionary<Process, List<Clicker>> instanceClickers = new();
        public MainWindow()
        {
            InitializeComponent();
            MainBindings = new();
            DataContext = MainBindings;
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainBindings == null)
                return;
            // First check an option is checked, otherwise there is nothing to do
            if(MainBindings.LeftTopCheckBox || MainBindings.RightTopCheckBox)
            {
                // Check the user has not deleted the data from the number selector
                if(MainBindings.LeftUpDownText != null && MainBindings.RightUpDownText != null)
                {
                    // Gather processes available and select one
                    GetInstances getInstances = new();
                    if (getInstances.Check())
                    {
                        MainBindings.IndicatorLabel = "Started At: " + DateTime.Now.ToString("MMMM dd HH:mm tt");
                        MainBindings.IndicatorLabelVisible = Visibility.Visible;
                        RunApplication(getInstances);
                    }
                }
                else
                {
                    MainBindings.IndicatorLabel = "Not started - no delay selected.";
                }
            }
            else
            {
                MainBindings.IndicatorLabel = "Not started - no selections made.";
            }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
        private void RunApplication(GetInstances getInstances)
        {
            foreach (Process process in getInstances.matchingProcesses)
            {

                IntPtr processHandle = process.MainWindowHandle;
                FocusToggle(processHandle);

                MainBindings.LeftButtonEnabled = false;
                MainBindings.LeftButtonContent = "Starting In: ";
                Thread.Sleep(500);

                MainBindings.LeftButtonContent += 5;
                Thread.Sleep(500);
                for (var i = 4; i > 0; i--)
                {
                    MainBindings.LeftButtonContent.Remove(MainBindings.LeftButtonContent.Length - 1);
                    MainBindings.LeftButtonContent += i;
                    Thread.Sleep(500);
                }

                MainBindings.LeftButtonContent = "Running...";
                Thread.Sleep(500);

                MainBindings.ApplicationEnabled = false;

                //Right click needs to be ahead of left click for concrete mining
                if (MainBindings.RightTopCheckBox)
                {
                    Clicker clicker = new(Win32Api.WmRbuttonDown, Win32Api.WmRbuttonDown + 1, processHandle);
                    AddToInstanceClickers(process, clicker);
                    TimeSpan ts = TimeSpan.FromMilliseconds(Convert.ToInt32(MainBindings.RightUpDownText));
                    clicker.Start(ts);
                }

                /*
                 * This sleep is needed, because if you want to mine concrete, then Minecraft starts to hold left click first
                 * and it won't place the block in your second hand for some reason...
                 */
                Thread.Sleep(100);
                if (MainBindings.LeftTopCheckBox)
                {
                    Clicker clicker = new(Win32Api.WmLbuttonDown, Win32Api.WmLbuttonDown + 1, processHandle);
                    AddToInstanceClickers(process, clicker);
                    TimeSpan ts = TimeSpan.FromMilliseconds(Convert.ToInt32(MainBindings.LeftUpDownText));
                    clicker.Start(ts);
                }
                MainBindings.RightButtonEnabled = true;
            }
        }
        private void Stop()
        {
            MainBindings.RightButtonEnabled = false;
            foreach (var clickers in instanceClickers.Values)
            {
                foreach (var clicker in clickers)
                {
                    clicker?.Dispose();
                }
            }

            instanceClickers.Clear();
            MainBindings.ApplicationEnabled = true;
            MainBindings.LeftButtonContent = "START";
            MainBindings.LeftButtonEnabled = true;
        }
        private void AddToInstanceClickers(Process mcProcess, Clicker clicker)
        {
            if (instanceClickers.ContainsKey(mcProcess))
                instanceClickers[mcProcess].Add(clicker);
            else
                instanceClickers.Add(mcProcess, new List<Clicker> { clicker });
        }
        private static void FocusToggle(IntPtr hwnd)
        {
            Thread.Sleep(200);
            Win32Api.SetForegroundWindow(hwnd);
        }
    }
}