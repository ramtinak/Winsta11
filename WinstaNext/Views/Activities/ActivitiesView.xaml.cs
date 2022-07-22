﻿using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Enums;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinstaCore.Interfaces.Views.Activities;
using WinstaNext.Helpers.ExtensionMethods;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinstaNext.Views.Activities
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ActivitiesView : BasePage, IActivitiesView
    {
        public ActivitiesView()
        {
            this.InitializeComponent();
        }

        void TextLinkClicked(object sender, LinkClickedEventArgs obj)
            => obj.HandleClickEvent();

        private void AcceptFriendshipRequest_Click(object sender, RoutedEventArgs e)
        {
            var dt = (sender as FrameworkElement).DataContext;
            if (dt is InstaRecentActivityFeed activityFeed)
                ViewModel.ApproveFollowRequestCommand.Execute(activityFeed);
        }

        private void RejectFriendshipRequest_Click(object sender, RoutedEventArgs e)
        {
            var dt = (sender as FrameworkElement).DataContext;
            if (dt is InstaRecentActivityFeed activityFeed)
                ViewModel.RejectFollowRequestCommand.Execute(activityFeed);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is InstaRecentActivityFeed activityFeed)
                if (activityFeed.Type == InstaActivityFeedType.Follow)
                    ViewModel.NavigationService.Navigate(typeof(FollowRequestsView));
        }
    }
}
