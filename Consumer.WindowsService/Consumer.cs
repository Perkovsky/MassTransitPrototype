using MassTransit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer.WindowsService
{
	public class Consumer
	{
		private readonly IBusControl _bus;

		public Consumer(IBusControl bus)
		{
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
		}

		public async Task<bool> StartAsync(CancellationToken cancellationToken)
		{
			await _bus.StartAsync(cancellationToken).ConfigureAwait(false);
			return await Task.FromResult(true);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return _bus.StopAsync(cancellationToken);
		}

		#region Need for Topshelf

		public bool Start()
		{
			//return StartAsync(new CancellationToken()).Result;
			Console.WriteLine("Listening Event Bus commands...");
			_bus.Start();
			return true;
		}

		public void Stop()
		{
			//StopAsync(new CancellationToken());
			_bus.Stop();
		}

		#endregion
	}
}
