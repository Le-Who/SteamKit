using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamTradeConfirmer.Models
{
    public class TradeOffer : INotifyPropertyChanged
    {
        private string _tradeOfferId = string.Empty;
        private string _accountName = string.Empty;
        private string _partnerName = string.Empty;
        private string _partnerAvatar = string.Empty;
        private string _itemsDescription = string.Empty;
        private DateTime _createdTime;
        private string _status = string.Empty;

        public string TradeOfferId
        {
            get => _tradeOfferId;
            set => SetProperty(ref _tradeOfferId, value);
        }

        public string AccountName
        {
            get => _accountName;
            set => SetProperty(ref _accountName, value);
        }

        public string PartnerName
        {
            get => _partnerName;
            set => SetProperty(ref _partnerName, value);
        }

        public string PartnerAvatar
        {
            get => _partnerAvatar;
            set => SetProperty(ref _partnerAvatar, value);
        }

        public string ItemsDescription
        {
            get => _itemsDescription;
            set => SetProperty(ref _itemsDescription, value);
        }

        public DateTime CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
