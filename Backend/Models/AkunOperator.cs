using System;

namespace SiagaApi.Backend.Models
{
    // Akun operator posko. PasswordHash sengaja tidak ada di sini: hash
    // hanya dibaca di dalam AkunOperatorRepository saat pengecekan login,
    // supaya tidak pernah ikut terbawa ke ViewModel atau UI.
    public class AkunOperator
    {
        public int Id { get; set; }
        public string NamaLengkap { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public PeranOperator Peran { get; set; }
        public StatusAkun Status { get; set; }
        public DateTime DibuatPada { get; set; }

        public bool SuperAdmin => Peran == PeranOperator.SuperAdmin;

        // Admin dan SuperAdmin sama-sama boleh membuka panel kelola akun.
        public bool Pengelola => Peran != PeranOperator.Operator;

        // Nama peran untuk ditampilkan, mis. di sidebar.
        public string LabelPeran => Peran == PeranOperator.SuperAdmin ? "Super Admin" : Peran.ToString();

        // ATURAN WEWENANG: siapa boleh mengelola akun siapa.
        //   SuperAdmin -> semua akun kecuali dirinya sendiri dan SuperAdmin lain
        //   Admin      -> hanya akun Operator
        //   Operator   -> tidak boleh
        public bool BolehMengelola(AkunOperator target)
        {
            if (Status != StatusAkun.Aktif || target.Id == Id)
                return false;

            return Peran switch
            {
                PeranOperator.SuperAdmin => target.Peran != PeranOperator.SuperAdmin,
                PeranOperator.Admin => target.Peran == PeranOperator.Operator,
                _ => false
            };
        }
    }
}
