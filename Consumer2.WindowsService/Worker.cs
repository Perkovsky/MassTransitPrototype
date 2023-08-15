using MassTransit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer2.WindowsService
{
	public class Worker
	{
		private readonly IBusControl _bus;

		public Worker(IBusControl bus)
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
			Console.WriteLine("Listening Event Bus commands...");
			_bus.Start();
			return true;
		}

		public void Stop()
		{
			_bus.Stop();
		}

		#endregion
	}
}
