﻿using System;
using System.Linq;
using Telegram.Td.Api;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Services;
using Unigram.Views;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace Unigram.ViewModels.Settings
{
    public class SettingsBackgroundsViewModel : TLViewModelBase
    {
        public SettingsBackgroundsViewModel(IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(protoService, cacheService, settingsService, aggregator)
        {
            Items = new MvxObservableCollection<Background>();

            RefreshItems();

            LocalCommand = new RelayCommand(LocalExecute);
            ColorCommand = new RelayCommand(ColorExecute);
            ResetCommand = new RelayCommand(ResetExecute);
        }

        private void RefreshItems()
        {
            ProtoService.Send(new GetBackgrounds(Settings.Appearance.IsDarkTheme()), result =>
            {
                if (result is Backgrounds wallpapers)
                {
                    var items = wallpapers.BackgroundsValue.ToList();
                    var background = CacheService.SelectedBackground;

                    var predefined = new Background(Constants.WallpaperLocalId, true, false, Constants.WallpaperDefaultFileName, null, null);

                    var selected = items.FirstOrDefault(x => x.Id == background?.Id);
                    if (selected != null)
                    {
                        items.Remove(selected);
                        items.Insert(0, selected);
                    }
                    else if (background == null)
                    {
                        selected = predefined;
                    }

                    items.Insert(0, predefined);

                    BeginOnUIThread(() =>
                    {
                        SelectedItem = selected;
                        Items.ReplaceWith(items);
                    });
                }
            });
        }

        private Background _selectedItem;
        public Background SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                Set(ref _selectedItem, value);
            }
        }

        public MvxObservableCollection<Background> Items { get; private set; }

        public RelayCommand LocalCommand { get; }
        private async void LocalExecute()
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.AddRange(Constants.PhotoTypes);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var token = StorageApplicationPermissions.FutureAccessList.Enqueue(file);
                    NavigationService.Navigate(typeof(BackgroundPage), Constants.WallpaperLocalFileName + $"#{token}");
                }
            }
            catch { }
        }

        public RelayCommand ColorCommand { get; }
        private void ColorExecute()
        {
            NavigationService.Navigate(typeof(BackgroundPage), Constants.WallpaperColorFileName);
        }

        public RelayCommand ResetCommand { get; }
        private async void ResetExecute()
        {
            var confirm = await MessagePopup.ShowAsync(Strings.Resources.ResetChatBackgroundsAlert, Strings.Resources.ResetChatBackgroundsAlertTitle, Strings.Resources.Reset, Strings.Resources.Cancel);
            if (confirm != Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return;
            }

            var response = await ProtoService.SendAsync(new ResetBackgrounds());
            if (response is Ok)
            {
                RefreshItems();
            }
            else if (response is Error error)
            {

            }
        }
    }
}
