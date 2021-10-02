using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Threading;
using System.Threading.Tasks;
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
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Data;
using System.Data.SQLite;



namespace tezKoklayici
{
    public partial class paketleriOkuma : Form
    {
        static Queue<Packet> kuyruk = new Queue<Packet>();
        private static ManualResetEvent durBasla = new ManualResetEvent(false);
        private static ManualResetEvent aramaDUR = new ManualResetEvent(false);
        static public int sira = 0;
        static Queue<Packet> paketDizisi = new Queue<Packet>();
        static Queue<Packet> elenmisPaketler = new Queue<Packet>();
        static object ortakKilit = new object();

        static public int sayac = 0;
        static string protokol = "";
        static string yon = "";
        static string ip = "";
        static string port = "";

        static public bool goster = true;

        private DateTime suan;

        public paketleriOkuma(LivePacketDevice cihaz)
        {
            InitializeComponent();
            hataYazisi.Visible = false;


            paketVerileri.Visible = false;
            paketVerileri.Width = 1000;
            paketVerileri.Height = 444;
            paketVerileri.View = View.Details;
            paketVerileri.GridLines = true;
            paketVerileri.FullRowSelect = true;
            paketVerileri.Font = new Font("Arial Black", 9f);
            paketVerileri.BackColor = Color.GhostWhite;
            paketVerileri.Columns.Add("Sıra", 75, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Zaman", 250, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Kaynak İp", 120, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Hedef İp", 120, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Protokol", 75, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Kaynak Port", 75, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("Hedef Port", 75, HorizontalAlignment.Left);
            paketVerileri.Columns.Add("İçerik", -2, HorizontalAlignment.Left);
            paketVerileri.ForeColor = Color.Crimson;



            aramaListesi.Visible = false;
            aramaListesi.Width = 1000;
            aramaListesi.Height = 444;
            aramaListesi.View = View.Details;
            aramaListesi.GridLines = true;
            aramaListesi.FullRowSelect = true;
            aramaListesi.Font = new Font("Arial Black", 9f);
            aramaListesi.BackColor = Color.FromArgb(140, 93, 60);
            aramaListesi.Columns.Add("Zaman", 250, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("Sıra", 75, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("Kaynak", 120, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("Hedef", 120, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("Protokol", 75, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("Uzunluk", 100, HorizontalAlignment.Left);
            aramaListesi.Columns.Add("İçerik", -2, HorizontalAlignment.Left);
            aramaListesi.ForeColor = Color.DarkOrange;

            paketListesi.View = View.Details;
            paketListesi.GridLines = true;
            paketListesi.FullRowSelect = true;
            paketListesi.Font = new Font("Arial Black", 9f);
            paketListesi.BackColor = Color.DarkSlateGray;
            paketListesi.Columns.Add("Zaman", 250, HorizontalAlignment.Left);
            paketListesi.Columns.Add("Sıra", 75, HorizontalAlignment.Left);
            paketListesi.Columns.Add("Kaynak", 120, HorizontalAlignment.Left);
            paketListesi.Columns.Add("Hedef", 120, HorizontalAlignment.Left);
            paketListesi.Columns.Add("Protokol", 75, HorizontalAlignment.Left);
            paketListesi.Columns.Add("Uzunluk", 100, HorizontalAlignment.Left);
            paketListesi.Columns.Add("İçerik", -2, HorizontalAlignment.Left);
            paketListesi.ForeColor = Color.DarkOrange;

            icerikGoster.View = View.Details;
            icerikGoster.GridLines = true;
            icerikGoster.FullRowSelect = true;
            icerikGoster.Font = new Font("Arial Black", 9f);
            icerikGoster.BackColor = Color.DarkSlateGray;
            icerikGoster.ForeColor = Color.DarkOrange;
            icerikGoster.Columns.Add("Paket", -2, HorizontalAlignment.Left);

            payloadGoster.BackColor = Color.FromArgb(140, 93, 60);
            payloadGoster.Font = new Font("Arial Black", 12f);

            Thread thYakala = new Thread(() => paketYakala(cihaz));
            Thread thOku = new Thread(() => paketOku(thYakala));

            sira = 0;
            thYakala.Start();
            suan = DateTime.Now;
            thOku.Start();
            durBasla.Set();
            aramaDUR.Set();

            zamanlayici.Tick += new EventHandler(zamanHesapla);
            zamanlayici.Start();


            pyBasla.Click += new EventHandler((sender, EventArgs) => pyBaslat(sender, EventArgs, thYakala, thOku/*, paketDizisi*/));
            kapatB.Click += new EventHandler((sender, EventArgs) => kapat(sender, EventArgs, thYakala, thOku/*, thAra*/));
            durdurB.Click += new EventHandler((sender, EventArgs) => durdur(sender, EventArgs, thYakala, thOku));
            araD.Click += new EventHandler(arama);
            paketListesi.Click += new EventHandler(paketSec);
            aramaListesi.Click += new EventHandler(elenmisPaketSec);
            zamanIslemi.Click += new EventHandler(zamandaAra);
            veriGetir.Click += new EventHandler(veriGoster);
        }
        private void kapat(object sender, EventArgs e, Thread thYakala, Thread thOku/*, Thread thAra*/)
        {
            lock (kuyruk)
            {
                paketDizisi.Clear();
                kuyruk.Clear();
            }
            durBasla.Reset();
            aramaDUR.Reset();
            //thAra.Abort();
            thYakala.Abort();
            thOku.Abort();
            this.Close();
        }
        private void durdur(object sender, EventArgs e, Thread thYakala, Thread thOku)
        {
            try
            {
                zamanlayici.Stop();
                durBasla.Reset();
                hataYazisi.Visible = false;
            }
            catch (Exception hata)
            {
                hataYazisi.Text = ("Hata: " + hata.ToString());
                hataYazisi.Visible = true;
            }
        }
        private void pyBaslat(object sender, EventArgs e, Thread thYakala, Thread thOku/*, BlockingCollection<Packet> liste*/)
        {
            try
            {
                sira = 0;
                lock (kuyruk)
                {
                    paketDizisi.Clear();
                    kuyruk.Clear();
                }
                paketListesi.Items.Clear();
                aramaListesi.Items.Clear();
                zamanlayici.Start();
                suan = DateTime.Now;
                durBasla.Set();
                hataYazisi.Visible = false;
            }
            catch (Exception hata)
            {
                hataYazisi.Text = ("Hata: " + hata.ToString());
                hataYazisi.Visible = true;
            }
        }
        
        private void veriGoster(object sender, EventArgs e)
        {
            if (goster == false)
            {
                goster = true;
                paketVerileri.Visible = false;
                paketVerileri.Items.Clear();
            }
            else
            {
                goster = false;
                string cs = @"URI=file:C:\sqlite\koklayici";
                using (var con = new SQLiteConnection(cs))
                {
                    con.Open();
                    string sorgu = "select * from paketler";
                    using (var sor = new SQLiteCommand(sorgu, con))
                    {
                        using (SQLiteDataReader okuyucu = sor.ExecuteReader())
                        {
                            hataYazisi.Visible = true;
                            DataSet ds = new DataSet();
                            try
                            {
                                while (okuyucu.Read())
                                {
                                    if (okuyucu.IsDBNull(0))
                                    {
                                        hataYazisi.Text = "Hata: Boş veri!";
                                    }
                                    else
                                    {
                                        ListViewItem veri = new ListViewItem(okuyucu[0].ToString());
                                        veri.SubItems.Add(okuyucu[1].ToString());
                                        veri.SubItems.Add(okuyucu[2].ToString());
                                        veri.SubItems.Add(okuyucu[3].ToString());
                                        veri.SubItems.Add(okuyucu[4].ToString());
                                        veri.SubItems.Add(okuyucu[5].ToString());
                                        veri.SubItems.Add(okuyucu[6].ToString());
                                        veri.SubItems.Add(okuyucu[7].ToString());
                                        paketVerileri.Items.Add(veri);
                                        paketVerileri.Visible = true;
                                        hataYazisi.Text = "oldu";
                                    }
                                }
                            }
                            catch (Exception hata)
                            {
                                MessageBox.Show(hata.ToString(), "HATA");
                            }
                        }
                    }
                    con.Close();
                }
            }

            
        }
        
        private void zamanHesapla(object sender, EventArgs e)
        {
            TimeSpan sure = DateTime.Now - suan;
            zamanlayici1Y.Text = "Geçen Süre : " + sure.TotalSeconds.ToString();
            lock (ortakKilit)
            {
                zamanlayici2Y.Text = "Gelen Paket Sayısı : " + sira.ToString();
            }
            float toplamBoyut = 0;
            foreach (Packet p in paketDizisi)
            {
                if (p.IsValid)
                {
                    toplamBoyut += p.Length;
                }
            }
            zamanlayici3Y.Text = "Toplam Boyut : " + toplamBoyut.ToString() + " Byte";
            zamanlayici4Y.Text = "Saniyede Gelen Byte : " + (8.0*(int)(toplamBoyut / sure.TotalSeconds)/1024.0) + " /Kbps";
        }
        private void paketSec(object sender, EventArgs e)
        {
            icerikGoster.Items.Clear();
            int secilenPaket = paketListesi.SelectedIndices[0];

            List<Packet> paketL;
            lock (kuyruk)
            {
                paketL = paketDizisi.ToList();
            }
            Packet p = paketL[secilenPaket];

            //hataYazisi.Text = p.Ethernet.IpV4.Tcp.Length.ToString() ;
            hataYazisi.Visible = true;

            //ListViewItem veri = new ListViewItem();

            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            EthernetDatagram ed = p.Ethernet;
            if (ed.EtherType == EthernetType.IpV4)
            {
                IpV4Datagram ip4DG = ed.IpV4;
                if (ip4DG.Protocol == IpV4Protocol.Udp)
                {
                    udpDG = ip4DG.Udp;
                    dg = udpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("UDP Protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak PORT = " + udpDG.SourcePort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef PORT = " + udpDG.DestinationPort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("UDP Paketi uzunluğu = " + udpDG.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;

                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
                else if (ip4DG.Protocol == IpV4Protocol.Tcp)
                {
                    tcpDG = ip4DG.Tcp;
                    dg = tcpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("TCP Protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak PORT = " + tcpDG.SourcePort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef PORT = " + tcpDG.DestinationPort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("TCP Paketi uzunluğu = " + tcpDG.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;
                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
                else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                {
                    icmpDG = ip4DG.Icmp;
                    dg = icmpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("ICMP protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("ICMP Paketi uzunluğu = " + icmpDG.Length.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;
                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
            }
            else if (ed.EtherType == EthernetType.Arp)
            {
                arpDG = ed.Arp;
                if (arpDG != null)
                {
                    ListViewItem veri = new ListViewItem("ARP Protokolü".ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Kaynak MAC = " + ed.Source.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Hedef MAC = " + ed.Destination.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Kaynak IP = " + arpDG.SenderProtocolIpV4Address.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Hedef IP = " + arpDG.TargetProtocolIpV4Address.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("ARP Paketi uzunluğu = " + arpDG.Length.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                    icerikGoster.Items.Add(veri);
                }
            }

        }
        private void elenmisPaketSec(object sender, EventArgs e)
        {
            icerikGoster.Items.Clear();
            int secilenPaket = aramaListesi.SelectedIndices[0];

            List<Packet> paketL;
            lock (kuyruk)
            {
                paketL = elenmisPaketler.ToList();
            }
            Packet p = paketL[secilenPaket];

            hataYazisi.Visible = true;


            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            EthernetDatagram ed = p.Ethernet;
            if (ed.EtherType == EthernetType.IpV4)
            {
                IpV4Datagram ip4DG = ed.IpV4;
                if (ip4DG.Protocol == IpV4Protocol.Udp)
                {
                    udpDG = ip4DG.Udp;
                    dg = udpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("UDP Protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak PORT = " + udpDG.SourcePort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef PORT = " + udpDG.DestinationPort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("UDP Paketi uzunluğu = " + udpDG.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;

                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
                else if (ip4DG.Protocol == IpV4Protocol.Tcp)
                {
                    tcpDG = ip4DG.Tcp;
                    dg = tcpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("TCP Protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak PORT = " + tcpDG.SourcePort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef PORT = " + tcpDG.DestinationPort.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("TCP Paketi uzunluğu = " + tcpDG.Length.ToString()); ;
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;
                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
                else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                {
                    icmpDG = ip4DG.Icmp;
                    dg = icmpDG.Payload;
                    if (dg != null)
                    {
                        ListViewItem veri = new ListViewItem("ICMP protokolü".ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Kaynak IP = " + ip4DG.Source.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Hedef IP = " + ip4DG.Destination.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("ICMP Paketi uzunluğu = " + icmpDG.Length.ToString());
                        icerikGoster.Items.Add(veri);
                        veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                        icerikGoster.Items.Add(veri);
                        int plUzunluk = dg.Length;
                        using (MemoryStream ms = dg.ToMemoryStream())
                        {
                            byte[] payloadB = new byte[plUzunluk];
                            ms.Read(payloadB, 0, plUzunluk);
                            payloadGoster.Clear();
                            payloadGoster.Text = System.Text.Encoding.ASCII.GetString(payloadB);
                        }
                    }
                }
            }
            else if (ed.EtherType == EthernetType.Arp)
            {
                arpDG = ed.Arp;
                if (arpDG != null)
                {
                    ListViewItem veri = new ListViewItem("ARP Protokolü".ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Kaynak MAC = " + ed.Source.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Hedef MAC = " + ed.Destination.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Kaynak IP = " + arpDG.SenderProtocolIpV4Address.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Hedef IP = " + arpDG.TargetProtocolIpV4Address.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Ethernet Paketi uzunluğu = " + ed.Length.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("ARP Paketi uzunluğu = " + arpDG.Length.ToString());
                    icerikGoster.Items.Add(veri);
                    veri = new ListViewItem("Zaman Damgası = " + p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff")); ;
                    icerikGoster.Items.Add(veri);
                }
            }
        }
        private void zamandaAra(object sender, EventArgs e)
        {
            if (paketDizisi.Count > 0)
            {
                using (zamanAralikArama zaa = new zamanAralikArama(paketDizisi))
                {
                    this.Visible = false;
                    zaa.ShowDialog(this);
                    this.Visible = true;
                }
            }
            else
            {
                MessageBox.Show("İlk önce programı çalıştırın!", "Kuyruk Boş");
            }
        }
        private void arama(object sender, EventArgs e)
        {
            string aranan = aranacak.Text.ToString().ToUpper();
            string[] bolunmusCumle = aranan.Split(' ');
            int kelimeSayisi = bolunmusCumle.Count();
            hataYazisi.Visible = false;
            if (kelimeSayisi == 1)
            {
                if (bolunmusCumle[0] == "")
                {
                    aramaListesi.Items.Clear();
                    aramaListesi.Visible = false;
                }
                else if (bolunmusCumle[0] == "UDP")
                {
                    lock (ortakKilit)
                    {
                        protokol = bolunmusCumle[0];
                        yon = "YOK";
                        ip = "YOK";
                        port = "YOK";
                    }
                    //hataYazisi.Text = bolunmusCumle[0];
                    //hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                    //hataYazisi.Visible = true;
                    protokoleGoreAra();
                }
                else if (bolunmusCumle[0] == "TCP")
                {
                    lock (ortakKilit)
                    {
                        protokol = bolunmusCumle[0];
                        yon = "YOK";
                        ip = "YOK";
                        port = "YOK";
                    }
                    //hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                    //hataYazisi.Visible = true;
                    protokoleGoreAra();
                }
                else if (bolunmusCumle[0] == "ARP")
                {
                    lock (ortakKilit)
                    {
                        protokol = bolunmusCumle[0];
                        yon = "YOK";
                        ip = "YOK";
                        port = "YOK";
                    }
                    //hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                    //hataYazisi.Visible = true;
                    protokoleGoreAra();
                }
                else if (bolunmusCumle[0] == "ICMP")
                {
                    lock (ortakKilit)
                    {
                        protokol = bolunmusCumle[0];
                        yon = "YOK";
                        ip = "YOK";
                        port = "YOK";
                    }
                    //hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                    //hataYazisi.Visible = true;
                    protokoleGoreAra();
                }
                else
                {
                    hataYazisi.Text = "Yanlış elgeç seçtiniz!";
                    hataYazisi.Visible = true;
                }
            }
            else if (kelimeSayisi == 3)
            {
                if (bolunmusCumle[0] == "IP")
                {
                    if (bolunmusCumle[1] == "=")
                    {
                        if (bolunmusCumle[2] != "")
                        {
                            //hataYazisi.Text = bolunmusCumle[0] + " " + bolunmusCumle[1] + " " + bolunmusCumle[2];
                            hataYazisi.Visible = true;
                            lock (ortakKilit)
                            {
                                protokol = "YOK";
                                yon = "YOK";
                                ip = bolunmusCumle[2];
                                port = "YOK";
                            }
                            hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                            ipyeGoreAra();
                        }
                        else
                        {
                            hataYazisi.Text = "ip giriniz!";
                            hataYazisi.Visible = true;
                        }
                    }
                    else
                    {
                        hataYazisi.Text = "IP den sonra = diyip ip sayısını giriniz!";
                        hataYazisi.Visible = true;
                    }
                }
                else if (bolunmusCumle[0] == "UDP" || bolunmusCumle[0] == "TCP")
                {
                    if (bolunmusCumle[1] == "=")
                    {
                        if (bolunmusCumle[2] != "")
                        {
                            hataYazisi.Visible = true;
                            lock (ortakKilit)
                            {
                                if (bolunmusCumle[0] == "UDP")
                                {
                                    protokol = "UDP";
                                }
                                else if (bolunmusCumle[0] == "TCP")
                                {
                                    protokol = "TCP";
                                }
                                yon = "YOK";
                                ip = "YOK";
                                port = bolunmusCumle[2];
                            }
                            hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                            portaGoreAra();
                        }
                        else
                        {
                            hataYazisi.Text = "port giriniz!";
                            hataYazisi.Visible = true;
                        }
                    }
                }
            }
            else if (kelimeSayisi == 4)
            {
                if (bolunmusCumle[0] == "HEDEF" || bolunmusCumle[0] == "KAYNAK")
                {
                    if(bolunmusCumle[1]=="UDP" || bolunmusCumle[1] == "TCP")
                    {
                        if (bolunmusCumle[2] == "=")
                        {
                            if (bolunmusCumle[3] != "")
                            {
                                hataYazisi.Visible = true;
                                lock (ortakKilit)
                                {
                                    if (bolunmusCumle[1] == "UDP")
                                    {
                                        protokol = "UDP";
                                    }
                                    else if (bolunmusCumle[1] == "TCP")
                                    {
                                        protokol = "TCP";
                                    }
                                    if (bolunmusCumle[0] == "HEDEF")
                                    {
                                        yon = "HEDEF";
                                    }
                                    else if (bolunmusCumle[0] == "KAYNAK")
                                    {
                                        yon = "KAYNAK";
                                    }
                                    ip = "YOK";
                                    port = bolunmusCumle[3];
                                }
                                portuYoneGoreAra();
                                hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                            }
                        }
                    }
                    else if (bolunmusCumle[1] == "IP")
                    {
                        if (bolunmusCumle[2] == "=")
                        {
                            if (bolunmusCumle[3] != "")
                            {
                                hataYazisi.Visible = true;
                                lock (ortakKilit)
                                {
                                    if (bolunmusCumle[0] == "HEDEF")
                                    {
                                        yon = "HEDEF";
                                    }
                                    else if (bolunmusCumle[0] == "KAYNAK")
                                    {
                                        yon = "KAYNAK";
                                    }
                                    protokol = "YOK";
                                    port = "YOK";
                                    ip = bolunmusCumle[3];
                                }
                                ipyiYoneGoreAra();
                                hataYazisi.Text = protokol + " | " + yon + " | " + ip + " | " + port;
                            }
                            else
                            {
                                hataYazisi.Text = "ip giriniz";
                                hataYazisi.Visible = true;
                            }
                        }
                    }
                }
                else
                {
                    hataYazisi.Text = "Yanlış elgeç girdiniz!";
                    hataYazisi.Visible = true;
                }
            }
        }

        void ipyiYoneGoreAra()
        {
            sayac = 0;
            elenmisPaketler.Clear();
            aramaListesi.Items.Clear();
            aramaListesi.Visible = true;
            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            string ayPisi = "";
            string yonu = "";
            foreach (Packet p in paketDizisi)
            {
                lock (ortakKilit)
                {
                    yonu = yon;
                    ayPisi = ip;
                }
                if (p.IsValid)
                {
                    EthernetDatagram ed = p.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4)
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp)
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (yonu == "HEDEF" && ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                            else if (yonu == "KAYNAK" && ip4DG.Source.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp)
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (yonu == "HEDEF" && ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                            else if (yonu == "KAYNAK" && ip4DG.Source.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                        {
                            icmpDG = ip4DG.Icmp;
                            dg = icmpDG.Payload;
                            if (yonu == "HEDEF" && ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("ICMP");
                                    veri.SubItems.Add(icmpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                            else if (yonu == "KAYNAK" && ip4DG.Source.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("ICMP");
                                    veri.SubItems.Add(icmpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                    }
                    else if (ed.EtherType == EthernetType.Arp)
                    {
                        arpDG = ed.Arp;
                        if (yonu == "HEDEF" && arpDG.TargetProtocolIpV4Address.ToString() == ayPisi)
                        {
                            sayac++;
                            ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                            veri.SubItems.Add(sayac.ToString());
                            veri.SubItems.Add(arpDG.SenderProtocolIpV4Address.ToString());
                            veri.SubItems.Add(arpDG.TargetProtocolIpV4Address.ToString());
                            veri.SubItems.Add("ARP");
                            veri.SubItems.Add(arpDG.Length.ToString());
                            veri.SubItems.Add(arpDG.HardwareType.ToString());
                            veri.ForeColor = Color.DeepSkyBlue;
                            aramaListesi.Items.Add(veri);
                            lock (ortakKilit)
                            {
                                elenmisPaketler.Enqueue(p);
                            }
                        }
                        else if (yonu == "KAYNAK" && arpDG.SenderProtocolIpV4Address.ToString() == ayPisi)
                        {
                            sayac++;
                            ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                            veri.SubItems.Add(sayac.ToString());
                            veri.SubItems.Add(arpDG.SenderProtocolIpV4Address.ToString());
                            veri.SubItems.Add(arpDG.TargetProtocolIpV4Address.ToString());
                            veri.SubItems.Add("ARP");
                            veri.SubItems.Add(arpDG.Length.ToString());
                            veri.SubItems.Add(arpDG.HardwareType.ToString());
                            veri.ForeColor = Color.DeepSkyBlue;
                            aramaListesi.Items.Add(veri);
                            lock (ortakKilit)
                            {
                                elenmisPaketler.Enqueue(p);
                            }
                        }
                    }
                }
            }
        }
        void portuYoneGoreAra()
        {
            sayac = 0;
            elenmisPaketler.Clear();
            aramaListesi.Items.Clear();
            aramaListesi.Visible = true;
            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            string portu = "";
            string protokolu = "";
            string yonu = "";
            foreach (Packet p in paketDizisi)
            {
                lock (ortakKilit)
                {
                    yonu = yon;
                    portu = port;
                    protokolu = protokol;
                }
                if (p.IsValid)
                {
                    EthernetDatagram ed = p.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4)
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp && protokolu == "UDP")
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (udpDG.DestinationPort.ToString() == portu && yonu == "HEDEF")
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                            else if (udpDG.SourcePort.ToString() == portu && yonu == "KAYNAK")
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp && protokolu == "TCP")
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (tcpDG.DestinationPort.ToString() == portu && yonu == "HEDEF")
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                            else if (tcpDG.SourcePort.ToString() == portu && yonu == "KAYNAK")
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void portaGoreAra()
        {
            sayac = 0;
            elenmisPaketler.Clear();
            aramaListesi.Items.Clear();
            aramaListesi.Visible = true;
            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            string portu = "";
            string protokolu = "";
            foreach (Packet p in paketDizisi)
            {
                lock (ortakKilit)
                {
                    portu = port;
                    protokolu = protokol;
                }
                if (p.IsValid)
                {
                    EthernetDatagram ed = p.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4)
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp && protokolu=="UDP")
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (udpDG.DestinationPort.ToString() == portu || udpDG.SourcePort.ToString() == portu)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp && protokolu=="TCP")
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (tcpDG.DestinationPort.ToString() == portu || tcpDG.SourcePort.ToString() == portu)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void ipyeGoreAra()
        {
            sayac = 0;
            elenmisPaketler.Clear();
            aramaListesi.Items.Clear();
            aramaListesi.Visible = true;
            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            string ayPisi = "";
            foreach (Packet p in paketDizisi)
            {
                lock (ortakKilit)
                {
                    ayPisi = ip;
                }
                if (p.IsValid)
                {
                    EthernetDatagram ed = p.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4)
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp)
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (ip4DG.Source.ToString() == ayPisi || ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("UDP");
                                    veri.SubItems.Add(udpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp)
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (ip4DG.Source.ToString() == ayPisi || ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("TCP");
                                    veri.SubItems.Add(tcpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                        {
                            icmpDG = ip4DG.Icmp;
                            dg = icmpDG.Payload;
                            if (ip4DG.Source.ToString() == ayPisi || ip4DG.Destination.ToString() == ayPisi)
                            {
                                if (dg != null)
                                {
                                    sayac++;
                                    ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                    veri.SubItems.Add(sayac.ToString());
                                    veri.SubItems.Add(ip4DG.Source.ToString());
                                    veri.SubItems.Add(ip4DG.Destination.ToString());
                                    veri.SubItems.Add("ICMP");
                                    veri.SubItems.Add(icmpDG.Length.ToString());
                                    veri.SubItems.Add(dg.ToString());
                                    veri.ForeColor = Color.DarkTurquoise;
                                    aramaListesi.Items.Add(veri);
                                    lock (ortakKilit)
                                    {
                                        elenmisPaketler.Enqueue(p);
                                    }
                                }
                            }
                        }
                    }
                    else if (ed.EtherType == EthernetType.Arp)
                    {
                        arpDG = ed.Arp;
                        if (arpDG.SenderProtocolIpV4Address.ToString() == ayPisi || arpDG.TargetProtocolIpV4Address.ToString() == ayPisi)
                        {
                            sayac++;
                            ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                            veri.SubItems.Add(sayac.ToString());
                            veri.SubItems.Add(arpDG.SenderProtocolIpV4Address.ToString());
                            veri.SubItems.Add(arpDG.TargetProtocolIpV4Address.ToString());
                            veri.SubItems.Add("ARP");
                            veri.SubItems.Add(arpDG.Length.ToString());
                            veri.SubItems.Add(arpDG.HardwareType.ToString());
                            veri.ForeColor = Color.DeepSkyBlue;
                            aramaListesi.Items.Add(veri);
                            lock (ortakKilit)
                            {
                                elenmisPaketler.Enqueue(p);
                            }
                        }
                    }
                }
            }
        }
        void protokoleGoreAra()
        {
            sayac = 0;
            elenmisPaketler.Clear();
            aramaListesi.Items.Clear();
            aramaListesi.Visible = true;
            UdpDatagram udpDG = null;
            TcpDatagram tcpDG = null;
            IcmpDatagram icmpDG = null;
            ArpDatagram arpDG = null;
            Datagram dg = null;
            string protokolu = "";
            foreach (Packet p in paketDizisi)
            {
                lock (ortakKilit)
                {
                    protokolu = protokol;
                }
                if (p.IsValid)
                {
                    EthernetDatagram ed = p.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4 && protokolu!="ARP")
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp && protokolu=="UDP")
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (dg != null)
                            {
                                sayac++;
                                ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                veri.SubItems.Add(sayac.ToString());
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("UDP");
                                veri.SubItems.Add(udpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.DarkTurquoise;
                                aramaListesi.Items.Add(veri);
                                lock (ortakKilit)
                                {
                                    elenmisPaketler.Enqueue(p);
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp && protokolu == "TCP")
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (dg != null)
                            {
                                sayac++;
                                ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                veri.SubItems.Add(sayac.ToString());
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("TCP");
                                veri.SubItems.Add(tcpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.DarkTurquoise;
                                aramaListesi.Items.Add(veri);
                                lock (ortakKilit)
                                {
                                    elenmisPaketler.Enqueue(p);
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol && protokolu == "ICMP")
                        {
                            icmpDG = ip4DG.Icmp;
                            dg = icmpDG.Payload;
                            if (dg != null)
                            {
                                sayac++;
                                ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                veri.SubItems.Add(sayac.ToString());
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("ICMP");
                                veri.SubItems.Add(icmpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.DarkTurquoise;
                                aramaListesi.Items.Add(veri);
                                lock (ortakKilit)
                                {
                                    elenmisPaketler.Enqueue(p);
                                }
                            }
                        }
                    }
                    else if (ed.EtherType == EthernetType.Arp && protokolu=="ARP")
                    {
                        arpDG = ed.Arp;
                        sayac++;
                        ListViewItem veri = new ListViewItem(p.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                        veri.SubItems.Add(sayac.ToString());
                        veri.SubItems.Add(arpDG.SenderProtocolIpV4Address.ToString());
                        veri.SubItems.Add(arpDG.TargetProtocolIpV4Address.ToString());
                        veri.SubItems.Add("ARP");
                        veri.SubItems.Add(arpDG.Length.ToString());
                        veri.SubItems.Add(arpDG.HardwareType.ToString());
                        veri.ForeColor = Color.DeepSkyBlue;
                        aramaListesi.Items.Add(veri);
                        lock (ortakKilit)
                        {
                            elenmisPaketler.Enqueue(p);
                        }
                    }
                }
            }
        }

        void paketYakala(LivePacketDevice cihaz)
        {
            using (PacketCommunicator iletisimAracisi = cihaz.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 100))
            {
                Packet paket;
                do
                {
                    PacketCommunicatorReceiveResult sonuc = iletisimAracisi.ReceivePacket(out paket);
                    switch (sonuc)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            lock (kuyruk)
                            {
                                kuyruk.Enqueue(paket);
                                //paketDizisi.Add(paket);
                                //paketDizisi.Enqueue(paket);
                                break;
                            }
                        default:
                            throw new InvalidOperationException("olmadı");
                    }
                } while (durBasla.WaitOne());
            }
        }
        void paketOku(Thread oku)
        {
            while (durBasla.WaitOne())
            {
                Packet paket;
                if (kuyruk.Count > 0)
                {
                    lock (kuyruk)
                    {
                        paket = kuyruk.Dequeue();
                    }
                } else continue;

                UdpDatagram udpDG = null;
                TcpDatagram tcpDG = null;
                IcmpDatagram icmpDG = null;
                ArpDatagram arpDG = null;
                IgmpDatagram igmp = null;
                VLanTaggedFrameDatagram vlanDG = null;
                GreDatagram greDG = null;
                DnsDatagram dnsDG = null;
                HttpDatagram httpDG = null;


                Datagram dg = null;
                if(paket.IsValid)
                {
                    EthernetDatagram ed = paket.Ethernet;
                    if (ed.EtherType == EthernetType.IpV4)
                    {
                        IpV4Datagram ip4DG = ed.IpV4;
                        if (ip4DG.Protocol == IpV4Protocol.Udp)
                        {
                            udpDG = ip4DG.Udp;
                            dg = udpDG.Payload;
                            if (dg != null)
                            {
                                lock (ortakKilit)
                                {
                                    sira++;
                                }
                                ListViewItem veri = new ListViewItem(paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                lock (ortakKilit)
                                {
                                    veri.SubItems.Add(sira.ToString());
                                }
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("UDP");
                                veri.SubItems.Add(udpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.DarkTurquoise;
                                paketListesi.Items.Add(veri);
                                lock (kuyruk)
                                {
                                    paketDizisi.Enqueue(paket);
                                }
                                string yol = @"URI=file:C:\sqlite\koklayici";
                                using (SQLiteConnection baglanti = new SQLiteConnection(yol))
                                {
                                    baglanti.Open();
                                    using (SQLiteCommand komut = new SQLiteCommand(baglanti))
                                    {
                                        string zaman = paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff");
                                        string kaynakIp = ip4DG.Source.ToString();
                                        string hedefIp = ip4DG.Destination.ToString();
                                        string proto = "UDP";
                                        string kaynakPort = udpDG.SourcePort.ToString();
                                        string hedefPort = udpDG.DestinationPort.ToString();
                                        string yuk = dg.ToString();
                                        var veri1 = new SQLiteParameter("@veri1", DbType.String) { Value = zaman };
                                        var veri2 = new SQLiteParameter("@veri2", DbType.String) { Value = kaynakIp };
                                        var veri3 = new SQLiteParameter("@veri3", DbType.String) { Value = hedefIp };
                                        var veri4 = new SQLiteParameter("@veri4", DbType.String) { Value = proto };
                                        var veri5 = new SQLiteParameter("@veri5", DbType.String) { Value = kaynakPort };
                                        var veri6 = new SQLiteParameter("@veri6", DbType.String) { Value = hedefPort };
                                        var veri7 = new SQLiteParameter("@veri7", DbType.String) { Value = yuk };
                                        komut.CommandText = "insert into paketler(zaman,kaynakIp,hedefIp,protokol,kaynakPort,hedefPort,yuk) values(@veri1,@veri2,@veri3,@veri4,@veri5,@veri6,@veri7)";
                                        komut.Parameters.Add(veri1);
                                        komut.Parameters.Add(veri2);
                                        komut.Parameters.Add(veri3);
                                        komut.Parameters.Add(veri4);
                                        komut.Parameters.Add(veri5);
                                        komut.Parameters.Add(veri6);
                                        komut.Parameters.Add(veri7);
                                        try
                                        {
                                            komut.ExecuteNonQuery();
                                        }
                                        catch
                                        {

                                        }
                                    }
                                    baglanti.Close();
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.Tcp)
                        {
                            tcpDG = ip4DG.Tcp;
                            dg = tcpDG.Payload;
                            if (dg != null)
                            {
                                lock (ortakKilit)
                                {
                                    sira++;
                                }
                                ListViewItem veri = new ListViewItem(paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                lock (ortakKilit)
                                {
                                    veri.SubItems.Add(sira.ToString());
                                }
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("TCP");
                                veri.SubItems.Add(tcpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.DarkSeaGreen;
                                paketListesi.Items.Add(veri);
                                lock (kuyruk)
                                {
                                    paketDizisi.Enqueue(paket);
                                }
                                string yol = @"URI=file:C:\sqlite\koklayici";
                                using (SQLiteConnection baglanti = new SQLiteConnection(yol))
                                {
                                    baglanti.Open();
                                    using (SQLiteCommand komut = new SQLiteCommand(baglanti))
                                    {
                                        string zaman = paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff");
                                        string kaynakIp = ip4DG.Source.ToString();
                                        string hedefIp = ip4DG.Destination.ToString();
                                        string proto = "TCP";
                                        string kaynakPort = tcpDG.SourcePort.ToString();
                                        string hedefPort = tcpDG.DestinationPort.ToString();
                                        string yuk = dg.ToString();
                                        var veri1 = new SQLiteParameter("@veri1", DbType.String) { Value = zaman };
                                        var veri2 = new SQLiteParameter("@veri2", DbType.String) { Value = kaynakIp };
                                        var veri3 = new SQLiteParameter("@veri3", DbType.String) { Value = hedefIp };
                                        var veri4 = new SQLiteParameter("@veri4", DbType.String) { Value = proto };
                                        var veri5 = new SQLiteParameter("@veri5", DbType.String) { Value = kaynakPort };
                                        var veri6 = new SQLiteParameter("@veri6", DbType.String) { Value = hedefPort };
                                        var veri7 = new SQLiteParameter("@veri7", DbType.String) { Value = yuk };
                                        komut.CommandText = "insert into paketler(zaman,kaynakIp,hedefIp,protokol,kaynakPort,hedefPort,yuk) values(@veri1,@veri2,@veri3,@veri4,@veri5,@veri6,@veri7)";
                                        komut.Parameters.Add(veri1);
                                        komut.Parameters.Add(veri2);
                                        komut.Parameters.Add(veri3);
                                        komut.Parameters.Add(veri4);
                                        komut.Parameters.Add(veri5);
                                        komut.Parameters.Add(veri6);
                                        komut.Parameters.Add(veri7);
                                        try
                                        {
                                            komut.ExecuteNonQuery();
                                        }
                                        catch
                                        {

                                        }
                                    }
                                    baglanti.Close();
                                }
                            }
                        }
                        else if (ip4DG.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                        {
                            icmpDG = ip4DG.Icmp;
                            dg = icmpDG.Payload;
                            if (dg != null)
                            {
                                lock (ortakKilit)
                                {
                                    sira++;
                                }
                                ListViewItem veri = new ListViewItem(paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                                lock (ortakKilit)
                                {
                                    veri.SubItems.Add(sira.ToString());
                                }
                                veri.SubItems.Add(ip4DG.Source.ToString());
                                veri.SubItems.Add(ip4DG.Destination.ToString());
                                veri.SubItems.Add("ICMP");
                                veri.SubItems.Add(icmpDG.Length.ToString());
                                veri.SubItems.Add(dg.ToString());
                                veri.ForeColor = Color.AliceBlue;
                                paketListesi.Items.Add(veri);
                                lock (kuyruk)
                                {
                                    paketDizisi.Enqueue(paket);
                                }
                                string yol = @"URI=file:C:\sqlite\koklayici";
                                using (SQLiteConnection baglanti = new SQLiteConnection(yol))
                                {
                                    baglanti.Open();
                                    using (SQLiteCommand komut = new SQLiteCommand(baglanti))
                                    {
                                        string zaman = paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff");
                                        string kaynakIp = ip4DG.Source.ToString();
                                        string hedefIp = ip4DG.Destination.ToString();
                                        string proto = "ICMP";
                                        string kaynakPort = "YOK";
                                        string hedefPort = "YOK";
                                        string yuk = dg.ToString();
                                        var veri1 = new SQLiteParameter("@veri1", DbType.String) { Value = zaman };
                                        var veri2 = new SQLiteParameter("@veri2", DbType.String) { Value = kaynakIp };
                                        var veri3 = new SQLiteParameter("@veri3", DbType.String) { Value = hedefIp };
                                        var veri4 = new SQLiteParameter("@veri4", DbType.String) { Value = proto };
                                        var veri5 = new SQLiteParameter("@veri5", DbType.String) { Value = kaynakPort };
                                        var veri6 = new SQLiteParameter("@veri6", DbType.String) { Value = hedefPort };
                                        var veri7 = new SQLiteParameter("@veri7", DbType.String) { Value = yuk };
                                        komut.CommandText = "insert into paketler(zaman,kaynakIp,hedefIp,protokol,kaynakPort,hedefPort,yuk) values(@veri1,@veri2,@veri3,@veri4,@veri5,@veri6,@veri7)";
                                        komut.Parameters.Add(veri1);
                                        komut.Parameters.Add(veri2);
                                        komut.Parameters.Add(veri3);
                                        komut.Parameters.Add(veri4);
                                        komut.Parameters.Add(veri5);
                                        komut.Parameters.Add(veri6);
                                        komut.Parameters.Add(veri7);
                                        try
                                        {
                                            komut.ExecuteNonQuery();
                                        }
                                        catch
                                        {

                                        }
                                    }
                                    baglanti.Close();
                                }
                            }
                        }
                    }
                    else if (ed.EtherType == EthernetType.Arp)
                    {
                        arpDG = ed.Arp;
                        dg = ed.Payload;
                        if (arpDG != null)
                        {
                            lock (ortakKilit)
                            {
                                sira++;
                            }
                            ListViewItem veri = new ListViewItem(paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff"));
                            lock (ortakKilit)
                            {
                                veri.SubItems.Add(sira.ToString());
                            }
                            veri.SubItems.Add(arpDG.SenderProtocolIpV4Address.ToString());
                            veri.SubItems.Add(arpDG.TargetProtocolIpV4Address.ToString());
                            veri.SubItems.Add("ARP");
                            veri.SubItems.Add(arpDG.Length.ToString());
                            veri.SubItems.Add(arpDG.HardwareType.ToString());
                            veri.ForeColor = Color.DeepSkyBlue;
                            paketListesi.Items.Add(veri);
                            lock (kuyruk)
                            {
                                paketDizisi.Enqueue(paket);
                            }
                            string yol = @"URI=file:C:\sqlite\koklayici";
                            using (SQLiteConnection baglanti = new SQLiteConnection(yol))
                            {
                                baglanti.Open();
                                using (SQLiteCommand komut = new SQLiteCommand(baglanti))
                                {
                                    string zaman = paket.Timestamp.ToString("dd-MMMM-yyyy HH:mm:ss.fffffff");
                                    string kaynakIp = arpDG.SenderProtocolIpV4Address.ToString();
                                    string hedefIp = arpDG.TargetProtocolIpV4Address.ToString();
                                    string proto = "ARP";
                                    string kaynakPort = "YOK";
                                    string hedefPort = "YOK";
                                    string yuk = dg.ToString();
                                    var veri1 = new SQLiteParameter("@veri1", DbType.String) { Value = zaman };
                                    var veri2 = new SQLiteParameter("@veri2", DbType.String) { Value = kaynakIp };
                                    var veri3 = new SQLiteParameter("@veri3", DbType.String) { Value = hedefIp };
                                    var veri4 = new SQLiteParameter("@veri4", DbType.String) { Value = proto };
                                    var veri5 = new SQLiteParameter("@veri5", DbType.String) { Value = kaynakPort };
                                    var veri6 = new SQLiteParameter("@veri6", DbType.String) { Value = hedefPort };
                                    var veri7 = new SQLiteParameter("@veri7", DbType.String) { Value = yuk };
                                    komut.CommandText = "insert into paketler(zaman,kaynakIp,hedefIp,protokol,kaynakPort,hedefPort,yuk) values(@veri1,@veri2,@veri3,@veri4,@veri5,@veri6,@veri7)";
                                    komut.Parameters.Add(veri1);
                                    komut.Parameters.Add(veri2);
                                    komut.Parameters.Add(veri3);
                                    komut.Parameters.Add(veri4);
                                    komut.Parameters.Add(veri5);
                                    komut.Parameters.Add(veri6);
                                    komut.Parameters.Add(veri7);
                                    try
                                    {
                                        komut.ExecuteNonQuery();
                                    }
                                    catch
                                    {

                                    }
                                }
                                baglanti.Close();
                            }
                        }
                    }
                }
            }
        }
    }
}