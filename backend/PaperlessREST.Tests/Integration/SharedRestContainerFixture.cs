using PaperlessREST.Host;

[assembly: CaptureConsole]
[assembly: CaptureTrace]

namespace PaperlessREST.Tests.Integration;

public sealed class SharedRestContainerFixture() : ContainerFixtureBase(usesPostgres: true)
{
	static SharedRestContainerFixture() => TestEnv.Load();

	public HttpClient Client { get; private set; } = null!;
	public IDbContextFactory<DocumentPersistence> DbFactory { get; private set; } = null!;

	public AsyncServiceScope CreateAsyncScope() => Services.CreateAsyncScope();

	private WebApplicationFactory<Program>? _factory;

	protected override async ValueTask ConfigureSutAsync()
	{
		// WebApplicationFactory's environment provider outranks test-host configuration,
		// so its process environment must contain the real container endpoints.
		Environment.SetEnvironmentVariable("CONNECTIONSTRINGS__PAPERLESSDB", PostgresConnectionString);
		Environment.SetEnvironmentVariable("CONNECTIONSTRINGS__HANGFIRE", PostgresConnectionString);
		Environment.SetEnvironmentVariable("RABBITMQ__URI", RabbitConnectionString);
		Environment.SetEnvironmentVariable("STORAGE__MINIO__ENDPOINT", MinioEndpoint);
		Environment.SetEnvironmentVariable("STORAGE__MINIO__ACCESSKEY", MinioAccessKey);
		Environment.SetEnvironmentVariable("STORAGE__MINIO__SECRETKEY", MinioSecretKey);
		Environment.SetEnvironmentVariable("STORAGE__MINIO__BUCKETNAME", BucketName);
		Environment.SetEnvironmentVariable("ELASTICSEARCH__URI", ElasticsearchUri);
		Environment.SetEnvironmentVariable("ELASTICSEARCH__DEFAULTINDEX", IndexName);

		var batchRoot = Path.Combine(Path.GetTempPath(), $"paperless-batch-{Guid.NewGuid():N}");
		Environment.SetEnvironmentVariable("BATCH__INPUTPATH", Path.Combine(batchRoot, "input"));
		Environment.SetEnvironmentVariable("BATCH__ARCHIVEPATH", Path.Combine(batchRoot, "archive"));
		Environment.SetEnvironmentVariable("BATCH__ERRORPATH", Path.Combine(batchRoot, "error"));
		Environment.SetEnvironmentVariable("BATCH__FILEPATTERN", "*.xml");
		Environment.SetEnvironmentVariable("BATCH__CRONEXPRESSION", "0 2 * * *");
		Environment.SetEnvironmentVariable("BATCH__TIMEZONEID", "UTC");

		_factory = new ConfiguredWebApplicationFactory(PostgresConnectionString);

		Client = _factory.CreateClient();
		Services = _factory.Services;
		DbFactory = Services.GetRequiredService<IDbContextFactory<DocumentPersistence>>();

		await using var db = await DbFactory.CreateDbContextAsync();
		await db.Database.MigrateAsync();
	}

	protected override async ValueTask DisposeSutAsync()
	{
		if (_factory is not null)
			await _factory.DisposeAsync();
	}

	private sealed class ConfiguredWebApplicationFactory(string postgresConnectionString)
		: WebApplicationFactory<Program>
	{
		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.UseEnvironment("Test");

			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<IHostedService>();

				services.RemoveAll<IDbContextFactory<DocumentPersistence>>();

				var dataSource = new NpgsqlDataSourceBuilder(postgresConnectionString)
					.MapEnum<DocumentStatus>("document_status")
					.Build();

				services.AddPooledDbContextFactory<DocumentPersistence>(opts =>
					opts.UseNpgsql(dataSource));

				services.RemoveAll<JobStorage>();
				services.AddSingleton<JobStorage>(new MemoryStorage());

				services.AddFakeLogging();
			});
		}
	}
}
