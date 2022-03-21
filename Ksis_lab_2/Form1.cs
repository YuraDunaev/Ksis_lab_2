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
            IPHostEntry iphe;
            try
            {
                iphe = Dns.Resolve(remoteHost);
            }catch (Exception ex)
            {
                richTextBox1.Text += "Error: Unknown host name\n";
                throw new Exception("Unknown host name");
            }
            IPEndPoint iep = new IPEndPoint(iphe.AddressList[0], 0);
            EndPoint ep = (EndPoint)iep;
            ICMP packet = new ICMP();

            packet.Type = 0x08;
            packet.Code = 0x00;
            packet.Checksum = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, packet.Message, 2, 2);
            data = Encoding.ASCII.GetBytes("test packet");
            Buffer.BlockCopy(data, 0, packet.Message, 4, data.Length);
            packet.MessageSize = data.Length + 4;
            int packetsize = packet.MessageSize + 4;

            ushort chcksum = packet.GetChecksum();
            packet.Checksum = chcksum;

            host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            int badcount = 0;

            for (int i = 1; i < 256; i++)
            {
                host.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);

                DateTime timestart = DateTime.Now;
                host.SendTo(packet.GetBytes(), packetsize, SocketFlags.None, iep);
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
                catch (SocketException)
                {
                    richTextBox1.Text += i + ": нет ответа от " + ep + " (" + iep + ") - ttl:" + Convert.ToString(host.Ttl) + "\n";
                    badcount++;

                    if (badcount == 5)
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
        public byte Type;
        public byte Code;
        public UInt16 Checksum;
        public int MessageSize;
        public byte[] Message = new byte[1024];

        public ICMP()
        {
        }

        public ICMP(byte[] data, int size)
        {
            Type = data[20];
            Code = data[21];
            Checksum = BitConverter.ToUInt16(data, 22);
            MessageSize = size - 24;
            Buffer.BlockCopy(data, 24, Message, 0, MessageSize);
        }

        public byte[] GetBytes()
        {
            byte[] data = new byte[MessageSize + 9];
            Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, data, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, data, 1, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, data, 2, 2);
            Buffer.BlockCopy(Message, 0, data, 4, MessageSize);
            return data;
        }

        public ushort GetChecksum()
        {
            uint chcksm = 0;
            byte[] data = GetBytes();
            int packetsize = MessageSize + 8;
            int index = 0;

            while (index < packetsize)
            {
                chcksm += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                index += 2;
            }
            chcksm = (chcksm >> 16) + (chcksm & 0xffff);
            chcksm += (chcksm >> 16);
            return (ushort)~chcksm;
        }
    }
}
