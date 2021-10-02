using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Concurrent;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Threading;
using PcapDotNet.Base;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Gre;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.Igmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Core.Extensions;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.IO;

namespace tezKoklayici
{
    public partial class zamanAralikArama : Form
    {
        static public int fark;
        static public int sayac = 0;
        public zamanAralikArama(Queue<Packet> tumPaketler)
        {
            InitializeComponent();
            veriEkleme(tumPaketler);
            kapatB.Click += new EventHandler(kapatI);
            hesaplaB.Click += new EventHandler((sender, EventArgs)=>hesaplaI(sender,EventArgs, tumPaketler));
        }
        private void veriEkleme(Queue<Packet> paket)
        {
            foreach (Packet p in paket)
            {
                sayac++;
                zaman1.Items.Add(/*sayac.ToString() + " - " + */p.Timestamp.ToString("H:mm:ss.fffffff"));
                zaman2.Items.Add(/*sayac.ToString() + " - " +*/ p.Timestamp.ToString("H:mm:ss.fffffff"));
            }
        }
        private void kapatI(object sender, EventArgs e)
        {
            this.Close();
        }
        private void hesaplaI(object sender,EventArgs e,Queue<Packet> paketDizisi)
        {
            DateTime bir = DateTime.ParseExact(zaman1.SelectedItem.ToString(), "H:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
            DateTime iki = DateTime.ParseExact(zaman2.SelectedItem.ToString(), "H:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);

            TimeSpan fark1 = iki.Subtract(bir);
            gecenSure.Text = "Geçen Süre : " + fark1.TotalSeconds.ToString();

            int sayi = 0;
            float toplamUzunluk = 0;
            Packet paket1 = null;
            Packet paket2 = null;
            foreach (Packet p in paketDizisi)
            {
                if (paket1 == null)
                {
                    if (p.Timestamp.ToString("H:mm:ss.fffffff") == bir.ToString("H:mm:ss.fffffff"))
                    {
                        paket1 = p;
                        sayi++;
                        toplamUzunluk = toplamUzunluk + p.Length;
                    }
                }
                else if (paket1 != null)
                {
                    if (paket2 == null)
                    {
                        sayi++;
                        toplamUzunluk = toplamUzunluk + p.Length;
                        if (p.Timestamp.ToString("H:mm:ss.fffffff") == iki.ToString("H:mm:ss.fffffff"))
                        {
                            //toplamUzunluk = toplamUzunluk + p.Length;
                            paket2 = p;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            //hesapY.Text = paket1.Timestamp.ToString("H:mm:ss.fffffff");
            paketSayisiY.Text = "Paket Sayısı : " + sayi.ToString();
            toplamBoyutY.Text = "Toplam Boyut : " + toplamUzunluk.ToString();
            sbdbs.Text = "Saniyede gelen Byte : " + (8.0*(int)(toplamUzunluk / fark1.TotalSeconds)/1024) + " /Kbps";
        }
    }
}