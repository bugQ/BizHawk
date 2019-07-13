﻿using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor
	public sealed partial class Chip6510
	{
		// ------------------------------------
		private readonly MOS6502X<CpuLink> _cpu;
		private bool _pinNmiLast;
		private LatchedPort _port;
		private bool _thisNmi;

		private struct CpuLink : IMOS6502XLink
		{
			private readonly Chip6510 _chip;

			public CpuLink(Chip6510 chip)
			{
				_chip = chip;
			}

			public byte DummyReadMemory(ushort address) => unchecked((byte)_chip.Read(address));

			public void OnExecFetch(ushort address) { }

			public byte PeekMemory(ushort address) => unchecked((byte)_chip.Peek(address));

			public byte ReadMemory(ushort address) => unchecked((byte)_chip.Read(address));

			public void WriteMemory(ushort address, byte value) => _chip.Write(address, value);
		}

		public Func<int, int> PeekMemory;
		public Action<int, int> PokeMemory;
		public Func<bool> ReadAec;
		public Func<bool> ReadIrq;
		public Func<bool> ReadNmi;
		public Func<bool> ReadRdy;
		public Func<int> ReadBus;
		public Func<int, int> ReadMemory;
		public Func<int> ReadPort;
		public Action<int, int> WriteMemory;
		public Action<int> WriteMemoryPort;

		public Action DebuggerStep;

		// ------------------------------------

		public Chip6510()
		{
			// configure cpu r/w
			_cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			// perform hard reset
			HardReset();
		}

		public string TraceHeader => "6510: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR)";

		public Action<TraceInfo> TraceCallback
		{
			get { return _cpu.TraceCallback; }
			set { _cpu.TraceCallback = value; }
		}

		public void HardReset()
		{
			_cpu.NESSoftReset();
			_port = new LatchedPort
			{
				Direction = 0x00,
				Latch = 0xFF
			};
			_pinNmiLast = true;
		}

		public void SoftReset()
		{
			_cpu.NESSoftReset();
			_port.Direction = 0x00;
			_port.Latch = 0xFF;
		}

		// ------------------------------------
		public void ExecutePhase()
		{
			_cpu.RDY = ReadRdy();
			_cpu.IRQ = !ReadIrq();
			_pinNmiLast = _thisNmi;
			_thisNmi = ReadNmi();
			_cpu.NMI |= _pinNmiLast && !_thisNmi;
			_cpu.ExecuteOne();
		}

		public int Peek(int addr)
		{
			switch (addr)
			{
				case 0x0000:
					return _port.Direction;
				case 0x0001:
					return PortData;
				default:
					return PeekMemory(addr);
			}
		}

		public void Poke(int addr, int val)
		{
			switch (addr)
			{
				case 0x0000:
					_port.Direction = val;
					break;
				case 0x0001:
					_port.Latch = val;
					break;
				default:
					PokeMemory(addr, val);
					break;
			}
		}

		public int PortData => _port.ReadInput(ReadPort());

		public int Read(int addr)
		{
			switch (addr)
			{
				case 0x0000:
					return _port.Direction;
				case 0x0001:
					return PortData;
				default:
					if (ReadAec())
						return ReadMemory(addr);
					else
						return ReadBus();
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Chip6510Cpu");
			_cpu.SyncState(ser);
			ser.EndSection();

			ser.Sync(nameof(_pinNmiLast), ref _pinNmiLast);

			ser.BeginSection(nameof(_port));
			_port.SyncState(ser);
			ser.EndSection();

			ser.Sync(nameof(_thisNmi), ref _thisNmi);
		}

		public void Write(int addr, int val)
		{
			switch (addr)
			{
				case 0x0000:
					_port.Direction = val;
					WriteMemoryPort(addr);
					break;
				case 0x0001:
					_port.Latch = val;
					WriteMemoryPort(addr);
					break;
				default:
					if (ReadAec())
						WriteMemory(addr, val);
					break;
			}
		}
	}
}
