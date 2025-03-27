using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using Backend.Models;
using Backend.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Backend.Services
{
    public class NetworkService
    {
        private readonly ILogger<NetworkService> _logger;

        public NetworkService(ILogger<NetworkService> logger)
        {
            _logger = logger;
        }

        public List<NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            try
            {
                _logger.LogInformation("Начало получения сетевых интерфейсов");
                var interfaces = new List<NetworkInterfaceInfo>();
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                _logger.LogInformation($"Найдено {networkInterfaces.Length} сетевых интерфейсов");

                foreach (var ni in networkInterfaces)
                {
                    try
                    {
                        // Пропускаем отключенные интерфейсы
                        if (ni.OperationalStatus != OperationalStatus.Up)
                        {
                            _logger.LogInformation($"Пропуск отключенного интерфейса: {ni.Name}");
                            continue;
                        }

                        var properties = ni.GetIPProperties();
                        
                        // Получаем все IP-адреса (IPv4 и IPv6)
                        var addresses = properties.UnicastAddresses
                            .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            .ToList();

                        if (addresses.Any())
                        {
                            _logger.LogInformation($"Обработка интерфейса: {ni.Name}");
                            
                            // Получаем основной IPv4 адрес
                            var mainAddress = addresses.First();
                            
                            interfaces.Add(new NetworkInterfaceInfo
                            {
                                Name = ni.Name,
                                Description = ni.Description,
                                IpAddress = mainAddress.Address.ToString(),
                                SubnetMask = mainAddress.IPv4Mask?.ToString() ?? "Unknown",
                                MacAddress = ni.GetPhysicalAddress()?.ToString() ?? "Unknown",
                                Status = ni.OperationalStatus,
                                Speed = ni.Speed,
                                InterfaceType = ni.NetworkInterfaceType
                            });

                            _logger.LogInformation($"Добавлен интерфейс: {ni.Name} с IP {mainAddress.Address}");
                        }
                        else
                        {
                            _logger.LogInformation($"Интерфейс {ni.Name} не имеет IPv4 адресов");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка при обработке интерфейса {ni.Name}");
                    }
                }

                _logger.LogInformation($"Успешно получено {interfaces.Count} интерфейсов");
                return interfaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка сетевых интерфейсов");
                throw;
            }
        }

        public async Task<UrlAnalysisResult> AnalyzeUrl(string url)
        {
            var result = new UrlAnalysisResult
            {
                OriginalUrl = url,
                Timestamp = DateTime.UtcNow,
                Scheme = string.Empty,
                Host = string.Empty,
                Path = string.Empty,
                Query = string.Empty,
                QueryParameters = new List<QueryParameter>(),
                Fragment = string.Empty,
                IsValid = false,
                IsAvailable = false,
                DnsRecords = Array.Empty<string>(),
                AddressType = "Unknown"
            };

            try
            {
                // Проверяем, начинается ли URL с протокола
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                var uri = new Uri(url);
                result.IsValid = true;

                // Основные компоненты
                result.Scheme = uri.Scheme;
                result.Host = uri.Host;
                result.Port = uri.Port;
                result.Path = uri.AbsolutePath;
                result.Query = uri.Query;
                result.Fragment = uri.Fragment;

                // Парсинг параметров запроса
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    var parameters = query.Split('&');
                    foreach (var param in parameters)
                    {
                        var parts = param.Split('=');
                        result.QueryParameters.Add(new QueryParameter
                        {
                            Name = parts[0],
                            Value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : null
                        });
                    }
                }

                // Дополнительные компоненты
                result.UserInfo = uri.UserInfo;
                result.Authority = uri.Authority;
                result.AbsoluteUri = uri.AbsoluteUri;
                result.LocalPath = uri.LocalPath;
                result.QueryString = uri.Query;
                result.PathAndQuery = uri.PathAndQuery;

                // Проверка доступности (ping)
                // Доступность означает, что сервер отвечает на ICMP-запросы (ping)
                // Это не гарантирует, что веб-сервер работает, но показывает, что хост активен
                result.IsAvailable = await CheckHostAvailability(uri.Host);

                // Получение DNS-информации
                result.DnsRecords = await GetDnsRecords(uri.Host);

                // Определение типа адреса
                result.AddressType = DetermineAddressType(uri.Host);

                _logger.LogInformation($"URL успешно проанализирован: {uri.AbsoluteUri}");
            }
            catch (UriFormatException ex)
            {
                result.IsValid = false;
                result.ValidationError = ex.Message;
                _logger.LogError(ex, "Ошибка формата URL");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationError = "Неизвестная ошибка при анализе URL";
                _logger.LogError(ex, "Ошибка при анализе URL");
            }

            return result;
        }

        private async Task<bool> CheckHostAvailability(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host);
                _logger.LogInformation($"Проверка доступности {host}: {reply.Status}");
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке доступности {host}");
                return false;
            }
        }

        private async Task<string[]> GetDnsRecords(string host)
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(host);
                var records = hostEntry.AddressList.Select(addr => addr.ToString()).ToArray();
                _logger.LogInformation($"Получены DNS записи для {host}: {string.Join(", ", records)}");
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении DNS записей для {host}");
                return Array.Empty<string>();
            }
        }

        private string DetermineAddressType(string host)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                if (IPAddress.IsLoopback(ipAddress))
                    return "Loopback";
                if (ipAddress.IsPrivate())
                    return "Private";
                return "Public";
            }
            return "Domain";
        }

        public async Task<SpeedTestResult> TestInterfaceSpeed(string interfaceName)
        {
            try
            {
                _logger.LogInformation($"Начало тестирования скорости для интерфейса: {interfaceName}");
                
                // Получаем IP-адрес интерфейса
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.Name == interfaceName);

                if (networkInterface == null)
                {
                    throw new ArgumentException($"Интерфейс {interfaceName} не найден");
                }

                var properties = networkInterface.GetIPProperties();
                var ipv4Address = properties.UnicastAddresses
                    .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork);

                if (ipv4Address == null)
                {
                    throw new ArgumentException($"Интерфейс {interfaceName} не имеет IPv4 адреса");
                }

                // Тестируем скорость загрузки
                var downloadSpeed = await MeasureDownloadSpeed(ipv4Address.Address);
                
                // Тестируем скорость отдачи
                var uploadSpeed = await MeasureUploadSpeed(ipv4Address.Address);

                _logger.LogInformation($"Тестирование скорости завершено для {interfaceName}");
                return new SpeedTestResult
                {
                    DownloadSpeed = downloadSpeed,
                    UploadSpeed = uploadSpeed,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при тестировании скорости для интерфейса {interfaceName}");
                throw;
            }
        }

        private async Task<double> MeasureDownloadSpeed(IPAddress interfaceAddress)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Список тестовых серверов, доступных из России
                var testServers = new[]
                {
                    "https://speed.cloudflare.com/__down",
                    "https://speedtest.selectel.ru/1000MB.bin",
                    "https://speedtest.rt.ru/1000MB.bin",
                    "https://speedtest.mts.ru/1000MB.bin"
                };

                double totalSpeed = 0;
                int successfulTests = 0;

                foreach (var server in testServers)
                {
                    try
                    {
                        var startTime = DateTime.Now;
                        var response = await client.GetAsync(server);
                        var endTime = DateTime.Now;

                        if (response.IsSuccessStatusCode)
                        {
                            var duration = (endTime - startTime).TotalSeconds;
                            var contentLength = response.Content.Headers.ContentLength ?? 0;
                            var speedMbps = (contentLength * 8.0) / (1024 * 1024 * duration);
                            totalSpeed += speedMbps;
                            successfulTests++;
                            _logger.LogInformation($"Измерение скорости с {server}: {speedMbps:F2} Мбит/с");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Ошибка при измерении скорости с {server}");
                    }
                }

                return successfulTests > 0 ? Math.Round(totalSpeed / successfulTests, 2) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при измерении скорости загрузки");
                return 0;
            }
        }

        private async Task<double> MeasureUploadSpeed(IPAddress interfaceAddress)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Список тестовых серверов для отдачи, доступных из России
                var testServers = new[]
                {
                    "https://speed.cloudflare.com/__up",
                    "https://speedtest.selectel.ru/upload.php",
                    "https://speedtest.rt.ru/upload.php",
                    "https://speedtest.mts.ru/upload.php"
                };

                double totalSpeed = 0;
                int successfulTests = 0;

                // Создаем тестовые данные разного размера
                var testDataSizes = new[] { 1024 * 1024, 2 * 1024 * 1024, 5 * 1024 * 1024 }; // 1MB, 2MB, 5MB

                foreach (var server in testServers)
                {
                    foreach (var size in testDataSizes)
                    {
                        try
                        {
                            var testData = new byte[size];
                            var content = new ByteArrayContent(testData);

                            var startTime = DateTime.Now;
                            var response = await client.PostAsync(server, content);
                            var endTime = DateTime.Now;

                            if (response.IsSuccessStatusCode)
                            {
                                var duration = (endTime - startTime).TotalSeconds;
                                var speedMbps = (size * 8.0) / (1024 * 1024 * duration);
                                totalSpeed += speedMbps;
                                successfulTests++;
                                _logger.LogInformation($"Измерение скорости отдачи с {server} (размер: {size/1024/1024}MB): {speedMbps:F2} Мбит/с");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Ошибка при измерении скорости отдачи с {server}");
                        }
                    }
                }

                return successfulTests > 0 ? Math.Round(totalSpeed / successfulTests, 2) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при измерении скорости отдачи");
                return 0;
            }
        }
    }

    public class SpeedTestResult
    {
        public double DownloadSpeed { get; set; }
        public double UploadSpeed { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 