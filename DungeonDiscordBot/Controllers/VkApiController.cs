using System.Threading.Tasks;

using DungeonDiscordBot.Controllers.Abstraction;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using VkNet.Abstractions;
using VkNet.AudioBypassService.Extensions;

using ILogger = Serilog.ILogger;

namespace DungeonDiscordBot.Controllers
{
    public class VkApiController : IVkApiController
    {
        private readonly ILogger _logger;
        private IVkApi _api = null!;
        
        
        public VkApiController(ILogger logger)
        {
            _logger = logger;
        }
        
        public async Task Init(IServicesAggregator aggregator)
        {
            _logger.Information("Initializing VK service...");

            ServiceCollection services = new ServiceCollection();
            services.AddAudioBypass();
            // services.AddLogging(builder => {
            //     builder.ClearProviders();
            //     builder.SetMinimumLevel(LogLevel.Information);
            //     builder.AddSerilog(_logger);
            // });
            
            // _api = new VkApi(services);   
            // await _api.AuthorizeAsync(new ApiAuthParams {
            //     Login = "incorobots@gmail.com",
            //     Password = "ckFM1598763~5",
            //     TwoFactorAuthorization = Console.ReadLine
            // });

            // var audio = await _api.Audio.GetAsync(new AudioGetParams() {
            //     AudioIds = new [] {
            //         (long)456239332
            //     },
            //     OwnerId = 170974401,
            //     AccessKey = "80be894787bd257ed1"
            // }); 
            //
            // string outputPath = Path.ChangeExtension(Path.GetTempFileName(), ".mp3");
            //
            // IConversionResult result = await FFmpeg.Conversions.New()
            //     .AddParameter("-http_persistent false")
            //     .AddParameter($"-i {audio.First().Url.AbsoluteUri}")
            //     .SetOutput(outputPath)
            //     .Start();

            //var users = await _api.Users.GetAsync(new [] { (long)_api.UserId! });
            
            _logger.Information("VK service initialized");
        }
    }
}