namespace PaperlessServices.Tests.Unit;

public sealed class ContainerFixtureBaseTests
{
	[Fact]
	public async Task ThrowDisposalFailuresAsync_WhenSeveralOperationsFail_ReportsEveryFailure()
	{
		List<string> disposed = [];
		Exception sutFailure = CaptureFailure("sut");
		Exception rabbitFailure = CaptureFailure("rabbit");
		Exception elasticFailure = CaptureFailure("elastic");

		await using OwnedAsyncDisposable rabbit = new("rabbit", disposed, rabbitFailure);
		await using OwnedAsyncDisposable minio = new("minio", disposed);
		await using OwnedAsyncDisposable elastic = new("elastic", disposed, elasticFailure);
		List<Task> containerDisposals =
		[
			rabbit.DisposeAsync().AsTask(),
			minio.DisposeAsync().AsTask(),
			elastic.DisposeAsync().AsTask()
		];

		Func<Task> act = () => ContainerFixtureBase.ThrowDisposalFailuresAsync(
			sutFailure,
			containerDisposals).AsTask();

		AggregateException thrown =
			(await act.Should().ThrowExactlyAsync<AggregateException>()).Which;

		thrown.InnerExceptions.Should().Equal(
			sutFailure,
			rabbitFailure,
			elasticFailure);
		disposed.Should().Equal("rabbit", "minio", "elastic");
	}

	[Fact]
	public async Task ThrowDisposalFailuresAsync_WhenOneOperationFails_PreservesExceptionAndOrigin()
	{
		List<string> disposed = [];
		Exception failure = CaptureFailure("rabbit");
		await using OwnedAsyncDisposable rabbit = new("rabbit", disposed, failure);

		Func<Task> act = () => ContainerFixtureBase.ThrowDisposalFailuresAsync(
			sutFailure: null,
			[rabbit.DisposeAsync().AsTask()]).AsTask();

		InvalidOperationException thrown =
			(await act.Should().ThrowExactlyAsync<InvalidOperationException>()).Which;

		thrown.Should().BeSameAs(failure);
		thrown.StackTrace.Should().Contain(nameof(CaptureFailure));
		disposed.Should().Equal("rabbit");
	}

	[Fact]
	public async Task ThrowDisposalFailuresAsync_WhenOneOperationThrowsAggregate_PreservesExceptionAndOrigin()
	{
		List<string> disposed = [];
		AggregateException failure = CaptureAggregateFailure();
		await using OwnedAsyncDisposable rabbit = new("rabbit", disposed, failure);

		Func<Task> act = () => ContainerFixtureBase.ThrowDisposalFailuresAsync(
			sutFailure: null,
			[rabbit.DisposeAsync().AsTask()]).AsTask();

		AggregateException thrown =
			(await act.Should().ThrowExactlyAsync<AggregateException>()).Which;

		thrown.Should().BeSameAs(failure);
		thrown.StackTrace.Should().Contain(nameof(CaptureAggregateFailure));
		disposed.Should().Equal("rabbit");
	}

	private static InvalidOperationException CaptureFailure(string message)
	{
		try
		{
			throw new InvalidOperationException(message);
		}
		catch (InvalidOperationException exception)
		{
			return exception;
		}
	}

	private static AggregateException CaptureAggregateFailure()
	{
		try
		{
			throw new AggregateException("rabbit", new InvalidOperationException("inner"));
		}
		catch (AggregateException exception)
		{
			return exception;
		}
	}

	private sealed class OwnedAsyncDisposable(
		string name,
		ICollection<string> disposed,
		Exception? failure = null) : IAsyncDisposable
	{
		private bool _disposed;

		public ValueTask DisposeAsync()
		{
			if (_disposed) return ValueTask.CompletedTask;

			_disposed = true;
			disposed.Add(name);

			return failure is null
				? ValueTask.CompletedTask
				: ValueTask.FromException(failure);
		}
	}
}
