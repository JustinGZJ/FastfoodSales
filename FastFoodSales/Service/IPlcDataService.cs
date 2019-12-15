using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using HslCommunication.Profinet.Melsec;

namespace DAQ.Service
{
    interface IPlcDataService
    {
        string AddressStart { get; set; }
        ushort Length { get; set; }
        byte[] GetBytes();
        void WriteBytes(byte[] bytes);
        int ErrorId { get; }
        string Error { get; }

    }

    public class PlcDataService : IPlcDataService
    {

        MelsecA1ENet melsec = new MelsecA1ENet();
        public ushort Length { get; set; } = 400;
        public string AddressStart { get; set; } = "D562";
        public PlcDataService(string addr,ushort len,string ip,int port)
        {
            AddressStart = AddressStart;
            Length = len;
            melsec.IpAddress = ip;
            melsec.Port = port;        
        }

        private int _errorId;

        public int ErrorId
        {
            get { return _errorId; }
        }
        private string _error;

        public string Error
        {
            get { return _error; }
        }

        public byte[] GetBytes()
        {
           var result= melsec.Read(AddressStart, Length);
            if(result.IsSuccess)
            {
                return result.Content;
            }
            else
            {
                _error = result.Message;
                _errorId = result.ErrorCode;
                return null;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
           var result= melsec.Write(AddressStart,bytes);
            if(!result.IsSuccess)
            {
                _error = result.Message;
                _errorId = result.ErrorCode;
            }
        }
    }

    public class DataObserver
    {
        byte[] _bytes = new byte[0];
        public int Offset { get; set; }
        public DataObserver(byte[] bytes)
        {
            if (bytes != null&&bytes.Length>0)
                _bytes = bytes;
        }

    }

}
