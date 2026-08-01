namespace Paperless.TestSupport;

/// <summary>
///     Owns shared integration infrastructure while derived fixtures own the system under test.
/// </summary>
public abstract class ContainerFixtureBase : IAsyncLifetime
{
	private const int ElasticsearchPort = 9200;

	private readonly PostgreSqlContainer? _postgres;
	private readonly RabbitMqContainer _rabbit = TestContainers.RabbitMq();
	private readonly MinioContainer _minio = TestContainers.Minio();
	private readonly ElasticsearchContainer _elastic = TestContainers.Elasticsearch();

	protected ContainerFixtureBase(bool usesPostgres)
	{
		_postgres = usesPostgres ? TestContainers.Postgres() : null;
	}

	/// <summary>Unique per-fixture bucket name; the bucket is created during init.</summary>
	protected string BucketName { get; } = $"test-{Guid.NewGuid():N}";

	/// <summary>Unique per-fixture default Elasticsearch index name.</summary>
	protected string IndexName { get; } = $"test_{Guid.NewGuid():N}";

	/// <summary>MinIO host:port endpoint string (valid after containers start).</summary>
	protected string MinioEndpoint => MinioBucket.Endpoint(_minio);

	protected string MinioAccessKey => _minio.GetAccessKey();
	protected string MinioSecretKey => _minio.GetSecretKey();
	protected string RabbitConnectionString => _rabbit.GetConnectionString();

	protected string ElasticsearchUri =>
		$"http://{_elastic.Hostname}:{_elastic.GetMappedPublicPort(ElasticsearchPort)}";

	/// <summary>Postgres connection string; throws when the fixture has no Postgres container.</summary>
	protected string PostgresConnectionString =>
		(_postgres ?? throw new InvalidOperationException(
			"This fixture did not request a Postgres container."))
		.GetConnectionString();

	/// <summary>Service provider for the constructed SUT. Assigned by <see cref="ConfigureSutAsync" />.</summary>
	public IServiceProvider Services { get; protected set; } = null!;

	public async ValueTask InitializeAsync()
	{
		var starts = new List<Task>
		{
			_rabbit.StartAsync(),
			_minio.StartAsync(),
			_elastic.StartAsync()
		};
		if (_postgres is not null) starts.Add(_postgres.StartAsync());
		await Task.WhenAll(starts);

		await MinioBucket.CreateBucketAsync(_minio, BucketName);

		await ConfigureSutAsync();
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			await DisposeSutAsync();
		}
		finally
		{
			List<Task> disposalTasks =
			[
				_rabbit.DisposeAsync().AsTask(),
				_minio.DisposeAsync().AsTask(),
				_elastic.DisposeAsync().AsTask()
			];
			if (_postgres is not null)
			{
				disposalTasks.Add(_postgres.DisposeAsync().AsTask());
			}

			await Task.WhenAll(disposalTasks);
		}
	}

	/// <summary>
	///     Builds the system under test and assigns <see cref="Services" />.
	///     Runs after containers are started and the bucket is created.
	/// </summary>
	protected abstract ValueTask ConfigureSutAsync();

	/// <summary>Tears down the system under test (host/factory). Default: no-op.</summary>
	protected virtual ValueTask DisposeSutAsync() => ValueTask.CompletedTask;

	/// <summary>
	///     Polls Elasticsearch until a document is found or timeout occurs.
	///     Replaces brittle Task.Delay patterns with deterministic polling.
	/// </summary>
	public async Task<GetResponse<T>> WaitForDocumentAsync<T>(
		string documentId,
		CancellationToken cancellationToken,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null)
	{
		timeout ??= TimeSpan.FromSeconds(10);
		pollInterval ??= TimeSpan.FromMilliseconds(100);

		var client = Services.GetRequiredService<ElasticsearchClient>();
		using CancellationTokenSource timeoutCts = new(timeout.Value);
		using CancellationTokenSource linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

		try
		{
			while (true)
			{
				var response = await client.GetAsync<T>(
					documentId,
					g => g.Index(client.ElasticsearchClientSettings.DefaultIndex),
					linkedCts.Token);

				if (response.Found)
				{
					return response;
				}

				await Task.Delay(pollInterval.Value, linkedCts.Token);
			}
		}
		catch (OperationCanceledException) when (
			timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			return await client.GetAsync<T>(
				documentId,
				g => g.Index(client.ElasticsearchClientSettings.DefaultIndex),
				cancellationToken);
		}
	}

	/// <summary>
	///     Polls Elasticsearch search until results are found or timeout occurs.
	/// </summary>
	public async Task<SearchResponse<T>> WaitForSearchResultsAsync<T>(
		Action<SearchRequestDescriptor<T>> configureSearch,
		CancellationToken cancellationToken,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null)
	{
		timeout ??= TimeSpan.FromSeconds(30);
		pollInterval ??= TimeSpan.FromMilliseconds(100);

		var client = Services.GetRequiredService<ElasticsearchClient>();
		using CancellationTokenSource timeoutCts = new(timeout.Value);
		using CancellationTokenSource linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

		try
		{
			// Refresh.True writes can still lag behind search on slow CI storage.
			await client.Indices.RefreshAsync(
				r => r.Indices(client.ElasticsearchClientSettings.DefaultIndex),
				linkedCts.Token);

			while (true)
			{
				var response = await client.SearchAsync<T>(configureSearch, linkedCts.Token);

				if (response.Documents.Count > 0)
				{
					return response;
				}

				await Task.Delay(pollInterval.Value, linkedCts.Token);
			}
		}
		catch (OperationCanceledException) when (
			timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			return await client.SearchAsync(configureSearch, cancellationToken);
		}
	}
}
