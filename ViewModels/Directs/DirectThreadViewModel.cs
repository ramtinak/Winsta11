﻿using Core.Collections.IncrementalSources.Directs;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using ViewModels;
using WinstaCore;
using WinstaCore.Converters.FileConverters;
using WinstaCore.Models.ConfigureDelays;
using Windows.UI.Xaml.Controls;
using WinstaCore.Helpers.ExtensionMethods;
using Windows.Storage;
using System.Text.RegularExpressions;
using WinstaCore.Constants;
#nullable enable

namespace WinstaNext.ViewModels.Directs
{
    [AddINotifyPropertyChangedInterface]
    public class DirectThreadViewModel : BaseViewModel
    {
        InstaDirectInboxThread DirectThread { get; set; }
        IncrementalDirectThread Instance { get; set; }
        public DirectMessagesInvertedCollection ThreadItems { get; private set; }

        public AsyncRelayCommand<ListView> GoBottomCommand { get; set; }
        public AsyncRelayCommand UploadImageCommand { get; set; }
        public AsyncRelayCommand UploadCameraCapturedImageCommand { get; set; }
        public AsyncRelayCommand UploadVideoCommand { get; set; }
        public AsyncRelayCommand SendMessageCommand { get; set; }
        public AsyncRelayCommand SendLikeCommand { get; set; }
        public AsyncRelayCommand<DependencyObject> OpenEmojisPanelCommand { get; set; }

        public RelayCommand IgnoreReplyCommand { get; set; }
        public RelayCommand GifPanelCommand { get; set; }

        public static DirectThreadViewModel? CurrentVM { get; set; } = null;

        public InstaDirectInboxItem? RepliedMessage { get; set; }

        public string MessageText { get; set; } = string.Empty;

        public string ThreadId { get; set; } = string.Empty;

        public Visibility GifPanelVisibility { get; set; } = Visibility.Collapsed;

        public DirectThreadViewModel(InstaDirectInboxThread directThread)
        {
            if (DirectThread != null && DirectThread.ThreadId == directThread.ThreadId) return;
            RepliedMessage = null;
            DirectThread = directThread;
            Instance = new(DirectThread);
            ThreadItems = new(Instance);
            GifPanelCommand = new(GifPanelShowHide);
            GoBottomCommand = new(GoToBottomAsync);
            SendLikeCommand = new(SendLikeAsync);
            IgnoreReplyCommand = new(IgnoreReply);
            UploadImageCommand = new(UploadImageAsync);
            UploadCameraCapturedImageCommand = new(UploadCameraCapturedImageAsync);
            UploadVideoCommand = new(UploadVideoAsync);
            SendMessageCommand = new(SendMessageAsync);
            OpenEmojisPanelCommand = new(OpenEmojisPanel);
            CurrentVM = this;
        }

        void GifPanelShowHide()
        {
            GifPanelVisibility = GifPanelVisibility == Visibility.Collapsed ?
                Visibility.Visible : Visibility.Collapsed;
        }

        async Task GoToBottomAsync(ListView? list)
        {
            if (list == null) return;
            await list.SmoothScrollIntoViewWithIndexAsync(list.Items.Count - 1);
        }

        void IgnoreReply() => RepliedMessage = null;

        async Task UploadImageAsync()
        {
            await UploadImageAsync(null);
        }

        public async Task UploadImageAsync(StorageFile? file = null)
        {
            if (file == null)
            {
                var fop = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    ViewMode = PickerViewMode.Thumbnail
                };
                fop.FileTypeFilter.Add(".jpg");
                fop.FileTypeFilter.Add(".png");
                fop.FileTypeFilter.Add(".bmp");
                file = await fop.PickSingleFileAsync();
                if (file == null) return;
            }

            var ip = await file.Properties.GetImagePropertiesAsync();

            var bytes = await ImageFileConverter.ConvertImageToJpegAsync(file);

            using (var Api = AppCore.Container.GetService<IInstaApi>())
            {
                Api.SetConfigureMediaDelay(new ImageConfigureMediaDelay());
                var result = await Api.MessagingProcessor.SendDirectPhotoAsync(new InstaImage()
                {
                    ImageBytes = bytes,
                    Height = (int)ip.Height,
                    Width = (int)ip.Width
                }, ThreadId);
                if (result.Succeeded)
                {
                    //ThreadItems.InsertNewTextMessage(result.Value, MessageText);
                    MessageText = string.Empty;
                }
            }
        }

        async Task UploadCameraCapturedImageAsync()
        {
            var camUI = new CameraCaptureUI();

            camUI.PhotoSettings.AllowCropping = true;
            camUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            var res = await camUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (res == null) return;
            var ip = await res.Properties.GetImagePropertiesAsync();

            var bytes = await ImageFileConverter.ConvertImageToJpegAsync(res);

            using (var Api = AppCore.Container.GetService<IInstaApi>())
            {
                Api.SetConfigureMediaDelay(new ImageConfigureMediaDelay());
                var result = await Api.MessagingProcessor.SendDirectPhotoAsync(new InstaImage()
                {
                    ImageBytes = bytes,
                    Height = (int)ip.Height,
                    Width = (int)ip.Width
                }, ThreadId);
                if (result.Succeeded)
                {
                    //ThreadItems.InsertNewTextMessage(result.Value, MessageText);
                    MessageText = string.Empty;
                }
            }
        }

        public async Task UploadVideoAsync()
        {
            await UploadVideoAsync(null);
        }

        public async Task UploadVideoAsync(StorageFile? file = null)
        {
            if (file == null)
            {
                var fop = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.VideosLibrary,
                    ViewMode = PickerViewMode.Thumbnail
                };
                fop.FileTypeFilter.Add(".mp4");
                file = await fop.PickSingleFileAsync();
                if (file == null) return;
            }
            var vp = await file.Properties.GetVideoPropertiesAsync();
            var thumb = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView);
            var openFile = await file.OpenReadAsync();
            var imageBytes = await ImageFileConverter.ConvertToBytesArray(thumb);
            var bytes = await ImageFileConverter.ConvertToBytesArray(openFile);

            using (var Api = AppCore.Container.GetService<IInstaApi>())
            {
                Api.SetConfigureMediaDelay(new VideoConfigureMediaDelay());
                var result = await Api.MessagingProcessor.SendDirectVideoAsync(new InstaVideoUpload()
                {
                    Video = new()
                    {
                        Height = (int)vp.Height,
                        Width = (int)vp.Width,
                        VideoBytes = bytes,
                    },
                    VideoThumbnail = new()
                    {
                        Height = (int)thumb.OriginalHeight,
                        Width = (int)thumb.OriginalWidth,
                        ImageBytes = imageBytes
                    }
                }, ThreadId);
                if (result.Succeeded)
                {
                    //ThreadItems.InsertNewTextMessage(result.Value, MessageText);
                    MessageText = string.Empty;
                }
            }
        }

        async Task SendMessageAsync()
        {
            if (SendMessageCommand.IsRunning) return;
            using (var Api = AppCore.Container.GetService<IInstaApi>())
            {
                IResult<InstaDirectRespondPayload> result;
                if (RepliedMessage == null)
                {
                    if (Regex.Match(MessageText, RegexConstants.WebUrlRegex) is Match match && match.Groups.Count > 1)
                    {
                        result = await Api.MessagingProcessor.SendDirectLinkAsync(MessageText, match.Value, ThreadId);
                    }
                    else if (Regex.Match(MessageText, RegexConstants.WebUrlWithoutPrefixRegex) is Match match2 && match2.Groups.Count > 1)
                    {
                        result = await Api.MessagingProcessor.SendDirectLinkAsync(MessageText, match2.Value, ThreadId);
                    }
                    else result = await Api.MessagingProcessor.SendDirectTextAsync(null, ThreadId, MessageText);
                }
                else
                {
                    result = await Api.MessagingProcessor.ReplyDirectMessageAsync(ThreadId, MessageText, RepliedMessage.ItemId, RepliedMessage.UserId, RepliedMessage.ClientContext, RepliedMessage.ItemType.ToString()); ;
                }
                if (result.Succeeded)
                {
                    ThreadItems.InsertNewTextMessage(result.Value, MessageText);
                    MessageText = string.Empty;
                    RepliedMessage = null;
                }
            }
        }

        async Task SendLikeAsync()
        {
            if (SendLikeCommand.IsRunning) return;
            using (var Api = AppCore.Container.GetService<IInstaApi>())
            {
                var result = await Api.MessagingProcessor.SendDirectLikeAsync(ThreadId);
                if (result.Succeeded)
                {
                    ThreadItems.InsertNewLikeMessage();
                }
            }
        }

        async Task OpenEmojisPanel(DependencyObject? obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            await obj.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (obj is TextBox txtbox)
                    txtbox.Focus(FocusState.Keyboard);
                CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            });
        }
    }
}
