using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace InforoomControlPanel.Helpers
{
	public enum Verbs
	{
		WILL = 251,
		WONT = 252,
		DO = 253,
		DONT = 254,
		IAC = 255
	}

	internal enum Options
	{
		SGA = 3
	}

	public enum VerbsByte : byte
	{
		WILL = 0xFB,
		WONT = 0xFC,
		DO = 0xFD,
		DONT = 0xFE,
		IAC = 0xFF,
		SB = 0xFA,
		SE = 0xF0
	}

	public enum TelnetOption : byte
	{
		/// <summary>
		/// Двоичный обмен
		/// </summary>
		BinaryTransmission = 0x00,

		/// <summary>
		/// Эхо
		/// </summary>
		Echo = 0x01,

		/// <summary>
		/// Повторное соединение
		/// </summary>
		Reconnection = 0x02,

		/// <summary>
		/// Подавление буферизации ввода
		/// </summary>
		SuppressGoAhead = 0x03,

		/// <summary>
		/// Диалог о размере сообщения
		/// </summary>
		ApproxMessageSizeNegotiation = 0x04,

		/// <summary>
		/// Статус
		/// </summary>
		Status = 0x05,

		/// <summary>
		/// Временная метка
		/// </summary>
		TimingMark = 0x06,

		/// <summary>
		/// Удаленный доступ и отклик
		/// </summary>
		RemoteControlledTransAndEcho = 0x07,

		/// <summary>
		/// Длина выходной строки
		/// </summary>
		OutputLineWidth = 0x08,

		/// <summary>
		/// Размер выходной страницы
		/// </summary>
		OutputPageSize = 0x09,

		/// <summary>
		/// Режим вывода символов <возврат каретки>
		/// </summary>
		OutputCarriageReturnDisposition = 0x0A,

		/// <summary>
		/// Вывод горизонтальной табуляции
		/// </summary>
		OutputHorizontalTabStops = 0x0B,

		/// <summary>
		/// Установка положения табуляции при выводе
		/// </summary>
		OutputHorizontalTabDisposition = 0x0C,

		/// <summary>
		/// Режим вывода команды смены страницы
		/// </summary>
		OutputFormfeedDisposition = 0x0D,

		/// <summary>
		/// Вывод вертикальной табуляции
		/// </summary>
		OutputVerticalTabstops = 0x0E,

		/// <summary>
		/// Определяет положение вертикальной табуляции
		/// </summary>
		OutputVerticalTabDisposition = 0x0F,

		/// <summary>
		/// Режим вывода символа "перевод строки"
		/// </summary>
		OutputLinefeedDisposition = 0x10,

		/// <summary>
		/// Расширенный набор кодов ASCII
		/// </summary>
		ExtendedASCII = 0x11,

		/// <summary>
		/// Возврат (logout)
		/// </summary>
		Logout = 0x12,

		/// <summary>
		/// Байт-макро
		/// </summary>
		ByteMacro = 0x13,

		/// <summary>
		/// Терминал ввода данных
		/// </summary>
		DataEntryTerminal = 0x14,
		SUPDUP = 0x15,
		SUPDUPOutput = 0x16,

		/// <summary>
		/// Место отправления
		/// </summary>
		SendLocation = 0x17,

		/// <summary>
		/// Тип терминала
		/// </summary>
		TerminalType = 0x18,

		/// <summary>
		/// Конец записи
		/// </summary>
		EndOfRecord = 0x19,

		/// <summary>
		/// Tacacs- идентификация пользователя
		/// </summary>
		TACACSUserIdentification = 0x1A,

		/// <summary>
		/// Пометка вывода
		/// </summary>
		OutputMarking = 0x1B,

		/// <summary>
		/// Код положения терминала
		/// </summary>
		TerminalLocationNumber = 0x1C,

		/// <summary>
		/// Режим 3270
		/// </summary>
		Telnet3270Regime = 0x1D,
		X3PAD = 0x1E,

		/// <summary>
		/// Размер окна
		/// </summary>
		NegotiateAboutWindowSize = 0x1F,
		TerminalSpeed = 0x20,
		RemoteFlowControl = 0x21,
		Linemode = 0x22,
		XDisplayLocation = 0x23,
		EnvironmentOption = 0x24,
		AuthenticationOption = 0x25,
		EncryptionOption = 0x26,
		NewEnvironmentOption = 0x27,
		TN3270E = 0x28,
		XAUTH = 0x29,
		CHARSET = 0x2A,
		TelnetRemoteSerialPort = 0x2B,
		ComPortControlOption = 0x2C,
		TelnetSuppressLocalEcho = 0x2D,
		TelnetStartTLS = 0x2E,
		KERMIT = 0x2F,
		SENDURL = 0x30,
		FORWARD_X = 0x31,
		// 0x32 - 0x89 - Unassigned
		TELOPT_PRAGMA_LOGON = 0x8A,
		TELOPT_SSPI_LOGON = 0x8B,
		TELOPT_PRAGMA_HEARTBEAT = 0x8C,
		// 0x8D - 0xFE - Unassigned
		IAC = 0xFF
	}

	public class TelnetConnection
	{
		private TcpClient tcpSocket;

		private int TimeOutMs = 100;

		public TelnetConnection(string Hostname, int Port)
		{
			tcpSocket = new TcpClient(Hostname, Port);
			InithialitionTerminal();
		}

		public void InithialitionTerminal()
		{
			var firstOptionBlock = new[] {
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.WILL,
				(byte)TelnetOption.NegotiateAboutWindowSize,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.DO,
				(byte)TelnetOption.OutputPageSize,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.WILL,
				(byte)TelnetOption.TerminalSpeed,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.WILL,
				(byte)TelnetOption.TerminalType,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.WILL,
				(byte)TelnetOption.NewEnvironmentOption,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.DO,
				(byte)TelnetOption.Echo,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.WILL,
				(byte)TelnetOption.SuppressGoAhead,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.DO,
				(byte)TelnetOption.SuppressGoAhead
			};

			tcpSocket.GetStream().Write(firstOptionBlock, 0, firstOptionBlock.Length);

			tcpSocket.GetStream().Flush();

			var NegotiateAboutWindowSizeOption = new byte[] {
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.SB,
				(byte)TelnetOption.NegotiateAboutWindowSize,
				(byte)TelnetOption.BinaryTransmission,
				0x6F,
				(byte)TelnetOption.BinaryTransmission,
				0x41,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.SE
			};

			tcpSocket.GetStream().Write(NegotiateAboutWindowSizeOption, 0, NegotiateAboutWindowSizeOption.Length);

			tcpSocket.GetStream().Flush();

			var TerminalTypeOption = new byte[] {
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.SB,
				(byte)TelnetOption.TerminalType,
				(byte)TelnetOption.BinaryTransmission,
				0x58,
				0x54,
				0x45,
				0x52,
				0x4D,
				(byte)VerbsByte.IAC,
				(byte)VerbsByte.SE
			};

			tcpSocket.GetStream().Write(TerminalTypeOption, 0, TerminalTypeOption.Length);
		}

		public string Login(string Username, string Password, int LoginTimeOutMs)
		{
			int oldTimeOutMs = TimeOutMs;
			TimeOutMs = LoginTimeOutMs;
			string s = Read();
			Thread.Sleep(500);
			bool loggined = false, passwored = false;
			var attemptsWithWrongPass = 3;
			while (!string.IsNullOrEmpty(s) && !(loggined && passwored)) {
				var line = s.Replace(" ", string.Empty).Trim().ToLower();

				var wasFailed = line.IndexOf("fail!") != -1;

				if ((!wasFailed && line.Contains("username")) ||
					(wasFailed && line.Substring(line.Length - "username:".Length) == "username:")) {
					Thread.Sleep(500);
					WriteLine(Username);
					loggined = true;
				}
				if ((!wasFailed && line.Contains("password")) || (wasFailed && line.Substring(line.Length - "password:".Length) == "password:")) {
					Thread.Sleep(500);
					WriteLine(Password);
					passwored = true;
				}
				Thread.Sleep(1000);
				s = Read();
				if (s.ToLower().IndexOf("fail!")!=-1) {
					loggined = false;
					passwored = false;
					--attemptsWithWrongPass;
					if (attemptsWithWrongPass == 0) {
						break;
					}
				}
			}
			if (!loggined || !passwored) {
				throw new Exception("Не могу авторизоваться");
			}

			TimeOutMs = oldTimeOutMs;
			return s;
		}

		public void WriteLine(string cmd)
		{
			Write(cmd + "\n");
		}

		public void Write(string cmd)
		{
			if (!tcpSocket.Connected)
				return;
			byte[] buf = System.Text.Encoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
			tcpSocket.GetStream().Write(buf, 0, buf.Length);
		}

		public string Read()
		{
			if (!tcpSocket.Connected)
				return null;
			StringBuilder sb = new StringBuilder();
			do {
				ParseTelnet(sb);
				System.Threading.Thread.Sleep(TimeOutMs);
			} while (tcpSocket.Available > 0);
			return sb.ToString();
		}

		public bool IsConnected
		{
			get { return tcpSocket.Connected; }
		}

		private void ParseTelnet(StringBuilder sb)
		{
			while (tcpSocket.Available > 0) {
				int input = tcpSocket.GetStream().ReadByte();
				switch (input) {
					case -1:
						break;
					case (int)Verbs.IAC:
						// interpret as command
						int inputverb = tcpSocket.GetStream().ReadByte();
						if (inputverb == -1)
							break;
						switch (inputverb) {
							case (int)Verbs.IAC:
								//literal IAC = 255 escaped, so append char 255 to string
								sb.Append(inputverb);
								break;
							case (int)Verbs.DO:
							case (int)Verbs.DONT:
							case (int)Verbs.WILL:
							case (int)Verbs.WONT:
								// reply to all commands with "WONT", unless it is SGA (suppres go ahead)
								int inputoption = tcpSocket.GetStream().ReadByte();
								if (inputoption == -1)
									break;
								tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
								if (inputoption == (int)Options.SGA)
									tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
								else
									tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
								tcpSocket.GetStream().WriteByte((byte)inputoption);
								break;
							default:
								break;
						}
						break;
					default:
						sb.Append((char)input);
						break;
				}
			}
		}
	}
}