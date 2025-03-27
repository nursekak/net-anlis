using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.Models;
using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NetworkController : ControllerBase
    {
        private readonly NetworkService _networkService;
        private readonly ILogger<NetworkController> _logger;

        public NetworkController(NetworkService networkService, ILogger<NetworkController> logger)
        {
            _networkService = networkService;
            _logger = logger;
        }

        [HttpGet("test")]
        public ActionResult<object> Test()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var result = new
                {
                    TotalInterfaces = networkInterfaces.Length,
                    Interfaces = networkInterfaces.Select(ni => new
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        Status = ni.OperationalStatus,
                        Type = ni.NetworkInterfaceType,
                        Speed = ni.Speed,
                        IsUp = ni.OperationalStatus == OperationalStatus.Up,
                        HasIPv4 = ni.GetIPProperties().UnicastAddresses
                            .Any(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при тестировании сетевых интерфейсов");
                return StatusCode(500, "Ошибка при тестировании");
            }
        }

        [HttpGet("interfaces")]
        public ActionResult<List<NetworkInterfaceInfo>> GetNetworkInterfaces()
        {
            try
            {
                _logger.LogInformation("Получен запрос на список сетевых интерфейсов");
                var interfaces = _networkService.GetNetworkInterfaces();
                _logger.LogInformation($"Успешно возвращено {interfaces.Count} интерфейсов");
                return Ok(interfaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка интерфейсов");
                return StatusCode(500, "Внутренняя ошибка сервера при получении списка интерфейсов");
            }
        }

        [HttpGet("analyze-url")]
        public async Task<ActionResult<UrlAnalysisResult>> AnalyzeUrl([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Получен пустой URL");
                return BadRequest("URL не может быть пустым");
            }

            try
            {
                _logger.LogInformation($"Получен запрос на анализ URL: {url}");
                var result = await _networkService.AnalyzeUrl(url);
                _logger.LogInformation("URL успешно проанализирован");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при анализе URL: {url}");
                return StatusCode(500, "Внутренняя ошибка сервера при анализе URL");
            }
        }

        [HttpGet("test-speed/{interfaceName}")]
        public async Task<ActionResult<SpeedTestResult>> TestInterfaceSpeed(string interfaceName)
        {
            try
            {
                var result = await _networkService.TestInterfaceSpeed(interfaceName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Ошибка при тестировании скорости интерфейса");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при тестировании скорости интерфейса");
                return StatusCode(500, "Ошибка при тестировании скорости интерфейса");
            }
        }
    }
} 