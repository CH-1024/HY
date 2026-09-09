using StackExchange.Redis;

namespace HY.ApiService.Services
{
    public class RedisTimeoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisTimeoutService> _logger;
        private readonly IRedisCallService _redisCallService;

        // 所有 Scope 服务都改为从 _scopeFactory 中获取
        //private readonly IChatNotificationService _chatNotificationService;

        private ISubscriber _subscriber = null!;

        public RedisTimeoutService(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis, ILogger<RedisTimeoutService> logger, IRedisCallService redisCallService/*, IChatNotificationService chatNotificationService*/)
        {
            _scopeFactory = scopeFactory;
            _redis = redis;
            _logger = logger;
            _redisCallService = redisCallService;
            //_chatNotificationService = chatNotificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _subscriber = _redis.GetSubscriber();

            // 监听 Redis 0 号数据库的 Key 过期事件
            await _subscriber.SubscribeAsync(RedisChannel.Literal("__keyevent@0__:expired"), async (channel, value) =>
            {
                // 当有 Key 过期时，调用处理方法
                await OnCallExpiry(value);
            });

            _logger.LogInformation("Redis timeout listener started.");

            // BackgroundService 必须一直运行
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_subscriber != null)
            {
                await _subscriber.UnsubscribeAsync(RedisChannel.Literal("__keyevent@0__:expired"));
            }

            _logger.LogInformation("Redis timeout listener stopped.");

            await base.StopAsync(cancellationToken);
        }




        private async Task OnCallExpiry(RedisValue value)
         {
            var key = value.ToString();

            _logger.LogInformation("Redis key expired: {Key}", key);

            // 只处理 Expire:{CallId}
            if (!key.StartsWith("Call:Expire:", StringComparison.Ordinal))
                return;

            var callId = key["Call:Expire:".Length..];

            if (string.IsNullOrWhiteSpace(callId))
                return;

            using var scope = _scopeFactory.CreateScope();

            var _chatNotificationService = scope.ServiceProvider.GetRequiredService<IChatNotificationService>();

            // 使用 Scoped 的 _redisCallService
            var callDto = await _redisCallService.ExpiryCall(callId);

            await _chatNotificationService.ExpiryCallNotify(callDto!);
        }
    }
}
