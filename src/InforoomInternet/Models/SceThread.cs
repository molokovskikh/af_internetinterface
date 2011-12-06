using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using InternetInterface.Models;

namespace InforoomInternet.Models
{
	public enum UnknownClientStatus
	{
		Connected,
		InProcess,
		Error,
		NoInfo
	}

	public class ReturnInFormInfo
	{
		public int Iteration;
		public bool WaitingInfo;
		public string Message;
		public UnknownClientStatus Status;
	}

	public class UnknownClientInfo
	{
		public DateTime UpdateDate;
		public UnknownClientStatus Status;
		public uint Client;
		public int? Interation;
	}

	public class ClientData
	{
		private static List<UnknownClientInfo> _info = new List<UnknownClientInfo>();
		private static Mutex _mutex = new Mutex();

		public static UnknownClientStatus Get(uint client)
		{
			_mutex.WaitOne();
			try
			{
				var info = GetInfo(client);
				if (info == null)
					return UnknownClientStatus.NoInfo;
				return info.Status;
			}
			finally {
				_mutex.ReleaseMutex();
			}
		}

		public static UnknownClientInfo GetInfo(uint client)
		{
			_mutex.WaitOne();
			try
			{
				return _info.Where(i => i.Client == client).FirstOrDefault();
			}
			finally {
				_mutex.ReleaseMutex();
			}
		}

		public static void Set(uint client, UnknownClientStatus status, int? iteration)
		{
			_mutex.WaitOne();
			try
			{
				var info = _info.Where(i => i.Client == client).FirstOrDefault();
				if (info == null)
				{
					_info.Add(new UnknownClientInfo
					{
						Client = client,
						Status = status,
						UpdateDate = DateTime.Now,
						Interation = iteration
					});
				}
				else
				{
					info.Status = status;
					info.UpdateDate = DateTime.Now;
					info.Interation = iteration;
				}
			}
			finally {
				_mutex.ReleaseMutex();
			}
		}

		public static void Set(uint client, UnknownClientStatus status)
		{
			Set(client, status, null);
		}
	}

	public class SceThread
	{
		public readonly Thread CurrentThread;

		private const int IterationCount = 100;

		private Lease _lease;
		private string _ip;
		private uint _client;

		public SceThread(Lease lease, string ip)
		{
			_lease = lease;
			_ip = ip;
			CurrentThread = new Thread(Do);
		}

		public void Go()
		{
			CurrentThread.Start();
		}

		private void StatusSet(UnknownClientStatus status, int? iteration = null)
		{
			ClientData.Set(_client , status, iteration);
		}

		private UnknownClientStatus ClientStatus
		{
			get { return ClientData.Get(_client); }
		}

		private void Do()
		{
			_client = _lease.Endpoint.Client.Id;
			for (int i = 0; i < IterationCount; i++) {
				try
				{
#if DEBUG
					Thread.Sleep(40);
					if (i < 70)
						throw new Exception();
#else
					SceHelper.Login(_lease, _ip);
#endif
					StatusSet(UnknownClientStatus.Connected, i);
					break;
				}
				catch (Exception) {
				}
				finally {
					if (ClientStatus != UnknownClientStatus.Connected)
						StatusSet(UnknownClientStatus.InProcess, i);
				}
			}
			if (ClientStatus == UnknownClientStatus.InProcess)
				StatusSet(UnknownClientStatus.Error);
		}
	}
}