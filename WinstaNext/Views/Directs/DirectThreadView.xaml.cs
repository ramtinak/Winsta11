﻿using Abstractions.Direct.Models;
using Core.Collections.IncrementalSources.Directs;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using PropertyChanged;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using WinstaNext.ViewModels.Directs;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinstaNext.Views.Directs
{
    [AddINotifyPropertyChangedInterface]
    public sealed partial class DirectThreadView : Page
    {
        public static readonly DependencyProperty DirectThreadProperty = DependencyProperty.Register(
          nameof(DirectThread),
          typeof(InstaDirectInboxThread),
          typeof(DirectThreadView),
          new PropertyMetadata(null));

        [OnChangedMethod(nameof(OnDirectItemChanged))]
        public InstaDirectInboxThread DirectThread
        {
            get { return (InstaDirectInboxThread)GetValue(DirectThreadProperty); }
            set
            {
                SetValue(DirectThreadProperty, value);
            }
        }

        public static double MessageBubbleMaxWidth { get; private set; }

        public DirectThreadViewModel ViewModel { get; set; }

        public DirectThreadView()
        {
            this.InitializeComponent();
        }

        ~DirectThreadView()
        {
            ViewModel = null;
        }

        void OnDirectItemChanged()
        {
            if (DirectThread == null) return;
            ViewModel = new(DirectThread);
            ViewModel.ThreadId = DirectThread.ThreadId;
            lst.ItemsSource = ViewModel.ThreadItems;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MessageBubbleMaxWidth = e.NewSize.Width - 150;
        }

        private void SendMessageKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            ViewModel.SendMessageCommand.Execute(null);
            args.Handled = true;
        }
    }

}
