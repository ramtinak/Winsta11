﻿using InstagramApiSharp.Classes.Models;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace WinstaNext.Converters.Media
{
    public class InstaUserProfilePictureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new BitmapImage(new Uri(value switch
            {
                InstaUser instaUser => !instaUser.HasAnonymousProfilePicture || (instaUser.ProfilePictureId != null && instaUser.ProfilePictureId != "unknown") ?
                          instaUser.ProfilePicUrl : "ms-appx:///Assets/Icons/NoOne.png",

                InstaUserShort instaUserShort => !instaUserShort.HasAnonymousProfilePicture || (instaUserShort.ProfilePictureId != null && instaUserShort.ProfilePictureId != "unknown") ?
                               instaUserShort.ProfilePicUrl : "ms-appx:///Assets/Icons/NoOne.png",

                InstaUserInfo instaUserInfo => !instaUserInfo.HasAnonymousProfilePicture || (instaUserInfo.ProfilePictureId != null && instaUserInfo.ProfilePictureId != "unknown") ?
                               instaUserInfo.ProfilePicUrl : "ms-appx:///Assets/Icons/NoOne.png",

                _ => "ms-appx:///Assets/Icons/NoOne.png"

            }, UriKind.RelativeOrAbsolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}