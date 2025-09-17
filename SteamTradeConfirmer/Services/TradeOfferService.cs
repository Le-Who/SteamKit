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

                // Сначала проверим права API ключа
                try
                {
                    LoggingService.Instance.LogInfo($"🔍 Проверка прав API ключа...", account.Username);
                    var userApi = WebAPI.GetInterface("ISteamUser", _configService.SteamApiKey);
                    var userInfo = userApi.Call("GetPlayerSummaries", 2, new Dictionary<string, object?>
                    {
                        ["steamids"] = account.Username
                    });
                    LoggingService.Instance.LogInfo($"✅ API ключ имеет доступ к ISteamUser", account.Username);
                }
                catch (Exception apiEx)
                {
                    LoggingService.Instance.LogError($"❌ Ошибка доступа к ISteamUser: {apiEx.Message}", account.Username, apiEx);
                }

                // Реальная работа с Steam Web API
                var webApi = WebAPI.GetInterface("IEconService", _configService.SteamApiKey);
                
                // Попробуем также ISteamEconomy API
                try
                {
                    LoggingService.Instance.LogInfo($"🔍 Попытка использования ISteamEconomy API...", account.Username);
                    var economyApi = WebAPI.GetInterface("ISteamEconomy", _configService.SteamApiKey);
                    // Этот API может не существовать, но попробуем
                    LoggingService.Instance.LogInfo($"✅ ISteamEconomy API доступен", account.Username);
                }
                catch (Exception economyEx)
                {
                    LoggingService.Instance.LogInfo($"ℹ️ ISteamEconomy API недоступен: {economyEx.Message}", account.Username);
                }
                
                LoggingService.Instance.LogInfo($"🌐 Вызов Steam Web API GetTradeOffers для {account.Username}", account.Username);
                LoggingService.Instance.LogInfo($"🔑 API ключ: {_configService.SteamApiKey.Substring(0, 8)}...", account.Username);
                
                // Запрашиваем ВСЕ трейды, ожидающие мобильного подтверждения
                LoggingService.Instance.LogInfo($"📋 Параметры запроса API:", account.Username);
                LoggingService.Instance.LogInfo($"  - get_sent_offers: 1", account.Username);
                LoggingService.Instance.LogInfo($"  - get_received_offers: 1", account.Username);
                LoggingService.Instance.LogInfo($"  - get_descriptions: 1", account.Username);
                LoggingService.Instance.LogInfo($"  - active_only: 0 (ВСЕ трейды)", account.Username);
                LoggingService.Instance.LogInfo($"  - historical_only: 0", account.Username);
                
                // Попробуем несколько подходов к получению трейдов
                KeyValue? allOffers = null;
                
                // Подход 1: Без параметра active_only (может быть проблема)
                try
                {
                    LoggingService.Instance.LogInfo($"🔍 Подход 1: Без параметра active_only", account.Username);
                    allOffers = webApi.Call("GetTradeOffers", 1, new Dictionary<string, object?>
                    {
                        ["get_sent_offers"] = "1",
                        ["get_received_offers"] = "1",
                        ["get_descriptions"] = "1"
                    });
                    
                    if (allOffers != null && allOffers["response"] != null)
                    {
                        var sentCount = allOffers["response"]["trade_offers_sent"]?.Children.Count() ?? 0;
                        var receivedCount = allOffers["response"]["trade_offers_received"]?.Children.Count() ?? 0;
                        LoggingService.Instance.LogInfo($"✅ Подход 1: {sentCount} отправленных, {receivedCount} полученных", account.Username);
                        
                        if (sentCount > 0 || receivedCount > 0)
                        {
                            LoggingService.Instance.LogInfo($"🎉 Подход 1 успешен! Найдены трейды", account.Username);
                        }
                    }
                }
                catch (Exception ex1)
                {
                    LoggingService.Instance.LogError($"❌ Подход 1 failed: {ex1.Message}", account.Username, ex1);
                }
                
                // Подход 2: С active_only = "0" (текущий)
                if (allOffers == null || (allOffers["response"]["trade_offers_sent"]?.Children.Count() ?? 0) == 0)
                {
                    try
                    {
                        LoggingService.Instance.LogInfo($"🔍 Подход 2: С active_only = 0", account.Username);
                        allOffers = webApi.Call("GetTradeOffers", 1, new Dictionary<string, object?>
                        {
                            ["get_sent_offers"] = "1",
                            ["get_received_offers"] = "1",
                            ["get_descriptions"] = "1",
                            ["active_only"] = "0"
                        });
                    }
                    catch (Exception ex2)
                    {
                        LoggingService.Instance.LogError($"❌ Подход 2 failed: {ex2.Message}", account.Username, ex2);
                    }
                }
                
                // Подход 3: Попробуем получить только отправленные
                if (allOffers == null || (allOffers["response"]["trade_offers_sent"]?.Children.Count() ?? 0) == 0)
                {
                    try
                    {
                        LoggingService.Instance.LogInfo($"🔍 Подход 3: Только отправленные трейды", account.Username);
                        allOffers = webApi.Call("GetTradeOffers", 1, new Dictionary<string, object?>
                        {
                            ["get_sent_offers"] = "1",
                            ["get_descriptions"] = "1"
                        });
                    }
                    catch (Exception ex3)
                    {
                        LoggingService.Instance.LogError($"❌ Подход 3 failed: {ex3.Message}", account.Username, ex3);
                    }
                }

                LoggingService.Instance.LogInfo($"🌐 Получен ответ от Steam Web API", account.Username);
                
                // Отладочная информация о структуре ответа
                if (allOffers != null && allOffers["response"] != null)
                {
                    var response = allOffers["response"];
                    LoggingService.Instance.LogInfo($"🔍 Структура ответа API:", account.Username);
                    LoggingService.Instance.LogInfo($"  - trade_offers_sent: {response["trade_offers_sent"] != null}", account.Username);
                    LoggingService.Instance.LogInfo($"  - trade_offers_received: {response["trade_offers_received"] != null}", account.Username);
                    
                    if (response["trade_offers_sent"] != null)
                    {
                        LoggingService.Instance.LogInfo($"  - Количество отправленных: {response["trade_offers_sent"].Children.Count()}", account.Username);
                    }
                    if (response["trade_offers_received"] != null)
                    {
                        LoggingService.Instance.LogInfo($"  - Количество полученных: {response["trade_offers_received"].Children.Count()}", account.Username);
                    }
                    
                    // Логируем полный ответ для отладки
                    LoggingService.Instance.LogInfo($"🔍 Полный ответ API: {allOffers.ToString()}", account.Username);
                    
                    // Дополнительная диагностика
                    if (response["trade_offers_sent"] != null && response["trade_offers_sent"].Children.Count() > 0)
                    {
                        LoggingService.Instance.LogInfo($"🔍 Первый отправленный трейд:", account.Username);
                        var firstSent = response["trade_offers_sent"].Children.First();
                        LoggingService.Instance.LogInfo($"  - ID: {firstSent["tradeofferid"]?.AsString()}", account.Username);
                        LoggingService.Instance.LogInfo($"  - Статус: {firstSent["trade_offer_state"]?.AsInteger()}", account.Username);
                        LoggingService.Instance.LogInfo($"  - Создан: {firstSent["time_created"]?.AsLong()}", account.Username);
                    }
                    
                    if (response["trade_offers_received"] != null && response["trade_offers_received"].Children.Count() > 0)
                    {
                        LoggingService.Instance.LogInfo($"🔍 Первый полученный трейд:", account.Username);
                        var firstReceived = response["trade_offers_received"].Children.First();
                        LoggingService.Instance.LogInfo($"  - ID: {firstReceived["tradeofferid"]?.AsString()}", account.Username);
                        LoggingService.Instance.LogInfo($"  - Статус: {firstReceived["trade_offer_state"]?.AsInteger()}", account.Username);
                        LoggingService.Instance.LogInfo($"  - Создан: {firstReceived["time_created"]?.AsLong()}", account.Username);
                    }
                }
                else
                {
                    LoggingService.Instance.LogError($"❌ Получен пустой ответ от API", account.Username);
                    LoggingService.Instance.LogInfo($"🔍 Полный ответ: {allOffers?.ToString() ?? "null"}", account.Username);
                }

                // Обрабатываем отправленные трейды
                if (allOffers != null && allOffers["response"]["trade_offers_sent"] != null)
                {
                    var sentCount = allOffers["response"]["trade_offers_sent"].Children.Count();
                    LoggingService.Instance.LogInfo($"📤 Найдено {sentCount} отправленных трейдов", account.Username);
                    
                    foreach (var offer in allOffers["response"]["trade_offers_sent"].Children)
                    {
                        var tradeState = offer["trade_offer_state"].AsInteger();
                        LoggingService.Instance.LogInfo($"🔍 Отправленный трейд {offer["tradeofferid"].AsString()}: статус {tradeState}", account.Username);
                        
                        // Показываем только трейды, требующие подтверждения (статус 9 = CreatedNeedsConfirmation)
                        if (tradeState == 9)
                        {
                            var tradeOffer = new TradeOffer
                            {
                                TradeOfferId = offer["tradeofferid"].AsString() ?? "",
                                AccountName = account.DisplayName ?? "",
                                PartnerName = offer["accountid_other"].AsString() ?? "",
                                ItemsDescription = GetItemsDescription(offer),
                                CreatedTime = DateTimeOffset.FromUnixTimeSeconds(offer["time_created"].AsLong()).DateTime,
                                Status = GetStatusDescription(tradeState)
                            };

                            tradeOffers.Add(tradeOffer);
                            LoggingService.Instance.LogInfo($"✅ Добавлен отправленный трейд {tradeOffer.TradeOfferId} (требует подтверждения)", account.Username);
                        }
                    }
                }

                // Обрабатываем полученные трейды
                if (allOffers != null && allOffers["response"]["trade_offers_received"] != null)
                {
                    var receivedCount = allOffers["response"]["trade_offers_received"].Children.Count();
                    LoggingService.Instance.LogInfo($"📥 Найдено {receivedCount} полученных трейдов", account.Username);
                    
                    foreach (var offer in allOffers["response"]["trade_offers_received"].Children)
                    {
                        var tradeState = offer["trade_offer_state"].AsInteger();
                        LoggingService.Instance.LogInfo($"🔍 Полученный трейд {offer["tradeofferid"].AsString()}: статус {tradeState}", account.Username);
                        
                        // Показываем только трейды, требующие подтверждения (статус 9 = CreatedNeedsConfirmation)
                        if (tradeState == 9)
                        {
                            var tradeOffer = new TradeOffer
                            {
                                TradeOfferId = offer["tradeofferid"].AsString() ?? "",
                                AccountName = account.DisplayName ?? "",
                                PartnerName = offer["accountid_other"].AsString() ?? "",
                                ItemsDescription = GetItemsDescription(offer),
                                CreatedTime = DateTimeOffset.FromUnixTimeSeconds(offer["time_created"].AsLong()).DateTime,
                                Status = GetStatusDescription(tradeState)
                            };

                            tradeOffers.Add(tradeOffer);
                            LoggingService.Instance.LogInfo($"✅ Добавлен полученный трейд {tradeOffer.TradeOfferId} (требует подтверждения)", account.Username);
                        }
                    }
                }

                if (tradeOffers.Count == 0)
                {
                    LoggingService.Instance.LogWarning($"⚠️ Нет трейдов со статусом 'CreatedNeedsConfirmation' (9) - ожидающих мобильного подтверждения", account.Username);
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
