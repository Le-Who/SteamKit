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
                LoggingService.Instance.LogInfo($"🔍 Начало получения трейдов для {account.Username}", account.Username);
                
                if (!account.IsAuthenticated)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Аккаунт {account.Username} не аутентифицирован", account.Username);
                    return tradeOffers;
                }

                LoggingService.Instance.LogInfo($"✅ Аккаунт {account.Username} аутентифицирован", account.Username);

                if (!_configService.IsApiKeyConfigured())
                {
                    LoggingService.Instance.LogWarning($"⚠️ Steam API ключ не настроен, возвращаем тестовые данные", account.Username);
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
                    LoggingService.Instance.LogInfo($"📋 Возвращено {tradeOffers.Count} тестовых трейдов", account.Username);
                    return tradeOffers;
                }

                LoggingService.Instance.LogInfo($"✅ Steam API ключ настроен, запрашиваем реальные трейды", account.Username);

                // Реальная работа с Steam Web API
                var webApi = WebAPI.GetInterface("IEconService", _configService.SteamApiKey);
                
                LoggingService.Instance.LogInfo($"🌐 Вызов Steam Web API GetTradeOffers для {account.Username}", account.Username);
                
                // Запрашиваем ВСЕ трейды, ожидающие мобильного подтверждения
                var allOffers = webApi.Call("GetTradeOffers", 1, new Dictionary<string, object?>
                {
                    ["get_sent_offers"] = "1",      // Отправленные трейды
                    ["get_received_offers"] = "1",  // Полученные трейды
                    ["get_descriptions"] = "1",     // Описания предметов
                    ["active_only"] = "1",          // Только активные
                    ["historical_only"] = "0",      // Не исторические
                    ["time_historical_cutoff"] = "0"
                });

                LoggingService.Instance.LogInfo($"🌐 Получен ответ от Steam Web API", account.Username);

                // Обрабатываем отправленные трейды
                if (allOffers != null && allOffers["response"]["trade_offers_sent"] != null)
                {
                    var sentCount = allOffers["response"]["trade_offers_sent"].Children.Count();
                    LoggingService.Instance.LogInfo($"📤 Найдено {sentCount} отправленных трейдов", account.Username);
                    
                    foreach (var offer in allOffers["response"]["trade_offers_sent"].Children)
                    {
                        var tradeOffer = new TradeOffer
                        {
                            TradeOfferId = offer["tradeofferid"].AsString() ?? "",
                            AccountName = account.DisplayName ?? "",
                            PartnerName = offer["accountid_other"].AsString() ?? "",
                            ItemsDescription = GetItemsDescription(offer),
                            CreatedTime = DateTimeOffset.FromUnixTimeSeconds(offer["time_created"].AsLong()).DateTime,
                            Status = GetStatusDescription(offer["trade_offer_state"].AsInteger())
                        };

                        tradeOffers.Add(tradeOffer);
                    }
                }

                // Обрабатываем полученные трейды
                if (allOffers != null && allOffers["response"]["trade_offers_received"] != null)
                {
                    var receivedCount = allOffers["response"]["trade_offers_received"].Children.Count();
                    LoggingService.Instance.LogInfo($"📥 Найдено {receivedCount} полученных трейдов", account.Username);
                    
                    foreach (var offer in allOffers["response"]["trade_offers_received"].Children)
                    {
                        var tradeOffer = new TradeOffer
                        {
                            TradeOfferId = offer["tradeofferid"].AsString() ?? "",
                            AccountName = account.DisplayName ?? "",
                            PartnerName = offer["accountid_other"].AsString() ?? "",
                            ItemsDescription = GetItemsDescription(offer),
                            CreatedTime = DateTimeOffset.FromUnixTimeSeconds(offer["time_created"].AsLong()).DateTime,
                            Status = GetStatusDescription(offer["trade_offer_state"].AsInteger())
                        };

                        tradeOffers.Add(tradeOffer);
                    }
                }

                if (tradeOffers.Count == 0)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Нет трейдов, ожидающих мобильного подтверждения", account.Username);
                }

                LoggingService.Instance.LogInfo($"📋 Итого обработано {tradeOffers.Count} трейдов", account.Username);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"❌ Ошибка получения обменов: {ex.Message}", account.Username, ex);
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
                
                var result = webApi.Call("ConfirmTradeOffer", 1, new Dictionary<string, object?>
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
                
                var result = webApi.Call("CancelTradeOffer", 1, new Dictionary<string, object?>
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
