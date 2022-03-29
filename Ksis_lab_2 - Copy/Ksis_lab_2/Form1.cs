using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Ksis_lab_2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        void Tracert(string remoteHost)
        {
            if (string.IsNullOrEmpty(remoteHost))
            {
                throw new ArgumentException($"'{nameof(remoteHost)}' cannot be null or empty.", nameof(remoteHost));
            }

            byte[] data = new byte[1024];
            int recv = 0;
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            IPAddress addres;
            try
            {
                var @var = Dns.GetHostEntry(remoteHost);
                addres = Dns.GetHostEntry(remoteHost).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            }catch (Exception ex)
            {
                richTextBox1.Text += "Error: Unknown host name\n";
                throw new Exception("Unknown host name");
            }
            IPEndPoint iep = new IPEndPoint(addres, 0);
            EndPoint ep = iep;

            ICMP packet = new ICMP(0x08, 0x00);
     
            host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            int badcount = 0;

            for (int i = 1; i < 256; i++)
            {
                host.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);

                DateTime timestart = DateTime.Now;
                host.SendTo(packet.GetBytes(), packet.PacketSize, SocketFlags.None, iep);
                try
                {
                    data = new byte[1024];
                    recv = host.ReceiveFrom(data, ref ep);
                    TimeSpan timestop = DateTime.Now - timestart;

                    ICMP response = new ICMP(data, recv);

                    if (response.Type == 11)
                    {
                        richTextBox1.Text += i + ":\t" + ep.ToString() + "\t" + timestop.Milliseconds.ToString() + "мс\n";
                    }

                    if (response.Type == 0)
                    {
                        richTextBox1.Text += "[" + ep.ToString() + "]" + " достигнут за " + i + " прыжков, " + (timestop.Milliseconds.ToString()) + "мс\n";
                        break;
                    }

                    badcount = 0;
                }
                catch (SocketException exeption)
                {
                    richTextBox1.Text += i + ": нет ответа от " + ep + " (" + iep + ") - ttl:" + Convert.ToString(host.Ttl) + "\n";
                    badcount++;

                    if (badcount == 3)
                    {
                        richTextBox1.Text += "Не удалось установить соединение\n";
                        break;
                    }
                }
            }
            host.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Tracert(textBox1.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }
    }

    class ICMP
    {
        private const int IcmpHeaderSize = 8;
        private const int IpHeaderSize = 20;
        private const int MaxIpHeaderSize = 24;

        public byte Type;
        public byte Code;
        public ushort Checksum;
        public int Size;
        private int MessageSize;
        public int PacketSize;
        public byte[] Message = new byte[1024];
        public byte[] Data;

        public ICMP(byte type, byte code)
        {
             
            Type = type;
            Code = code;
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, Message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, Message, 2, 2);
            Data = Encoding.ASCII.GetBytes("hello");
            Buffer.BlockCopy(Data, 0, Message, 4, Data.Length); // 4 from upper block copy 
            Size = Data.Length + IcmpHeaderSize;
            Checksum = 0;
            Checksum = GetChecksum();
            MessageSize = Data.Length;// + "hello".Length;
            PacketSize = MessageSize + IcmpHeaderSize;//
        }

        public ICMP(byte[] rawIpPacket, int size)
        {
            rawIpPacket = rawIpPacket.Skip(IpHeaderSize).ToArray();
            Type = rawIpPacket[0];
            Code = rawIpPacket[1];
            Data = Encoding.ASCII.GetBytes("test packet");
            //Checksum = BitConverter.ToUInt16(rawIpPacket, 2);
            Checksum = GetChecksum();
            MessageSize = Data.Length;
            Buffer.BlockCopy(rawIpPacket, MaxIpHeaderSize, Message, 0, MessageSize);
        }

        public byte[] GetBytes()
        {
            MessageSize = Data.Length;// + "test packet".Length;
            byte[] data = new byte[1024];
            Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, data, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, data, 1, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, data, 2, 2);
            Buffer.BlockCopy(Data, 0, data, 8, MessageSize); //from upper block copy
            return data;
        }

        public ushort GetChecksum()
        {
            uint chcksm = 0;
            byte[] data = GetBytes();
            int packetsize = MessageSize + IcmpHeaderSize;
            int index = 0;

            while (index < packetsize)
            {
                chcksm += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                index += 2;
            }
            chcksm = (chcksm >> 16) + (chcksm & 0xffff);// 4+4 = 8; 24-8 = 16
            chcksm += (chcksm >> 16);
            return (ushort)~chcksm;
        }
    }
}
