using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HslCommunication.Core;
using HslCommunication.Profinet.Melsec;


namespace DAQ
{
    public struct Edge
    {
        public bool RisingEdge { get; private set; }
        public bool FallingEdge { get; private set; }
        private bool _currentValue;
        public bool CurrentValue
        {
            get => _currentValue;
            set
            {
                RisingEdge = ((!_currentValue) && (value));
                FallingEdge = ((_currentValue) && (!value));
                _currentValue = value;
            }
        }
    }
    public class MCPLCDataAccess
    {
        private readonly ushort capcity = 960;
        private CancellationTokenSource _ct;
        private MelsecMcNet _rw;
        private IByteTransform _transform;

        public string StartAddress { get; set; } = "D63000";
        public int StartIndex => int.Parse(StartAddress.Substring(1));
        public ushort Length { get; set; } = 200;
        public byte[] Bytes { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        public string TriggerAddress { get; set; } = "M300000";

        public event Action<byte[]> OnDataReady;
        public event Action<string> OnError;
        public event Action<bool> OnDataTriger;

        ~MCPLCDataAccess()
        {
            _rw?.ConnectClose();
        }

        public void Connect()
        {
            _rw = new MelsecMcNet();
            _transform = _rw.ByteTransform;
            Bytes = new byte[Length * 2];
            _rw.IpAddress = Ip;
            _rw.Port = Port;
            if (_ct != null && !_ct.IsCancellationRequested) _ct.Cancel();
            _rw.ConnectServer();
            _ct = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!_ct.IsCancellationRequested)
                {
                    ReadData();
                    ReadTriger();
                    Thread.Sleep(10);
                }
            }, _ct.Token);
        }

        public void Stop()
        {
            _ct?.Cancel();
            _rw?.ConnectClose();
            _rw = null;
        }

        private void ReadData()
        {
            int UintToRead = Length;
            ushort cnt = 0;
            if (_rw != null)
            {
                do
                {
                    int num;
                    num = UintToRead > capcity ? capcity : UintToRead;

                    var address = $"D{StartIndex + cnt * capcity}";

                    var OP = _rw.Read(address, (ushort)num);
                    if (OP.IsSuccess)
                    {
                        Array.Copy(OP.Content,
                            0,
                            Bytes,
                            capcity * cnt * 2,
                            OP.Content.Length);
                    }
                    else
                    {
                        OnError?.Invoke(OP.Message);
                    }
                    cnt++;
                    UintToRead -= capcity;
                } while (UintToRead > 0);
                OnDataReady?.Invoke(Bytes);
            }
        }

        Edge Edge;
        private void ReadTriger()
        {
            var result = _rw.ReadBool(TriggerAddress);
            if (result.IsSuccess)
            {
                Edge.CurrentValue = result.Content;
                if(Edge.RisingEdge)
                {
                    OnDataTriger?.Invoke(result.Content);
                }
            }
            else
            {
                OnError?.Invoke(result.Message);
            }
        }

        public short ReadInt16(int index)
        {
            return _transform.TransInt16(Bytes, index * 2);
        }

        public ushort ReadUInt16(int index)
        {
            return _transform.TransUInt16(Bytes, index * 2);
        }

        public int ReadInt32(int index)
        {
            return _transform.TransInt32(Bytes, index * 2);
        }

        public uint ReadUInt32(int index)
        {
            return _transform.TransUInt32(Bytes, index * 2);
        }


        public long ReadInt64(int index)
        {
            return _transform.TransInt64(Bytes, index * 2);
        }

        public ulong ReadUInt64(int index)
        {
            return _transform.TransUInt64(Bytes, index * 2);
        }


        public float ReadFloat(int index)
        {
            return _transform.TransSingle(Bytes, index * 2);
        }

        public double ReadDouble(int index)
        {
            return _transform.TransDouble(Bytes, index * 2);
        }

        public string ReadString(int index, ushort length)
        {
            var mBytes = new byte[length * 2];
            Array.Copy(Bytes, index * 2, mBytes, 0, length * 2);
            for (int i = 0; i < length; i++)
            {
                byte m = mBytes[2 * i];
                byte n = mBytes[2 * i + 1];
                mBytes[2 * i] = n;
                mBytes[2 * i + 1] = m;
            }
            return _transform.TransString(mBytes, 0, length * 2, Encoding.UTF8);
        }

        public bool ReadBool(int index, int bit)
        {
            var m = ReadUInt16(index);
            return (m & (1 << bit)) > 0;
        }

        private string TransformAddress(int index)
        {
            var regex = new Regex(@"(\D{1,2})(\d{1,5})(\.(\d{1,2}))?");
            var match = regex.Match(StartAddress);
            if (match.Success)
                return match.Groups[1].Value + (int.Parse(match.Groups[2].Value) + index);
            return "";
        }

        public bool Write(int index, int bit, bool value)
        {
            if (_rw == null)
                return false;
            var m = _rw.ReadUInt16(TransformAddress(index));
            if (!m.IsSuccess)
                return false;
            var val = value ? (ushort)(m.Content | (1 << bit)) : (ushort)(m.Content & ~(1 << bit));
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, short value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, ushort value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, int value)
        {
            if (_rw == null)
                return false;
            var address = TransformAddress(index);
            return _rw.Write(address, value).IsSuccess;
        }

        public bool Write(int index, uint value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, float value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, double value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }

        public bool Write(int index, string value)
        {
            if (_rw == null)
                return false;
            return _rw.Write(TransformAddress(index), value).IsSuccess;
        }
    }
}