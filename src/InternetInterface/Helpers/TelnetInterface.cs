using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace InternetInterface.Helpers
{
	public enum Verbs {
		WILL = 251,
		WONT = 252,
		DO = 253,
		DONT = 254,
		IAC = 255
	}

	enum Options
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
		/// �������� �����
		/// </summary>
		BinaryTransmission = 0x00,
		/// <summary>
		/// ���
		/// </summary>
		Echo = 0x01,
		/// <summary>
		/// ��������� ����������
		/// </summary>
		Reconnection = 0x02,
		/// <summary>
		/// ���������� ����������� �����
		/// </summary>
		SuppressGoAhead = 0x03,
		/// <summary>
		/// ������ � ������� ���������
		/// </summary>
		ApproxMessageSizeNegotiation = 0x04,
		/// <summary>
		/// ������
		/// </summary>
		Status = 0x05,
		/// <summary>
		/// ��������� �����
		/// </summary>
		TimingMark = 0x06,
		/// <summary>
		/// ��������� ������ � ������
		/// </summary>
		RemoteControlledTransAndEcho = 0x07,
		/// <summary>
		/// ����� �������� ������
		/// </summary>
		OutputLineWidth = 0x08,
		/// <summary>
		/// ������ �������� ��������
		/// </summary>
		OutputPageSize = 0x09,
		/// <summary>
		/// ����� ������ �������� <������� �������>
		/// </summary>
		OutputCarriageReturnDisposition = 0x0A,
		/// <summary>
		/// ����� �������������� ���������
		/// </summary>
		OutputHorizontalTabStops = 0x0B,
		/// <summary>
		/// ��������� ��������� ��������� ��� ������
		/// </summary>
		OutputHorizontalTabDisposition = 0x0C,
		/// <summary>
		/// ����� ������ ������� ����� ��������
		/// </summary>
		OutputFormfeedDisposition = 0x0D,
		/// <summary>
		/// ����� ������������ ���������
		/// </summary>
		OutputVerticalTabstops = 0x0E,
		/// <summary>
		/// ���������� ��������� ������������ ���������
		/// </summary>
		OutputVerticalTabDisposition = 0x0F,
		/// <summary>
		/// ����� ������ ������� "������� ������"
		/// </summary>
		OutputLinefeedDisposition = 0x10,
		/// <summary>
		/// ����������� ����� ����� ASCII
		/// </summary>
		ExtendedASCII = 0x11,
		/// <summary>
		/// ������� (logout)
		/// </summary>
		Logout = 0x12,
		/// <summary>
		/// ����-�����
		/// </summary>
		ByteMacro = 0x13,
		/// <summary>
		/// �������� ����� ������
		/// </summary>
		DataEntryTerminal = 0x14,
		SUPDUP = 0x15,
		SUPDUPOutput = 0x16,
		/// <summary>
		/// ����� �����������
		/// </summary>
		SendLocation = 0x17,
		/// <summary>
		/// ��� ���������
		/// </summary>
		TerminalType = 0x18,
		/// <summary>
		/// ����� ������
		/// </summary>
		EndOfRecord = 0x19,
		/// <summary>
		/// Tacacs- ������������� ������������
		/// </summary>
		TACACSUserIdentification = 0x1A,
		/// <summary>
		/// ������� ������
		/// </summary>
		OutputMarking = 0x1B,
		/// <summary>
		/// ��� ��������� ���������
		/// </summary>
		TerminalLocationNumber = 0x1C,
		/// <summary>
		/// ����� 3270
		/// </summary>
		Telnet3270Regime = 0x1D,
		X3PAD = 0x1E,
		/// <summary>
		/// ������ ����
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
		TcpClient tcpSocket;

		int TimeOutMs = 100;

		public TelnetConnection(string Hostname, int Port)
		{
			tcpSocket = new TcpClient(Hostname, Port);
			InithialitionTerminal();
		}

		public void InithialitionTerminal()
		{

			var firstOptionBlock = new [] {
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

		public string Login(string Username,string Password,int LoginTimeOutMs)
		{
			int oldTimeOutMs = TimeOutMs;
			TimeOutMs = LoginTimeOutMs;
			string s = Read();
			Thread.Sleep(500);
			bool loggined = false, passwored = false;
			while (!string.IsNullOrEmpty(s) && !(loggined && passwored)) {
				if (s.Replace(" ", string.Empty).Trim().Contains("Username")) {
					WriteLine(Username);
					loggined = true;
				}


				if (s.Replace(" ", string.Empty).Trim().Contains("Password")) {
					WriteLine(Password);
					passwored = true;
				}
				s = Read();
			}
			if (!loggined || !passwored) {
				throw new Exception("�� ���� ��������������");
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
			if (!tcpSocket.Connected) return;
			byte[] buf = System.Text.Encoding.ASCII.GetBytes(cmd.Replace("\0xFF","\0xFF\0xFF"));
			tcpSocket.GetStream().Write(buf, 0, buf.Length);
		}

		public string Read()
		{
			if (!tcpSocket.Connected) return null;
			StringBuilder sb=new StringBuilder();
			do
			{
				ParseTelnet(sb);
				System.Threading.Thread.Sleep(TimeOutMs);
			} while (tcpSocket.Available > 0);
			return sb.ToString();
		}

		public bool IsConnected
		{
			get { return tcpSocket.Connected; }
		}

		void ParseTelnet(StringBuilder sb)
		{
			while (tcpSocket.Available > 0)
			{
				int input = tcpSocket.GetStream().ReadByte();
				switch (input)
				{
					case -1 :
						break;
					case (int)Verbs.IAC:
						// interpret as command
						int inputverb = tcpSocket.GetStream().ReadByte();
						if (inputverb == -1) break;
						switch (inputverb)
						{
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
								if (inputoption == -1) break;
								tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
								if (inputoption == (int)Options.SGA )
									tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL:(byte)Verbs.DO); 
								else
									tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT); 
								tcpSocket.GetStream().WriteByte((byte)inputoption);
								break;
							default:
								break;
						}
						break;
					default:
						sb.Append( (char)input );
						break;
				}
			}
		}
	}
}
