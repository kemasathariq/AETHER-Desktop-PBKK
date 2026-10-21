# Panduan AETHER Desktop

Aplikasi Windows untuk mengelola insiden manufaktur dan tindak lanjutnya secara lokal.

## Mulai menggunakan

1. Ekstrak seluruh isi AetherDesktop-win-x64.zip ke satu folder.
2. Buka AetherDesktop.exe di dalam folder tersebut. Jangan memindahkan EXE sendirian karena file pendukungnya diperlukan.
3. Pada pembukaan pertama, aplikasi membuat database dan mengimpor data kasus. Windows 10/11 x64 diperlukan; instalasi .NET atau server database tidak diperlukan untuk paket mandiri.

## Menu aplikasi

- Dashboard: ringkasan jumlah insiden, downtime, kerugian, status, grafik, dan insiden terbaru. Data diperbarui setiap kali halaman dibuka.
- Incidents: cari berdasarkan judul, tag, referensi, plant, atau mekanisme kegagalan. Pilih baris untuk melihat detail. New incident, Edit, dan Delete mengelola data; Save menyimpan dan Cancel membatalkan perubahan form. Geser scrollbar horizontal untuk melihat kolom di kanan.
- Asset Monitoring: pilih satu dari lima aset yang memiliki pengukuran, lalu pilih parameter. Grafik dan tabel menampilkan catatan historis, unit, tanggal, serta batas alarm/trip yang tersedia.
- Incident Priority: pilih insiden untuk melihat kategori, faktor, poin, dan alasan. Buka How the priority is calculated untuk membaca aturannya. Hilangkan Hide closed and canceled untuk melihat insiden yang sudah ditutup.
- Action Tracker: New action untuk mencatat deskripsi, insiden terkait, pemilik, target tanggal, dan status Open / In Progress / Closed. Pilih baris untuk mengubah atau menghapus tindakan.

## Pengisian form

Kolom bertanda * wajib diisi. Angka boleh menggunakan titik atau koma sebagai pemisah desimal, tanpa pemisah ribuan: 12.5 atau 12,5. Nilai negatif dan teks bukan angka ditolak. Kolom angka kosong berarti tidak tercatat, bukan nol.

Jika hasil simpan tidak terlihat, gunakan Clear filters. Penghapusan meminta konfirmasi dan tidak dapat dibatalkan. Menghapus insiden juga menghapus tindakan yang terhubung; jumlahnya disebutkan dalam dialog.

## Penyimpanan dan cadangan

Semua perubahan tersimpan di %LOCALAPPDATA%\AetherDesktop\aether.db dan tetap ada setelah aplikasi ditutup. Lokasi ini juga ditampilkan pada sidebar. Menyalin folder aplikasi tidak ikut menyalin database.

Untuk membuat cadangan, tutup aplikasi lalu salin aether.db ke lokasi cadangan. Untuk memulihkan, tutup aplikasi, cadangkan database saat ini, lalu ganti dengan salinan cadangan. Jangan mengganti database ketika aplikasi berjalan.

## Memahami datanya

Data awal berisi 380 insiden, 5 aset, 520 pengukuran mingguan, dan 20 tindakan. Data kondisi hanya tersedia untuk lima aset. Angka kerugian menggunakan k USD (ribuan dolar AS). Nilai kasus merupakan catatan historis, bukan hasil penghematan aplikasi.

Prioritas merupakan aturan tim berdasarkan downtime, kerugian aktual, dan kondisi aset yang dapat dibandingkan. Pre-Risk dari sumber ditampilkan terpisah. Aplikasi ini tidak memakai AI, tidak menerima data pabrik secara live, dan tidak memerlukan login.

## Jika terjadi masalah

Pastikan seluruh file paket sudah diekstrak, folder database dapat ditulis, dan ukuran jendela minimal 1100 x 680. Pesan kesalahan tampil di halaman atau dialog. Untuk build dari kode sumber dan rincian pengujian, baca README.md di repository proyek.
