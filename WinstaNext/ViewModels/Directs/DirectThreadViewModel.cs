﻿using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml;
using WinstaNext.Abstractions.Direct.Models;
using WinstaNext.Converters.FileConverters;
using WinstaNext.Core.Collections.IncrementalSources.Directs;
using WinstaNext.Models.ConfigureDelays;
using WinstaNext.Views.Directs;

namespace WinstaNext.ViewModels.Directs
{
    public class DirectThreadViewModel : BaseViewModel
    {
        InstaDirectInboxThread DirectThread { get; set; }
        IncrementalDirectThread Instance { get; set; }
        public DirectMessagesInvertedCollection ThreadItems { get; private set; }

        public AsyncRelayCommand UploadImageCommand { get; set; }
        public AsyncRelayCommand UploadCameraCapturedImageCommand { get; set; }
        public AsyncRelayCommand UploadVideoCommand { get; set; }
        public AsyncRelayCommand SendMessageCommand { get; set; }
        public AsyncRelayCommand SendLikeCommand { get; set; }
        public AsyncRelayCommand<DependencyObject> OpenEmojisPanelCommand { get; set; }

        public string MessageText { get; set; }

        public string ThreadId { get; set; }
        public override string PageHeader { get; protected set; }

        public DirectThreadViewModel(InstaDirectInboxThread directThread)
        {
            if (DirectThread != null && DirectThread.ThreadId == directThread.ThreadId) return;
            DirectThread = directThread;
            Instance = new(DirectThread);
            ThreadItems = new(Instance);
            SendLikeCommand = new(SendLikeAsync);
            UploadImageCommand = new(UploadImageAsync);
            UploadCameraCapturedImageCommand = new(UploadCameraCapturedImageAsync);
            UploadVideoCommand = new(UploadVideoAsync);
            SendMessageCommand = new(SendMessageAsync);
            OpenEmojisPanelCommand = new(OpenEmojisPanel);
        }

        ~DirectThreadViewModel()
        {
            DirectThread = null;
            Instance = null;
            ThreadItems = null;
            SendMessageCommand = null;
        }

        async Task UploadImageAsync()
        {
            var fop = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            fop.FileTypeFilter.Add(".jpg");
            fop.FileTypeFilter.Add(".png");
            fop.FileTypeFilter.Add(".bmp");
            var res = await fop.PickSingleFileAsync();
            if (res == null) return;
            var ip = await res.Properties.GetImagePropertiesAsync();

            var bytes = await ImageFileConverter.ConvertImageToJpegAsync(res);

            using (var Api = App.Container.GetService<IInstaApi>())
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

            using (var Api = App.Container.GetService<IInstaApi>())
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
            var fop = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            fop.FileTypeFilter.Add(".mp4");
            var res = await fop.PickSingleFileAsync();
            if (res == null) return;
            var vp = await res.Properties.GetVideoPropertiesAsync();
            var thumb = await res.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView);
            var openFile = await res.OpenReadAsync();
            var imageBytes = await ImageFileConverter.ConvertToBytesArray(thumb);
            var bytes = await ImageFileConverter.ConvertToBytesArray(openFile);
            
            using (var Api = App.Container.GetService<IInstaApi>())
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
            using (var Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.MessagingProcessor.SendDirectTextAsync(null, ThreadId, MessageText);
                if (result.Succeeded)
                {
                    ThreadItems.InsertNewTextMessage(result.Value, MessageText);
                    MessageText = string.Empty;
                }
            }
        }

        async Task SendLikeAsync()
        {
            if (SendLikeCommand.IsRunning) return;
            using (var Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.MessagingProcessor.SendDirectLikeAsync(ThreadId);
                if (result.Succeeded)
                {
                    ThreadItems.InsertNewLikeMessage();
                }
            }
        }

        async Task OpenEmojisPanel(DependencyObject obj)
        {
            await obj.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            });
        }
    }
}
