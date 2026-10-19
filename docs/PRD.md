# AETHER Desktop: Product Requirements Document

Salinan Markdown dari `references/requirements/AETHER_Desktop_PRD_v1.1-2.docx`. Isi dan ruang lingkup mengikuti dokumen asli; yang ditambahkan hanya
bagian 17 (pemetaan ke implementasi). Dokumen asli tetap menjadi acuan jika ada perbedaan.

| Keterangan | Isi |
|---|---|
| Nama produk | AETHER Desktop |
| Versi dokumen | 1.1 |
| Status | Draft untuk pengembangan MVP |
| Mata kuliah | Pemrograman Berbasis Kerangka Kerja (PBKK) |
| Platform | Windows Desktop |
| Teknologi | WPF, C#, XAML, SQLite |
| Tim | Kemas Muhammad Athariq, Brilian Kurniawan Prasisto, Ferdian Ardra Hafizhan |
| Dokumen acuan | PRD SISMA sebagai acuan format |

## 1. Ringkasan Eksekutif

AETHER Desktop adalah aplikasi Windows untuk melihat dan mengelola data insiden manufaktur. Aplikasi menampilkan ringkasan insiden, data kondisi aset yang
tersedia, prioritas berdasarkan aturan sederhana, dan daftar tindakan terkait insiden. Aplikasi ini dibuat untuk tugas UTS mata kuliah PBKK.

**1.1 Tujuan produk**
- Mengelola data insiden dalam satu aplikasi.
- Menampilkan ringkasan melalui kartu statistik, tabel, dan grafik.
- Menampilkan data kondisi aset yang tersedia pada dataset.
- Menjelaskan dasar perhitungan prioritas insiden.
- Menerapkan WPF, C#, data binding, CRUD, dan penyimpanan database.

**1.2 Pernyataan produk:** AETHER Desktop membantu pengguna meninjau data insiden manufaktur dan tindak lanjutnya melalui aplikasi desktop lokal yang sederhana.

## 2. Latar Belakang dan Permasalahan

- Data insiden perlu disajikan dalam bentuk yang mudah dicari dan dibaca.
- Pengguna membutuhkan ringkasan jumlah insiden, downtime, dan kerugian yang tercatat.
- Prioritas perlu disertai penjelasan agar dasar perhitungannya dapat dipahami.
- Data yang diubah harus tetap tersimpan setelah aplikasi dibuka kembali.
- Data kondisi aset tidak mencakup semua insiden. Tampilan harus mengikuti data yang tersedia.

## 3. Sasaran, Indikator, dan Batasan

| Sasaran | Indikator keberhasilan |
|---|---|
| Pengelolaan insiden | Pengguna dapat menambah, melihat, mengubah, dan menghapus data insiden. |
| Dashboard | Statistik dan grafik dihitung dari database. |
| Data aset | Pengukuran ditampilkan bersama unit dan waktu jika tersedia. |
| Prioritas | Hasil mengikuti aturan terdokumentasi dan disertai alasan. |
| Tindakan | Tindakan terkait insiden dapat dikelola dan disimpan. |
| Penyimpanan | Data tetap tersedia setelah aplikasi dibuka kembali. |
| Rilis | Aplikasi dapat dijalankan pada Windows sesuai petunjuk README. |

Angka pada data kasus merupakan catatan historis, bukan angka penghematan atau hasil yang telah dicapai oleh aplikasi.

## 4. Pengguna dan Hak Akses

| Pengguna | Kebutuhan | Akses |
|---|---|---|
| Peninjau insiden | Melihat data insiden, downtime, kerugian, dan prioritas. | Dashboard, daftar insiden, aset, dan prioritas. |
| Analis data | Melihat pola data dan dasar perhitungan. | Tabel, grafik, filter, dan rincian data. |
| Evaluator | Memeriksa fitur dan implementasi PBKK. | Menggunakan seluruh fitur MVP. |

Aplikasi digunakan secara lokal untuk demonstrasi. Login dan pembagian hak akses tidak termasuk dalam rilis awal.

## 5. Ruang Lingkup Produk

**5.1 In scope**
- Dashboard berisi jumlah insiden, downtime dan kerugian tercatat, distribusi status, grafik, dan insiden terbaru.
- Daftar insiden dengan pencarian, filter dasar, tambah, lihat, ubah, dan hapus.
- Halaman pemantauan aset untuk pengukuran kondisi yang tersedia.
- Perhitungan prioritas berdasarkan aturan yang dapat dijelaskan.
- Daftar tindakan terkait insiden, berisi deskripsi, pemilik, target tanggal, dan status.
- Penyimpanan lokal menggunakan SQLite.
- Validasi form, konfirmasi penghapusan, dan pesan hasil operasi.
- Dokumentasi cara menjalankan, menguji, dan membangun aplikasi Windows.

**5.2 Out of scope**
- Login dan hak akses multi-user.
- Notifikasi otomatis, background job, dan workflow terjadwal.
- Verifikasi efektivitas tindakan atau approval bertingkat.
- AI atau machine learning terlatih.
- Integrasi dengan DCS, historian, CMMS, atau sistem pabrik live.
- Cloud, simulasi digital twin kompleks, dan prediksi energi.
- Klaim penghematan atau ROI yang tidak didukung data.

## 6. Struktur Navigasi dan Halaman

| Halaman | Elemen utama | Tujuan |
|---|---|---|
| Dashboard | Kartu statistik, grafik, insiden terbaru. | Melihat ringkasan data. |
| Insiden | Pencarian, filter, tabel, tombol tambah/edit/hapus. | Mengelola data insiden. |
| Pemantauan Aset | Daftar aset, nilai, unit, waktu, grafik jika tersedia. | Melihat data kondisi aset. |
| Prioritas Insiden | Kategori, faktor perhitungan, penjelasan. | Memahami alasan prioritas. |
| Pelacakan Tindakan | Tindakan, insiden terkait, pemilik, tanggal, status. | Mengelola tindak lanjut. |

Navigasi menggunakan sidebar pada satu jendela utama. Form tambah dan ubah dapat menggunakan dialog atau panel yang sama.

## 7. Kebutuhan Fungsional

| ID | Fitur | Persyaratan | Prioritas |
|---|---|---|---|
| FR-01 | Dashboard | Menampilkan statistik, grafik, dan insiden terbaru dari database. | Must |
| FR-02 | Daftar insiden | Menampilkan tabel dengan pencarian dan filter dasar. | Must |
| FR-03 | Tambah insiden | Menyimpan insiden baru setelah input valid. | Must |
| FR-04 | Ubah insiden | Menampilkan data terpilih dan menyimpan perubahan yang valid. | Must |
| FR-05 | Hapus insiden | Meminta konfirmasi sebelum menghapus data. | Must |
| FR-06 | Grafik | Menggunakan hasil query, bukan angka statis pada UI. | Must |
| FR-07 | Pemantauan aset | Menampilkan pengukuran yang tersedia, termasuk unit dan waktu jika tercatat. | Must |
| FR-08 | Prioritas | Menghitung kategori prioritas dengan aturan terdokumentasi. | Must |
| FR-09 | Pelacakan tindakan | Menyediakan tambah, lihat, ubah, dan hapus tindakan terkait insiden. | Must |
| FR-10 | Status tindakan | Menyediakan status Open, In Progress, dan Closed. | Must |
| FR-11 | Penyimpanan | Menyimpan perubahan di SQLite setelah aplikasi dibuka kembali. | Must |
| FR-12 | Validasi | Memeriksa kolom wajib dan nilai numerik sebelum penyimpanan. | Must |
| FR-13 | Kesalahan | Menampilkan pesan yang jelas saat operasi gagal. | Must |
| FR-14 | Data kosong | Menampilkan pesan jika data tidak tersedia. | Must |

## 8. User Stories dan Kriteria Penerimaan

**US-01. Melihat dashboard.** Sebagai pengguna, saya ingin melihat ringkasan insiden agar dapat memahami data dengan cepat.
Statistik berasal dari database. Grafik mengikuti data tersimpan. Jika tidak ada data, aplikasi menampilkan nol atau pesan data belum tersedia.

**US-02. Mengelola insiden.** Sebagai pengguna, saya ingin mengelola data insiden melalui tabel dan form.
Kolom wajib diperiksa sebelum penyimpanan. Data yang disimpan muncul pada tabel. Perubahan tetap ada setelah aplikasi dibuka kembali. Penghapusan meminta konfirmasi.

**US-03. Melihat prioritas.** Sebagai pengguna, saya ingin mengetahui alasan sebuah insiden mendapat prioritas tertentu.
Aturan perhitungan didokumentasikan. Faktor yang memengaruhi hasil ditampilkan. Data yang tidak tersedia tidak dianggap nol tanpa penjelasan.

**US-04. Mengelola tindakan.** Sebagai pengguna, saya ingin mencatat tindakan yang berkaitan dengan suatu insiden.
Tindakan memiliki deskripsi, insiden terkait, pemilik, target tanggal, dan status. Tindakan dapat ditambah, diubah, dan dihapus. Data tetap tersedia setelah aplikasi dibuka kembali.

## 9. Kebutuhan UI/UX dan Design System

| Aspek | Ketentuan |
|---|---|
| Gaya visual | Tampilan desktop yang rapi dan mudah dibaca. |
| Layout | Sidebar, header, kartu statistik, tabel, grafik, dan form. |
| Tipografi | Gunakan font yang mudah dibaca dengan ukuran konsisten. |
| Komponen | Tombol, input, dropdown, DataGrid, dialog konfirmasi, dan penanda status. |
| Pesan sistem | Sediakan pesan berhasil, gagal, dan data kosong. |
| Konsistensi | Gunakan warna, jarak, ukuran kontrol, dan ikon yang konsisten. |
| Aksesibilitas | Label kontrol jelas, kontras cukup, dan fokus keyboard terlihat. |
| Ukuran jendela | Tampilan tetap terbaca pada ukuran minimum yang ditentukan. |

Dark mode dan light mode bersifat opsional, bukan syarat utama rilis awal.

## 10. Kebutuhan Nonfungsional

| ID | Kategori | Persyaratan |
|---|---|---|
| NFR-01 | Platform | Aplikasi berjalan pada Windows sesuai versi .NET yang dipilih. |
| NFR-02 | Penyimpanan | SQLite menyimpan data secara lokal. |
| NFR-03 | Query | Input pengguna dikirim melalui query berparameter. |
| NFR-04 | Struktur kode | Kode tampilan, aturan bisnis, dan akses database dipisahkan. |
| NFR-05 | Pengujian | Aturan prioritas dan operasi repository memiliki pengujian. |
| NFR-06 | Integritas data | Database menggunakan primary key, foreign key, dan constraint yang diperlukan. |
| NFR-07 | Kesalahan | Kesalahan ditangani dengan pesan yang dapat dipahami. |
| NFR-08 | Distribusi | Cara build dan publish Windows dijelaskan di README. |
| NFR-09 | Sumber data | Nilai sumber dibedakan dari hasil perhitungan aplikasi. |

## 11. Rancangan Data Awal

| Entitas | Atribut utama | Catatan |
|---|---|---|
| Incident | ID, referensi, judul/deskripsi, lokasi, aset, tanggal, status, downtime, kerugian, klasifikasi risiko jika tersedia. | Pemetaan akhir mengikuti kolom dataset. |
| Asset | ID, kode aset, nama aset, lokasi, jenis aset jika tersedia. | Gunakan atribut yang didukung sumber. |
| ConditionRecord | ID, ID aset, waktu, parameter, nilai, unit, ambang batas jika tersedia. | Jangan mengisi data yang tidak ada di sumber. |
| ActionItem | ID, ID insiden, deskripsi, pemilik, target tanggal, status. | ID insiden menjadi foreign key. |

**11.1 Relasi data.** Satu aset dapat memiliki beberapa catatan kondisi. Satu insiden dapat memiliki beberapa tindakan. Kebijakan penghapusan insiden yang masih memiliki
tindakan harus ditentukan sebelum implementasi. *(Diputuskan: lihat `DECISIONS.md` D-05.)*

**11.2 Aturan data.** Gunakan ID sebagai primary key. Sesuaikan pemetaan dengan nama kolom dan tipe data sumber. Gunakan format tanggal yang konsisten. Catat nilai turunan dan
cara perhitungannya. Jangan membuat nilai pengukuran atau atribut sumber yang tidak tersedia.

## 12. Rancangan Teknis

| Bagian | Tanggung jawab |
|---|---|
| Presentation | Menampilkan halaman dan menerima input pengguna. |
| Core | Menyimpan model, validasi, dan aturan prioritas. |
| Infrastructure | Mengelola koneksi SQLite dan operasi repository. |
| Database | Menyimpan data insiden, aset, catatan kondisi, dan tindakan. |

Aplikasi menggunakan WPF, C#, dan XAML. Database menggunakan SQLite melalui paket Microsoft.Data.Sqlite. Akses data mengikuti pola ADO.NET, seperti connection, command, reader,
dan query berparameter. SQL Server tidak digunakan pada rilis awal. MVVM dapat digunakan dengan struktur sederhana.

## 13. Aturan Prioritas Insiden

Prioritas dihitung menggunakan aturan yang ditentukan tim. Hasilnya bukan keluaran AI dan bukan penetapan root cause.
- Gunakan downtime atau kerugian jika nilainya tersedia dan valid.
- Gunakan data kondisi aset hanya jika parameter, unit, dan batas yang diperlukan diketahui.
- Tampilkan kategori prioritas dan faktor yang memengaruhi hasil.
- Bedakan klasifikasi risiko dari sumber dengan prioritas hasil perhitungan.
- Tandai data yang tidak lengkap atau tidak dapat dibandingkan.
- Jangan menggunakan Pre-Risk sebagai satu-satunya ukuran konsekuensi.

## 14. Teknologi yang Digunakan

| Komponen | Pilihan |
|---|---|
| Antarmuka | WPF, XAML, C# |
| Framework | Versi .NET ditentukan setelah memeriksa kebutuhan praktikum dan komputer Windows. *(Dipilih: .NET 8, `DECISIONS.md` D-01.)* |
| Database | SQLite |
| Provider | Microsoft.Data.Sqlite |
| Struktur proyek | Aether.Desktop, Aether.Core, Aether.Infrastructure |
| Pengujian | Aether.Core.Tests dan Aether.Infrastructure.Tests |
| Rilis | Paket Windows x64 diuji sebelum dibagikan. |

Opsi self-contained atau single-file ditetapkan setelah paket rilis berhasil diuji pada Windows. *(Lihat `DECISIONS.md` D-14.)*

## 15. Pengujian dan Kriteria Selesai

**15.1 Kriteria penerimaan**

| ID | Kriteria | Cara pemeriksaan |
|---|---|---|
| AC-01 | Semua halaman dapat dibuka pada Windows. | Jalankan aplikasi dan periksa navigasi. |
| AC-02 | Statistik dan grafik mengikuti database. | Bandingkan tampilan dengan data uji. |
| AC-03 | CRUD insiden berfungsi. | Uji seluruh operasi CRUD. |
| AC-04 | Pencarian dan filter sesuai. | Uji kata kunci yang cocok dan tidak cocok. |
| AC-05 | Halaman aset hanya menampilkan data tersedia. | Uji aset dengan dan tanpa pengukuran. |
| AC-06 | Prioritas memiliki alasan yang dapat diperiksa. | Uji beberapa kondisi data. |
| AC-07 | Tindakan dapat dikelola dan terhubung ke insiden. | Uji operasi dan relasi database. |
| AC-08 | Data tetap ada setelah aplikasi dibuka kembali. | Tutup lalu buka aplikasi. |
| AC-09 | Input tidak valid ditolak. | Uji kolom kosong dan nilai numerik. |
| AC-10 | Pengujian kode dapat dijalankan. | Jalankan perintah test. |
| AC-11 | README menjelaskan cara menjalankan dan membangun aplikasi. | Ikuti petunjuk Windows. |
| AC-12 | Paket rilis berjalan tanpa database server. | Uji pada komputer Windows. |

**15.2 Kriteria selesai:** fitur wajib selesai; CRUD dan penyimpanan diuji; statistik dan grafik memakai database; validasi dan pesan kesalahan tersedia; aturan prioritas
didokumentasikan dan diuji; README dan naskah demo tersedia; build dan paket rilis diuji pada Windows.

## 16. Risiko dan Mitigasi

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Pemetaan kolom keliru | Data tidak tepat. | Periksa dataset sebelum membuat model dan query. |
| Statistik tidak diperbarui | Dashboard berbeda dari data terbaru. | Hitung ulang statistik setelah perubahan. |
| Aturan prioritas tidak jelas | Hasil sulit dipahami. | Dokumentasikan rumus dan uji dengan data contoh. |
| Data kondisi terbatas | Tampilan aset dapat disalahartikan. | Tampilkan cakupan data dan jangan mengisi nilai yang tidak tersedia. |
| Kode sulit diuji | Perubahan lebih berisiko. | Pisahkan tampilan, aturan bisnis, dan repository. |
| Build Windows gagal | Aplikasi tidak dapat didemokan. | Uji build dan paket rilis pada Windows. |
| Penambahan fitur di luar scope | Fitur utama terlambat. | Prioritaskan kebutuhan wajib pada dokumen ini. |

## 17. Pemetaan ke implementasi (tambahan, bukan bagian PRD asli)

Status ini menggambarkan kondisi kode saat ini. "Teruji otomatis" berarti ada tes yang dijalankan dan lulus di macOS; "Perlu uji Windows" berarti belum pernah dijalankan di Windows.

| Persyaratan | Lokasi implementasi | Status |
|---|---|---|
| FR-01, FR-06, US-01 | `DashboardViewModel`, `SqliteDashboardRepository`, `Controls/*Chart` | Query teruji otomatis; tampilan perlu uji Windows |
| FR-02 | `IncidentsViewModel`, `SqliteIncidentRepository.Search` | Query teruji otomatis; tampilan perlu uji Windows |
| FR-03, FR-04, FR-05, US-02 | `IncidentsViewModel`, `IncidentValidator`, repository | Repository dan validasi teruji otomatis; dialog dan form perlu uji Windows |
| FR-07 | `AssetsViewModel`, `SqliteAssetRepository` | Query teruji otomatis; tampilan perlu uji Windows |
| FR-08, US-03, bagian 13 | `PriorityService`, `PriorityViewModel` | Aturan teruji otomatis; tampilan perlu uji Windows |
| FR-09, FR-10, US-04 | `ActionsViewModel`, `ActionValidator`, `SqliteActionRepository` | Repository dan validasi teruji otomatis; tampilan perlu uji Windows |
| FR-11 | seluruh repository | Teruji otomatis (restart database) |
| FR-12, FR-13, FR-14 | validator, `PageViewModel.TryRun`, pesan status dan keadaan kosong | Validasi teruji otomatis; pesan di UI perlu uji Windows |
| NFR-03 | semua SQL berparameter | Teruji otomatis (uji input berisi tanda kutip) |
| NFR-06 | skema `AetherDatabase` | Teruji otomatis |
| NFR-09 | label "(source)" dan "(calculated)", kolom `Origin` | Perlu pemeriksaan visual di Windows |

Rincian per kriteria penerimaan AC-01 sampai AC-12: `TESTING.md`.
