using Confluent.Kafka;

namespace WarehouseManager.Services;

public class DefectReportConsumer : BackgroundService
{
    private readonly ILogger<DefectReportConsumer> _logger;
    private readonly IConfiguration _configuration;
    private const string _kafkaTopic = "defectsTopic";

    public DefectReportConsumer(ILogger<DefectReportConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "defect-report-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(_kafkaTopic);
                _logger.LogInformation("DefectReportConsumer connected, listening on topic '{Topic}'", _kafkaTopic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    _logger.LogInformation("Defect report event received — Key: {Key}, Value: {Value}",
                        consumeResult.Message.Key, consumeResult.Message.Value);
                }

                consumer.Close();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DefectReportConsumer error, retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
