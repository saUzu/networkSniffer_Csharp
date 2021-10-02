# networkSniffer

WireShark benzeri basit bir ağ koklayıcısıdır.(Network Monitoring) 64-Bitlik PcapDotNet kullanılmıştır.

C# ile PcapDotNet kullanılarak yazıldı. Ağa gelen ve ağdan giden paketleri yakalayarak bunları arkada SQLite'da oluşturulan veri tabanına kaydeder. Daha sonra bu paketler üzerinde arama yapılabilir. Veri tabanına kaydedilmeden önce de IP, PORT veya Protokole göre arama yapılabilir. Paketlerin içeriğini gösteriyor. Eğer paket şifrelenmemişse rahatça görebilirsiniz. Her protokolü desteklememektedir. ARP, ICMP, TCP ve UDP protokolleri içindir sadece.

Bilgisayar Mühendisliği 2. Dönem tezimdir.
