using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamTradeConfirmer.Models;

namespace SteamTradeConfirmer.Services
{
    public class TradeOfferService
    {
        private readonly SteamClientService _steamClientService;
        private readonly ConfigurationService _configService;

        public TradeOfferService(SteamClientService steamClientService)
        {
            _steamClientService = steamClientService;
            _configService = ConfigurationService.Instance;
        }

        public List<TradeOffer> GetTradeOffers(SteamAccount account)
        {
            var tradeOffers = new List<TradeOffer>();

            try
            {
                if (!account.IsAuthenticated)
                {
                    return tradeOffers;
                }

                if (!_configService.IsApiKeyConfigured())
                {
                    // Возвращаем тестовые данные, если API ключ не настроен
                    var testOffer = new TradeOffer
                    {
                        TradeOfferId = "123456789",
                        AccountName = account.DisplayName,
                        PartnerName = "TestUser",
                        ItemsDescription = "Отдаем: AK-47 Redline, Получаем: AWP Dragon Lore",
                        CreatedTime = DateTime.Now.AddHours(-2),
                        Status = "Активен (тестовый режим)"
                    };
                    tradeOffers.Add(testOffer);
                    return tradeOffers;
                }

                // Реальная работа с Steam Web API
                var webApi = WebAPI.GetInterface("IEconService", _configService.SteamApiKey);
                
                var sentOffers = webApi.Call("GetTradeOffers", 1, new Dictionary<string, object>
                {
                    ["get_sent_offers"] = "1",
                    ["get_received_offers"] = "0",
                    ["get_descriptions"] = "1",
                    ["active_only"] = "1",
                    ["historical_only"] = "0",
                    ["time_historical_cutoff"] = "0"
                });

                if (sentOffers != null && sentOffers["response"]["trade_offers_sent"] != null)
                {
                    foreach (var offer in sentOffers["response"]["trade_offers_sent"].Children)
                    {
                        var tradeOffer = new TradeOffer
                        {
                            TradeOfferId = offer["tradeofferid"].AsString(),
                            AccountName = account.DisplayName,
                            PartnerName = offer["accountid_other"].AsString(),
                            ItemsDescription = GetItemsDescription(offer),
                            CreatedTime = DateTimeOffset.FromUnixTimeSeconds(offer["time_created"].AsLong()).DateTime,
                            Status = GetStatusDescription(offer["trade_offer_state"].AsInteger())
                        };

                        tradeOffers.Add(tradeOffer);
                    }
                }
            }
            catch (Exception ex)
            {
                account.ErrorMessage = $"Ошибка получения обменов: {ex.Message}";
            }

            return tradeOffers;
        }

        public bool ConfirmTradeOffer(SteamAccount account, string tradeOfferId)
        {
            try
            {
                if (!account.IsAuthenticated)
                {
                    return false;
                }

                if (!_configService.IsApiKeyConfigured())
                {
                    // Тестовый режим
                    System.Threading.Thread.Sleep(1000);
                    return true;
                }

                // Реальная работа с Steam Web API
                var webApi = WebAPI.GetInterface("IEconService", _configService.SteamApiKey);
                
                var result = webApi.Call("ConfirmTradeOffer", 1, new Dictionary<string, object>
                {
                    ["tradeofferid"] = tradeOfferId
                });

                return result != null && result["response"]["success"].AsBoolean();
            }
            catch (Exception ex)
            {
                account.ErrorMessage = $"Ошибка подтверждения обмена: {ex.Message}";
                return false;
            }
        }

        public bool CancelTradeOffer(SteamAccount account, string tradeOfferId)
        {
            try
            {
                if (!account.IsAuthenticated)
                {
                    return false;
                }

                if (!_configService.IsApiKeyConfigured())
                {
                    // Тестовый режим
                    System.Threading.Thread.Sleep(1000);
                    return true;
                }

                // Реальная работа с Steam Web API
                var webApi = WebAPI.GetInterface("IEconService", _configService.SteamApiKey);
                
                var result = webApi.Call("CancelTradeOffer", 1, new Dictionary<string, object>
                {
                    ["tradeofferid"] = tradeOfferId
                });

                return result != null && result["response"]["success"].AsBoolean();
            }
            catch (Exception ex)
            {
                account.ErrorMessage = $"Ошибка отмены обмена: {ex.Message}";
                return false;
            }
        }

        private string GetItemsDescription(KeyValue offer)
        {
            var items = new List<string>();
            
            if (offer["items_to_give"] != null)
            {
                foreach (var item in offer["items_to_give"].Children)
                {
                    items.Add($"Отдаем: {item["market_name"].AsString()}");
                }
            }

            if (offer["items_to_receive"] != null)
            {
                foreach (var item in offer["items_to_receive"].Children)
                {
                    items.Add($"Получаем: {item["market_name"].AsString()}");
                }
            }

            return items.Count > 0 ? string.Join(", ", items) : "Нет предметов";
        }

        private string GetStatusDescription(int status)
        {
            return status switch
            {
                1 => "Активен",
                2 => "Принят",
                3 => "Отклонен",
                4 => "Отменен",
                5 => "Истек",
                6 => "Некоторые предметы недоступны",
                7 => "Отменен второй стороной",
                8 => "В ожидании подтверждения",
                9 => "Отменен покупателем",
                10 => "В ожидании подтверждения",
                11 => "Отменен покупателем",
                _ => "Неизвестно"
            };
        }

    }
}
