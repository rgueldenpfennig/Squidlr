﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Squidlr.App;

internal class MainPageViewModel : INotifyPropertyChanged
{
    private string? _url;

    private bool _isValidUrl;
    private readonly UrlResolver _urlResolver;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SubmitCommand { private set; get; }

    public string? Url
    {
        private set
        {
            OnUrlChanged(value);
            SetProperty(ref _url, value);
        }

        get => _url;
    }

    public bool IsValidUrl
    {
        private set => SetProperty(ref _isValidUrl, value);
        get => _isValidUrl;
    }

    public MainPageViewModel(UrlResolver urlResolver)
    {
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));

        SubmitCommand = new Command(
            execute: ExecuteSubmitCommand,
            canExecute: (_) =>
            {
                return IsValidUrl;
            });
    }

    private void ExecuteSubmitCommand(object arg)
    {

    }

    private void OnUrlChanged(string? value)
    {
        var contentIdentifier = _urlResolver.ResolveUrl(value);
        if (contentIdentifier == ContentIdentifier.Unknown)
        {
            IsValidUrl = true;
        }
        else
        {
            IsValidUrl = false;
        }
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}